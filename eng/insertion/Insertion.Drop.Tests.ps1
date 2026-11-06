$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking

function Assert-Drop([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-DropFailure([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message.Contains('do-not-log') -or $_.Exception.Message.Contains('fixture-not-a-credential')) {
            throw 'Drop error disclosed credential-bearing input.'
        }
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Expected drop failure containing '$Message'."
}
function Copy-DropFixture($Value) { return ConvertFrom-InsertionJson ($Value | ConvertTo-Json -Depth 30) }

$root = Join-Path ([IO.Path]::GetTempPath()) ('vstest-drop-contract-' + [Guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path $root
    $component = 'Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI'
    $buildNumber = '20260921.8'
    $prefix = "https://vsdrop.corp.microsoft.com/file/v1/Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15.9/$buildNumber;"
    $dropName = "Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15.9/$buildNumber"
    $metadataApi = "https://devdiv.artifacts.visualstudio.com/_apis/drop/drop/$dropName`?api-version=7.1-preview.1"
    $listId = 'F0' * 33
    $listApi = "https://devdiv.vsblob.visualstudio.com/_apis/blob/blobs/$listId"
    $names = @("$component.vsman", 'TestPlatform.vsix', "${component}_sbom.json")
    $entries = @()
    $blobs = @{}
    for ($index = 0; $index -lt $names.Count; $index++) {
        $id = ('{0:X2}' -f ($index + 1)) * 33
        $bytes = [Text.Encoding]::UTF8.GetBytes("Exact fixture bytes for $($names[$index])")
        $entries += @{ path = "/$($names[$index])"; blob = @{ id = $id; size = $bytes.Length; expiration = $null; url = "https://devdiv.vsblob.visualstudio.com/_apis/blob/blobs/$id" } }
        $blobs[$id] = $bytes
    }
    $state = @{
        Entries = $entries; Blobs = $blobs; Calls = [Collections.Generic.List[string]]::new()
        Metadata = @{
            'drop:name' = $dropName; 'drop:finalized' = 'Finalized'; 'drop:fileListBlob2' = $listId
            'drop:content' = @{ 'cr:root' = "/drop/$dropName"; 'cr:blobs' = 'https://devdiv.vsblob.visualstudio.com/' }
        }
        MetadataStatus = 200; ListStatus = 200; BlobStatus = 200; ThrowNetworkError = $false; ItemCountDelta = 0
        ReturnedSizeDelta = 0
    }
    $headers = @{ Authorization = 'Bearer fixture-not-a-credential' }
    $assert = Get-Command Assert-Drop
    $send = {
        param($Uri, $Authentication)
        & $assert ($Authentication.Authorization -ceq 'Bearer fixture-not-a-credential') 'Unexpected authentication input.'
        $state.Calls.Add($Uri)
        if ($state.ThrowNetworkError) { throw [Net.Http.HttpRequestException]::new('Synthetic network failure') }
        if ($Uri -ceq $metadataApi) {
            $state.Metadata['drop:itemCount'] = [string]($state.Entries.Count + $state.ItemCountDelta)
            return @{ StatusCode = $state.MetadataStatus; Bytes = [Text.Encoding]::UTF8.GetBytes(($state.Metadata | ConvertTo-Json -Depth 30)) }
        }
        if ($Uri -ceq $listApi) {
            return @{ StatusCode = $state.ListStatus; Bytes = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject @($state.Entries) -Depth 30)) }
        }
        $id = ([Uri]$Uri).Segments[-1]
        if (-not $state.Blobs.ContainsKey($id)) { throw 'Unexpected blob request.' }
        $bytes = $state.Blobs[$id]
        if ($state.ReturnedSizeDelta -ne 0) { $bytes = [byte[]](@(0) + $bytes) }
        return @{ StatusCode = $state.BlobStatus; Bytes = $bytes }
    }.GetNewClosure()
    $output = Join-Path $root 'download'

    foreach ($name in $names) {
        $canonical = $prefix + $(if ($name.EndsWith('.vsman')) { '' } else { '/' }) + $name
        Receive-InsertionDropFile $canonical $output $buildNumber $headers $send
        $entry = $entries | Where-Object path -ceq "/$name"
        Assert-Drop ([Convert]::ToBase64String([IO.File]::ReadAllBytes($output)) -ceq [Convert]::ToBase64String($blobs[$entry.blob.id])) 'Drop download bytes changed.'
        Assert-Drop ((Get-InsertionDropLocation $canonical $buildNumber).MetadataUrl -ceq $metadataApi) 'Wrong producer drop mapping.'
    }
    Assert-Drop (@($state.Calls | Where-Object { $_ -like '*vsdrop.corp*' }).Count -eq 0) 'Cloud reader contacted the corporate gateway.'
    Write-Host 'PASS Drop: advertised manifest schema maps all three canonical files to exact producer cloud blobs.'

    foreach ($property in @('drop:name', 'drop:finalized', 'drop:fileListBlob2')) {
        $original = $state.Metadata[$property]
        $state.Metadata[$property] = 'wrong'
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'exact finalized producer drop'
        $state.Metadata[$property] = $original
    }
    foreach ($property in @('cr:root', 'cr:blobs')) {
        $original = $state.Metadata['drop:content'][$property]
        $state.Metadata['drop:content'][$property] = 'https://unexpected.example/'
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'exact finalized producer drop'
        $state.Metadata['drop:content'][$property] = $original
    }
    $state.ItemCountDelta = 1
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'file list is incomplete'
    $state.ItemCountDelta = 0
    Write-Host 'PASS Drop: exact finalized drop identity, advertised trusted file-list blob and complete population are required.'

    $calls = $state.Calls.Count
    $badCanonicalUrls = @(
        ($prefix.Replace('https:', 'http:') + $names[0]),
        ($prefix.Replace('vsdrop.corp.microsoft.com', 'unexpected.example') + $names[0]),
        ($prefix.Replace('/20260921.8;', '/20260921.7;') + $names[0]),
        ($prefix.Replace('/realsigned-DO-NOT-SHIP-15.9/', '/realsigned-DO-NOT-SHIP-15.9-vs-insertion/') + $names[0]),
        ($prefix + '../TestPlatform.vsix'), ($prefix + '/folder/TestPlatform.vsix'),
        ($prefix + '/%2e%2e/TestPlatform.vsix'), ($prefix + '//TestPlatform.vsix'),
        ($prefix + '/TestPlatform.vsix?secret=value'), ($prefix + '/TestPlatform.vsix#fragment')
    )
    Assert-Drop ($badCanonicalUrls.Count -eq 10) 'Canonical URL negative cases were not enumerated separately.'
    foreach ($canonical in $badCanonicalUrls) {
        Assert-DropFailure { Receive-InsertionDropFile $canonical $output $buildNumber $headers $send } 'drop'
    }
    Assert-Drop ($state.Calls.Count -eq $calls) 'Invalid canonical producer/path reached transport.'
    Assert-DropFailure { Assert-InsertionDropServiceUri ($metadataApi.Replace('/realsigned-DO-NOT-SHIP-15.9/', '/realsigned-DO-NOT-SHIP-15.9-vs-insertion/')) } 'Untrusted cloud drop URI'
    Write-Host 'PASS Drop: wrong producer, canonical origin, traversal, query and ambiguous paths reject before transport.'

    $badBlobUrls = @(
        ('http://devdiv.vsblob.visualstudio.com/_apis/blob/blobs/' + $entries[0].blob.id),
        ('https://unexpected.example/_apis/blob/blobs/' + $entries[0].blob.id),
        ('https://devdiv.vsblob.visualstudio.com.evil.example/_apis/blob/blobs/' + $entries[0].blob.id),
        ('https://devdiv.vsblob.visualstudio.com:444/_apis/blob/blobs/' + $entries[0].blob.id),
        ('https://user@devdiv.vsblob.visualstudio.com/_apis/blob/blobs/' + $entries[0].blob.id),
        ($entries[0].blob.url + '?credential=do-not-log'),
        ($entries[0].blob.url + '#fragment'),
        ('https://devdiv.vsblob.visualstudio.com/_apis/blob/../blobs/' + $entries[0].blob.id)
    )
    Assert-Drop ($badBlobUrls.Count -eq 8) 'Blob origin negative cases were not enumerated separately.'
    foreach ($uri in $badBlobUrls) {
        $state.Entries = Copy-DropFixture $entries
        $state.Entries[0].blob.url = $uri
        $before = $state.Calls.Count
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'Untrusted cloud drop URI'
        Assert-Drop ($state.Calls.Count -eq ($before + 2)) 'Bearer reached untrusted blob URI.'
    }
    $state.Entries = Copy-DropFixture $entries
    $state.Entries[0].blob.url = $entries[0].blob.url.Replace('.com/', '.com:443/')
    Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send
    $state.Entries = Copy-DropFixture $entries
    Write-Host 'PASS Drop: exact HTTPS origins/default port only; host, scheme, userinfo, query, fragment and traversal rejected.'

    $state.Entries = @($entries[1], $entries[2])
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'exactly one'
    $state.Entries = @($entries) + @($entries[0])
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'duplicate relative path'
    foreach ($path in @('/../TestPlatform.vsix', '/folder/../TestPlatform.vsix', '/%2e%2e/TestPlatform.vsix', '//TestPlatform.vsix', 'C:\TestPlatform.vsix')) {
        $state.Entries = Copy-DropFixture $entries
        $state.Entries[0].path = $path
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'unsafe or duplicate relative path'
    }
    $state.Entries = Copy-DropFixture $entries
    $state.Entries[0].blob.url = $entries[1].blob.url
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'does not identify'
    $state.Entries = Copy-DropFixture $entries
    Write-Host 'PASS Drop: missing, duplicate, unsafe manifest entries and mismatched blob identities fail closed.'

    Remove-Item -LiteralPath $output
    foreach ($status in @(301, 302, 307, 308, 401, 403, 404, 429, 500, 503)) {
        $state.MetadataStatus = $status
        $before = $state.Calls.Count
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } "HTTP $status"
        Assert-Drop ($state.Calls.Count -eq ($before + 1) -and -not (Test-Path -LiteralPath $output)) 'Manifest error was followed, retried or written.'
        $state.MetadataStatus = 200
        $state.ListStatus = $status
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } "HTTP $status"
        Assert-Drop (-not (Test-Path -LiteralPath $output)) 'File-list error wrote success-shaped content.'
        $state.ListStatus = 200
        $state.BlobStatus = $status
        Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } "HTTP $status"
        Assert-Drop (-not (Test-Path -LiteralPath $output)) 'Blob error wrote success-shaped content.'
        $state.BlobStatus = 200
    }
    $state.Blobs[$entries[0].blob.id] = [byte[]]@()
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'returned no bytes'
    $state.Blobs[$entries[0].blob.id] = [Text.Encoding]::UTF8.GetBytes("Exact fixture bytes for $($names[0])")
    $state.ThrowNetworkError = $true
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'Synthetic network failure'
    $state.ThrowNetworkError = $false
    $state.ReturnedSizeDelta = 1
    Assert-DropFailure { Receive-InsertionDropFile ($prefix + $names[0]) $output $buildNumber $headers $send } 'length differs'
    $state.ReturnedSizeDelta = 0
    Assert-Drop (-not (Test-Path -LiteralPath $output)) 'Transport error produced a destination.'
    Write-Host 'PASS Drop: redirects, authorization/not-found/transient/network errors and service size mismatch never fall back.'

    $artifact = New-Item -ItemType Directory -Path (Join-Path $root 'Artifact')
    $work = New-Item -ItemType Directory -Path (Join-Path $root 'Work')
    $manifestPath = Join-Path $artifact "$component.vsman"
    [IO.File]::WriteAllBytes($manifestPath, $blobs[$entries[0].blob.id])
    [IO.File]::WriteAllBytes((Join-Path $artifact 'TestPlatform.vsix'), $blobs[$entries[1].blob.id])
    $componentInfo = @{
        Url = $prefix + $names[0]
        Manifest = @{ packages = @(@{ payloads = @(@{
            fileName = 'TestPlatform.vsix'; url = $prefix + '/TestPlatform.vsix'
            sha256 = (Get-FileHash (Join-Path $artifact 'TestPlatform.vsix')).Hash
            size = $blobs[$entries[1].blob.id].Length
        }) }) }
    }
    $receive = Get-Command Receive-InsertionDropFile
    $download = { param($Uri, $Path) & $receive $Uri $Path $buildNumber $headers $send }.GetNewClosure()
    Test-InsertionPublishedContent $manifestPath $componentInfo $work.FullName $download
    $original = $state.Blobs[$entries[0].blob.id]
    $wrong = [byte[]]$original.Clone()
    $wrong[0] = $wrong[0] -bxor 1
    $state.Blobs[$entries[0].blob.id] = $wrong
    Assert-DropFailure { Test-InsertionPublishedContent $manifestPath $componentInfo $work.FullName $download } 'manifest differs'
    $state.Blobs[$entries[0].blob.id] = $original
    $wrong = [byte[]]$state.Blobs[$entries[1].blob.id].Clone()
    $wrong[0] = $wrong[0] -bxor 1
    $state.Blobs[$entries[1].blob.id] = $wrong
    Assert-DropFailure { Test-InsertionPublishedContent $manifestPath $componentInfo $work.FullName $download } 'mismatched payload'
    Write-Host 'PASS Drop: cloud transport still requires exact artifact manifest and declared payload SHA-256.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
Write-Host 'Insertion cloud drop self-tests passed: 7.'
