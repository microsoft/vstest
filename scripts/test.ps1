# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [Alias("c")]
    [System.String] $Configuration = "Debug",

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
# Folders to build. TODO move to props
Write-Verbose "Setup build configuration."
$Script:TPT_Configuration = $Configuration
$Script:TPT_SourceFolders =  @("test")
$Script:TPT_TargetFramework = "net46"
$Script:TPT_TargetRuntime = "win7-x64"
$Script:TPT_SkipProjects = @("Microsoft.TestPlatform.CoreUtilities.UnitTests")
$Script:TPT_Pattern = $Pattern
$Script:TPT_FailFast = $FailFast

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
        $vstestConsolePath = Join-Path (Get-PackageDirectory) "vstest.console.exe"
        if (!(Test-Path $vstestConsolePath)) {
            Write-Log "Unable to find vstest.console.exe at $vstestConsolePath. Did you run build.cmd?"
            Write-Error "Test aborted."
        }

        foreach ($fx in $Script:TPT_TargetFramework) {
            Get-ChildItem -Recurse -Path $src -Include "project.json" | ForEach-Object {
                Write-Log ".. Test: Source: $_"

                # Tests are only built for x86 at the moment, though we don't have architecture requirement
                $testAdapterPath = "$env:TP_PACKAGES_DIR\MSTest.TestAdapter\1.0.3-preview\build\_common"
                $testContainerName = $_.Directory.Name
                $testOutputPath = Join-Path $_.Directory.FullName "bin/$($Script:TPT_Configuration)/$($Script:TPT_TargetFramework)/win7-x86"
                $testContainerPath = Join-Path $testOutputPath "$($testContainerName).dll"

                if ($Script:TPT_SkipProjects.Contains($testContainerName)) {
                    Write-Log ".. . $testContainerName is in skipped test list."
                } elseif (!($testContainerName -match $Script:TPT_Pattern)) {
                    Write-Log ".. . $testContainerName doesn't match test container pattern '$($Script:TPT_Pattern)'. Skipped from run."
                } else {
                    Write-Verbose "vstest.console.exe $testContainerPath /testAdapterPath:$testAdapterPath"
                    $output = & $vstestConsolePath $testContainerPath /testAdapterPath:"$testAdapterPath"

                    #Write-Verbose "$dotnetExe test $_ --configuration $Configuration"
                    #& $dotnetExe test $_ --configuration $Configuration

                    if ($output[-2].Contains("Test Run Successful.")) {
                        Write-Log ".. . $($output[-3])"
                    } else {
                        Write-Log ".. . $($output[-2])"
                        Write-Log ".. . Failed tests:"
                        Write-Log ".. .  $($output -match '^Failed')"

                        if ($Script:TPT_FailFast) {
                            Write-Log ".. Stop execution since fail fast is enabled."
                            continue
                        }
                    }
                }

                Write-Log ".. Test: Complete."
            }
        }
        #Write-Verbose "$dotnetExe test $src\**\project.json --configuration $Configuration"
        #& $dotnetExe test $_ $src\**\project.json --configuration $Configuration
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

function Get-PackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$($Script:TPT_Configuration)\$($Script:TPT_TargetFramework)\$($Script:TPT_TargetRuntime)")
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

# Execute build
$timer = Start-Timer
Write-Log "Build started: args = '$args'"
Write-Log "Test platform environment variables: "
Get-ChildItem env: | Where-Object -FilterScript { $_.Name.StartsWith("TP_") } | Format-Table

Write-Log "Test run configuration: "
Get-Variable | Where-Object -FilterScript { $_.Name.StartsWith("TPT_") } | Format-Table

Invoke-Test

Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
