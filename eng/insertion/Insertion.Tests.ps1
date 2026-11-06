$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking

$script:passed = 0
function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
function Test-Case([string] $Name, [scriptblock] $Action) {
    & $Action
    $script:passed++
    Write-Host "PASS $Name"
}
function Assert-Throws([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Expected failure containing '$Message'."
}
function Copy-Object($Value) { return ConvertFrom-InsertionJson ($Value | ConvertTo-Json -Depth 50) }
function Get-XmlBytes([string] $Text) { return ,([Text.Encoding]::UTF8.GetBytes($Text)) }

$componentName = 'Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI'
$componentPath = '/.corext/Configs/components.json'
$appPath = '/src/vset/Agile/TestPlatform/RocksteadyCLI/App.config'
$legacyRevision = '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/revision.txt'
$modernRevision = '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/Shared/revision.txt'
$head = '4878a2ea60d7464762c6668926d1685b35024d39'
$build = @{
    id = 3080344; buildNumber = '20260918.26'
    status = 'completed'; result = 'succeeded'; reason = 'individualCI'
    sourceVersion = $head; sourceBranch = 'refs/heads/internal/rel/15.9'
    definition = @{ id = 1222 }; repository = @{ id = 'ef257391-4d76-4240-8649-3f21ac49c392' }
}
$sourceBranch = 'internal/rel/15.9'
$drop = 'https://vsdrop.corp.microsoft.com/file/v1/Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15.9/20260918.26'
$component = @{ FileName = "$componentName.vsman"; Url = "$drop;$componentName.vsman"; Version = '15.9.3.2026091826' }
$legacyDocument = @'
{
  "Components": {
    "Unrelated": { "url": "unchanged", "fileName": "keep.vsman" },
    "Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI": {
      "fileName": "Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI.vsman",
      "url": "https://old/20190305-03;Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI.vsman"
    }
  }
}
'@

Test-Case 'Reproduce old missing-props lookup; insert into default.config-only legacy layout' {
    $fixture = @{
        '/.corext/Configs/default.config' = '<configuration><packages><package id="Keep" version="1" /></packages></configuration>'
        $componentPath = $legacyDocument
    }
    # The failed CoreXT loader chooses Packages.props when Directory.Packages.props is absent.
    Assert-Throws {
        $props = if ($fixture.ContainsKey('/Directory.Packages.props')) { '/Directory.Packages.props' } else { '/Packages.props' }
        if (-not $fixture.ContainsKey($props)) { throw "TF401174: missing $props" }
    } 'TF401174: missing /Packages.props'
    $changes = @(New-ComponentChange @{ $componentPath = $fixture[$componentPath] } $component)
    Assert-True ($changes.Count -eq 1 -and $changes[0].Path -ceq $componentPath) 'Legacy component change failed.'
    $updated = ConvertFrom-InsertionJson $changes[0].After
    Assert-True ($updated.Components[$componentName].version -ceq $component.Version) 'Component version missing.'
    Assert-True ($updated.Components.Unrelated.url -ceq 'unchanged') 'Unrelated component changed.'
    Assert-True ($fixture['/.corext/Configs/default.config'].Contains('version="1"')) 'Package update invented.'
    Assert-InsertionChanges $changes $componentPath
}

Test-Case 'Modern imported component layout preserves package files and formatting outside the entry' {
    $root = '{"Imports":["testtools-components.json"],"Components":{}}'
    $path = @(Get-ComponentImports $root)[0]
    $documents = @{ $componentPath = $root; $path = $legacyDocument.Replace("`n", "`r`n") }
    $change = New-ComponentChange $documents $component
    Assert-True ($change.Path -ceq '/.corext/Configs/testtools-components.json') 'Wrong component owner.'
    Assert-True ($documents[$componentPath] -ceq $root) 'Root/imports changed.'
    $before = ConvertFrom-InsertionJson $change.Before
    $after = ConvertFrom-InsertionJson $change.After
    $before.Components.Remove($componentName)
    $after.Components.Remove($componentName)
    Assert-True (($before | ConvertTo-Json -Depth 20 -Compress) -ceq ($after | ConvertTo-Json -Depth 20 -Compress)) 'Unrelated JSON changed.'
    Assert-True ($change.After.Contains("`r`n")) 'Newline style changed.'
    Assert-True (@(New-ComponentChange @{ $path = $change.After } $component).Count -eq 0) 'Identical component created a change.'
}

Test-Case 'Missing, duplicate and ambiguous component layouts fail closed' {
    Assert-Throws { New-ComponentChange @{ $componentPath = '{"Components":{}}' } $component } 'Expected one component'
    Assert-Throws { New-ComponentChange @{ $componentPath = $legacyDocument; '/.corext/Configs/other.json' = $legacyDocument } $component } 'Expected one component'
    Assert-Throws { ConvertFrom-InsertionJson '{"Components":{},"Components":{}}' } 'Duplicate JSON property'
    Assert-Throws { Get-ComponentImports '{"Imports":["../../Packages.props"]}' } 'Unsupported component import'
    Assert-Throws { New-ComponentChange @{ '/Packages.props' = $legacyDocument } $component } 'Unsupported component path'
}

Test-Case 'Legacy revision180 increments; exact App.config bytes are used' {
    $source = "<?xml version=`"1.0`"?><configuration />`n"
    $changes = @(New-AppConfigChanges (Get-XmlBytes $source) (Get-XmlBytes '<configuration><runtime /></configuration>') @{ $legacyRevision = "180`n" })
    Assert-True ($changes.Count -eq 2 -and $changes[0].After -ceq [Convert]::ToBase64String((Get-XmlBytes $source))) 'Source App.config not preserved.'
    Assert-True ($changes[0].ContentType -ceq 'base64encoded') 'App.config must use the byte-preserving git push format.'
    Assert-True ($changes[1].Path -ceq $legacyRevision -and $changes[1].After -ceq "181`n") 'Legacy revision failed.'
    Assert-InsertionChanges $changes $componentPath
}

Test-Case 'Modern revision preserves GUID and CRLF' {
    $suffix = "`r`n258c8e00-f938-4679-ac7f-198a615f3337`r`n"
    $changes = @(New-AppConfigChanges (Get-XmlBytes '<configuration />') (Get-XmlBytes '<configuration></configuration>') @{ $modernRevision = "279$suffix" })
    Assert-True ($changes[1].Path -ceq $modernRevision -and $changes[1].After -ceq "280$suffix") 'Modern revision changed trailing content.'
}

Test-Case 'Equal App.config never bumps revision, including line endings' {
    Assert-True (@(New-AppConfigChanges (Get-XmlBytes '<configuration />') (Get-XmlBytes '<configuration />') @{}).Count -eq 0) 'Equal App.config changed.'
    $changes = @(New-AppConfigChanges (Get-XmlBytes "<configuration />`n") (Get-XmlBytes "<configuration />`r`n") @{ $legacyRevision = '180' })
    Assert-True ($changes[0].After -ceq [Convert]::ToBase64String((Get-XmlBytes "<configuration />`n"))) 'Byte-exact source equality was not used.'
}

Test-Case 'Actual 15.9 App.config BOM survives XML validation and base64 push' {
    $sourcePath = Join-Path $PSScriptRoot '..\..\src\vstest.console\app.config'
    $source = [IO.File]::ReadAllBytes($sourcePath)
    Assert-True ([BitConverter]::ToString($source[0..2]) -ceq 'EF-BB-BF') 'The actual 15.9 source fixture must retain its UTF8 BOM.'
    $changes = @(New-AppConfigChanges $source (Get-XmlBytes '<configuration />') @{ $legacyRevision = "180`n" })
    $pushedBytes = [Convert]::FromBase64String($changes[0].After)
    Assert-True ($pushedBytes.Length -eq $source.Length -and
        (Get-InsertionBlobId $pushedBytes) -ceq (Get-InsertionBlobId $source)) 'BOM, newline or other source bytes changed.'
    Assert-True (@(New-AppConfigChanges $source $source @{}).Count -eq 0) 'Byte-identical actual source must not bump revision.'
}

Test-Case 'BOM-only App.config difference is a real byte change; identical BOM bytes are a no-op' {
    $plain = Get-XmlBytes "<?xml version=`"1.0`" encoding=`"utf-8`"?><configuration />`n"
    $withBom = [byte[]](@(0xEF, 0xBB, 0xBF) + $plain)
    $changes = @(New-AppConfigChanges $withBom $plain @{ $legacyRevision = "180`n" })
    Assert-True ($changes.Count -eq 2 -and $changes[1].After -ceq "181`n") 'BOM-only change was ignored.'
    Assert-True ([Convert]::FromBase64String($changes[0].After).Length -eq ($plain.Length + 3)) 'UTF8 BOM bytes were lost.'
    Assert-True ((Get-InsertionBlobId $withBom) -cne (Get-InsertionBlobId $plain)) 'BOM-only difference must have distinct git blob IDs.'
    Assert-True (@(New-AppConfigChanges $withBom $withBom @{}).Count -eq 0) 'Identical BOM bytes produced a change.'
}

Test-Case 'Missing, ambiguous, malformed and overflowing revisions fail closed' {
    $source = Get-XmlBytes '<configuration />'
    $current = Get-XmlBytes '<configuration></configuration>'
    Assert-Throws { New-AppConfigChanges $source $current @{} } 'exactly one supported revision'
    Assert-Throws { New-AppConfigChanges $source $current @{ $legacyRevision = '1'; $modernRevision = '1' } } 'exactly one supported revision'
    foreach ($revision in @('abc', '1bad', '-1', '2147483647', '999999999999999999999')) {
        Assert-Throws { New-AppConfigChanges $source $current @{ $legacyRevision = $revision } } 'revision.txt counter'
    }
    Assert-Throws { New-AppConfigChanges ([byte[]]@()) $current @{} } 'must both exist'
    Assert-Throws { New-AppConfigChanges (Get-XmlBytes '<invalid') $current @{} } 'Unexpected end of file'
    Assert-Throws { New-AppConfigChanges (Get-XmlBytes '<wrong />') $current @{} } 'configuration root'
}

Test-Case 'Only exact successful current producer identity is accepted' {
    Assert-InsertionProducer $build $head $sourceBranch
    foreach ($key in @('sourceVersion', 'sourceBranch', 'status', 'result', 'reason', 'buildNumber')) {
        $bad = Copy-Object $build
        $bad[$key] = if ($key -eq 'reason') { 'pullRequest' } else { 'wrong' }
        Assert-Throws { Assert-InsertionProducer $bad $head $sourceBranch } 'exact current component source'
    }
    $bad = Copy-Object $build
    $bad.definition.id = 1595
    Assert-Throws { Assert-InsertionProducer $bad $head $sourceBranch } 'exact current component source'
    $bad = Copy-Object $build
    $bad.repository.id = 'a290117c-5a8a-40f7-bc2c-f14dbe3acf6d'
    Assert-Throws { Assert-InsertionProducer $bad $head $sourceBranch } 'exact current component source'
    foreach ($branch in @('internal/experiments/15.9-vs-insertion', 'rel/15.9', 'internal/rel/17.11', 'main')) {
        $bad = Copy-Object $build
        $bad.sourceBranch = "refs/heads/$branch"
        Assert-Throws { Assert-InsertionProducer $bad $head $sourceBranch } 'exact current component source'
        Assert-Throws { Assert-InsertionProducer $bad $head $branch } 'exact current component source'
    }
}

Test-Case 'PR identification uses structured repo, refs, commit and hold, never changelog links' {
    $pr = @{
        pullRequestId = 999; repository = @{ id = 'a290117c-5a8a-40f7-bc2c-f14dbe3acf6d' }
        sourceRefName = 'refs/heads/dev/dotnet-bot/insertions/vstest15.9'; targetRefName = 'refs/heads/rel/d15.9'
        lastMergeSourceCommit = @{ commitId = $head }; lastMergeTargetCommit = @{ commitId = 'b' * 40 }
        status = 'active'; title = 'DO NOT MERGE VS Test Platform'
        autoCompleteSetBy = $null; description = 'Changelog: https://dev.azure.com/dnceng/internal/_git/microsoft-vstest/pullrequest/65486'
    }
    Assert-InsertionPr $pr $pr.sourceRefName $pr.targetRefName $head ('b' * 40)
    foreach ($key in @('sourceRefName', 'targetRefName', 'title', 'status', 'autoCompleteSetBy')) {
        $bad = Copy-Object $pr
        $bad[$key] = 'wrong'
        Assert-Throws { Assert-InsertionPr $bad $pr.sourceRefName $pr.targetRefName $head ('b' * 40) } 'PR identity'
    }
    $bad = Copy-Object $pr
    $bad.repository.id = $build.repository.id
    Assert-Throws { Assert-InsertionPr $bad $pr.sourceRefName $pr.targetRefName $head ('b' * 40) } 'PR identity'
    Assert-Throws { Assert-InsertionPr $pr $pr.sourceRefName $pr.targetRefName ('a' * 40) ('b' * 40) } 'PR identity'
    Assert-Throws { Assert-InsertionPr $pr $pr.sourceRefName $pr.targetRefName $head ('d' * 40) } 'PR identity'
}

Test-Case 'Bounded diff rejects unrelated, duplicate, no-op and revision-only edits' {
    Assert-Throws { Assert-InsertionChanges @() $componentPath } 'Empty'
    Assert-Throws { Assert-InsertionChanges @(@{ Path = '/Packages.props'; Before = ''; After = 'dummy' }) $componentPath } 'Unrelated'
    Assert-Throws { Assert-InsertionChanges @(@{ Path = $componentPath; Before = 'same'; After = 'same' }) $componentPath } 'No-op'
    Assert-Throws { Assert-InsertionChanges @(@{ Path = $legacyRevision; Before = '1'; After = '2' }) $componentPath } 'must change together'
    Assert-Throws { Assert-InsertionChanges @(@{ Path = $appPath; Before = 'old'; After = 'new' }) $componentPath } 'must change together'
    $change = @{ Path = $componentPath; Before = 'old'; After = 'new' }
    Assert-Throws { Assert-InsertionChanges @($change, $change) $componentPath } 'duplicate'
}

$root = Join-Path ([IO.Path]::GetTempPath()) ('vstest-insertion-' + [Guid]::NewGuid().ToString('N'))
try {
    $insertion = New-Item -ItemType Directory -Path (Join-Path $root 'Insertion')
    $staging = New-Item -ItemType Directory -Path (Join-Path $root 'Staging')
    $vsix = Join-Path $insertion 'TestPlatform.vsix'
    [IO.File]::WriteAllText($vsix, 'synthetic signed-container bytes; never executed')
    $payload = @{
        fileName = 'TestPlatform.vsix'; sha256 = (Get-FileHash $vsix).Hash
        size = (Get-Item $vsix).Length; url = "$drop;/TestPlatform.vsix"; signer = @{ '$ref' = '1' }
    }
    $manifest = @{
        manifestVersion = '1.1'; info = @{ manifestName = $componentName; buildVersion = $component.Version }
        signers = @(@{ '$id' = '1'; subjectName = 'fixture' })
        packages = @(@{ id = $componentName; version = $component.Version; type = 'Vsix'; payloads = @($payload) })
    }
    $sbomName = "${componentName}_sbom.json"
    $sbomPath = Join-Path $insertion $sbomName
    [IO.File]::WriteAllText($sbomPath, '{"fixture":"unchanged generated metadata"}')
    $manifest.packages[0].payloads += @{
        fileName = $sbomName; sha256 = (Get-FileHash $sbomPath).Hash
        size = (Get-Item $sbomPath).Length; url = "$drop;/$sbomName"
    }
    $manifestPath = Join-Path $insertion "$componentName.vsman"
    $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath

    Test-Case 'Manifest binds exact producer private drop and component version' {
        $actual = Get-InsertionManifest $manifestPath $build $sourceBranch
        Assert-True ($actual.Url -ceq $component.Url) 'Wrong manifest URL.'
        $qualified = Get-InsertionManifest $manifestPath $build "refs/heads/$sourceBranch"
        Assert-True ($qualified.Url -ceq $component.Url) 'Producer environment branch changed the drop URL.'
        Assert-InsertionPayload $vsix $payload
        $badBuild = Copy-Object $build
        $badBuild.buildNumber = '20260918.27'
        Assert-Throws { Get-InsertionManifest $manifestPath $badBuild $sourceBranch } 'does not identify producer'
        foreach ($url in @('https://public.example/payload', "$drop;/../TestPlatform.vsix", "$drop;/other.vsix")) {
            $bad = Copy-Object $manifest
            $bad.packages[0].payloads[0].url = $url
            $bad | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
            Assert-Throws { Get-InsertionManifest $manifestPath $build $sourceBranch } 'exact producer private-drop URL'
        }
        $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
    }

    Test-Case 'Experiment and public branches cannot stage a servicing drop' {
        foreach ($branch in @('internal/experiments/15.9-vs-insertion', 'rel/15.9', 'internal/rel/17.11', 'main')) {
            foreach ($name in @($branch, "refs/heads/$branch")) {
                Assert-Throws { Copy-InsertionDrop $insertion.FullName $staging.FullName $build $name } 'private internal/rel/15.9'
            }
        }
        Assert-True (@(Get-ChildItem -LiteralPath $staging).Count -eq 0) 'Rejected branch staged a file.'
    }

    Test-Case 'Manifest rejects traversal, absolute, duplicate and ambiguous payload names' {
        foreach ($name in @('../TestPlatform.vsix', '/TestPlatform.vsix', 'C:\TestPlatform.vsix', '..', 'folder\payload.vsix')) {
            $bad = Copy-Object $manifest
            $bad.packages[0].payloads[0].fileName = $name
            $bad.packages[0].payloads[0].url = "$drop;/$name"
            $bad | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
            Assert-Throws { Get-InsertionManifest $manifestPath $build $sourceBranch } 'unique flat filename'
        }
        $bad = Copy-Object $manifest
        $bad.packages[0].payloads += $bad.packages[0].payloads[0]
        $bad | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
        Assert-Throws { Get-InsertionManifest $manifestPath $build $sourceBranch } 'unique flat filename'
        $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
    }

    Test-Case 'Private drop includes original manifest and verified payload, not reconstructed metadata' {
        foreach ($number in 1..9) { [IO.File]::WriteAllText((Join-Path $staging "package$number.nupkg"), "package bytes $number") }
        $packagesBefore = @(Get-ChildItem -LiteralPath $staging -Filter '*.nupkg' | Get-FileHash)
        Copy-InsertionDrop $insertion.FullName $staging.FullName $build $sourceBranch
        Assert-True ((Get-FileHash (Join-Path $staging $component.FileName)).Hash -ceq (Get-FileHash $manifestPath).Hash) 'Manifest bytes changed.'
        Assert-True ((Get-FileHash (Join-Path $staging 'TestPlatform.vsix')).Hash -ceq $payload.sha256) 'Payload bytes changed.'
        Assert-True ((Get-FileHash (Join-Path $staging $sbomName)).Hash -ceq (Get-FileHash $sbomPath).Hash) 'SBOM bytes changed.'
        $packagesAfter = @(Get-ChildItem -LiteralPath $staging -Filter '*.nupkg' | Get-FileHash)
        Assert-True ($packagesAfter.Count -eq 9 -and
            (($packagesAfter.Hash -join ',') -ceq ($packagesBefore.Hash -join ','))) 'Existing package count/content changed.'
        Copy-InsertionDrop $insertion.FullName $staging.FullName $build $sourceBranch
        [IO.File]::WriteAllText((Join-Path $staging 'TestPlatform.vsix'), 'different payload')
        Assert-Throws { Copy-InsertionDrop $insertion.FullName $staging.FullName $build $sourceBranch } 'different bytes'
        [IO.File]::WriteAllText($vsix, 'corrupt')
        Assert-Throws { Assert-InsertionPayload $vsix $payload } 'mismatched payload'
        Assert-Throws { Assert-InsertionPayload (Join-Path $insertion 'missing.vsix') $payload } 'Missing'
    }
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
Write-Host "Insertion self-tests passed: $script:passed."
& (Join-Path $PSScriptRoot 'Insertion.Workflow.Tests.ps1')
& (Join-Path $PSScriptRoot 'Insertion.Entrypoint.Tests.ps1')
& (Join-Path $PSScriptRoot 'Insertion.Drop.Tests.ps1')
& (Join-Path $PSScriptRoot 'Insertion.Diff.Tests.ps1')
& (Join-Path $PSScriptRoot 'Insertion.Producer.Tests.ps1')
& (Join-Path $PSScriptRoot 'Insertion.Retention.Tests.ps1')
