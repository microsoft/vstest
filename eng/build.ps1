[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('c')]$configuration = "Debug",
  [string]$platform = $null,
  [string] $projects,
  [string][Alias('v')]$verbosity = "minimal",
  [string] $msbuildEngine = $null,
  [bool] $warnAsError = $true,
  [bool] $nodeReuse = $true,
  [switch] $buildCheck = $false,
  [switch][Alias('r')]$restore,
  [switch] $deployDeps,
  [switch][Alias('b')]$build,
  [switch] $rebuild,
  [switch] $deploy,
  [switch][Alias('t')]$test,
  [string] $filter,
  [switch] $smokeTest,
  [switch] $integrationTest,
  [switch] $performanceTest,
  [switch] $compatibilityTest,
  # Skip the build when running multiple categories from the integration tests. This is useful mostly in CI where we want to split the runs to different jobs, but
  # they all fall back to the same project and initialization.
  [switch] $skipIngegrationTestBuild,
  [switch] $sign,
  [switch] $pack,
  [switch] $publish,
  [switch] $clean,
  [switch][Alias('pb')]$productBuild,
  [switch]$fromVMR,
  [switch][Alias('bl')]$binaryLog,
  [switch][Alias('nobl')]$excludeCIBinarylog,
  [switch] $ci,
  [switch] $prepareMachine,
  [string] $runtimeSourceFeed = '',
  [string] $runtimeSourceFeedKey = '',
  [switch] $excludePrereleaseVS,
  [switch] $nativeToolsOnMachine,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

# Add steps that need to happen before build here

if ($properties -like "*TestRunnerAdditionalArguments*--filter*") {
  throw "Use --filter instead of passing filter as an additional argument to TestRunnerAdditionalArguments."
}

$filters = @()
# This translates to properties on test context, the only way MSTest allows us to pass info dynamically to AssemblyInitialize
$testParameters = @{}
if ($skipIntegrationTestBuild) {
  $testParameters['SkipIntegrationTestBuild'] = $true
}

if ($filter) {
  $filters += $filter
}

if ($smokeTest -and $integrationTest) {
  throw "Cannot specify both smoke and integration tests. Smoke tests are a subset of integration tests, so specifying both is redundant and will run all integration tests."
}

if ($compatibilityTest -and $integrationTest) {
  throw "Cannot specify both compatibility and integration tests. Compatibility tests additional tests on top of integration tests, you probably don't want to run both at the same time."
}

if ($performanceTest -and $integrationTest) {
  throw "Cannot specify both performance and integration tests. Performance tests additional tests on top of integration tests, you probably don't want to run both at the same time."
}

if ($smokeTest) {
  $filters += "TestCategory=Smoke"
}
else {
  $filters += "TestCategory!=Smoke"
}

if ($compatibilityTest) {
  $testParameters['BuildCompatibility'] = $true
  $filters += "TestCategory=Compatibility"
}
else {
  $filters += "TestCategory!=Compatibility"
}

if ($performanceTest) {
  $filters += "TestCategory=TelemetryPerf"
}
else {
  $filters += "TestCategory!=TelemetryPerf"
}

$null = $PSBoundParameters.Remove("filter")
$null = $PSBoundParameters.Remove("smokeTest")
$null = $PSBoundParameters.Remove("compatibilityTest")
$null = $PSBoundParameters.Remove("performanceTest")
$null = $PSBoundParameters.Remove("skipIntegrationTestBuild")

if ($integrationTest -or $performanceTest -or $compatibilityTest -or $smokeTest) {
  $PSBoundParameters['integrationTest'] = $true
  $PSBoundParameters['test'] = $false
}

if ($filters.Count -gt 0 -or $testParameters.Count -gt 0) {
  # We have to double escape, otherwise the filter is passed as string with & in it and interpreted directly as a separate comand to run.
  $filterString = "--filter \`"$($filters -join '&')\`""
  $testParameterString = ($testParameters.GetEnumerator() | ForEach-Object { "--test-parameter $($_.Key)=$($_.Value)" }) -join ' '
  
  $PSBoundParameters['properties'] += "/p:TestRunnerAdditionalArguments=$filterString $testParameterString"
}

Write-Host ($PSBoundParameters | fl -force  * | out-string)
# Call the build script provided by Arcade
& $PSScriptRoot/common/build.ps1 @PSBoundParameters

# Forward exit code of the parent script
exit $LastExitCode
