[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'AcceptanceTestResults.ps1')

function Assert-Rejected([scriptblock] $Action, [string] $Name) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    if (-not $rejected) { throw "Negative control was accepted: $Name" }
    Write-Host "Rejected: $Name"
}

$root = Split-Path $PSScriptRoot -Parent
$temporary = Join-Path $root "artifacts\tmp\acceptance-gate-$([Guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $temporary
$passed = @'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results><UnitTestResult outcome="Passed" /></Results>
  <TestDefinitions><UnitTest><TestMethod className="Microsoft.TestPlatform.AcceptanceTests.ExecutionTests" /></UnitTest></TestDefinitions>
  <ResultSummary><Counters total="1" executed="1" passed="1" failed="0" /></ResultSummary>
</TestRun>
'@
$runs = @(
    @{ TargetFramework = 'net451'; Results = Join-Path $temporary 'net451.trx'; ExitCode = 0 },
    @{ TargetFramework = 'netcoreapp2.1'; Results = Join-Path $temporary 'netcoreapp2.1.trx'; ExitCode = 0 }
)
try {
    $runs | ForEach-Object { Set-Content -LiteralPath $_.Results -Value $passed }
    Assert-NormalAcceptanceTestResults -Runs $runs
    Assert-Rejected { Assert-NormalAcceptanceTestResults -Runs @($runs[0]) } 'missing framework'
    Assert-Rejected { Assert-NormalAcceptanceTestResults -Runs @($runs[0], $runs[0]) } 'duplicate framework'
    $runs[1].ExitCode = 1
    Assert-Rejected { Assert-NormalAcceptanceTestResults -Runs $runs } 'runner failure with passing TRX'
    $runs[1].ExitCode = 0
    Remove-Item -LiteralPath $runs[1].Results
    Assert-Rejected { Assert-NormalAcceptanceTestResults -Runs $runs } 'missing TRX'
    foreach ($case in @(
        @{ Name = 'zero tests'; Xml = $passed.Replace('total="1"', 'total="0"') },
        @{ Name = 'no executed tests'; Xml = $passed.Replace('executed="1"', 'executed="0"') },
        @{ Name = 'failed test'; Xml = $passed.Replace('outcome="Passed"', 'outcome="Failed"') },
        @{ Name = 'failed data row'; Xml = $passed.Replace('<UnitTestResult outcome="Passed" />', '<UnitTestResult outcome="Passed"><InnerResults><UnitTestResult outcome="Failed" /></InnerResults></UnitTestResult>') },
        @{ Name = 'failure counter'; Xml = $passed.Replace('failed="0"', 'failed="1"') },
        @{ Name = 'wrong suite'; Xml = $passed.Replace('Microsoft.TestPlatform.AcceptanceTests.', 'UnrelatedTests.') },
        @{ Name = 'missing result rows'; Xml = $passed.Replace('<UnitTestResult outcome="Passed" />', '') },
        @{ Name = 'malformed TRX'; Xml = '<TestRun' }
    )) {
        Set-Content -LiteralPath $runs[1].Results -Value $case.Xml
        Assert-Rejected { Assert-NormalAcceptanceTestResults -Runs $runs } $case.Name
    }

    $runner = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Run-NormalAcceptanceTests.ps1') -Raw
    foreach ($required in @(
        "TargetFramework = 'net451'; Framework = '.NETFramework,Version=v4.5.1'",
        "TargetFramework = 'netcoreapp2.1'; Framework = '.NETCoreApp,Version=v2.1'",
        "'exec', `$LocalVSTestConsolePath, `$run.Assembly",
        "`$env:VSTEST_ACCEPTANCE_DOTNET_SDK = Join-Path `$patchedSdk 'dotnet.dll'",
        'TestCategory!=Compatibility&TestCategory!=TelemetryPerf&TestCategory!=Performance',
        'Assert-NormalAcceptanceTestResults -Runs $runs'
    )) {
        if (-not $runner.Contains($required)) { throw "Acceptance selection invariant missing: $required" }
    }
    $global = Get-Content -LiteralPath (Join-Path $root 'global.json') -Raw | ConvertFrom-Json
    if ($global.tools.runtimes.'dotnet/x64' -notcontains '5.0.0') {
        throw 'The existing net5.0 x64 acceptance rows require their pinned runtime.'
    }
    foreach ($asset in @('AppDomainGetAssembliesTestProject', 'EnvironmentVariablesTestProject')) {
        [xml] $project = Get-Content -LiteralPath (Join-Path $root "test\TestAssets\$asset\$asset.csproj") -Raw
        $sdk = @($project.Project.ItemGroup.PackageReference | Where-Object Include -EQ 'Microsoft.NET.Test.Sdk')
        if ($sdk.Count -ne 1 -or $sdk[0].Version -ne '$(NETTestSdkVersion)') {
            throw "$asset must use the locally built test SDK, not the project's default PackageVersion."
        }
    }
    $assetsSolution = Get-Content -LiteralPath (Join-Path $root 'test\TestAssets\TestAssets.sln') -Raw
    if (-not $assetsSolution.Contains('CUITTestProject\CUITTestProject.csproj')) {
        throw 'The CUIT acceptance prerequisite is not selected by TestAssets.sln.'
    }
    [xml] $discovery = Get-Content -LiteralPath (Join-Path $root 'test\TestAssets\DiscoveryTestProject\DiscoveryTestProject.csproj') -Raw
    $discoveryFrameworks = ($discovery.SelectSingleNode('//TargetFrameworks').InnerText -split ';')
    if ($discoveryFrameworks -notcontains 'netcoreapp2.1' -or $discoveryFrameworks -notcontains 'net451') {
        throw 'Discovery cancellation needs both acceptance asset frameworks.'
    }
    $frameworkAsset = Get-Content -LiteralPath (Join-Path $root 'test\TestAssets\MultitargetedNetFrameworkProject\UnitTest1.cs') -Raw
    foreach ($framework in @('NET451', 'NET452', 'NET46', 'NET461', 'NET462', 'NET47', 'NET471', 'NET472', 'NET48')) {
        if (-not [regex]::IsMatch($frameworkAsset, "(?m)^#if $framework`r?$")) {
            throw "The existing framework asset definition was removed: $framework"
        }
    }
    if (-not $frameworkAsset.Contains('Assert.Equal(expected, TargetFramework, ignoreCase: true)')) {
        throw 'The framework asset must validate the child-scoped Golden expectation.'
    }
    foreach ($legacy in @(
        @{ File = 'CUITTest.cs'; Method = 'CUITRunAllTests'; Reason = 'Legacy Coded UI test. Ignored by user request.' },
        @{ File = 'DifferentTestFrameworkSimpleTests.cs'; Method = 'CodedWebTestRunAllTests'; Reason = 'Legacy coded WebTest. Ignored by user request.' }
    )) {
        $source = Get-Content -LiteralPath (Join-Path $root "test\Microsoft.TestPlatform.AcceptanceTests\$($legacy.File)") -Raw
        $ignore = '[Ignore("' + $legacy.Reason + '")]'
        $methodPattern = [regex]::Escape($ignore) + '\s*(?:\[[^\r\n]+\]\s*)*public void ' + $legacy.Method + '\('
        if (-not [regex]::IsMatch($source, $methodPattern) -or ([regex]::Matches($source, '\[Ignore\(')).Count -ne 1) {
            throw "The legacy ignore must apply only to $($legacy.Method) with its explicit reason."
        }
    }
    $pipeline = Get-Content -LiteralPath (Join-Path $root 'azure-pipelines-official.yml') -Raw
    foreach ($required in @(
        '- pwsh: eng\Run-NormalAcceptanceTests.ps1',
        'name: NormalAcceptanceTests',
        'displayName: Publish normal acceptance TRX results',
        'failTaskOnMissingResultsFile: true',
        'failTaskOnFailedTests: true'
    )) {
        if (-not $pipeline.Contains($required)) { throw "The official pipeline omits acceptance wiring: $required" }
    }
    Write-Host 'Acceptance selection and fail-closed result checks passed.'
}
finally {
    foreach ($run in $runs) {
        if (Test-Path -LiteralPath $run.Results) { Remove-Item -LiteralPath $run.Results }
    }
    Remove-Item -LiteralPath $temporary
}
