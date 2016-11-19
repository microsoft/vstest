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
    [Switch] $FailFast = $false
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
Write-Verbose "Setup build configuration."
$Script:TPT_Configuration = $Configuration
$Script:TPT_SourceFolders =  @("test")
$Script:TPT_TargetFrameworks =@("netcoreapp1.0","net46")
$Script:TPT_TargetRuntime = $TargetRuntime
$Script:TPT_SkipProjectsDotNet = @("testhost.UnitTests","Microsoft.TestPlatform.Utilities.UnitTests")
$Script:TPT_SkipProjects = @("vstest.console.UnitTests","datacollector.x86.UnitTests","testhost.UnitTests","Microsoft.TestPlatform.Utilities.UnitTests")
$Script:TPT_Pattern = $Pattern
$Script:TPT_FailFast = $FailFast
#
# Capture error state in any step globally to modify return code
$Script:ScriptFailed = $false

function Write-Log ([string] $message)
{
    $currentColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = "Green"
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

function Invoke-Test
{
    $timer = Start-Timer
    Write-Log "Invoke-Test: Start test."
    $dotnetExe = Get-DotNetPath

    foreach ($src in $Script:TPT_SourceFolders) {
        # Invoke test for each project.json since we want a custom output
        # path.
        foreach ($fx in $Script:TPT_TargetFrameworks) {
            Get-ChildItem -Recurse -Path $src -Include *.csproj | ForEach-Object {
                Write-Log ".. Test: Source: $_"

                # Tests are only built for x86 at the moment, though we don't have architecture requirement
                $testContainerName = $_.Name
                
                $skip="False"

                # Target framework for projects that should be skipped for testing.
                $targetFrameworkSkippedProjects=$Script:TPT_SkipProjects
                if($fx -eq "netcoreapp1.0")
                {
                    $targetFrameworkSkippedProjects=$Script:TPT_SkipProjectsDotNet
                }

                foreach ($project in $targetFrameworkSkippedProjects) {
                   if($_.Name.Contains($project))
                   {
                       $skip="True"
                       break
                   }
                }

                if ($skip -eq "True") {
                     Write-Log ".. . $testContainerName is in skipped test list."
                } elseif (!($testContainerName.Contains("Unit"))) {
                     Write-Log ".. . $testContainerName doesn't match test container pattern '$($Script:TPT_Pattern)'. Skipped from run."
                } else {
                    Set-TestEnvironment

                    Write-Log "$dotnetExe test $_ -f $fx"
                    $restore = & $dotnetExe restore $_
                    $output = & $dotnetExe test $_ -f $fx

                    Reset-TestEnvironment

                    if ($output[-4].Contains("Test Run Successful.")) {
                        Write-Log ".. . $($output[-3])"
                        Write-Log ".. . $($output[-5])"
                    } else {
                        Write-Log ".. . $($output[-2])"
                        Write-Log ".. . Failed tests:"
                        Write-Log ".. .  $($output -match '^Failed')"

                        Set-ScriptFailed

                        if ($Script:TPT_FailFast) {
                            Write-Log ".. Stop execution since fail fast is enabled."
                            continue
                        }
                    }

                }

                Write-Log ".. Test: Complete."
            }
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

Write-Log $Script:ScriptFailed

if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
