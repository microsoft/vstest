[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot "..\artifacts\tmp\test-results-gate-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $root
$root = (Resolve-Path -LiteralPath $root).Path
$baseline = Join-Path $root 'baseline.json'
$result = Join-Path $root 'unit.trx'
$gate = Join-Path $PSScriptRoot 'Assert-KnownTestFailures.ps1'
$shell = (Get-Process -Id $PID).Path

function Assert-GateResult([string] $RequiredFile, [int] $ExpectedExit, [string] $ExpectedMessage) {
    $output = & $shell -NoProfile -File $gate -ResultsDirectory $root -BaselinePath $baseline -RequiredResultFiles $RequiredFile 2>&1
    if ($LASTEXITCODE -ne $ExpectedExit -or ($output -join "`n") -notlike "*$ExpectedMessage*") {
        throw "Gate result differed for '$RequiredFile': expected exit $ExpectedExit and '$ExpectedMessage'.`n$($output -join "`n")"
    }
}

try {
    '[]' | Set-Content -LiteralPath $baseline -Encoding UTF8
    @'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testId="1" testName="Passes" outcome="Passed" />
  </Results>
  <TestDefinitions>
    <UnitTest id="1" name="Passes"><TestMethod className="GateTests" name="Passes" /></UnitTest>
  </TestDefinitions>
  <ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" /></ResultSummary>
</TestRun>
'@ | Set-Content -LiteralPath $result -Encoding UTF8
    Assert-GateResult 'acceptance.trx' 1 'Required test results are missing'
    Assert-GateResult 'unit.trx' 0 'PASSED'

    @'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results />
  <ResultSummary outcome="Completed"><Counters total="0" executed="0" passed="0" /></ResultSummary>
</TestRun>
'@ | Set-Content -LiteralPath $result -Encoding UTF8
    Assert-GateResult 'unit.trx' 1 'ZERO results'

    Write-Host 'Required, missing and empty test-result checks passed.'
}
finally {
    Remove-Item -LiteralPath $result, $baseline -Force
    Remove-Item -LiteralPath $root
}

exit 0
