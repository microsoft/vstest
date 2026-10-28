[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$packageDirectory = Join-Path $repoRoot "artifacts\packages\$Configuration\Shipping"
$packages = @(Get-ChildItem -LiteralPath $packageDirectory -Filter 'Microsoft.TestPlatform.CLI.*.nupkg' -File |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' })
if ($packages.Count -ne 1) {
    throw "Expected one Microsoft.TestPlatform.CLI package in $packageDirectory; found $($packages.Count)."
}

$framework = 'netcoreapp3.1'
$binLayout = Join-Path $repoRoot "artifacts\bin\vstest.console\$Configuration\$framework"
foreach ($file in @('vstest.console.dll', 'vstest.console.deps.json', 'vstest.console.runtimeconfig.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $binLayout $file) -PathType Leaf)) {
        throw "Missing console build output: $binLayout/$file."
    }
}

$destination = Join-Path $repoRoot "artifacts\tmp\$Configuration\localConsole"
if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($packages[0].FullName, $destination)
$consoleDirectory = Join-Path $destination "contentFiles\any\$framework"
$console = Join-Path $consoleDirectory 'vstest.console.dll'
if (-not (Test-Path -LiteralPath $console -PathType Leaf)) {
    throw "The CLI package does not contain $console."
}

# The package supplies Extensions and TestHostNetFramework; the build output supplies
# the runtime-specific dependencies referenced by vstest.console.deps.json.
Copy-Item -Path (Join-Path $binLayout '*') -Destination $consoleDirectory -Recurse -Force
foreach ($logger in @('Trx', 'Html')) {
    $loggerPath = Join-Path $consoleDirectory "Extensions\Microsoft.VisualStudio.TestPlatform.Extensions.$logger.TestLogger.dll"
    if (-not (Test-Path -LiteralPath $loggerPath -PathType Leaf)) {
        throw "The local console is missing its $logger logger: $loggerPath."
    }
}

$dotnetName = if ($env:OS -eq 'Windows_NT') { 'dotnet.exe' } else { 'dotnet' }
$dotnet = Join-Path (Join-Path $repoRoot '.dotnet') $dotnetName
if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw "The repository's dotnet host was not found: $dotnet."
}

# Exercise the same dotnet-test DLL entry point as Arcade, not just console startup.
$env:DOTNET_ROOT = Join-Path $repoRoot '.dotnet'
$env:VSTEST_CONSOLE_PATH = $console
$testAssembly = Join-Path $repoRoot "artifacts\bin\TranslationLayer.UnitTests\$Configuration\net6.0\TranslationLayer.UnitTests.dll"
$resultsDirectory = Join-Path $repoRoot "artifacts\TestResults\$Configuration\LocalConsoleSmoke"
$resultFile = Join-Path $resultsDirectory 'local-console.trx'
if (Test-Path -LiteralPath $resultFile) {
    Remove-Item -LiteralPath $resultFile -Force
}
$testClass = 'Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests'
$filter = "FullyQualifiedName=$testClass.VsTestConsoleRequestSenderTests.InitializeCommunicationShouldNotHandshakeWhenClientIsNotAccepted|FullyQualifiedName=$testClass.VsTestConsoleWrapperTests.StartSessionAsyncShouldListenThenLaunchThenWaitForConnection"
$diagnosticLog = Join-Path $resultsDirectory 'local-console.log'
& $dotnet test $testAssembly --framework net6.0 --filter $filter --diag $diagnosticLog --logger 'trx;LogFileName=local-console.trx' --results-directory $resultsDirectory
if ($LASTEXITCODE -ne 0) {
    throw "The locally built console failed its translation-layer smoke tests (exit $LASTEXITCODE)."
}
if (-not (Test-Path -LiteralPath $resultFile -PathType Leaf)) {
    throw "The local console did not produce $resultFile."
}
[xml] $results = Get-Content -LiteralPath $resultFile -Raw
$counters = $results.TestRun.ResultSummary.Counters
if ($counters.total -ne '2' -or $counters.executed -ne '2' -or $counters.passed -ne '2' -or
    $counters.failed -ne '0' -or $results.TestRun.ResultSummary.outcome -ne 'Completed') {
    throw "Expected both translation-layer smoke tests to pass. Inspect $resultFile."
}

Write-Host "package : $($packages[0].Name)"
Write-Host "console : $console"
Write-Host "##vso[task.setvariable variable=_LocalVSTestConsolePath]$console"
