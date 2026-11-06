[CmdletBinding()]
param(
    [string] $DiffPath = (Join-Path $PSScriptRoot 'fixtures\actual-15.9-diff.json'),
    [string] $PlanPath = (Join-Path $PSScriptRoot 'fixtures\actual-15.9-file-plan.json')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking

function Copy-DiffFixture($Value) { return ConvertFrom-InsertionJson ($Value | ConvertTo-Json -Depth 100) }
function Assert-Diff([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Assert-DiffFailure([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Expected diff verifier failure containing '$Message'."
}

# Snapshot of the actual 16-entry response and the retained plan's file identities.
# Only per-item navigation URLs and plan file contents are omitted from the fixtures.
$diff = ConvertFrom-InsertionJson ([IO.File]::ReadAllText($DiffPath))
$plan = ConvertFrom-InsertionJson ([IO.File]::ReadAllText($PlanPath))
$planned = $plan.Changes
$blobIndex = 2
$treeIndex = 0

Assert-Diff ($diff.changes.Count -eq 16 -and $planned.Count -eq 3) 'Retained real response/plan populations changed.'
Assert-DiffFailure {
    if ($diff.changes.Count -ne $planned.Count) { throw 'Old verifier rejects 13 ancestor trees plus 3 files.' }
} 'Old verifier rejects'
$files = @(Get-InsertionFileChanges $diff $planned)
Assert-Diff ($files.Count -eq 3 -and @($files | Where-Object { $_.item.gitObjectType -cne 'blob' }).Count -eq 0) 'Expected exactly three genuine file edits.'
Assert-Diff ((@($files | ForEach-Object { $_.item.path } | Sort-Object) -join '|') -ceq (@($planned.Path | Sort-Object) -join '|')) 'Retained planned file set differs.'
$withoutTrees = Copy-DiffFixture $diff
$withoutTrees.changes = @($files)
Assert-Diff (@(Get-InsertionFileChanges $withoutTrees $planned).Count -eq 3) 'A complete file-only response must remain supported.'
Write-Host 'PASS Diff: retained real 16-entry response verifies exactly three planned blobs; ancestor trees are metadata.'

foreach ($complete in @($false, 'true', 1, $null)) {
    $bad = Copy-DiffFixture $diff
    $bad.allChangesIncluded = $complete
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'incomplete or unexpected diff'
}
$bad = Copy-DiffFixture $diff
$bad.Remove('allChangesIncluded')
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'incomplete or unexpected diff'
foreach ($changes in @($null, 'not-an-array', @{ item = 'not-an-array' })) {
    $bad = Copy-DiffFixture $diff
    $bad.changes = $changes
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'incomplete or unexpected diff'
}
$bad = Copy-DiffFixture $diff
$bad.Remove('changes')
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'incomplete or unexpected diff'
Write-Host 'PASS Diff: incomplete, missing or malformed response populations cannot pass.'

$bad = Copy-DiffFixture $diff
$extra = Copy-DiffFixture $bad.changes[$blobIndex]
$extra.item.path = '/src/unrelated.txt'
$bad.changes += $extra
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unexpected diff file'
$bad = Copy-DiffFixture $diff
$bad.changes = @($bad.changes | Where-Object { $_.item.path -cne $planned[0].Path })
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'incomplete or unexpected diff file set'
foreach ($index in @($blobIndex, $treeIndex)) {
    $bad = Copy-DiffFixture $diff
    $bad.changes += Copy-DiffFixture $bad.changes[$index]
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'duplicate item'
}
Assert-DiffFailure { Get-InsertionFileChanges $diff @($planned[0], $planned[0], $planned[1]) } 'duplicate planned'
Write-Host 'PASS Diff: extra, missing and duplicate files or directory entries fail closed.'

foreach ($type in @('rename', 'delete', 'add', 'edit, rename', '', $null)) {
    foreach ($index in @($blobIndex, $treeIndex)) {
        $bad = Copy-DiffFixture $diff
        $bad.changes[$index].changeType = $type
        Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or non-edit change'
    }
}
foreach ($kind in @('commit', 'unknown', 'Blob', '')) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex].item.gitObjectType = $kind
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unsupported Git object kind'
}
Write-Host 'PASS Diff: rename, delete, add, composite edits and unsupported object kinds are never ignored.'

foreach ($path in @('/unrelated', '/.core', '/src/vset/Agile/TestPlatform/Rocksteady', '/src/SetupPackagesX')) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$treeIndex].item.path = $path
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unexpected directory change'
}
$bad = Copy-DiffFixture $diff
$bad.changes[$treeIndex].item.isFolder = $false
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unexpected directory change'
$bad.changes[$treeIndex].item.Remove('isFolder')
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unexpected directory change'
$bad = Copy-DiffFixture $diff
$bad.changes[$blobIndex].item.isFolder = $true
Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'unexpected diff file'
Write-Host 'PASS Diff: only genuine edited trees at exact planned-file ancestors may be classified as metadata.'

foreach ($property in @('path', 'gitObjectType', 'objectId', 'originalObjectId')) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex].item.Remove($property)
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or duplicate item'
}
foreach ($path in @('', 'relative/file', '/src/../file', '/src/./file', '/src//file', '/src\', "/src/`nfile")) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$treeIndex].item.path = $path
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or duplicate item'
}
foreach ($value in @('true', 1, $null)) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex].item.isFolder = $value
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or duplicate item'
}
foreach ($value in @($true, 'false', $null)) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex].item.isSymbolicLink = $value
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or duplicate item'
}
foreach ($property in @('objectId', 'originalObjectId')) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex].item[$property] = 'invalid'
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or duplicate item'
}
foreach ($entry in @($null, 'invalid', @{ changeType = 'edit' }, @{ item = @{} })) {
    $bad = Copy-DiffFixture $diff
    $bad.changes[$blobIndex] = $entry
    Assert-DiffFailure { Get-InsertionFileChanges $bad $planned } 'malformed or non-edit change'
}
Write-Host 'PASS Diff: malformed paths, metadata, flags, object IDs and entries cannot masquerade as harmless trees.'
Write-Host 'Insertion diff self-tests passed: 6.'
