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
    [System.String] $Version = "15.6.0",

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

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR
$env:NUGET_EXE_Version = "3.4.3"
$env:DOTNET_CLI_VERSION = "2.1.0-preview1-007372"
# $env:DOTNET_RUNTIME_VERSION = "LATEST"
$env:VSWHERE_VERSION = "2.0.2"
$env:MSBUILD_VERSION = "15.0"

#
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_Solution = "TestPlatform.sln"
$TPB_TestAssets_Solution = Join-Path $env:TP_ROOT_DIR "test\TestAssets\TestAssets.sln"
$TPB_TargetFramework = "net451"
$TPB_TargetFrameworkCore = "netcoreapp1.0"
$TPB_TargetFrameworkCore20 = "netcoreapp2.0"
$TPB_TargetFrameworkUap = "uap10.0"
$TPB_TargetFrameworkNS1_4 = "netstandard1.4"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime
# Version suffix is empty for RTM releases
$TPB_Version = if ($VersionSuffix -ne '') { $Version + "-" + $VersionSuffix } else { $Version }
$TPB_CIBuild = $CIBuild
$TPB_LocalizedBuild = !$DisableLocalizedBuild

$language = @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant")

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

    # Pull in additional shared frameworks.
    # Get netcoreapp1.0 shared components.
    if (!(Test-Path "$dotnetInstallPath\shared\Microsoft.NETCore.App\1.0.5")) {
        & $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version '1.0.5' -Channel 'preview'
    }
    
    # Get netcoreapp1.1 shared components.
    if (!(Test-Path "$dotnetInstallPath\shared\Microsoft.NETCore.App\1.1.2")) {
        & $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version '1.1.2' -Channel 'release/1.1.0'
    }

    # Get netcoreapp2.0 shared components.
    if (!(Test-Path "$dotnetInstallPath\shared\Microsoft.NETCore.App\2.0.0")) {
        & $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version '2.0.0' -Channel 'release/2.0.0'
    }

    # Get shared components which is compatible with dotnet cli version $env:DOTNET_CLI_VERSION
    #if (!(Test-Path "$dotnetInstallPath\shared\Microsoft.NETCore.App\$env:DOTNET_RUNTIME_VERSION")) {
        #& $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version $env:DOTNET_RUNTIME_VERSION -Channel 'master'
    #}
    
    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Restore-Package
{
    $timer = Start-Timer
    Write-Log "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Restore-Package: Source: $env:TP_ROOT_DIR\src\package\external\external.csproj"
    & $dotnetExe restore $env:TP_ROOT_DIR\src\package\external\external.csproj --packages $env:TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version
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

    Write-Log ".. .. Build: Source: $TPB_TestAssets_Solution"
    Write-Verbose "$dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild"
    & $dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
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
    $coreCLR20TestHostPackageDir = Get-CoreCLR20TestHostPackageDirectory
    $packageProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "package\package.csproj"
    $testHostProject = Join-Path $env:TP_ROOT_DIR "src\testhost\testhost.csproj"
    $testHostx86Project = Join-Path $env:TP_ROOT_DIR "src\testhost.x86\testhost.x86.csproj"
    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework\$TPB_TargetRuntime")
    $testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore")
    $testhostNS1_4PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkNS1_4")
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
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    # Publish testhost
    
    Write-Log "Package: Publish testhost\testhost.csproj"
    Publish-PackageInternal $testHostProject $TPB_TargetFramework $testhostFullPackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore $testhostCorePackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkNS1_4 $testhostNS1_4PackageDir

    Write-Log "Package: Publish testhost.x86\testhost.x86.csproj"
    Publish-PackageInternal $testHostx86Project $TPB_TargetFramework $testhostFullPackageDir  

    # Copy over the Full CLR built testhost package assemblies to the Core CLR and Full CLR package folder.
    $coreCLRFull_Dir = "TestHost"
    $fullDestDir = Join-Path $coreCLR20PackageDir $coreCLRFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force -recurse

    # Copy over the Full CLR built datacollector package assemblies to the Core CLR package folder along with testhost
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework $fullDestDir
    
    New-Item -ItemType directory -Path $fullCLRPackageDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force -recurse

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    # Publish platform abstractions
    $platformAbstraction = Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.PlatformAbstractions\bin\$TPB_Configuration"
    $platformAbstractionNetFull = Join-Path $platformAbstraction $TPB_TargetFramework
    $platformAbstractionNetCore = Join-Path $platformAbstraction $TPB_TargetFrameworkCore
    $platformAbstractionUap = Join-Path $platformAbstraction $TPB_TargetFrameworkUap
    Copy-Item $platformAbstractionNetFull\* $fullCLRPackageDir -Force
    Copy-Item $platformAbstractionNetCore\* $coreCLR20PackageDir -Force
    Copy-Item $platformAbstractionUap\* $testhostNS1_4PackageDir -Force
    
    # Publish msdia
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any\ComComponents"
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCorePackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostFullPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostNS1_4PackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $coreCLR20TestHostPackageDir -Force

    # Copy over the logger assemblies to the Extensions folder.
    $extensions_Dir = "Extensions"
    $fullCLRExtensionsDir = Join-Path $fullCLRPackageDir $extensions_Dir
    $coreCLRExtensionsDir = Join-Path $coreCLR20PackageDir $extensions_Dir
    # Create an extensions directory.
    New-Item -ItemType directory -Path $fullCLRExtensionsDir -Force | Out-Null
    New-Item -ItemType directory -Path $coreCLRExtensionsDir -Force | Out-Null

    # If there are some dependencies for the logger assemblies, those need to be moved too.
    # Ideally we should just be publishing the loggers to the Extensions folder.
    $loggers = @("Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.pdb")
    foreach($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move trx logger resource dlls
    if($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackageDir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
    }

    # Copy Blame Datacollector to Extensions folder.
    $TPB_TargetFrameworkStandard = "netstandard1.5"
    $blameDataCollector = Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.Extensions.BlameDataCollector\bin\$TPB_Configuration"
    $blameDataCollectorNetFull = Join-Path $blameDataCollector $TPB_TargetFramework
    $blameDataCollectorNetStandard = Join-Path $blameDataCollector $TPB_TargetFrameworkStandard
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $coreCLRExtensionsDir -Force

    # Copy blame data collector resource dlls
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $blameDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
        Copy-Loc-Files $blameDataCollectorNetStandard $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
    }

    # Copy Event Log Datacollector to Extensions folder.
    $eventLogDataCollector = Join-Path $env:TP_ROOT_DIR "src\DataCollectors\Microsoft.TestPlatform.Extensions.EventLogCollector\bin\$TPB_Configuration"
    $eventLogDataCollectorNetFull = Join-Path $eventLogDataCollector $TPB_TargetFramework
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $coreCLRExtensionsDir -Force

    # Copy EventLogCollector resource dlls
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $eventLogDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
        Copy-Loc-Files $eventLogDataCollectorNetFull $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
    }

    # If there are some dependencies for the TestHostRuntimeProvider assemblies, those need to be moved too.
    $runtimeproviders = @("Microsoft.TestPlatform.TestHostRuntimeProvider.dll", "Microsoft.TestPlatform.TestHostRuntimeProvider.pdb")
    foreach($file in $runtimeproviders) {
        Write-Verbose "Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move TestHostRuntimeProvider resource dlls
    if($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackageDir $fullCLRExtensionsDir "Microsoft.TestPlatform.TestHostRuntimeProvider.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.TestPlatform.TestHostRuntimeProvider.resources.dll"
    }

    # Copy dependency of Microsoft.TestPlatform.TestHostRuntimeProvider
    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\9.0.1\lib\net45\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $fullCLRPackageDir -Force"
    Copy-Item $newtonsoft $fullCLRPackageDir -Force

    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $coreCLR20PackageDir -Force"
    Copy-Item $newtonsoft $coreCLR20PackageDir -Force

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    Copy-PackageItems "Microsoft.TestPlatform.Build"

    # Copy IntelliTrace components.
    $intellitraceSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Intellitrace\15.5.0-preview-20171018-01\tools"
    $intellitraceTargetDirectory = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Intellitrace"

    if (-not (Test-Path $intellitraceTargetDirectory)) {
        New-Item $intellitraceTargetDirectory -Type Directory -Force | Out-Null
    }

    Copy-Item -Recurse $intellitraceSourceDirectory\* $intellitraceTargetDirectory -Force
    
    Write-Log "Publish-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Publish-PackageInternal($packagename, $framework, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
}

function Copy-Loc-Files($sourceDir, $destinationDir, $dllName)
{
	foreach($lang in $language) {
        $dllToCopy = Join-Path $sourceDir\$lang $dllName
        $destinationFolder = Join-Path $destinationDir $lang
        if (-not (Test-Path $destinationFolder)) {
            New-Item $destinationFolder -Type Directory -Force | Out-Null
        }
        Copy-Item $dllToCopy $destinationFolder -Force
	}
}

function Move-Loc-Files($sourceDir, $destinationDir, $dllName)
{
	foreach($lang in $language) {
        $dllToCopy = Join-Path $sourceDir\$lang $dllName
        $destinationFolder = Join-Path $destinationDir $lang
        if (-not (Test-Path $destinationFolder)) {
            New-Item $destinationFolder -Type Directory -Force | Out-Null
        }
        Move-Item $dllToCopy $destinationFolder -Force
	}
}

function Create-VsixPackage
{
    Write-Log "Create-VsixPackage: Started."
    $timer = Start-Timer

    $vsixSourceDir = Join-Path $env:TP_ROOT_DIR "src\package\VSIXProject"
    $vsixProjectDir = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\VSIX"
    $packageDir = Get-FullCLRPackageDirectory
    $extensionsPackageDir = Join-Path $packageDir "Extensions"
    $testImpactComComponentsDir = Join-Path $extensionsPackageDir "TestImpact"
    $legacyTestImpactComComponentsDir = Join-Path $extensionsPackageDir "V1\TestImpact"

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\15.5.0-preview-1102755\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy QtAgent Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools\15.5.0-preview-1050541\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Legacy data collectors Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools.DataCollectors\15.5.0-preview-1050541\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy COM Components and their manifests over
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any\ComComponents"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force    

    # Copy COM Components and their manifests over to Extensions Test Impact directory
    $comComponentsDirectoryTIA = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any"
    if (-not (Test-Path $testImpactComComponentsDir)) {
        New-Item $testImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $testImpactComComponentsDir -Force

	if (-not (Test-Path $legacyTestImpactComComponentsDir)) {
        New-Item $legacyTestImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $legacyTestImpactComComponentsDir -Force
    
    $fileToCopy = Join-Path $env:TP_PACKAGE_PROJ_DIR "ThirdPartyNotices.txt"
    Copy-Item $fileToCopy $packageDir -Force

    Write-Verbose "Locating MSBuild install path..."
    $msbuildPath = Locate-MSBuildPath
    
    # Create vsix only when msbuild is installed.
    if(![string]::IsNullOrEmpty($msbuildPath))
    {
        # Copy the vsix project to artifacts directory to modify manifest
        New-Item $vsixProjectDir -Type Directory -Force
        Copy-Item -Recurse $vsixSourceDir\* $vsixProjectDir -Force

        # Update version of VSIX
        Update-VsixVersion $vsixProjectDir

        # Build vsix project to get TestPlatform.vsix
        Write-Verbose "$msbuildPath\msbuild.exe $vsixProjectDir\TestPlatform.csproj -p:Configuration=$Configuration"
        & $msbuildPath\msbuild.exe "$vsixProjectDir\TestPlatform.csproj" -p:Configuration=$Configuration
    }
    else
    {
        Write-Log ".. Create-VsixPackage: Cannot generate vsix as msbuild.exe not found at '$msbuildPath'."
    }

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-NugetPackages
{
    $timer = Start-Timer

    Write-Log "Create-NugetPackages: Started."
    $stagingDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration
    $packageOutputDir = (Join-Path $env:TP_OUT_DIR $TPB_Configuration\packages )

    if (-not (Test-Path $packageOutputDir)) {
        New-Item $packageOutputDir -type directory -Force
    }

    $tpNuspecDir = Join-Path $env:TP_PACKAGE_PROJ_DIR "nuspec"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec", "TestPlatform.ObjectModel.nuspec", "TestPlatform.TestHost.nuspec", "TestPlatform.CLI.nuspec", "TestPlatform.Build.nuspec", "Microsoft.Net.Test.Sdk.nuspec", "Microsoft.TestPlatform.nuspec", "Microsoft.TestPlatform.Portable.nuspec")
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

    #Copy Uap target, & props
    $testhostNS1_4PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkNS1_4")
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.props" $testhostNS1_4PackageDir\Microsoft.TestPlatform.TestHost.props -Force
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.targets" $testhostNS1_4PackageDir\Microsoft.TestPlatform.TestHost.targets -Force

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
    # E.g. src\Microsoft.TestPlatform.ObjectModel\bin\Debug\net451\* is copied
    # to artifacts\Debug\Microsoft.TestPlatform.ObjectModel\net451
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

function Get-CoreCLR20TestHostPackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20\TestHost")
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
   $vswhere = Join-Path -path $env:TP_PACKAGES_DIR -ChildPath "vswhere\$env:VSWHERE_VERSION\tools\vswhere.exe"
   if (!(Test-Path -path $vswhere)) {
       throw "Unable to locate vswhere in path '$vswhere'."
   }

   Write-Verbose "Using '$vswhere' to locate VS installation path."

   $requiredPackageIds = @("Microsoft.Component.MSBuild", "Microsoft.Net.Component.4.6.TargetingPack", "Microsoft.VisualStudio.Component.VSSDK")
   Write-Verbose "VSInstallation requirements : $requiredPackageIds"

   Try
   {
       Write-Verbose "VSWhere command line: $vswhere -latest -prerelease -products * -requires $requiredPackageIds -property installationPath"
       if ($TPB_CIBuild) {
           $vsInstallPath = & $vswhere -latest -products * -requires $requiredPackageIds -property installationPath
       }
       else {
           # Allow using pre release versions of VS for dev builds
           $vsInstallPath = & $vswhere -latest -prerelease -products * -requires $requiredPackageIds -property installationPath
       }
   }
   Catch [System.Management.Automation.MethodInvocationException]
   {
       Write-Verbose "Failed to find VS installation with requirements : $requiredPackageIds"
   }

   Write-Verbose "VSInstallPath is : $vsInstallPath"
   return $vsInstallPath
}

function Update-VsixVersion($vsixProjectDir)
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

    $manifestContentWithVersion = Get-Content "$vsixProjectDir\source.extension.vsixmanifest" -raw | % {$_.ToString().Replace("`$version`$", "$vsixVersion") } 
    Set-Content -path "$vsixProjectDir\source.extension.vsixmanifest" -value $manifestContentWithVersion

    Write-Log "Update-VsixVersion: Completed."
}

function Build-SpecificProjects
{
    Write-Log "Build-SpecificProjects: Started for pattern: $ProjectNamePatterns"
    # FrameworksAndOutDirs format ("<target_framework>", "<output_dir>").
    $FrameworksAndOutDirs =( ("net451", "net451\win7-x64"), ("netstandard1.5", "netcoreapp2.0"), ("netcoreapp1.0", "netcoreapp2.0"), ("netcoreapp2.0", "netcoreapp2.0"))
    $dotnetPath = Get-DotNetPath

    # Get projects to build.
    Get-ChildItem -Recurse -Path $env:TP_ROOT_DIR -Include *.csproj | ForEach-Object {
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

        if (-Not ($ProjectToBuild.FullName -contains "$($env:TP_ROOT_DIR)$([IO.Path]::DirectorySeparatorChar)src")) {
            # Don't copy artifacts for non src folders.
            continue;
        }

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
Install-DotNetCli
Restore-Package
Update-LocalizedResources
Invoke-Build
Publish-Package
Create-VsixPackage
Create-NugetPackages
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
