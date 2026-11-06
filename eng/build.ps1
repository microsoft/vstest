[CmdletBinding(PositionalBinding = $false)]
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
  [switch] $skipIntegrationTestBuild,
  [switch] $compatibilityTestBuild,
  [switch] $sign,
  [switch] $pack,
  # Satellite assemblies come from culture .resx that fmdev.xlftool writes out of the checked-in .xlf.
  # Pass this to skip that and build English-only, which is what the pre-Arcade script called
  # -DisableLocalizedBuild.
  [switch] $disableLocalizedBuild,
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
  [switch] $vs,
  [switch] $vscode,
  [Parameter(ValueFromRemainingArguments = $true)][String[]]$properties
)

if ($vs -or $vscode) {
  . $PSScriptRoot\common\tools.ps1

  # This tells .NET Core to use the bootstrapped runtime
  $env:DOTNET_ROOT = InitializeDotNetCli -install:$true -createSdkLocationFile:$true

  # This tells MSBuild to load the SDK from the directory of the bootstrapped SDK
  $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $env:DOTNET_ROOT

  # This tells .NET Core not to go looking for .NET Core in other places
  $env:DOTNET_MULTILEVEL_LOOKUP = 0

  # Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
  $env:PATH = ($env:DOTNET_ROOT + ";" + $env:PATH)

  # Disable .NET runtime signature validation errors which errors for local builds
  $env:VSDebugger_ValidateDotnetDebugLibSignatures = 0

  if ($vs) {
    # Launch Visual Studio with the locally defined environment variables
    & "$PSScriptRoot\..\TestPlatform.sln"
  }
  else {
    if (Get-Command code -ErrorAction Ignore) {
      & code "$PSScriptRoot\.."
    }
    elseif (Get-Command code-insiders -ErrorAction Ignore) {
      & code-insiders "$PSScriptRoot\.."
    }
    else {
      Write-Error "VS Code not found. Please install it from https://code.visualstudio.com/"
      return
    }
  }

  return
}

# Add steps that need to happen before build here

if ($properties -like "*TestRunnerAdditionalArguments*--filter*") {
  throw "Use --filter instead of passing filter as an additional argument to TestRunnerAdditionalArguments."
}

if (-not $PSBoundParameters.ContainsKey('properties')) {
  $PSBoundParameters['properties'] = @()
}
$PSBoundParameters['properties'] += "/p:PerformanceTest=$performanceTest"

if ($smokeTest -and $integrationTest) {
  throw "Cannot specify both smoke and integration tests. Smoke tests are a subset of integration tests, so specifying both is redundant and will run all integration tests."
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

if ([System.Environment]::OSVersion.Platform -notlike "Win*") {
  $filters += "TestCategory!=Windows&TestCategory!=Windows-Review"
}

# Skip linux only tests on any non-linux system, not just on Windows: the categorized tests use Linux-specific
# behavior (e.g. stat -c) that does not work on macOS either.
$runningOnLinux = $false
try {
  $runningOnLinux = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)
}
catch {
  $runningOnLinux = $false
}
if (-not $runningOnLinux) {
  $filters += "TestCategory!=Linux"
}

# Skip macOS only tests on any non-macOS system: the categorized tests use Darwin-specific behavior (confstr)
# that does not work on Windows or Linux. Added to the same accumulating $filters array as every other
# exclusion above, so it still ends up joined into the one --filter this script passes - see the throw at the
# top of this file for why a second, separate --filter is not an option.
$runningOnMacOS = $false
try {
  $runningOnMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
}
catch {
  $runningOnMacOS = $false
}
if (-not $runningOnMacOS) {
  $filters += "TestCategory!=macOS"
}

if ($smokeTest) {
  $filters += "TestCategory=Smoke"
}
else {
  # Don't exclude smoke tests if not specified explicitly, those are integration tests that should
  # run by default when running integration tests. We want to make sure we can run just Smoke tests,
  # but should not skip them when not specified.
  # $filters += "TestCategory!=Smoke"
}

if ($compatibilityTestBuild) {
  $testParameters['CompatibilityTestBuild'] = $true
}

if ($compatibilityTest) {
  if (-not $integrationTest) {
    # We specified just compatibility so run just compatibility tests. If there are both
    # we need to run all, but we don't have a filter for uncategorized tests, https://github.com/microsoft/testfx/issues/5136
    # so we simply don't provide an include filter.
    $filters += "TestCategory=Compatibility"
  }
}
else {
  $filters += "TestCategory!=Compatibility"
}

if ($performanceTest) {
  # We don't have any perf tests in the library.integrationtest.csproj, so providing this alone will fail
  # but we will use it together with compatibility tests in a nightly run, so good enough for now.
  if (-not $integrationTest) {
    # We specified just perf tests so run just perf tests. If there are both
    # we need to run all, but we don't have a filter for uncategorized tests, https://github.com/microsoft/testfx/issues/5136
    # so we simply don't provide an include filter.
    $filters += "FullyQualifiedName~Microsoft.TestPlatform.PerformanceTests"
  }
  
}
else {
  $filters += "TestCategory!=TelemetryPerf"
  $filters += "TestCategory!=Performance"
}

$null = $PSBoundParameters.Remove("filter")
$null = $PSBoundParameters.Remove("smokeTest")
$null = $PSBoundParameters.Remove("compatibilityTest")
$null = $PSBoundParameters.Remove("performanceTest")
$null = $PSBoundParameters.Remove("skipIntegrationTestBuild")
$null = $PSBoundParameters.Remove("compatibilityTestBuild")

if ($integrationTest -or $performanceTest -or $compatibilityTest -or $smokeTest) {
  # Rest of the infra knows nothing about or additional categories for tests. They simply consider them
  # integration tests, so mark that.
  $PSBoundParameters['integrationTest'] = $true
  # This is also non-default, normally we would run also unit tests, but if we filter anything that matches 0 tests in a project
  # the project will fail.
  $PSBoundParameters['test'] = $false
}

if ($performanceTest -and -not ($integrationTest -or $compatibilityTest -or $smokeTest)) {
  $PSBoundParameters['integrationTest'] = $false
  $PSBoundParameters['test'] = $true
  $PSBoundParameters['projects'] = "$PSScriptRoot\..\test\Microsoft.TestPlatform.PerformanceTests\Microsoft.TestPlatform.PerformanceTests.csproj"
}

if ($filters.Count -gt 0 -or $testParameters.Count -gt 0) {
  if ($filters.Count -gt 0) {
    
    # MSBuild decodes %22 after command-line parsing. The quotes must reach cmd.exe or '&'
    # in a compound filter becomes a command separator rather than part of the filter.
    $filterString = "--filter %22$($filters -join '&')%22"
    $filterParameters = "$filterString"
  }

  $testParameterString = if ($testParameters.Count -gt 0) {
    '-- ' + (($testParameters.GetEnumerator() | ForEach-Object {
      "TestRunParameters.Parameter(name=\`"$($_.Key)\`",value=\`"$($_.Value)\`")"
    }) -join ' ')
  }
  
  if (-not $PSBoundParameters.ContainsKey('properties')) {
    $PSBoundParameters['properties'] = @()
  }
  $PSBoundParameters['properties'] += "/p:TestRunnerAdditionalArguments=$filterParameters $testParameterString"
}

# Localization, phase 1. Has to sit between restore and build, and that is why this splits Arcade's
# single invocation in two.
#
# Satellites on this branch are not produced by the compiler from anything checked in. fmdev.xlftool
# converts the checked-in .xlf into culture .resx, and only then does a build embed them. The two
# cannot share one invocation: TestPlatform.Settings.targets sets EnableDefaultEmbeddedResourceItems
# to false, so the generated files are picked up solely by the ItemGroup in
# TestPlatform.Localization.targets, which is evaluated before any target of that same build runs.
# The pre-Arcade script had the same shape - scripts\build.ps1:1265 UpdateLocalization, then :1269 Build.
#
# Restore has to come first for a second reason: on a clean agent neither the SDK nor the converter
# exists until Arcade has run once.
#
# Scoped to -ci and -pack because it costs a couple of minutes and only packaged output needs
# satellites. Inner-loop builds and test runs stay English-only, as they were before this change.
$localize = ($build -or $rebuild) -and -not $disableLocalizedBuild -and ($ci -or $pack)
$null = $PSBoundParameters.Remove("disableLocalizedBuild")

if ($localize) {
  # LocalizedBuild and the generation step are set together, deliberately. Either one alone fails
  # silently: generation without the property leaves .resx on disk that nothing embeds, and the
  # property without generation embeds only the neutral .resx. Neither reports an error.
  if (-not $PSBoundParameters.ContainsKey('properties')) {
    $PSBoundParameters['properties'] = @()
  }
  $PSBoundParameters['properties'] += "/p:LocalizedBuild=true"

  # Step 1 - restore only. Bootstraps the SDK into .dotnet and fetches fmdev.xlftool via the
  # PackageDownload on the product projects.
  $restoreParameters = @{}
  foreach ($key in $PSBoundParameters.Keys) { $restoreParameters[$key] = $PSBoundParameters[$key] }
  $restoreParameters['restore'] = $true
  foreach ($skip in 'build', 'rebuild', 'test', 'integrationTest', 'pack', 'sign', 'publish', 'deploy', 'deployDeps', 'clean') {
    $null = $restoreParameters.Remove($skip)
  }

  & $PSScriptRoot/common/build.ps1 @restoreParameters
  if ($LastExitCode -ne 0) { exit $LastExitCode }

  # Step 2 - generate the culture .resx.
  #
  # Explicitly the SDK Arcade just bootstrapped, never a bare 'dotnet'. A machine-wide SDK on the
  # agent would be a different compiler writing these files, and this branch has already traded
  # compiler fidelity once.
  $repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
  $dotnetExe = if ($env:DOTNET_ROOT) { Join-Path $env:DOTNET_ROOT 'dotnet.exe' } else { Join-Path $repoRoot '.dotnet\dotnet.exe' }
  if (-not (Test-Path $dotnetExe)) {
    $dotnetExe = Join-Path $repoRoot '.dotnet\dotnet.exe'
  }
  if (-not (Test-Path $dotnetExe)) {
    Write-Error "Localization needs the SDK that restore bootstrapped, and it is not at '$dotnetExe'."
    exit 1
  }

  Write-Host "Localization phase 1: generating culture .resx using $dotnetExe"
  & $dotnetExe --version
  & $dotnetExe msbuild (Join-Path $repoRoot 'src\package\localize\localize.proj') -t:LocalizeResxOnly -m -nologo -v:minimal -nodeReuse:False
  if ($LastExitCode -ne 0) {
    Write-Error "Localization phase 1 failed."
    exit $LastExitCode
  }

  # Step 3 - build, without restoring a second time. Falls through to the call below.
  $PSBoundParameters['restore'] = $false
}

# Call the build script provided by Arcade
& $PSScriptRoot/common/build.ps1 @PSBoundParameters

# Forward exit code of the parent script
exit $LastExitCode
