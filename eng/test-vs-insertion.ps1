# Run with: pwsh -NoProfile -File eng/test-vs-insertion.ps1
# Tests the pipeline's post-insertion script offline. No tools, tokens, or network access are needed.

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$pipelinePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'azure-pipelines-insertion.yml'
$pipelineLines = Get-Content -LiteralPath $pipelinePath
$scriptStart = [Array]::IndexOf($pipelineLines, '                  inlineScript: |')
if ($scriptStart -lt 0) {
    throw 'Could not find the insertion inline script.'
}

$scriptLines = foreach ($line in $pipelineLines[($scriptStart + 1)..($pipelineLines.Count - 1)]) {
    if ($line.Trim().Length -eq 0) {
        ''
    }
    elseif ($line.StartsWith('                    ')) {
        $line.Substring(20)
    }
    else {
        break
    }
}

$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput(
    ($scriptLines -join "`n"), [ref]$null, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) {
    throw "Insertion script has syntax errors: $($parseErrors -join '; ')"
}

# Start after roslyn-tools has run, so installation, authentication, and insertion never execute.
$headersAssignment = @($ast.EndBlock.Statements | Where-Object {
    $_ -is [System.Management.Automation.Language.AssignmentStatementAst] -and
    $_.Left.Extent.Text -eq '$headers'
})
if ($headersAssignment.Count -ne 1) {
    throw 'Expected exactly one post-insertion headers assignment.'
}

$postInsertionStatements = $ast.EndBlock.Statements | Where-Object {
    $_.Extent.StartOffset -ge $headersAssignment[0].Extent.StartOffset
}
$postInsertionScript = [scriptblock]::Create(($postInsertionStatements.Extent.Text -join "`n"))

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -cne $Actual) {
        throw "${Message}: expected '$Expected', got '$Actual'."
    }
}

function Test-InsertionOutput {
    param(
        [string[]]$OutputLines,
        [int]$ExpectedMatches = 1,
        [switch]$ConfigChanged,
        [switch]$ByteContent
    )

    $roslynOutput = $OutputLines
    $dncengToken = 'offline-test-token'
    $config = "<configuration>new</configuration>`n"
    $currentConfig = if ($ConfigChanged) { '<configuration>old</configuration>' } else { $config }
    $revision = "42`n00000000-0000-0000-0000-000000000001`n"
    $expectedSourceCommit = '1234567890123456789012345678901234567890'
    $latestCommit = 'abcdefabcdefabcdefabcdefabcdefabcdefabcd'
    $vsApi = 'https://dev.azure.com/devdiv/DevDiv/_apis/git/repositories/a290117c-5a8a-40f7-bc2c-f14dbe3acf6d'
    $mirrorApi = 'https://dnceng.visualstudio.com/internal/_apis'
    $appConfigPath = '/src/vset/Agile/TestPlatform/RocksteadyCLI/App.config'
    $revisionPath = '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/Shared/revision.txt'
    $state = @{
        Requests = [System.Collections.Generic.List[string]]::new()
        Pushes = [System.Collections.Generic.List[object]]::new()
        RevisionReads = 0
    }

    function Invoke-RestMethod {
        param($Uri, $Headers, $Method, $Body, $ContentType)

        $state.Requests.Add("$Method $Uri")
        switch ("$Method $Uri") {
            "Get $vsApi/pullrequests/735439?api-version=7.1" {
                return @{ sourceRefName = 'refs/heads/insertion/test'; targetRefName = 'refs/heads/main' }
            }
            "Get $vsApi/refs?filter=heads%2Finsertion%2Ftest&api-version=7.1" {
                return @{ value = @(@{ objectId = $latestCommit }) }
            }
            "Get $mirrorApi/build/builds?buildNumber=20260921.1&api-version=7.1" {
                return @{ value = @(@{ definition = @{ name = 'microsoft-vstest' }; sourceVersion = $expectedSourceCommit }) }
            }
            "Post $vsApi/pushes?api-version=7.1" {
                $state.Pushes.Add(([System.Text.Encoding]::UTF8.GetString($Body) | ConvertFrom-Json))
                return @{ commits = @(@{ commitId = 'offline-push' }) }
            }
            default { throw "Unexpected REST request: $Method $Uri" }
        }
    }

    function Invoke-WebRequest {
        param($Uri, $Headers, $Method, [switch]$UseBasicParsing)

        $state.Requests.Add("$Method $Uri")
        $content = switch ("$Method $Uri") {
            "Get $mirrorApi/git/repositories/microsoft-vstest/items?path=%2Fsrc%2Fvstest.console%2Fapp.config&versionDescriptor.version=$expectedSourceCommit&versionDescriptor.versionType=commit&api-version=7.1" {
                $config
            }
            "Get $vsApi/items?path=$([Uri]::EscapeDataString($appConfigPath))&versionDescriptor.version=insertion%2Ftest&versionDescriptor.versionType=branch&api-version=7.1" {
                $currentConfig
            }
            "Get $vsApi/items?path=$([Uri]::EscapeDataString($revisionPath))&versionDescriptor.version=main&versionDescriptor.versionType=branch&api-version=7.1" {
                $state.RevisionReads++
                $revision
            }
            default { throw "Unexpected web request: $Method $Uri" }
        }

        if ($ByteContent) {
            return @{ Content = [System.Text.Encoding]::UTF8.GetBytes($content) }
        }
        return @{ Content = $content }
    }

    $failure = $null
    try {
        & $postInsertionScript 6>$null
    }
    catch {
        $failure = $_
    }

    if ($ExpectedMatches -ne 1) {
        if ($null -eq $failure) {
            throw "Expected rejection of $ExpectedMatches insertion PR lines."
        }
        $expectedMessage = "Expected exactly one 'Insertion PR: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/<id>' line in roslyn-tools output, but found $ExpectedMatches. Cannot push App.config and revision.txt."
        Assert-Equal $expectedMessage $failure.Exception.Message 'Parser error'
        Assert-Equal 0 $state.Requests.Count 'Rejected output must not make HTTP requests'
        return
    }

    if ($null -ne $failure) {
        throw $failure
    }
    Assert-Equal "Get $vsApi/pullrequests/735439?api-version=7.1" $state.Requests[0] 'Selected insertion PR'
    if (-not $ConfigChanged) {
        Assert-Equal 0 $state.RevisionReads 'Unchanged App.config must not read revision.txt'
        Assert-Equal 0 $state.Pushes.Count 'Unchanged App.config must not push either file'
        return
    }

    Assert-Equal 1 $state.RevisionReads 'Changed App.config must read revision.txt'
    Assert-Equal 1 $state.Pushes.Count 'Changed App.config must make one push'
    $push = $state.Pushes[0]
    Assert-Equal 'refs/heads/insertion/test' $push.refUpdates[0].name 'Push branch'
    Assert-Equal $latestCommit $push.refUpdates[0].oldObjectId 'Push parent commit'
    Assert-Equal 1 $push.commits.Count 'Push commit count'
    Assert-Equal 2 $push.commits[0].changes.Count 'Push file count'
    Assert-Equal $appConfigPath $push.commits[0].changes[0].item.path 'App.config path'
    Assert-Equal $config $push.commits[0].changes[0].newContent.content 'App.config content'
    Assert-Equal $revisionPath $push.commits[0].changes[1].item.path 'revision.txt path'
    Assert-Equal ($revision -replace '^42', '43') $push.commits[0].changes[1].newContent.content 'Bumped revision with preserved GUID'
}

$insertionLine = 'Insertion PR: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/735439'
$buildLine = 'Found microsoft-vstest build number 20260921.1'
$changelogLines = @(
    'Updating VS Test Platform: [component PR](https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/111111)'
    'https://dev.azure.com/devdiv/DevDiv/_apis/git/repositories/component/pullrequests/222222'
)
$cases = @(
    @{ Name = 'Preceding changelog links do not select a component PR'; OutputLines = $changelogLines + @($buildLine, $insertionLine); ConfigChanged = $true }
    @{ Name = 'Unchanged App.config skips both files'; OutputLines = @($buildLine, $insertionLine) }
    @{ Name = 'Unchanged byte content skips both files'; OutputLines = @($buildLine, $insertionLine); ByteContent = $true }
    @{ Name = 'CRLF output accepts exactly one insertion line'; OutputLines = @(($changelogLines + @($buildLine, $insertionLine)) -join "`r`n"); ConfigChanged = $true; ByteContent = $true }
    @{ Name = 'Empty output fails'; OutputLines = @(); ExpectedMatches = 0 }
    @{ Name = 'Changelog links without insertion output fail'; OutputLines = $changelogLines; ExpectedMatches = 0 }
    @{ Name = 'API PR link without insertion output fails'; OutputLines = @($changelogLines[1]); ExpectedMatches = 0 }
    @{ Name = 'Another repository is not an insertion PR'; OutputLines = @($insertionLine.Replace('/_git/VS/', '/_git/component/')); ExpectedMatches = 0 }
    @{ Name = 'Embedded insertion text is not an explicit line'; OutputLines = @("Changelog: $insertionLine"); ExpectedMatches = 0 }
    @{ Name = 'Trailing URL text is not an insertion line'; OutputLines = @("$insertionLine/commits"); ExpectedMatches = 0 }
    @{ Name = 'Multiple insertion lines fail'; OutputLines = @($insertionLine, $insertionLine.Replace('735439', '735440')); ExpectedMatches = 2 }
    @{ Name = 'Repeated insertion lines fail even for the same PR'; OutputLines = @($insertionLine, $insertionLine); ExpectedMatches = 2 }
)

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($case in $cases) {
    $arguments = $case.Clone()
    $arguments.Remove('Name')
    try {
        Test-InsertionOutput @arguments
        Write-Host "PASS: $($case.Name)"
    }
    catch {
        $failures.Add("$($case.Name): $_")
        Write-Host "FAIL: $($case.Name): $_"
    }
}

if ($failures.Count -ne 0) {
    throw "$($failures.Count) of $($cases.Count) VS insertion tests failed.`n$($failures -join "`n")"
}
Write-Host "All $($cases.Count) VS insertion tests passed."
