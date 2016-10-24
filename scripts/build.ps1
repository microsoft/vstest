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

    [Parameter(Mandatory=$false)]
    [Alias("v")]
    [System.String] $Version = "15.0.0",

    [Parameter(Mandatory=$false)]
    [Alias("vs")]
    [System.String] $VersionSuffix = "dev",

    [Parameter(Mandatory=$false)]
    [Alias("ff")]
    [System.Boolean] $FailFast = $true
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
$env:TP_PACKAGE_PROJ_DIR = Join-Path $env:TP_ROOT_DIR "src\package"

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
$TPB_Version = $Version
$TPB_VersionSuffix = $VersionSuffix

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
        New-Item $env:TP_TOOLS_DIR -Type Directory | Out-Null
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
        Write-Verbose "$dotnetExe build $src\**\project.json --configuration $TPB_Configuration --runtime $TPB_TargetRuntime --version-suffix $TPB_VersionSuffix"
        & $dotnetExe build $_ $src\**\project.json --configuration $TPB_Configuration --runtime $TPB_TargetRuntime --version-suffix $TPB_VersionSuffix

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
    $testHostProjectDirectory = Join-Path $env:TP_ROOT_DIR "src\testhost"
    $testHostx86ProjectDirectory = Join-Path $env:TP_ROOT_DIR "src\testhost.x86"
    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework\$TPB_TargetRuntime")
    $testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore")
    $vstestConsoleProjectDirectory = Join-Path $env:TP_ROOT_DIR "src\vstest.console"
    $dataCollectorProjectDirectory = Join-Path $env:TP_ROOT_DIR "src\datacollector"
    $dataCollectorx86ProjectDirectory = Join-Path $env:TP_ROOT_DIR "src\datacollector.x86"

    Write-Log "Package: Publish package\project.json"
    Publish-Package-Internal $env:TP_PACKAGE_PROJ_DIR\project.json $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $env:TP_PACKAGE_PROJ_DIR\project.json $TPB_TargetFrameworkCore $coreCLRPackageDir

    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.
    Write-Log "Package: Publish src\vstest.console\project.json"
    Publish-Package-Internal $vstestConsoleProjectDirectory\project.json $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $vstestConsoleProjectDirectory\project.json $TPB_TargetFrameworkCore $coreCLRPackageDir

    Write-Log "Package: Publish src\datacollector\project.json"
    Publish-Package-Internal $dataCollectorProjectDirectory\project.json $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $dataCollectorProjectDirectory\project.json $TPB_TargetFrameworkCore $coreCLRPackageDir

    Write-Log "Package: Publish src\datacollector.x86\project.json"
    Publish-Package-Internal $dataCollectorx86ProjectDirectory\project.json $TPB_TargetFramework $fullCLRPackageDir

    # Publish testhost
    Write-Log "Package: Publish testhost\project.json"
    Publish-Package-Internal $testHostProjectDirectory\project.json $TPB_TargetFramework $testhostFullPackageDir
    Publish-Package-Internal $testHostProjectDirectory\project.json $TPB_TargetFrameworkCore $testhostCorePackageDir

    Write-Log "Package: Publish testhost.x86\project.json"
    Publish-Package-Internal $testHostx86ProjectDirectory\project.json $TPB_TargetFramework $testhostFullPackageDir

    # Copy over the Full CLR built testhost package assemblies to the $fullCLRPackageDir
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force

    # Copy over the Full CLR built testhost package assemblies to the Core CLR package folder.
    $netFull_Dir = "TestHostfx"
    $fullDestDir = Join-Path $coreCLRPackageDir $netFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    # Copy over the logger assemblies to the Extensions folder.
    $extensions_Dir = "Extensions"
    $fullCLRExtensionsDir = Join-Path $fullCLRPackageDir $extensions_Dir
    $coreCLRExtensionsDir = Join-Path $coreCLRPackageDir $extensions_Dir
    # Create an extensions directory.
    New-Item -ItemType directory -Path $fullCLRExtensionsDir -Force | Out-Null
    New-Item -ItemType directory -Path $coreCLRExtensionsDir -Force | Out-Null

    # Note Note: If there are some dependencies for the logger assemblies, those need to be moved too. 
    # Ideally we should just be publishing the loggers to the Extensions folder.
    $loggers = @("Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.pdb")
    foreach($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force

        Write-Verbose "Move-Item $coreCLRPackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLRPackageDir\$file $coreCLRExtensionsDir -Force
    }

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    Copy-PackageItems "Microsoft.TestPlatform.Build"

    Write-Log "Publish-Package: Complete. {$(Get-ElapsedTime($timer))}"
}


function Publish-Package-Internal($packagename, $framework, $output, $runtime)
{
    Write-Verbose "$dotnetExe publish $packagename --no-build --configuration $TPB_Configuration --framework $framework --output $output"
    & $dotnetExe publish $packagename --no-build --configuration $TPB_Configuration --framework $framework --output $output
}

function Create-VsixPackage
{
    $timer = Start-Timer

    Write-Log "Create-VsixPackage: Started."
    $packageDir = Get-FullCLRPackageDirectory

    # Copy vsix manifests
    $vsixManifests = @("*Content_Types*.xml",
        "extension.vsixmanifest",
        "TestPlatform.ObjectModel.manifest",
        "TestPlatform.ObjectModel.x86.manifest")
    foreach ($file in $vsixManifests) {
        Copy-Item $env:TP_PACKAGE_PROJ_DIR\$file $packageDir -Force
    }

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\15.0.0\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy COM Components and their manifests over
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force

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
    $tpSrcDir = Join-Path $env:TP_ROOT_DIR "src"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec", "TestPlatform.ObjectModel.nuspec", "TestPlatform.TestHost.nuspec", "TestPlatform.nuspec", "TestPlatform.CLI.nuspec", "TestPlatform.Build.nuspec", "Microsoft.Net.Test.Sdk.nuspec")
    $targetFiles = @("Microsoft.Net.Test.Sdk.targets")
    # Nuget pack analysis emits warnings if binaries are packaged as content. It is intentional for the below packages.
    $skipAnalysis = @("TestPlatform.CLI.nuspec")
    foreach ($file in $nuspecFiles + $targetFiles) {
        Copy-Item $tpSrcDir\$file $stagingDir -Force
    }

    # Copy over empty file
    Copy-Item -Recurse $tpSrcDir\package\"_._" $stagingDir -Force

    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir -Version=$Version-$VersionSuffix -Properties Version=$Version-$VersionSuffix $additionalArgs"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir -Version $Version-$VersionSuffix -Properties Version=$Version-$VersionSuffix $additionalArgs
    }

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-PackageItems($packageName)
{
    # Packages published separately are copied into their own artifacts directory
    # E.g. src\Microsoft.TestPlatform.ObjectModel\bin\Debug\net46\* is copied
    # to artifacts\Debug\Microsoft.TestPlatform.ObjectModel\net46
    $binariesDirectory = [System.IO.Path]::Combine("src", "$packageName", "bin", "$TPB_Configuration", "*.*")
    $publishDirectory = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$packageName")
    Write-Log "Copy-PackageItems: Package: $packageName"
    Write-Verbose "Create $publishDirectory"
    New-Item -ItemType directory -Path $publishDirectory -Force | Out-Null

    Write-Verbose "Copy binaries for package '$packageName' from '$binariesDirectory' to '$publishDirectory'"
    Copy-Item -Path $binariesDirectory -Destination $publishDirectory -Recurse -Force
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
    if ($FailFast -eq $true) {
        Write-Error "Build failed. Stopping as fail fast is set."
    }

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
