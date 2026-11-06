$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$root = Join-Path ([IO.Path]::GetTempPath()) ('vstest-skip-gate-' + [Guid]::NewGuid().ToString('N'))
$gate = Join-Path $PSScriptRoot 'Assert-KnownTestFailures.ps1'
$baselinePath = Join-Path $root 'baseline.json'
$resultPath = Join-Path $root 'result.trx'

function Invoke-SkipCase([string] $DisplayName, [string] $Reason, [string] $Outcome, [int] $ExpectedExit, [switch] $RequirePassingTests) {
    $name = [Security.SecurityElement]::Escape($DisplayName)
    $message = [Security.SecurityElement]::Escape($Reason)
    $xml = @"
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions><UnitTest id="test-id"><TestMethod className="Fixture" name="Method" /></UnitTest></TestDefinitions>
  <Results><UnitTestResult testId="test-id" testName="$name" outcome="$Outcome"><Output><ErrorInfo><Message>$message</Message></ErrorInfo></Output></UnitTestResult></Results>
  <ResultSummary><Counters total="1" executed="1" /></ResultSummary>
</TestRun>
"@
    Set-Content -LiteralPath $resultPath -Value $xml -Encoding UTF8
    $extra = @()
    if ($RequirePassingTests) { $extra += '-RequirePassingTests' }
    & powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File $gate -ResultsDirectory $root -BaselinePath $baselinePath @extra *> (Join-Path $root 'gate.log')
    if ($LASTEXITCODE -ne $ExpectedExit) {
        Get-Content (Join-Path $root 'gate.log')
        throw "Skip gate returned $LASTEXITCODE, expected $ExpectedExit for $Outcome / $DisplayName / $Reason."
    }
}

try {
    New-Item -ItemType Directory -Path $root | Out-Null
    $entry = [ordered]@{
        test = 'Fixture.Method'
        outcome = 'NotExecuted'
        signature = ''
        displayName = 'Method (Core)'
        skipReason = 'Unsupported runner'
        cause = 'Synthetic source-declared unsupported combination.'
        classifiedOnRun = 'self-test'
        classifiedOn = 'self-test'
    }
    ConvertTo-Json -InputObject @($entry) | Set-Content -LiteralPath $baselinePath -Encoding UTF8
    Invoke-SkipCase 'Method (Core)' 'Unsupported runner' 'NotExecuted' 0
    Invoke-SkipCase 'Method (Framework)' 'Unsupported runner' 'NotExecuted' 1
    Invoke-SkipCase 'Method (Core)' 'Class initialization failed' 'NotExecuted' 1
    Invoke-SkipCase 'Method (Core)' 'Unsupported runner' 'Failed' 1
    Invoke-SkipCase 'Method (Core)' '' 'NotExecuted' 1

    $entry.Remove('displayName')
    $entry.Remove('skipReason')
    ConvertTo-Json -InputObject @($entry) | Set-Content -LiteralPath $baselinePath -Encoding UTF8
    Invoke-SkipCase 'Method (Core)' 'Existing unit-test behavior' 'NotExecuted' 0
    $entry.outcome = 'Failed'
    $entry.signature = 'Known failure'
    ConvertTo-Json -InputObject @($entry) | Set-Content -LiteralPath $baselinePath -Encoding UTF8
    Invoke-SkipCase 'Method (Core)' 'Known failure' 'Failed' 0
    Invoke-SkipCase 'Method (Core)' 'Known failure' 'Failed' 1 -RequirePassingTests
    $strictOutput = Get-Content (Join-Path $root 'gate.log') -Raw
    if ($strictOutput -notmatch '1 blocking test condition\(s\)' -or
        $strictOutput -notmatch '1 baselined failure\(s\) rejected by RequirePassingTests' -or
        $strictOutput -match '1 unclassified test result\(s\)') {
        throw "Strict gate diagnostics misclassified the baselined failure:`n$strictOutput"
    }
    Write-Host 'Skip gate self-test passed: exact skip criteria and strict rejection of baselined failures.'
}
finally {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
