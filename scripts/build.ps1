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
    [System.String] $TargetRuntime = "win7-x64"
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
$env:TP_SRC_DIR = Join-Path $env:TP_ROOT_DIR "src"
$env:TP_PACKAGE_PROJ_DIR = Join-Path $env:TP_ROOT_DIR "src\package"
$env:NETCORE_DIR = "NetCore"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR
$env:NUGET_EXE_Version = "3.4.3"

#
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_SourceFolders = @("src", "test")
$TPB_TargetFramework = "net46"
$TPB_TargetFrameworkCore = "netcoreapp1.0"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime

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

function Remove-Tools
{
}

function Install-DotNetCli
{
    $timer = Start-Timer

    Write-Log "Install-DotNetCli: Get dotnet-install.ps1 script..."
    $dotnetInstallRemoteScript = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1"
    $dotnetInstallScript = Join-Path $env:TP_TOOLS_DIR "dotnet-install.ps1"
    if (-not (Test-Path $env:TP_TOOLS_DIR)) {
        New-Item $env:TP_TOOLS_DIR -Type Directory
    }

    (New-Object System.Net.WebClient).DownloadFile($dotnetInstallRemoteScript, $dotnetInstallScript)

    if (-not (Test-Path $dotnetInstallScript)) {
        Write-Error "Failed to download dotnet install script."
    }

    Unblock-File $dotnetInstallScript

    Write-Log "Install-DotNetCli: Get the latest dotnet cli toolset..."
    $dotnetInstallPath = Join-Path $env:TP_TOOLS_DIR "dotnet"
    & $dotnetInstallScript -InstallDir $dotnetInstallPath -NoPath

    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Restore-Package
{
    $timer = Start-Timer
    Write-Log "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    $dotnetExe = Get-DotNetPath

    foreach ($src in $TPB_SourceFolders) {
        Write-Log "Restore-Package: Restore for source directory: $src"
        & $dotnetExe restore $src --packages $env:TP_PACKAGES_DIR
    }

    Write-Log "Restore-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Invoke-Build
{
    $timer = Start-Timer
    Write-Log "Invoke-Build: Start build."
    $dotnetExe = Get-DotNetPath

    foreach ($src in $TPB_SourceFolders) {
        # Invoke build for each project.json since we want a custom output
        # path.
        Write-Log ".. Build: Source directory: $src"
        #foreach ($fx in $TPB_TargetFramework) {
            #Get-ChildItem -Recurse -Path $src -Include "project.json" | ForEach-Object {
                #Write-Log ".. .. Build: Source: $_"
                #$binPath = Join-Path $env:TP_OUT_DIR "$fx\$src\$($_.Directory.Name)\bin"
                #$objPath = Join-Path $env:TP_OUT_DIR "$fx\$src\$($_.Directory.Name)\obj"
                #Write-Verbose "$dotnetExe build $_ --output $binPath --build-base-path $objPath --framework $fx"
                #& $dotnetExe build $_ --output $binPath --build-base-path $objPath --framework $fx
                #Write-Log ".. .. Build: Complete."
            #}
        #}
        Write-Verbose "$dotnetExe build $src\**\project.json --configuration $TPB_Configuration --runtime $TPB_TargetRuntime"
        & $dotnetExe build $_ $src\**\project.json --configuration $TPB_Configuration --runtime $TPB_TargetRuntime

        if ($lastExitCode -ne 0) {
            Set-ScriptFailed
        }
    }

    Write-Log "Invoke-Build: Complete. {$(Get-ElapsedTime($timer))}"
}

function Publish-Package
{
    $timer = Start-Timer
    Write-Log "Publish-Package: Started."
    $dotnetExe = Get-DotNetPath
    $fullCLRPackageDir = Get-FullCLRPackageDirectory
    $coreCLRPackageDir = Get-CoreCLRPackageDirectory

    Write-Log ".. Package: Publish package\project.json"
    
    Write-Verbose "$dotnetExe publish $env:TP_PACKAGE_PROJ_DIR\project.json --runtime $TPB_TargetRuntime --framework $TPB_TargetFramework --no-build --configuration $TPB_Configuration --output $fullCLRPackageDir"
    & $dotnetExe publish $env:TP_PACKAGE_PROJ_DIR\project.json --runtime $TPB_TargetRuntime --framework $TPB_TargetFramework --no-build --configuration $TPB_Configuration --output $fullCLRPackageDir
    
    Write-Verbose "$dotnetExe publish $env:TP_PACKAGE_PROJ_DIR\project.json --framework $TPB_TargetFrameworkCore --no-build --configuration $TPB_Configuration --output $coreCLRPackageDir"
    & $dotnetExe publish $env:TP_PACKAGE_PROJ_DIR\project.json --framework $TPB_TargetFrameworkCore --no-build --configuration $TPB_Configuration --output $coreCLRPackageDir

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    # Copy over the Core CLR built assemblies to the Full CLR package folder.
    $coreDestDir = Join-Path $fullCLRPackageDir $env:NETCORE_DIR
    New-Item -ItemType directory -Path $coreDestDir -Force
    Copy-Item -Recurse $coreCLRPackageDir\* $coreDestDir -Force

    Write-Log "Publish-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-VsixPackage
{
    $timer = Start-Timer

    Write-Log "Create-VsixPackage: Started."
    $packageDir = Get-FullCLRPackageDirectory

    # Copy vsix manifests
    $vsixManifests = @("*Content_Types*.xml",
        "extension.vsixmanifest",
        "testhost.x86.exe.config",
        "testhost.exe.config")
    foreach ($file in $vsixManifests) {
        Copy-Item $env:TP_PACKAGE_PROJ_DIR\$file $packageDir -Force
    }

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\15.0.0\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Zip the folder
    # TODO remove vsix creator
    & src\Microsoft.TestPlatform.VSIXCreator\bin\$TPB_Configuration\net461\Microsoft.TestPlatform.VSIXCreator.exe $packageDir $env:TP_OUT_DIR\$TPB_Configuration

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-NugetPackages
{
    $timer = Start-Timer

    Write-Log "Create-NugetPackages: Started."
    $stagingDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec", "TestPlatform.ObjectModel.nuspec")
    foreach ($file in $nuspecFiles) {
        Copy-Item $env:TP_SRC_DIR\$file $stagingDir -Force
    }

    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    foreach ($file in $nuspecFiles) {
        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir
    }

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
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

function Get-FullCLRPackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFramework\$TPB_TargetRuntime")
}

function Get-CoreCLRPackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore")
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

# Execute build
$timer = Start-Timer
Write-Log "Build started: args = '$args'"
Write-Log "Test platform environment variables: "
Get-ChildItem env: | Where-Object -FilterScript { $_.Name.StartsWith("TP_") } | Format-Table

Write-Log "Test platform build variables: "
Get-Variable | Where-Object -FilterScript { $_.Name.StartsWith("TPB_") } | Format-Table

Install-DotNetCli
Restore-Package
Invoke-Build
Publish-Package
Create-VsixPackage
Create-NugetPackages

Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"

if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
