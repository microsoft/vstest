# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [Alias("c")]
    [System.String] $Configuration = "Debug",

    [Parameter(Mandatory=$false)]
    [ValidateSet("win7-x64", "win7-x86")]
    [Alias("r")]
    [System.String] $TargetRuntime = "win7-x64",

    # Only test sources matching the pattern are run.
    # Use End2End to run E2E tests. Or to run any one assembly tests, use the 
    # assembly name. E.g. test -p Microsoft.TestPlatform.CoreUtilities.UnitTests 
    [Parameter(Mandatory=$false)]
    [Alias("p")]
    [System.String] $Pattern = "Unit",

    # Stop test run on first failure
    [Parameter(Mandatory=$false)]
    [Alias("ff")]
    [Switch] $FailFast = $false,

    [Parameter(Mandatory=$false)]
    [Switch] $Parallel = $false
)

$ErrorActionPreference = "Stop"

#
# Variables
#
Write-Verbose "Setup environment variables."
$env:TP_ROOT_DIR = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName
$env:TP_TOOLS_DIR = Join-Path $env:TP_ROOT_DIR "tools"
$env:TP_PACKAGES_DIR = Join-Path $env:TP_ROOT_DIR "packages"
$env:TP_OUT_DIR = Join-Path $env:TP_ROOT_DIR "artifacts"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR

#
# Test configuration
#
$TPT_TargetFrameworkFullCLR = "net46"
$TPT_TargetFrameworkCore = "netcoreapp1.0"
Write-Verbose "Setup build configuration."
$Script:TPT_Configuration = $Configuration
$Script:TPT_SourceFolders =  @("test")
$Script:TPT_TargetFrameworks =@($TPT_TargetFrameworkCore, $TPT_TargetFrameworkFullCLR)
$Script:TPT_TargetRuntime = $TargetRuntime
$Script:TPT_Pattern = $Pattern
$Script:TPT_FailFast = $FailFast
$Script:TPT_Parallel = $Parallel
$Script:TPT_TestResultsDir = Join-Path $env:TP_ROOT_DIR "TestResults"
$Script:TPT_DefaultTrxFileName = "TrxLogResults.trx"
$Script:TPT_ErrorMsgColor = "Red"

#
# Capture error state in any step globally to modify return code
$Script:ScriptFailed = $false

function Write-Log ([string] $message, $messageColor = "Green")
{
    $currentColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $messageColor
    if ($message)
    {
        Write-Output "... $message"
    }
    $Host.UI.RawUI.ForegroundColor = $currentColor
}

function Write-VerboseLog([string] $message)
{
    Write-Verbose $message
}

function Print-FailedTests($TrxFilePath)
{
    if(![System.IO.File]::Exists($TrxFilePath)){
      Write-Log "TrxFile: $TrxFilePath doesn't exists"
      return
    }
    $xdoc = [xml] (get-content $TrxFilePath)
    $FailedTestIds = $xdoc.TestRun.Results.UnitTestResult |?{$_.GetAttribute("outcome") -eq "Failed"} | %{$_.testId}
    if ($FailedTestIds) {
        Write-Log (".. .. . " + ($xdoc.TestRun.TestDefinitions.UnitTest | ?{ $FailedTestIds.Contains($_.GetAttribute("id")) } | %{ "$($_.TestMethod.className).$($_.TestMethod.name)"})) $Script:TPT_ErrorMsgColor
    }
}

function Invoke-Test
{
    $timer = Start-Timer
    $dotNetPath = Get-DotNetPath

    Write-Log "Invoke-Test: Start test."

    foreach ($src in $Script:TPT_SourceFolders) {
        Write-Log ".. Test: Computing sources"
        Get-ChildItem -Recurse -Path $src -Include *.csproj | Where-Object { $_.FullName -inotmatch "TestAssets" } | ForEach-Object {
            $testContainerName = $_.Directory.Name
            $testOutputPath = Join-Path $_.Directory.FullName "bin/$($Script:TPT_Configuration)/{0}"
            $testContainerPath = Join-Path $testOutputPath "$($testContainerName).dll"
            
            if (!($testContainerName -match $Script:TPT_Pattern)) {
                 Write-Log ".. . $testContainerName doesn't match test container pattern '$($Script:TPT_Pattern)'. Skipped from run."
            } else {
                $testContainers += ,"$testContainerPath"
            }
        }

        # Invoke test for each project.json since we want a custom output
        # path.
        foreach ($fx in $Script:TPT_TargetFrameworks) {
            Write-Log ".. Start run ($fx)"

            # Tests are only built for x86 at the moment, though we don't have architecture requirement
            $testAdapterPath = "$env:TP_PACKAGES_DIR\MSTest.TestAdapter\1.1.6-preview\build\_common"
            $testArchitecture = ($Script:TPT_TargetRuntime).Split("-")[-1]

            if($fx -eq $TPT_TargetFrameworkCore)
            {
                $testFrameWork = ".NETCoreApp,Version=v1.0"
                $vstestConsoleFileName = "vstest.console.dll"
                $targetRunTime = ""
            } else{

                $testFrameWork = ".NETFramework,Version=v4.6"
                $vstestConsoleFileName = "vstest.console.exe"
                $targetRunTime = $Script:TPT_TargetRuntime
            }

            $vstestConsolePath = Join-Path (Get-PackageDirectory $fx $targetRuntime) $vstestConsoleFileName
            if (!(Test-Path $vstestConsolePath)) {
                Write-Log "Unable to find $vstestConsoleFileName at $vstestConsolePath. Did you run build.cmd?"
                Write-Error "Test aborted."
            }

            if ($TPT_Parallel) {
                # Fill in the framework in test containers
                $testContainerSet = $testContainers | % { [System.String]::Format($_, $fx) }
                $trxLogFileName  =  [System.String]::Format("Parallel_{0}_{1}", $fx, $Script:TPT_DefaultTrxFileName)
                Set-TestEnvironment
                if($fx -eq $TPT_TargetFrameworkFullCLR) {

                    Write-Verbose "$vstestConsolePath $testContainerSet /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:$testAdapterPath /parallel /logger:`"trx;LogFileName=$trxLogFileName`""
                    $output = & $vstestConsolePath $testContainerSet /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:"$testAdapterPath" /parallel /logger:"trx;LogFileName=$trxLogFileName"
                } else {

                    Write-Verbose "$dotNetPath $vstestConsolePath $testContainerSet /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:$testAdapterPath /parallel /logger:`"trx;LogFileName=$trxLogFileName`""
                    $output = & $dotNetPath $vstestConsolePath $testContainerSet /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:"$testAdapterPath" /parallel /logger:"trx;LogFileName=$trxLogFileName"
                }

                Reset-TestEnvironment

                if ($output[-2].Contains("Test Run Successful.")) {
                    Write-Log ".. . $($output[-3])"
                } else {
                    Write-Log ".. . $($output[-2])"
                    Write-Log ".. . Failed tests:" $Script:TPT_ErrorMsgColor
                    Print-FailedTests (Join-Path $Script:TPT_TestResultsDir $trxLogFileName)

                    Set-ScriptFailed

                    if ($Script:TPT_FailFast) {
                        Write-Log ".. Stop execution since fail fast is enabled."
                        continue
                    }
                }
            } else {
                $testContainers |  % {
                    # Fill in the framework in test containers
                    $testContainer = [System.String]::Format($_, $fx)
                    $trxLogFileName =  [System.String]::Format("{0}_{1}_{2}", ($(Get-ChildItem $testContainer).Name), $fx, $Script:TPT_DefaultTrxFileName)

                    Write-Log ".. Container: $testContainer"

                    Set-TestEnvironment
                    
                    if($fx -eq $TPT_TargetFrameworkFullCLR) {

                        Write-Verbose "$vstestConsolePath $testContainer /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:$testAdapterPath /logger:`"trx;LogFileName=$trxLogFileName`""
                        $output = & $vstestConsolePath $testContainer /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:"$testAdapterPath" /logger:"trx;LogFileName=$trxLogFileName"
                    } else {

                        Write-Verbose "$dotNetPath $vstestConsolePath $testContainer /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:$testAdapterPath /logger:`"trx;LogFileName=$trxLogFileName`""
                        $output = & $dotNetPath $vstestConsolePath $testContainer /platform:$testArchitecture /framework:$testFrameWork /testAdapterPath:"$testAdapterPath" /logger:"trx;LogFileName=$trxLogFileName"
                    }

                    Reset-TestEnvironment
                    if ($output[-2].Contains("Test Run Successful.")) {
                        Write-Log ".. . $($output[-3])"
                    } else {
                        Write-Log ".. . $($output[-2])"
                        Write-Log ".. . Failed tests:" $Script:TPT_ErrorMsgColor
                        Print-FailedTests (Join-Path $Script:TPT_TestResultsDir $trxLogFileName)

                        Set-ScriptFailed

                        if ($Script:TPT_FailFast) {
                            Write-Log ".. Stop execution since fail fast is enabled."
                            continue
                        }
                    }
                }
            }

            Write-Log ".. Test: Complete ($fx)."
        }
    }

    Write-Log "Invoke-Test: Complete. {$(Get-ElapsedTime($timer))}"
}

#
# Helper functions
#
function Get-DotNetPath
{
    $dotnetPath = Join-Path $env:TP_TOOLS_DIR "dotnet\dotnet.exe"
    if (-not (Test-Path $dotnetPath)) {
        Write-Error "Dotnet.exe not found at $dotnetPath. Did the dotnet cli installation succeed?"
    }

    return $dotnetPath
}

function Get-PackageDirectory($framework, $targetRuntime)
{
    return $(Join-Path $env:TP_OUT_DIR "$($Script:TPT_Configuration)\$($framework)\$($targetRuntime)")
}

function Start-Timer
{
    return [System.Diagnostics.Stopwatch]::StartNew()
}

function Get-ElapsedTime([System.Diagnostics.Stopwatch] $timer)
{
    $timer.Stop()
    return $timer.Elapsed
}

function Set-ScriptFailed
{
    $Script:ScriptFailed = $true
}

function Set-TestEnvironment
{
    $env:TPT_TargetFramework = $Script:TPT_TargetFramework
    $env:TPT_TargetRuntime = $Script:TPT_TargetRuntime
}

function Reset-TestEnvironment
{
    $env:TPT_TargetFramework = $null
    $env:TPT_TargetRuntime = $null
}

# Execute build
$timer = Start-Timer
Write-Log "Build started: args = '$args'"
Write-Log "Test platform environment variables: "
Get-ChildItem env: | Where-Object -FilterScript { $_.Name.StartsWith("TP_") } | Format-Table

Write-Log "Test run configuration: "
Get-Variable | Where-Object -FilterScript { $_.Name.StartsWith("TPT_") } | Format-Table

Invoke-Test

Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"

if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
