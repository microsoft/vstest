# Requires PowerShell 7, as used by the insertion pipeline's Azure CLI task.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Component = 'Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI'
$script:SourceBranch = 'internal/rel/15.9'
$script:ComponentRepo = 'ef257391-4d76-4240-8649-3f21ac49c392'
$script:VsRepo = 'a290117c-5a8a-40f7-bc2c-f14dbe3acf6d'
$script:ComponentApi = 'https://dev.azure.com/dnceng/internal/_apis'
$script:VsApi = 'https://dev.azure.com/devdiv/DevDiv/_apis'
$script:AppConfig = '/src/vset/Agile/TestPlatform/RocksteadyCLI/App.config'
$script:RevisionPaths = @(
    '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/Shared/revision.txt'
    '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/revision.txt'
)

function ConvertFrom-InsertionJson([string] $Text) {
    # ConvertFrom-Json otherwise silently keeps the last duplicate property.
    $document = [System.Text.Json.JsonDocument]::Parse($Text)
    try {
        function Test-Properties($Element) {
            if ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
                $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
                foreach ($property in $Element.EnumerateObject()) {
                    if (-not $names.Add($property.Name)) { throw "Duplicate JSON property: $($property.Name)" }
                    Test-Properties $property.Value
                }
            }
            elseif ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
                foreach ($item in $Element.EnumerateArray()) { Test-Properties $item }
            }
        }
        Test-Properties $document.RootElement
    }
    finally { $document.Dispose() }
    return ConvertFrom-Json -InputObject $Text -AsHashtable -Depth 100
}

function Assert-InsertionProducer($Build, [string] $SourceHead, [string] $SourceBranch) {
    if ($SourceBranch -cne $script:SourceBranch -or $SourceHead -cnotmatch '^[a-f0-9]{40}$' -or
        $Build.sourceVersion -cne $SourceHead -or
        $Build.sourceBranch -cne "refs/heads/$SourceBranch" -or
        $Build.repository.id -cne $script:ComponentRepo -or
        $Build.definition.id -ne 1222 -or
        $Build.status -cne 'completed' -or $Build.result -cne 'succeeded' -or
        $Build.reason -eq 'pullRequest' -or
        $Build.id -le 0 -or $Build.buildNumber -cnotmatch '^\d{8}\.\d+$') {
        throw 'Producer must be a successful official definition 1222 build of the exact current component source.'
    }
}

function Get-InsertionManifest([string] $Path, $Build, [string] $SourceBranch) {
    if ($SourceBranch -cnotin @($script:SourceBranch, "refs/heads/$script:SourceBranch")) {
        throw 'Insertion manifests require the private internal/rel/15.9 servicing branch.'
    }
    $manifest = ConvertFrom-InsertionJson ([IO.File]::ReadAllText($Path))
    if ($manifest.manifestVersion -cne '1.1' -or
        $manifest.info.manifestName -cne $script:Component -or
        $manifest.packages.Count -ne 1 -or
        $manifest.packages[0].id -cne $script:Component -or
        $manifest.packages[0].type -cne 'Vsix' -or
        $manifest.packages[0].version -cne $manifest.info.buildVersion -or
        $manifest.info.buildVersion -cnotmatch '^15\.9\.3\.\d+$') {
        throw 'Unexpected VSTest 15.9 insertion manifest identity or version.'
    }
    if ($Build.buildNumber -cnotmatch '^(\d{8})\.(\d+)$') { throw 'Invalid producer build number.' }
    $expectedVersion = '15.9.3.' + $Matches[1] + ('{0:D2}' -f [int]$Matches[2])
    if ($manifest.info.buildVersion -cne $expectedVersion) {
        throw "Manifest version does not identify producer $($Build.buildNumber)."
    }
    $line = ($SourceBranch -split '/')[-1]
    $drop = "https://vsdrop.corp.microsoft.com/file/v1/Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-$line/$($Build.buildNumber)"
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $vsixCount = 0
    foreach ($payload in $manifest.packages[0].payloads) {
        $name = $payload.fileName
        if ($name -cnotmatch '^[A-Za-z0-9_.-]+$' -or $name -in '.', '..' -or -not $seen.Add($name) -or
            $payload.sha256 -cnotmatch '^[a-fA-F0-9]{64}$' -or
            $payload.url -cne "$drop;/$name") {
            throw 'Payload must have a unique flat filename, SHA-256 and exact producer private-drop URL.'
        }
        if ($name -cnotin @('TestPlatform.vsix', "${script:Component}_sbom.json")) {
            throw "Unexpected payload in the single-component 15.9 manifest: $name"
        }
        if ($name.EndsWith('.vsix', [StringComparison]::OrdinalIgnoreCase)) {
            $vsixCount++
            if (-not $payload.Contains('signer') -or -not $manifest.signers) {
                throw 'The VSIX payload must identify its signer.'
            }
        }
    }
    if ($vsixCount -ne 1) { throw 'Expected exactly one VSIX payload.' }
    return @{
        Manifest = $manifest
        FileName = "$script:Component.vsman"
        Url = "$drop;$script:Component.vsman"
        Version = $manifest.info.buildVersion
    }
}

function Assert-InsertionPayload([string] $Path, $Payload) {
    if (-not [IO.File]::Exists($Path) -or
        (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ine $Payload.sha256) {
        throw "Missing or mismatched payload: $($Payload.fileName)"
    }
    $actualSize = (Get-Item -LiteralPath $Path).Length
    if ($actualSize -ne $Payload.size) {
        # Size/SBOM metadata findings are reported, not repaired by an insertion.
        Write-Warning "Payload $($Payload.fileName) declares $($Payload.size) bytes; actual size is $actualSize. SHA-256 matches."
    }
}

function Get-InsertionDropLocation([string] $CanonicalUrl, [string] $BuildNumber) {
    if ($BuildNumber -cnotmatch '^\d{8}\.\d+$') { throw 'Invalid expected drop build number.' }
    $dropName = "Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15.9/$BuildNumber"
    $prefix = "https://vsdrop.corp.microsoft.com/file/v1/$dropName;"
    if (-not $CanonicalUrl.StartsWith($prefix, [StringComparison]::Ordinal)) {
        throw 'Canonical drop URL does not match the exact servicing producer.'
    }
    $file = $CanonicalUrl.Substring($prefix.Length)
    if ($file.StartsWith('/', [StringComparison]::Ordinal)) { $file = $file.Substring(1) }
    if ($file -cnotin @("$script:Component.vsman", 'TestPlatform.vsix', "${script:Component}_sbom.json")) {
        throw 'Unexpected canonical insertion drop file.'
    }
    return @{
        DropName = $dropName
        MetadataUrl = "https://devdiv.artifacts.visualstudio.com/_apis/drop/drop/$dropName`?api-version=7.1-preview.1"
        Path = "/$file"
    }
}

function Assert-InsertionDropServiceUri([string] $Uri) {
    $parsed = $null
    if (-not [Uri]::TryCreate($Uri, [UriKind]::Absolute, [ref]$parsed) -or
        $parsed.Scheme -cne 'https' -or -not $parsed.IsDefaultPort -or
        $parsed.UserInfo -or $parsed.Fragment) {
        throw 'Untrusted cloud drop URI origin.'
    }
    $manifestPattern = '\Ahttps://devdiv\.artifacts\.visualstudio\.com(?::443)?/_apis/drop/drop/Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15\.9/\d{8}\.\d+\?api-version=7\.1-preview\.1\z'
    $blobPattern = '\Ahttps://devdiv\.vsblob\.visualstudio\.com(?::443)?/_apis/blob/blobs/[a-fA-F0-9]{66}\z'
    if ($Uri -cnotmatch $manifestPattern -and $Uri -cnotmatch $blobPattern) {
        throw 'Untrusted cloud drop URI host, path or query.'
    }
}

function Invoke-InsertionDropGet([string] $Uri, [hashtable] $Headers) {
    Assert-InsertionDropServiceUri $Uri
    if (-not $Headers.ContainsKey('Authorization') -or $Headers.Authorization -cnotmatch '^Bearer \S+$') {
        throw 'The existing insertion bearer is required for cloud drop reads.'
    }
    $handler = [Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromMinutes(5)
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get, $Uri)
    $request.Headers.Add('Authorization', $Headers.Authorization)
    $response = $null
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $status = [int]$response.StatusCode
        if ($status -ne 200) {
            # Do not log Location, response bodies or signed URLs. The observed
            # Metadata and advertised blob routes return 200 directly. Do not use
            # the manifests route, which redirects to a credential-bearing SAS URL.
            throw "Cloud drop GET failed with HTTP $status; redirects are not followed."
        }
        return @{ StatusCode = $status; Bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult() }
    }
    catch [Net.Http.HttpRequestException] {
        throw 'Cloud drop transport failed before a usable response; no fallback or credential forwarding was attempted.'
    }
    catch [OperationCanceledException] {
        throw 'Cloud drop GET timed out; no fallback or retry was attempted.'
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        $request.Dispose()
        $client.Dispose()
    }
}

function Get-InsertionDropResponse([scriptblock] $Send, [string] $Uri, [hashtable] $Headers) {
    Assert-InsertionDropServiceUri $Uri
    $response = & $Send $Uri $Headers
    if ($response.StatusCode -ne 200) {
        throw "Cloud drop GET failed with HTTP $($response.StatusCode); redirects are not followed."
    }
    if ($response.Bytes -isnot [byte[]] -or $response.Bytes.Length -eq 0) { throw 'Cloud drop GET returned no bytes.' }
    return $response
}

function Receive-InsertionDropFile(
    [string] $CanonicalUrl, [string] $Path, [string] $BuildNumber,
    [hashtable] $Headers, [scriptblock] $Send
) {
    $location = Get-InsertionDropLocation $CanonicalUrl $BuildNumber
    $encoding = [Text.UTF8Encoding]::new($false, $true)
    $response = Get-InsertionDropResponse $Send $location.MetadataUrl $Headers
    $metadata = ConvertFrom-InsertionJson ($encoding.GetString($response.Bytes))
    $itemCount = 0L
    if ($metadata['drop:name'] -cne $location.DropName -or $metadata['drop:finalized'] -cne 'Finalized' -or
        $metadata['drop:content']['cr:root'] -cne "/drop/$($location.DropName)" -or
        $metadata['drop:content']['cr:blobs'] -cne 'https://devdiv.vsblob.visualstudio.com/' -or
        $metadata['drop:fileListBlob2'] -cnotmatch '\A[a-fA-F0-9]{66}\z' -or
        -not [long]::TryParse([string]$metadata['drop:itemCount'], [ref]$itemCount) -or $itemCount -le 0) {
        throw 'Cloud drop metadata does not identify the exact finalized producer drop and file list.'
    }
    $listUrl = "https://devdiv.vsblob.visualstudio.com/_apis/blob/blobs/$($metadata['drop:fileListBlob2'])"
    $response = Get-InsertionDropResponse $Send $listUrl $Headers
    $listText = $encoding.GetString($response.Bytes)
    $document = [System.Text.Json.JsonDocument]::Parse($listText)
    try {
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
            throw 'Published drop file list must be a JSON array.'
        }
    }
    finally { $document.Dispose() }
    $entries = @(ConvertFrom-InsertionJson $listText)
    if ($entries.Count -ne $itemCount) { throw 'Published drop file list is incomplete.' }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        if ($entry.path -cnotmatch '\A/(?:[A-Za-z0-9_-][A-Za-z0-9_.-]*/)*[A-Za-z0-9_-][A-Za-z0-9_.-]*\z' -or
            -not $seen.Add($entry.path)) {
            throw 'Cloud drop manifest contains an unsafe or duplicate relative path.'
        }
    }
    $matching = @($entries | Where-Object { $_.path -ceq $location.Path })
    if ($matching.Count -ne 1) { throw 'Expected exactly one requested file in the published drop manifest.' }
    $blob = $matching[0].blob
    $size = 0L
    if ($blob.id -cnotmatch '\A[a-fA-F0-9]{66}\z' -or
        -not [long]::TryParse([string]$blob.size, [ref]$size) -or $size -le 0) {
        throw 'Invalid cloud drop blob identity or size.'
    }
    Assert-InsertionDropServiceUri $blob.url
    $blobUri = [Uri]$blob.url
    if ($blobUri.Host -cne 'devdiv.vsblob.visualstudio.com' -or
        $blobUri.AbsolutePath -cne "/_apis/blob/blobs/$($blob.id)") {
        throw 'Published drop blob URL does not identify the advertised blob.'
    }
    $content = Get-InsertionDropResponse $Send $blob.url $Headers
    if ($content.Bytes.LongLength -ne $size) { throw 'Published drop blob length differs from its service manifest.' }
    [IO.File]::WriteAllBytes($Path, $content.Bytes)
}

function New-InsertionDropDownloader([string] $BuildNumber, [hashtable] $Headers) {
    $receive = Get-Command Receive-InsertionDropFile
    $get = Get-Command Invoke-InsertionDropGet
    $send = { param($Uri, $Authentication) & $get $Uri $Authentication }.GetNewClosure()
    return {
        param($Uri, $Path)
        & $receive $Uri $Path $BuildNumber $Headers $send
    }.GetNewClosure()
}

function Test-InsertionPublishedContent([string] $ManifestPath, $Component, [string] $WorkDirectory, [scriptblock] $Download) {
    $liveManifest = Join-Path $WorkDirectory 'live.vsman'
    & $Download $Component.Url $liveManifest
    if ((Get-FileHash -LiteralPath $liveManifest).Hash -cne (Get-FileHash -LiteralPath $ManifestPath).Hash) {
        throw 'Live private-drop manifest differs from the selected producer artifact.'
    }
    foreach ($payload in $Component.Manifest.packages[0].payloads) {
        Assert-InsertionPayload (Join-Path (Split-Path -Parent $ManifestPath) $payload.fileName) $payload
        $downloadPath = Join-Path $WorkDirectory $payload.fileName
        & $Download $payload.url $downloadPath
        Assert-InsertionPayload $downloadPath $payload
    }
}

function Copy-InsertionDrop([string] $InsertionDirectory, [string] $Destination, $Build, [string] $SourceBranch) {
    $manifestPath = Join-Path $InsertionDirectory "$script:Component.vsman"
    $component = Get-InsertionManifest $manifestPath $Build $SourceBranch
    $files = @($component.FileName) + @($component.Manifest.packages[0].payloads.fileName)
    foreach ($payload in $component.Manifest.packages[0].payloads) {
        Assert-InsertionPayload (Join-Path $InsertionDirectory $payload.fileName) $payload
    }
    # Check every collision before copying anything into the existing private drop.
    foreach ($name in $files) {
        $from = Join-Path $InsertionDirectory $name
        $to = Join-Path $Destination $name
        if ([IO.File]::Exists($to) -and
            (Get-FileHash -LiteralPath $from).Hash -cne (Get-FileHash -LiteralPath $to).Hash) {
            throw "Drop staging already contains different bytes for $name."
        }
    }
    foreach ($name in $files) {
        Copy-Item -LiteralPath (Join-Path $InsertionDirectory $name) -Destination (Join-Path $Destination $name)
        Write-Host "Staged insertion file: $name"
    }
}

function Get-ComponentImports([string] $Content) {
    $json = ConvertFrom-InsertionJson $Content
    if ($json.Contains('Imports')) {
        foreach ($name in $json.Imports) {
            if ($name -cnotmatch '^[A-Za-z0-9_-]+\.json$') { throw "Unsupported component import: $name" }
            "/.corext/Configs/$name"
        }
    }
}

function New-ComponentChange([System.Collections.IDictionary] $Documents, $Component) {
    $owners = @()
    foreach ($path in $Documents.Keys) {
        if ($path -cnotmatch '^/\.corext/Configs/[A-Za-z0-9_-]+\.json$') { throw "Unsupported component path: $path" }
        $json = ConvertFrom-InsertionJson $Documents[$path]
        if ($json.Contains('Components') -and $json.Components.Contains($script:Component)) {
            $owners += $path
        }
    }
    if ($owners.Count -ne 1) { throw "Expected one component definition; found $($owners.Count)." }
    $path = $owners[0]
    $original = $Documents[$path]
    $document = [System.Text.Json.JsonDocument]::Parse($original)
    try { $oldEntry = $document.RootElement.GetProperty('Components').GetProperty($script:Component).GetRawText() }
    finally { $document.Dispose() }
    $entry = ConvertFrom-InsertionJson $oldEntry
    if (-not $entry.Contains('fileName') -or -not $entry.Contains('url')) { throw 'Component entry lacks fileName or url.' }
    $entry.fileName = $Component.FileName
    $entry.url = $Component.Url
    $entry.version = $Component.Version
    if ((ConvertFrom-InsertionJson $oldEntry | ConvertTo-Json -Depth 20 -Compress) -ceq
        ($entry | ConvertTo-Json -Depth 20 -Compress)) { return }
    $index = $original.IndexOf($oldEntry, [StringComparison]::Ordinal)
    if ($index -lt 0 -or $original.IndexOf($oldEntry, $index + 1, [StringComparison]::Ordinal) -ge 0) {
        throw 'Component JSON span is ambiguous.'
    }
    $lineStart = $original.LastIndexOf("`n", $index)
    $indent = [regex]::Match($original.Substring($lineStart + 1, $index - $lineStart - 1), '^\s*').Value
    $newline = if ($original.Contains("`r`n")) { "`r`n" } else { "`n" }
    $replacement = ($entry | ConvertTo-Json -Depth 20) -replace '\r?\n', ($newline + $indent)
    $updated = $original.Substring(0, $index) + $replacement + $original.Substring($index + $oldEntry.Length)
    return @{ Path = $path; Before = $original; After = $updated }
}

function Get-InsertionBlobId([byte[]] $Bytes) {
    $header = [Text.Encoding]::ASCII.GetBytes("blob $($Bytes.Length)`0")
    $hash = [Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA1)
    try {
        $hash.AppendData($header)
        $hash.AppendData($Bytes)
        return [Convert]::ToHexString($hash.GetHashAndReset()).ToLowerInvariant()
    }
    finally { $hash.Dispose() }
}

function Assert-AppConfigXml([byte[]] $Bytes) {
    $stream = [IO.MemoryStream]::new($Bytes, $false)
    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = $null
    try {
        $reader = [Xml.XmlReader]::Create($stream, $settings)
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        if ($document.DocumentElement.Name -cne 'configuration') { throw 'App.config must contain a configuration root.' }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        $stream.Dispose()
    }
}

function New-AppConfigChanges([byte[]] $Source, [byte[]] $Current, [System.Collections.IDictionary] $Revisions) {
    if ($null -eq $Source -or $Source.Length -eq 0 -or $null -eq $Current -or $Current.Length -eq 0) {
        throw 'Source and target App.config must both exist.'
    }
    # XML parsing is validation only. Never serialize its decoded view back to git.
    Assert-AppConfigXml $Source
    Assert-AppConfigXml $Current
    $sourceBase64 = [Convert]::ToBase64String($Source)
    $currentBase64 = [Convert]::ToBase64String($Current)
    if ($sourceBase64 -ceq $currentBase64) { return }
    $paths = @($script:RevisionPaths | Where-Object { $Revisions.Contains($_) })
    if ($paths.Count -ne 1) { throw "Changed App.config requires exactly one supported revision.txt; found $($paths.Count)." }
    $path = $paths[0]
    $revision = $Revisions[$path]
    $match = [regex]::Match($revision, '\A([0-9]+)(?=\r?\n|\z)')
    $number = 0
    if (-not $match.Success -or -not [int]::TryParse($match.Value, [ref]$number) -or $number -eq [int]::MaxValue) {
        throw 'Invalid or overflowing revision.txt counter.'
    }
    @{ Path = $script:AppConfig; Before = $currentBase64; After = $sourceBase64; ContentType = 'base64encoded' }
    @{ Path = $path; Before = $revision; After = ($number + 1).ToString() + $revision.Substring($match.Length) }
}

function Assert-InsertionChanges([object[]] $Changes, [string] $ComponentPath) {
    $paths = @($Changes | ForEach-Object { $_.Path })
    if ($paths.Count -eq 0 -or $paths.Count -gt 3 -or
        @($paths | Sort-Object -Unique).Count -ne $paths.Count) { throw 'Empty, duplicate or excessive insertion changes.' }
    foreach ($change in $Changes) {
        if ($change.Path -cne $ComponentPath -and $change.Path -cne $script:AppConfig -and
            $change.Path -cnotin $script:RevisionPaths) { throw "Unrelated insertion change: $($change.Path)" }
        if ($change.Before -ceq $change.After) { throw "No-op insertion change: $($change.Path)" }
    }
    $revisionCount = @($paths | Where-Object { $_ -cin $script:RevisionPaths }).Count
    if (($script:AppConfig -cin $paths) -ne ($revisionCount -eq 1) -or $revisionCount -gt 1) {
        throw 'App.config and exactly one revision.txt must change together.'
    }
}

function Assert-InsertionPr($Pr, [string] $SourceRef, [string] $TargetRef, [string] $Commit, [string] $TargetCommit) {
    if ($Pr.pullRequestId -le 0 -or $Pr.repository.id -cne $script:VsRepo -or
        $Pr.sourceRefName -cne $SourceRef -or $Pr.targetRefName -cne $TargetRef -or
        $Pr.status -cne 'active' -or $Pr.title -cnotmatch '^DO NOT MERGE(?: |$)' -or
        ($Pr.Contains('autoCompleteSetBy') -and $null -ne $Pr.autoCompleteSetBy) -or
        $Pr.lastMergeSourceCommit.commitId -cne $Commit -or
        $Pr.lastMergeTargetCommit.commitId -cne $TargetCommit) {
        throw 'PR identity, current source/target snapshots, active hold or disabled auto-complete verification failed.'
    }
}

function Invoke-InsertionRequest([string] $Method, [string] $Uri, [hashtable] $Headers, $Body) {
    if (-not ($Uri.StartsWith("$script:ComponentApi/", [StringComparison]::Ordinal) -or
        $Uri.StartsWith("$script:VsApi/", [StringComparison]::Ordinal))) { throw 'Unexpected insertion API destination.' }
    $arguments = @{ Method = $Method; Uri = $Uri; Headers = $Headers; ErrorAction = 'Stop' }
    if ($null -ne $Body) {
        $arguments.Body = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $Body -Depth 100))
        $arguments.ContentType = 'application/json; charset=utf-8'
    }
    $response = Invoke-WebRequest @arguments
    if ($Method -ceq 'GET' -and $Uri -cmatch '/blobs/[a-f0-9]{40}\?download=true&\$format=octetStream&api-version=7\.1$') {
        $stream = [IO.MemoryStream]::new()
        try {
            $response.RawContentStream.CopyTo($stream)
            return @{ Bytes = $stream.ToArray() }
        }
        finally { $stream.Dispose() }
    }
    return ConvertFrom-InsertionJson $response.Content
}

function Get-InsertionRef([scriptblock] $Request, [string] $Api, [string] $Repo, [string] $Branch) {
    $filter = [Uri]::EscapeDataString("heads/$Branch")
    $refs = & $Request 'GET' "$Api/git/repositories/$Repo/refs?filter=$filter&api-version=7.1" $null
    $matchingRefs = @($refs.value | Where-Object { $_.name -ceq "refs/heads/$Branch" })
    if ($matchingRefs.Count -ne 1 -or $matchingRefs[0].objectId -cnotmatch '^[a-f0-9]{40}$') {
        throw "Expected exactly one valid ref for $Branch."
    }
    return $matchingRefs[0].objectId
}

function Get-InsertionItem([scriptblock] $Request, [string] $Api, [string] $Repo, [string] $Commit, [string] $Path) {
    $encodedPath = [Uri]::EscapeDataString($Path)
    $item = & $Request 'GET' "$Api/git/repositories/$Repo/items?path=$encodedPath&versionDescriptor.version=$Commit&versionDescriptor.versionType=commit&includeContent=true&`$format=json&api-version=7.1" $null
    if ($item.path -cne $Path -or $item.gitObjectType -cne 'blob' -or -not $item.Contains('content')) {
        throw "Expected a text blob at $Path."
    }
    return $item.content
}

function Get-InsertionBlob([scriptblock] $Request, [string] $Api, [string] $Repo, [string] $Commit, [string] $Path) {
    if ($Commit -cnotmatch '^[a-f0-9]{40}$') { throw 'A full immutable commit is required for blob lookup.' }
    $encodedPath = [Uri]::EscapeDataString($Path)
    $item = & $Request 'GET' "$Api/git/repositories/$Repo/items?path=$encodedPath&versionDescriptor.version=$Commit&versionDescriptor.versionType=commit&`$format=json&api-version=7.1" $null
    if ($item.path -cne $Path -or $item.gitObjectType -cne 'blob' -or $item.objectId -cnotmatch '^[a-f0-9]{40}$') {
        throw "Expected immutable blob metadata at $Path."
    }
    $blob = & $Request 'GET' "$Api/git/repositories/$Repo/blobs/$($item.objectId)?download=true&`$format=octetStream&api-version=7.1" $null
    if ($blob.Bytes -isnot [byte[]] -or (Get-InsertionBlobId $blob.Bytes) -cne $item.objectId) {
        throw "Raw git blob bytes do not match the immutable object ID at $Path."
    }
    return @{ Bytes = $blob.Bytes; ObjectId = $item.objectId; Base64 = [Convert]::ToBase64String($blob.Bytes) }
}

function Get-InsertionProducer([scriptblock] $Request) {
    $head = Get-InsertionRef $Request $script:ComponentApi $script:ComponentRepo $script:SourceBranch
    $encodedBranch = [Uri]::EscapeDataString("refs/heads/$script:SourceBranch")
    $builds = & $Request 'GET' "$script:ComponentApi/build/builds?definitions=1222&branchName=$encodedBranch&statusFilter=completed&resultFilter=succeeded&queryOrder=finishTimeDescending&`$top=1&api-version=7.1" $null
    if ($builds.value.Count -ne 1) { throw 'No successful official 15.9 producer exists.' }
    $build = $builds.value[0]
    Assert-InsertionProducer $build $head $script:SourceBranch
    $artifacts = & $Request 'GET' "$script:ComponentApi/build/builds/$($build.id)/artifacts?api-version=7.1" $null
    $setup = @($artifacts.value | Where-Object { $_.name -ceq 'VSSetup' -and $_.resource.type -ceq 'PipelineArtifact' })
    if ($setup.Count -ne 1) { throw 'Producer must publish exactly one VSSetup pipeline artifact.' }
    $timeline = & $Request 'GET' "$script:ComponentApi/build/builds/$($build.id)/timeline?api-version=7.1" $null
    $dropTaskName = 'Drop signed artifacts to the internal validation namespace'
    foreach ($name in @('Unit Test', 'Integration Test', 'Require passing tests and declared skips before signing', 'Sign', 'Validate the insertion manifest', $dropTaskName)) {
        $acceptedNames = @($name)
        if ($name -ceq $dropTaskName) {
            # 1ES prefixes protected upload tasks in the executed timeline.
            $acceptedNames += [char]::ConvertFromUtf32(0x1F512) + ' ' + $name
        }
        $tasks = @($timeline.records | Where-Object { $_.type -ceq 'Task' -and $_.name -cin $acceptedNames })
        if ($tasks.Count -ne 1 -or $tasks[0].state -cne 'completed' -or $tasks[0].result -cne 'succeeded') {
            throw "Producer prerequisite did not succeed exactly once: $name"
        }
    }
    return $build
}

function Get-InsertionDocuments([scriptblock] $Request, [string] $Commit) {
    $documents = [ordered]@{}
    $pending = [Collections.Generic.Queue[string]]::new()
    $pending.Enqueue('/.corext/Configs/components.json')
    while ($pending.Count -gt 0) {
        $path = $pending.Dequeue()
        if ($documents.Contains($path) -or $documents.Count -ge 100) { throw 'Duplicate, cyclic or excessive component imports.' }
        $content = Get-InsertionItem $Request $script:VsApi $script:VsRepo $Commit $path
        $documents[$path] = $content
        foreach ($import in @(Get-ComponentImports $content)) { $pending.Enqueue($import) }
    }
    return $documents
}

function Get-InsertionPlan([scriptblock] $Request, $Build, $Component, [string] $TargetCommit) {
    $documents = Get-InsertionDocuments $Request $TargetCommit
    $componentChange = New-ComponentChange $documents $Component
    $owners = @($documents.Keys | Where-Object {
        $json = ConvertFrom-InsertionJson $documents[$_]
        $json.Contains('Components') -and $json.Components.Contains($script:Component)
    })
    if ($owners.Count -ne 1) { throw 'Ambiguous component owner.' }
    $sourceConfig = Get-InsertionBlob $Request $script:ComponentApi $script:ComponentRepo $Build.sourceVersion '/src/vstest.console/app.config'
    $currentConfig = Get-InsertionBlob $Request $script:VsApi $script:VsRepo $TargetCommit $script:AppConfig
    $revisions = @{}
    if ($sourceConfig.Base64 -cne $currentConfig.Base64) {
        $items = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/items?scopePath=/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core&recursionLevel=Full&versionDescriptor.version=$TargetCommit&versionDescriptor.versionType=commit&api-version=7.1" $null
        foreach ($item in $items.value) {
            if ($item.path -cin $script:RevisionPaths) {
                $revisions[$item.path] = Get-InsertionItem $Request $script:VsApi $script:VsRepo $TargetCommit $item.path
            }
        }
    }
    $changes = @()
    if ($null -ne $componentChange) { $changes += $componentChange }
    $changes += @(New-AppConfigChanges $sourceConfig.Bytes $currentConfig.Bytes $revisions)
    if ($changes.Count -gt 0) { Assert-InsertionChanges $changes $owners[0] }
    return @{
        TargetCommit = $TargetCommit; ComponentPath = $owners[0]; Changes = $changes
        SourceAppConfigBase64 = $sourceConfig.Base64; SourceAppConfigBlobId = $sourceConfig.ObjectId
    }
}

function Get-InsertionCandidates([scriptblock] $Request, $Build, [string] $SourceRef) {
    $candidates = @()
    for ($skip = 0; $skip -lt 1000; $skip += 100) {
        $page = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/pullrequests?searchCriteria.status=active&searchCriteria.targetRefName=refs%2Fheads%2Frel%2Fd15.9&`$top=100&`$skip=$skip&api-version=7.1" $null
        foreach ($pr in $page.value) {
            if ($pr.sourceRefName.StartsWith('refs/heads/dev/dotnet-bot/insertions/vstest15.9', [StringComparison]::Ordinal) -or
                $pr.title.Contains('VS Test Platform')) {
                $candidates += $pr
            }
        }
        if ($page.value.Count -lt 100) { return $candidates }
    }
    throw 'Active PR search exceeded the bounded page limit.'
}

function Get-InsertionBranchBase([scriptblock] $Request, [string] $SourceRef) {
    $filter = [Uri]::EscapeDataString($SourceRef.Substring(5))
    $refs = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/refs?filter=$filter&api-version=7.1" $null
    $matching = @($refs.value | Where-Object { $_.name -ceq $SourceRef })
    if ($matching.Count -eq 0) { return '0' * 40 }
    if ($matching.Count -ne 1 -or $matching[0].objectId -cnotmatch '^[a-f0-9]{40}$') {
        throw 'Insertion branch has an ambiguous or invalid head.'
    }
    $head = $matching[0].objectId
    $encodedRef = [Uri]::EscapeDataString($SourceRef)
    $closed = @()
    for ($skip = 0; $skip -lt 1000; $skip += 100) {
        $page = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/pullrequests?searchCriteria.status=all&searchCriteria.sourceRefName=$encodedRef&`$top=100&`$skip=$skip&api-version=7.1" $null
        foreach ($pr in $page.value) {
            if ($pr.sourceRefName -cne $SourceRef -or $pr.targetRefName -cne 'refs/heads/rel/d15.9' -or
                $pr.repository.id -cne $script:VsRepo -or $pr.status -cnotin @('completed', 'abandoned')) {
                throw 'Insertion branch history is active, unrelated or malformed; inspect before reusing it.'
            }
            if ($pr.lastMergeSourceCommit.commitId -ceq $head) { $closed += $pr }
        }
        if ($page.value.Count -lt 100) {
            if ($closed.Count -ne 1) {
                throw 'The insertion branch already exists without exactly one closed PR for its current head. Inspect the prior attempt; do not overwrite.'
            }
            return $head
        }
    }
    throw 'Insertion branch history exceeded the bounded page limit.'
}

function Get-InsertionFileChanges($Diff, [object[]] $PlannedChanges) {
    if ($Diff -isnot [Collections.IDictionary] -or -not $Diff.Contains('allChangesIncluded') -or
        $Diff.allChangesIncluded -isnot [bool] -or -not $Diff.allChangesIncluded -or
        -not $Diff.Contains('changes') -or $Diff.changes -isnot [Collections.IList]) {
        throw 'Insertion branch has an incomplete or unexpected diff.'
    }
    $expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($change in $PlannedChanges) {
        if ($change -isnot [Collections.IDictionary] -or -not $change.Contains('Path') -or
            $change.Path -isnot [string] -or -not $change.Path.StartsWith('/', [StringComparison]::Ordinal) -or
            -not $expected.Add($change.Path)) { throw 'Invalid or duplicate planned insertion path.' }
    }
    if ($expected.Count -eq 0) { throw 'Expected a nonempty insertion file plan.' }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $files = [Collections.Generic.List[object]]::new()
    foreach ($change in $Diff.changes) {
        if ($change -isnot [Collections.IDictionary] -or -not $change.Contains('item') -or
            $change.item -isnot [Collections.IDictionary] -or -not $change.Contains('changeType') -or
            $change.changeType -isnot [string] -or $change.changeType -cne 'edit') {
            throw 'Insertion diff contains a malformed or non-edit change.'
        }
        $item = $change.item
        if (-not $item.Contains('path') -or $item.path -isnot [string] -or
            $item.path -cnotmatch '\A/(?:[^/\\\x00-\x1f]+/)*[^/\\\x00-\x1f]+\z' -or
            @($item.path.Split('/') | Where-Object { $_ -in @('.', '..') }).Count -ne 0 -or
            -not $seen.Add($item.path) -or -not $item.Contains('gitObjectType') -or
            $item.gitObjectType -isnot [string] -or
            -not $item.Contains('objectId') -or $item.objectId -isnot [string] -or
            $item.objectId -cnotmatch '\A[a-fA-F0-9]{40}\z' -or
            -not $item.Contains('originalObjectId') -or $item.originalObjectId -isnot [string] -or
            $item.originalObjectId -cnotmatch '\A[a-fA-F0-9]{40}\z' -or
            ($item.Contains('isFolder') -and $item.isFolder -isnot [bool]) -or
            ($item.Contains('isSymbolicLink') -and ($item.isSymbolicLink -isnot [bool] -or $item.isSymbolicLink))) {
            throw 'Insertion diff contains a malformed or duplicate item.'
        }
        if ($item.gitObjectType -ceq 'tree') {
            # Azure DevOps includes edited ancestor trees alongside leaf files.
            if (-not $item.Contains('isFolder') -or -not $item.isFolder -or
                @($expected | Where-Object { $_.StartsWith($item.path + '/', [StringComparison]::Ordinal) }).Count -eq 0) {
                throw 'Insertion diff contains an unexpected directory change.'
            }
        }
        elseif ($item.gitObjectType -ceq 'blob') {
            if (($item.Contains('isFolder') -and $item.isFolder) -or -not $expected.Contains($item.path)) {
                throw 'Insertion branch has an unexpected diff file.'
            }
            $files.Add($change)
        }
        else { throw 'Insertion diff contains an unsupported Git object kind.' }
    }
    if ($files.Count -ne $expected.Count) { throw 'Insertion branch has an incomplete or unexpected diff file set.' }
    return $files.ToArray()
}

function Assert-InsertionReadback([scriptblock] $Request, [string] $SourceCommit, $Plan) {
    $diff = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/diffs/commits?baseVersion=$($Plan.TargetCommit)&baseVersionType=commit&targetVersion=$SourceCommit&targetVersionType=commit&`$top=100&api-version=7.1" $null
    $files = @(Get-InsertionFileChanges $diff $Plan.Changes)
    foreach ($actual in $files) {
        $expected = @($Plan.Changes | Where-Object { $_.Path -ceq $actual.item.path })
        if ($expected.Count -ne 1 -or $actual.changeType -cne 'edit') { throw 'Insertion branch contains an unrelated change.' }
        $content = if ($actual.item.path -ceq $script:AppConfig) {
            (Get-InsertionBlob $Request $script:VsApi $script:VsRepo $SourceCommit $script:AppConfig).Base64
        }
        else { Get-InsertionItem $Request $script:VsApi $script:VsRepo $SourceCommit $actual.item.path }
        if ($content -cne $expected[0].After) { throw "Insertion content mismatch: $($actual.item.path)" }
    }
    $config = Get-InsertionBlob $Request $script:VsApi $script:VsRepo $SourceCommit $script:AppConfig
    if ($config.ObjectId -cne $Plan.SourceAppConfigBlobId -or $config.Base64 -cne $Plan.SourceAppConfigBase64) {
        throw 'Insertion App.config bytes differ from the exact selected producer.'
    }
}

function Get-StableInsertionPr([scriptblock] $Request, [int] $Id, [string] $SourceRef, [string] $SourceCommit, [string] $TargetCommit) {
    $pr = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/pullrequests/$Id`?api-version=7.1" $null
    Assert-InsertionPr $pr $SourceRef 'refs/heads/rel/d15.9' $SourceCommit $TargetCommit
    $liveSource = Get-InsertionRef $Request $script:VsApi $script:VsRepo $SourceRef.Substring('refs/heads/'.Length)
    $liveTarget = Get-InsertionRef $Request $script:VsApi $script:VsRepo 'rel/d15.9'
    if ($liveSource -cne $SourceCommit -or $liveTarget -cne $TargetCommit) {
        throw 'Insertion source or target ref changed during PR verification. Re-inspect before retrying.'
    }
    return $pr
}

function Get-CreatedInsertionPr([scriptblock] $Request, [int] $Id) {
    if ($Id -le 0) { throw 'PR creation did not return a valid structured PR ID; inspect the saved response.' }
    # Azure DevOps may populate the merge-source snapshot after returning the new PR.
    # Only reads are retried; push and create-PR requests are never retried here.
    for ($attempt = 0; $attempt -lt 12; $attempt++) {
        $pr = & $Request 'GET' "$script:VsApi/git/repositories/$script:VsRepo/pullrequests/$Id`?api-version=7.1" $null
        if ($pr.Contains('lastMergeSourceCommit') -and $null -ne $pr.lastMergeSourceCommit -and
            $pr.lastMergeSourceCommit.Contains('commitId')) { return $pr }
        Start-Sleep -Seconds 5
    }
    throw 'Created PR merge-source snapshot is still unavailable; inspect the existing PR before retrying.'
}

function Get-InsertionRetentionLease([scriptblock] $Request, [int] $BuildId, [string] $OwnerId) {
    $response = & $Request 'GET' "$script:ComponentApi/build/builds/$BuildId/leases?api-version=7.1" $null
    if ($response -isnot [Collections.IDictionary] -or -not $response.Contains('value') -or
        $response.value -isnot [Collections.IList] -or -not $response.Contains('count') -or
        $response.count -ne $response.value.Count) {
        throw 'Producer retention lease response is incomplete or malformed.'
    }
    $matching = @($response.value | Where-Object { $_.ownerId -ceq $OwnerId })
    if ($matching.Count -gt 1) { throw 'Producer has duplicate insertion retention leases; inspect before retrying.' }
    if ($matching.Count -eq 1) { return $matching[0] }
}

function Set-InsertionProducerRetention([scriptblock] $Request, $Build, [int] $PrId) {
    Assert-InsertionProducer $Build $Build.sourceVersion $script:SourceBranch
    if ($PrId -le 0) { throw 'A verified VS PR ID is required for producer retention.' }
    $owner = "vstest-insertion:$script:VsRepo/$PrId"
    $lease = Get-InsertionRetentionLease $Request $Build.id $owner
    if ($null -eq $lease) {
        # The documented lease equivalent of keepForever is more than 36500 days.
        $body = @(@{ daysValid = 36501; definitionId = 1222; ownerId = $owner; protectPipeline = $false; runId = $Build.id })
        $null = & $Request 'POST' "$script:ComponentApi/build/retention/leases?api-version=7.1" $body
        $lease = Get-InsertionRetentionLease $Request $Build.id $owner
    }
    $createdOn = [DateTimeOffset]::MinValue
    $validUntil = [DateTimeOffset]::MinValue
    if ($null -eq $lease -or $lease.leaseId -le 0 -or $lease.runId -ne $Build.id -or
        $lease.definitionId -ne 1222 -or $lease.ownerId -cne $owner -or
        $lease.protectPipeline -isnot [bool] -or $lease.protectPipeline -or
        -not [DateTimeOffset]::TryParse([string]$lease.createdOn, [ref]$createdOn) -or
        -not [DateTimeOffset]::TryParse([string]$lease.validUntil, [ref]$validUntil) -or
        ($validUntil - $createdOn).TotalDays -le 36500 -or $validUntil -le [DateTimeOffset]::UtcNow) {
        throw 'Producer insertion retention lease was not verified; inspect before retrying.'
    }
    return $lease
}

function Invoke-ComponentInsertion(
    [scriptblock] $Request,
    [scriptblock] $Download,
    [string] $Phase,
    [string] $WorkDirectory,
    [string] $ArtifactDirectory,
    [string] $TitlePrefix = 'DO NOT MERGE',
    [bool] $CreateDraftPR = $false
) {
    if ($Phase -cnotin @('Select', 'Validate', 'Insert') -or $TitlePrefix -cne 'DO NOT MERGE') {
        throw 'Only Select, Validate or Insert with the DO NOT MERGE hold is supported.'
    }
    $null = New-Item -ItemType Directory -Path $WorkDirectory -Force
    $producerPath = Join-Path $WorkDirectory 'producer.json'
    $build = Get-InsertionProducer $Request
    if ($Phase -ceq 'Select') {
        $build | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $producerPath -Encoding utf8
        Write-Host "Selected producer $($build.id), $($build.buildNumber), source $($build.sourceVersion)."
        Write-Host "##vso[task.setvariable variable=InsertionProducerId]$($build.id)"
        return
    }
    $selected = ConvertFrom-InsertionJson ([IO.File]::ReadAllText($producerPath))
    if ($selected.id -ne $build.id -or $selected.sourceVersion -cne $build.sourceVersion) {
        throw 'Selected producer changed after artifact selection. Refusing stale or unrelated bytes.'
    }
    $manifests = @(Get-ChildItem -LiteralPath $ArtifactDirectory -Recurse -File -Filter '*.vsman')
    if ($manifests.Count -ne 1 -or $manifests[0].Name -cne "$script:Component.vsman") {
        throw 'Expected exactly one VSTest component manifest in the selected VSSetup artifact.'
    }
    $component = Get-InsertionManifest $manifests[0].FullName $build $script:SourceBranch
    Test-InsertionPublishedContent $manifests[0].FullName $component $WorkDirectory $Download
    $target = Get-InsertionRef $Request $script:VsApi $script:VsRepo 'rel/d15.9'
    $sourceRef = 'refs/heads/dev/dotnet-bot/insertions/vstest15.9'
    $candidates = @(Get-InsertionCandidates $Request $build $sourceRef)
    if ($candidates.Count -gt 1) { throw 'More than one current-producer insertion PR exists.' }
    if ($candidates.Count -eq 1) {
        if ($candidates[0].sourceRefName -cne $sourceRef) {
            throw 'An active VSTest insertion PR uses another source branch. Inspect the existing held PR; do not create or update an insertion.'
        }
        $id = $candidates[0].pullRequestId
        $sourceCommit = Get-InsertionRef $Request $script:VsApi $script:VsRepo $sourceRef.Substring('refs/heads/'.Length)
        $pr = Get-StableInsertionPr $Request $id $sourceRef $sourceCommit $target
        $plan = Get-InsertionPlan $Request $build $component $target
        Assert-InsertionReadback $Request $sourceCommit $plan
        $pr = Get-StableInsertionPr $Request $id $sourceRef $sourceCommit $target
        Write-Host "Reusing verified held insertion PR: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/$id"
        $pr | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'pr.json')
        if ($Phase -ceq 'Insert') {
            Set-InsertionProducerRetention $Request $build $id |
                ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'producer-retention.json')
        }
        return
    }
    $plan = Get-InsertionPlan $Request $build $component $target
    $plan | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'plan.json')
    if ($plan.Changes.Count -eq 0) { throw 'The target already contains this insertion; no new PR is needed.' }
    $branchBase = Get-InsertionBranchBase $Request $sourceRef
    Write-Host "Planned insertion changes: $($plan.Changes.Path -join ', ')"
    if ($Phase -ceq 'Validate') { return }

    # Recheck mutable refs and duplicates immediately before the only git write.
    $current = Get-InsertionProducer $Request
    if ($current.id -ne $build.id -or $current.sourceVersion -cne $build.sourceVersion -or
        (Get-InsertionRef $Request $script:VsApi $script:VsRepo 'rel/d15.9') -cne $target -or
        @(Get-InsertionCandidates $Request $build $sourceRef).Count -ne 0) {
        throw 'Insertion inputs changed during validation. Re-inspect before retrying.'
    }
    if ((Get-InsertionBranchBase $Request $sourceRef) -cne $branchBase) {
        throw 'Insertion branch changed during validation. Re-inspect before retrying.'
    }
    # Build the tree from the current VS target. Retain the previous source as a
    # parent so the fixed insertion branch advances without a force push.
    $parents = @($target)
    if ($branchBase -cne ('0' * 40) -and $branchBase -cne $target) { $parents += $branchBase }
    $pushBody = @{
        refUpdates = @(@{ name = $sourceRef; oldObjectId = $branchBase })
        commits = @(@{
            comment = "Insert VS Test Platform $($build.buildNumber)`n`n$([char]::ConvertFromUtf32(0x1F916))"
            parents = $parents
            changes = @($plan.Changes | ForEach-Object {
                $contentType = if ($_.Contains('ContentType')) { $_.ContentType } else { 'rawtext' }
                @{ changeType = 'edit'; item = @{ path = $_.Path }; newContent = @{ content = $_.After; contentType = $contentType } }
            })
        })
    }
    $push = & $Request 'POST' "$script:VsApi/git/repositories/$script:VsRepo/pushes?api-version=7.1" $pushBody
    $push | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'push.json')
    if ($push.commits.Count -ne 1 -or $push.commits[0].commitId -cnotmatch '^[a-f0-9]{40}$' -or
        ($push.commits[0].parents -join ',') -cne ($parents -join ',')) {
        throw 'Unexpected push commit or ancestry; inspect before retrying.'
    }
    $commit = $push.commits[0].commitId
    $buildUrl = "https://dev.azure.com/dnceng/internal/_build/results?buildId=$($build.id)"
    $prBody = @{
        sourceRefName = $sourceRef
        targetRefName = 'refs/heads/rel/d15.9'
        title = "$TitlePrefix VS Test Platform '$script:SourceBranch/$($build.buildNumber)' Insertion into rel/d15.9"
        description = "Insert [internal/microsoft-vstest $($build.buildNumber)]($buildUrl), source ``$($build.sourceVersion)``.`n`nKeep this PR held. The insertion changes only the VSTest component and its matching App.config/revision when needed.`n`n$([char]::ConvertFromUtf32(0x1F916))"
        isDraft = $CreateDraftPR
        reviewers = @(@{ id = '36ea7e54-f558-41df-8bc8-9e06ea8e1f86' })
    }
    $created = & $Request 'POST' "$script:VsApi/git/repositories/$script:VsRepo/pullrequests?api-version=7.1" $prBody
    $created | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'pr-created.json')
    $id = $created.pullRequestId
    Write-Host "Insertion PR: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/$id"
    $pr = Get-CreatedInsertionPr $Request $id
    Assert-InsertionPr $pr $sourceRef 'refs/heads/rel/d15.9' $commit $target
    $pr = Get-StableInsertionPr $Request $id $sourceRef $commit $target
    Assert-InsertionReadback $Request $commit $plan
    $pr = Get-StableInsertionPr $Request $id $sourceRef $commit $target
    $pr | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'pr.json')
    # Ordinary non-draft PR creation triggers the target's policies. Do not vote,
    # override policies, or enable auto-complete.
    Set-InsertionProducerRetention $Request $build $id |
        ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'producer-retention.json')
}

Export-ModuleMember -Function *
