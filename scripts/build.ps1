# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [Alias("c")]
    [System.String] $Configuration = "Debug",

    [Parameter(Mandatory=$false)]
    [Alias("r")]
    [System.String] $TargetRuntime = "win7-x64",

    # Versioning scheme = Major(15).Minor(RTW, Updates).SubUpdates(preview4, preview5, RC etc)
    # E.g. VS 2017 Update 1 Preview will have version 15.1.1
    [Parameter(Mandatory=$false)]
    [Alias("v")]
    [System.String] $Version = "15.1.0",

    [Parameter(Mandatory=$false)]
    [Alias("vs")]
    [System.String] $VersionSuffix = "dev",

    [Parameter(Mandatory=$false)]
    [Alias("ff")]
    [System.Boolean] $FailFast = $true,

    [Parameter(Mandatory=$false)]
    [Alias("noloc")]
    [Switch] $DisableLocalizedBuild = $false,

    [Parameter(Mandatory=$false)]
    [Alias("ci")]
    [Switch] $CIBuild = $false,

    # Build specific projects
    [Parameter(Mandatory=$false)]
    [Alias("p")]
    [System.String[]] $ProjectNamePatterns = @()
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
$env:TP_SRC_DIR = Join-Path $env:TP_ROOT_DIR "src"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR
$env:NUGET_EXE_Version = "3.4.3"
$env:DOTNET_CLI_VERSION = "latest"
$env:LOCATE_VS_API_VERSION = "0.2.4-beta"
$env:MSBUILD_VERSION = "15.0"

#
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_Solution = "TestPlatform.sln"
$TPB_TargetFramework = "net46"
$TPB_TargetFrameworkCore = "netcoreapp1.0"
$TPB_TargetFrameworkCore20 = "netcoreapp2.0"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime
# Version suffix is empty for RTM releases
$TPB_Version = if ($VersionSuffix -ne '') { $Version + "-" + $VersionSuffix } else { $Version }
$TPB_CIBuild = $CIBuild
$TPB_LocalizedBuild = !$DisableLocalizedBuild
$TPB_VSIX_DIR = Join-Path $env:TP_ROOT_DIR "src\package\VSIXProject"

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
    $dotnetInstallRemoteScript = "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1"
    $dotnetInstallScript = Join-Path $env:TP_TOOLS_DIR "dotnet-install.ps1"
    if (-not (Test-Path $env:TP_TOOLS_DIR)) {
        New-Item $env:TP_TOOLS_DIR -Type Directory | Out-Null
    }

    $dotnet_dir= Join-Path $env:TP_TOOLS_DIR "dotnet"

    if (-not (Test-Path $dotnet_dir)) {
        New-Item $dotnet_dir -Type Directory | Out-Null
    }

    (New-Object System.Net.WebClient).DownloadFile($dotnetInstallRemoteScript, $dotnetInstallScript)

    if (-not (Test-Path $dotnetInstallScript)) {
        Write-Error "Failed to download dotnet install script."
    }

    Unblock-File $dotnetInstallScript

    Write-Log "Install-DotNetCli: Get the latest dotnet cli toolset..."
    $dotnetInstallPath = Join-Path $env:TP_TOOLS_DIR "dotnet"
    New-Item -ItemType directory -Path $dotnetInstallPath -Force | Out-Null
    & $dotnetInstallScript -Channel "master" -InstallDir $dotnetInstallPath -NoPath -Version $env:DOTNET_CLI_VERSION

    # Uncomment to pull in additional shared frameworks.
    # This is added to get netcoreapp1.1 shared components.
    & $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version '1.1.0' -Channel 'release/1.1.0'
    
    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Restore-Package
{
    $timer = Start-Timer
    Write-Log "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Restore-Package: Source: $TPB_Solution"
    & $dotnetExe restore $TPB_Solution --packages $env:TP_PACKAGES_DIR -v:minimal -warnaserror
    Write-Log ".. .. Restore-Package: Source: $env:TP_ROOT_DIR\src\package\external\external.csproj"
    & $dotnetExe restore $env:TP_ROOT_DIR\src\package\external\external.csproj --packages $env:TP_PACKAGES_DIR -v:minimal -warnaserror
    Write-Log ".. .. Restore-Package: Complete."

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    Write-Log "Restore-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Invoke-Build
{
    $timer = Start-Timer
    Write-Log "Invoke-Build: Start build."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Build: Source: $TPB_Solution"
    Write-Verbose "$dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
    Write-Log ".. .. Build: Complete."

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
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
    $coreCLR20PackageDir = Get-CoreCLR20PackageDirectory
    $packageProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "package\package.csproj"
    $testHostProject = Join-Path $env:TP_ROOT_DIR "src\testhost\testhost.csproj"
    $testHostx86Project = Join-Path $env:TP_ROOT_DIR "src\testhost.x86\testhost.x86.csproj"
    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework\$TPB_TargetRuntime")
    $testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore")
    $vstestConsoleProject = Join-Path $env:TP_ROOT_DIR "src\vstest.console\vstest.console.csproj"
    $dataCollectorProject = Join-Path $env:TP_ROOT_DIR "src\datacollector\datacollector.csproj"

    Write-Log "Package: Publish src\package\package\package.csproj"


    Publish-PackageInternal $packageProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-PackageInternal $packageProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.
    
    Write-Log "Package: Publish src\vstest.console\vstest.console.csproj"
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    Write-Log "Package: Publish src\datacollector\datacollector.csproj"
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework $fullCLRPackageDir
    #Publish-PackageInternal $dataCollectorProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    # Publish testhost
    
    Write-Log "Package: Publish testhost\testhost.csproj"
    Publish-PackageInternal $testHostProject $TPB_TargetFramework $testhostFullPackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore $testhostCorePackageDir

    Write-Log "Package: Publish testhost.x86\testhost.x86.csproj"
    Publish-PackageInternal $testHostx86Project $TPB_TargetFramework $testhostFullPackageDir

    # Copy over the Full CLR built testhost package assemblies to the $fullCLRPackageDir
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force

    # Copy over the Full CLR built testhost package assemblies to the Core CLR package folder.
    $netFull_Dir = "TestHost"
    $fullDestDir = Join-Path $coreCLR20PackageDir $netFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    # Publish platform abstractions
    $platformAbstraction = Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.PlatformAbstractions\bin\$TPB_Configuration"
    $platformAbstractionNet46 = Join-Path $platformAbstraction $TPB_TargetFramework
    $platformAbstractionNetCore = Join-Path $platformAbstraction $TPB_TargetFrameworkCore
    Copy-Item $platformAbstractionNet46\* $fullCLRPackageDir -Force
    Copy-Item $platformAbstractionNetCore\* $coreCLR20PackageDir -Force
    
    # Copy over the logger assemblies to the Extensions folder.
    $extensions_Dir = "Extensions"
    $fullCLRExtensionsDir = Join-Path $fullCLRPackageDir $extensions_Dir
    $coreCLRExtensionsDir = Join-Path $coreCLR20PackageDir $extensions_Dir
    # Create an extensions directory.
    New-Item -ItemType directory -Path $fullCLRExtensionsDir -Force | Out-Null
    New-Item -ItemType directory -Path $coreCLRExtensionsDir -Force | Out-Null

    # Note Note: If there are some dependencies for the logger assemblies, those need to be moved too. 
    # Ideally we should just be publishing the loggers to the Extensions folder.
    $loggers = @("Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.pdb")
    foreach($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    Copy-PackageItems "Microsoft.TestPlatform.Build"

    Write-Log "Publish-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Publish-PackageInternal($packagename, $framework, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:LocalizedBuild=$TPB_LocalizedBuild
}

function Create-VsixPackage
{
    Write-Log "Create-VsixPackage: Started."
    $timer = Start-Timer

    $packageDir = Get-FullCLRPackageDirectory

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\15.0.0\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy COM Components and their manifests over
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force

    $fileToCopy = Join-Path $env:TP_PACKAGE_PROJ_DIR "ThirdPartyNotices.txt"
    Copy-Item $fileToCopy $packageDir -Force

    Write-Verbose "Locating MSBuild install path..."
    $msbuildPath = Locate-MSBuildPath
    
    # Create vsix only when msbuild is installed.
    if(![string]::IsNullOrEmpty($msbuildPath))
    {
        # Update version of VSIX
        Update-VsixVersion

        # Build vsix project to get TestPlatform.vsix
        Write-Verbose "$msbuildPath\msbuild.exe $TPB_VSIX_DIR\TestPlatform.csproj -p:Configuration=$Configuration"
        & $msbuildPath\msbuild.exe "$TPB_VSIX_DIR\TestPlatform.csproj" -p:Configuration=$Configuration
    }
    else
    {
        Write-Log ".. Create-VsixPackage: Cannot generate vsix as msbuild.exe not found"
    }

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-NugetPackages
{
    $timer = Start-Timer

    Write-Log "Create-NugetPackages: Started."
    $stagingDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration
    $packageOutputDir = (Join-Path $env:TP_OUT_DIR $TPB_Configuration\packages )
    New-Item $packageOutputDir -type directory -Force
    $tpNuspecDir = Join-Path $env:TP_PACKAGE_PROJ_DIR "nuspec"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec", "TestPlatform.ObjectModel.nuspec", "TestPlatform.TestHost.nuspec", "TestPlatform.nuspec", "TestPlatform.CLI.nuspec", "TestPlatform.Build.nuspec", "Microsoft.Net.Test.Sdk.nuspec")
    $targetFiles = @("Microsoft.Net.Test.Sdk.targets")
    # Nuget pack analysis emits warnings if binaries are packaged as content. It is intentional for the below packages.
    $skipAnalysis = @("TestPlatform.CLI.nuspec")
    foreach ($file in $nuspecFiles + $targetFiles) {
        Copy-Item $tpNuspecDir\$file $stagingDir -Force
    }

    # Copy over props, empty and third patry notice file
    Copy-Item $tpNuspecDir\"Microsoft.Net.Test.Sdk.props" $stagingDir -Force
    Copy-Item $tpNuspecDir\"_._" $stagingDir -Force
    Copy-Item $tpNuspecDir\..\"ThirdPartyNotices.txt" $stagingDir -Force

    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    # package them from stagingDir
    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version $additionalArgs"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version`;Runtime=$TPB_TargetRuntime`;NetCoreTargetFramework=$TPB_TargetFrameworkCore20 $additionalArgs
    }

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-PackageItems($packageName)
{
    # Packages published separately are copied into their own artifacts directory
    # E.g. src\Microsoft.TestPlatform.ObjectModel\bin\Debug\net46\* is copied
    # to artifacts\Debug\Microsoft.TestPlatform.ObjectModel\net46
    $binariesDirectory = [System.IO.Path]::Combine("src", "$packageName", "bin", "$TPB_Configuration")
    $binariesDirectory = $(Join-Path $binariesDirectory "*")
    $publishDirectory = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$packageName")
    Write-Log "Copy-PackageItems: Package: $packageName"
    Write-Verbose "Create $publishDirectory"
    New-Item -ItemType directory -Path $publishDirectory -Force | Out-Null

    Write-Log "Copy binaries for package '$packageName' from '$binariesDirectory' to '$publishDirectory'"
    Copy-Item -Path $binariesDirectory -Destination $publishDirectory -Recurse -Force
}

function Update-LocalizedResources
{
    $timer = Start-Timer
    $dotnetExe = Get-DotNetPath

    Write-Log ".. Update-LocalizedResources: Started: $TPB_Solution"
    if (!$TPB_LocalizedBuild) {
        Write-Log ".. Update-LocalizedResources: Skipped based on user setting."
        return
    }

    $localizationProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "Localize\Localize.proj"
    Write-Verbose "& $dotnetExe msbuild $localizationProject -m -nologo -v:minimal -t:Localize -p:LocalizeResources=true"
    & $dotnetExe msbuild $localizationProject -m -nologo -v:minimal -t:Localize -p:LocalizeResources=true

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    Write-Log ".. Update-LocalizedResources: Complete. {$(Get-ElapsedTime($timer))}"
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

function Get-CoreCLR20PackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20")
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

function PrintAndExit-OnError([System.String] $output)
{
    if ($? -eq $false){
        Write-Error $output
        Exit 1
    }
}

function Locate-MSBuildPath 
{
    $vsInstallPath = Locate-VsInstallPath

    if([string]::IsNullOrEmpty($vsInstallPath))
    {
      return $null
    }

    $vsInstallPath = Resolve-Path -path $vsInstallPath
    $msbuildPath = Join-Path -path $vsInstallPath -childPath "MSBuild\$env:MSBUILD_VERSION\Bin"

    Write-Verbose "msbuildPath is : $msbuildPath"
    return $msbuildPath
}

function Locate-VsInstallPath
{
   $locateVsApi = Locate-LocateVsApi

   $requiredPackageIds = @("Microsoft.Component.MSBuild", "Microsoft.Net.Component.4.6.TargetingPack", "Microsoft.VisualStudio.Component.Roslyn.Compiler", "Microsoft.VisualStudio.Component.VSSDK")

   Write-Verbose "VSInstallation requirements : $requiredPackageIds"

   Add-Type -path $locateVsApi
   Try
   {
     $vsInstallPath = [LocateVS.Instance]::GetInstallPath($env:MSBUILD_VERSION, $requiredPackageIds)
   }
   Catch [System.Management.Automation.MethodInvocationException]
   {
      Write-Verbose "Failed to find VS installation with requirements : $requiredPackageIds"
   }

   Write-Verbose "VSInstallPath is : $vsInstallPath"

   return $vsInstallPath
}

function Locate-LocateVsApi
{
  $locateVsApi = Join-Path -path $env:TP_PACKAGES_DIR -ChildPath "RoslynTools.Microsoft.LocateVS\$env:LOCATE_VS_API_VERSION\tools\LocateVS.dll"

  if (!(Test-Path -path $locateVsApi)) {
    throw "The specified LocateVS API version ($env:LOCATE_VS_API_VERSION) could not be located."
  }

  Write-Verbose "locateVsApi is : $locateVsApi"
  return $locateVsApi
}

function Update-VsixVersion
{
    Write-Log "Update-VsixVersion: Started."

    $packageDir = Get-FullCLRPackageDirectory
    $vsixVersion = $Version

    # VersionSuffix in microbuild comes in the form preview-20170111-01(preview-yyyymmdd-buildNoOfThatDay)
    # So Version of the vsix will be 15.1.0.2017011101
    $vsixVersionSuffix = $VersionSuffix.Split("-");
    if($vsixVersionSuffix.Length -ige 2) {
        $vsixVersion = "$vsixVersion.$($vsixVersionSuffix[1])$($vsixVersionSuffix[2])"
    }

    $manifestContentWithVersion = Get-Content "$TPB_VSIX_DIR\source.extension.vsixmanifest" -raw | % {$_.ToString().Replace("`$version`$", "$vsixVersion") } 
    Set-Content -path "$TPB_VSIX_DIR\source.extension.vsixmanifest" -value $manifestContentWithVersion

    Write-Log "Update-VsixVersion: Completed."
}

function Build-SpecificProjects
{
    Write-Log "Build-SpecificProjects: Started for pattern: $ProjectNamePatterns"
    # FrameworksAndOutDirs format ("<target_framework>", "<output_dir>").
    $FrameworksAndOutDirs =( ("net46", "net46\win7-x64"), ("netstandard1.5", "netcoreapp1.0"), ("netcoreapp1.0", "netcoreapp1.0"))
    $dotnetPath = Get-DotNetPath

    # Get projects to build.
    Get-ChildItem -Recurse -Path $env:TP_SRC_DIR -Include *.csproj | ForEach-Object {
        foreach ($ProjectNamePattern in $ProjectNamePatterns) {
            if($_.FullName -match  $ProjectNamePattern) {
                $ProjectsToBuild += ,"$_"
            }
        }
    }

    if( $ProjectsToBuild -eq $null){
        Write-Error "No csproj name match for given pattern: $ProjectNamePatterns"
    }

    # Build Projects.
    foreach($ProjectToBuild in $ProjectsToBuild) {
        Write-Log "Building Project $ProjectToBuild"
        # Restore and Build
        $output = & $dotnetPath restore $ProjectToBuild
        PrintAndExit-OnError $output
        $output = & $dotnetPath build $ProjectToBuild
        PrintAndExit-OnError $output

        # Copy artifacts
        $ProjectDir = [System.IO.Path]::GetDirectoryName($ProjectToBuild)
        foreach($FrameworkAndOutDir in $FrameworksAndOutDirs) {
            $fromDir = $([System.IO.Path]::Combine($ProjectDir, "bin", $TPB_Configuration, $FrameworkAndOutDir[0]))
            $toDir = $([System.IO.Path]::Combine($env:TP_OUT_DIR, $TPB_Configuration, $FrameworkAndOutDir[1]))
            if ( Test-Path $fromDir){
                Write-Log "Copying articates from $fromDir to $toDir"
                Get-ChildItem $fromDir | ForEach-Object {
                    if(-not ($_.PSIsContainer)) {
                        copy $_.FullName $toDir
                    }
                }
            }
        }
    }
}

if ($ProjectNamePatterns.Count -ne 0)
{
    # Build Specific projects.
    Build-SpecificProjects
    Exit
}

# Execute build
$timer = Start-Timer
Write-Log "Build started: args = '$args'"
Write-Log "Test platform environment variables: "
Get-ChildItem env: | Where-Object -FilterScript { $_.Name.StartsWith("TP_") } | Format-Table
Write-Log "Test platform build variables: "
Get-Variable | Where-Object -FilterScript { $_.Name.StartsWith("TPB_") } | Format-Table
#Install-DotNetCli
#Restore-Package
#Update-LocalizedResources
#Invoke-Build
#Publish-Package
#Create-VsixPackage
Create-NugetPackages
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
