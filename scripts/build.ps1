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
    [System.String] $Version, # Will set this later by reading TestPlatform.Settings.targets file.

    [Parameter(Mandatory=$false)]
    [Alias("vs")]
    [System.String] $VersionSuffix = "dev",

    [Parameter(Mandatory=$false)]
    [Alias("bn")]
    [System.String] $BuildNumber = "20991231-99",

    [Parameter(Mandatory=$false)]
    [Alias("ff")]
    [System.Boolean] $FailFast = $true,

    [Parameter(Mandatory=$false)]
    [Alias("noloc")]
    [Switch] $DisableLocalizedBuild = $false,

    [Parameter(Mandatory=$false)]
    [Alias("ci")]
    [Switch] $CIBuild = $false,

	[Parameter(Mandatory=$false)]
    [Alias("pt")]
    [Switch] $PublishTestArtifacts = $false,

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
$CurrentScriptDir = (Get-Item (Split-Path $MyInvocation.MyCommand.Path))
$env:TP_ROOT_DIR = $CurrentScriptDir.Parent.FullName
$env:TP_TOOLS_DIR = Join-Path $env:TP_ROOT_DIR "tools"
$env:TP_PACKAGES_DIR = Join-Path $env:TP_ROOT_DIR "packages"
$env:TP_OUT_DIR = Join-Path $env:TP_ROOT_DIR "artifacts"
$env:TP_TESTARTIFACTS = Join-Path $env:TP_OUT_DIR "testArtifacts"
$env:TP_PACKAGE_PROJ_DIR = Join-Path $env:TP_ROOT_DIR "src\package"

# Set Version from scripts/build/TestPlatform.Settings.targets, when we are running locally and not providing the version as the parameter 
# or when the build is done directly in VS
if([string]::IsNullOrWhiteSpace($Version))
{
    $Version = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Settings.targets)).Project.PropertyGroup[0].TPVersionPrefix | 
        ForEach-Object { $_.Trim() } |
        Select-Object -First 1 

    Write-Verbose "Version was not provided using version '$Version' from TestPlatform.Settings.targets"    
}

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR
$env:NUGET_EXE_Version = "3.4.3"
$env:DOTNET_CLI_VERSION = "3.1.101"
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
$TPB_TargetFramework472 = "net472"
$TPB_TargetFrameworkCore20 = "netcoreapp2.1"
$TPB_TargetFrameworkUap = "uap10.0"
$TPB_TargetFrameworkNS2_0 = "netstandard2.0"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime
$TPB_X64_Runtime = "win7-x64"
$TPB_X86_Runtime = "win7-x86"

# Version suffix is empty for RTM release
$TPB_Version = if ($VersionSuffix -ne '') { $Version + "-" + $VersionSuffix } else { $Version }
$TPB_CIBuild = $CIBuild
$TPB_PublishTests = $PublishTestArtifacts
$TPB_LocalizedBuild = !$DisableLocalizedBuild
$TPB_PackageOutDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration\packages

$language = @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant")

# Capture error state in any step globally to modify return code
$Script:ScriptFailed = $false

Import-Module "$($CurrentScriptDir.FullName)\verify-nupkgs.ps1"

# Update the version in the dependencies props to be the TPB_version version, this is not ideal but because changing how this is resolved would 
# mean that we need to change the whole build process this is a solution with the least amount of impact, that does not require us to keep track of 
# the version in multiple places
$dependenciesPath = "$env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props"
$dependencies = Get-Content -Raw -Encoding UTF8 $dependenciesPath
$updatedDependencies = $dependencies -replace "<NETTestSdkVersion>.*?</NETTestSdkVersion>", "<NETTestSdkVersion>$TPB_Version</NETTestSdkVersion>"
$updatedDependencies | Set-Content -Encoding UTF8 $dependenciesPath -NoNewline

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
    & $dotnetInstallScript -Channel "master" -InstallDir $dotnetInstallPath -Version $env:DOTNET_CLI_VERSION

    # Pull in additional shared frameworks.
    # Get netcoreapp2.1 shared components.
    
    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Runtime 'dotnet' -Version '2.1.0' -Channel '2.1.0' -Architecture x64 -NoPath
    $env:DOTNET_ROOT= $dotnetInstallPath

    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Runtime 'dotnet' -Version '2.1.0' -Channel '2.1.0' -Architecture x86 -NoPath
    ${env:DOTNET_ROOT(x86)} = "${dotnetInstallPath}_x86"
    
    $env:DOTNET_MULTILEVEL_LOOKUP=0

    "---- dotnet environment variables"
    Get-ChildItem "Env:\dotnet_*"
    
    "`n`n---- x64 dotnet"
    & "$env:DOTNET_ROOT\dotnet.exe" --info

    "`n`n---- x86 dotnet"
    # avoid erroring out because we don't have the sdk for x86 that global.json requires
    try {
        & "${env:DOTNET_ROOT(x86)}\dotnet.exe" --info 2> $null
    } catch {}
    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Clear-Package {
    # find all microsoft packages that have the same version as we specified
    # this is cache-busting the nuget packages, so we don't reuse them from cache 
    # after we built new ones
    if (Test-Path $env:TP_PACKAGES_DIR) {
        $devPackages = Get-ChildItem $env:TP_PACKAGES_DIR/microsoft.*/$TPB_Version | Select-Object -ExpandProperty FullName 
        $devPackages | Remove-Item -Force -Recurse -Confirm:$false
    }
}

function Restore-Package
{
    $timer = Start-Timer
    Write-Log "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Restore-Package: Source: $env:TP_ROOT_DIR\src\package\external\external.csproj"
    & $dotnetExe restore $env:TP_ROOT_DIR\src\package\external\external.csproj --packages $env:TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version
    Write-Log ".. .. Restore-Package: Complete."

    Set-ScriptFailedOnError

    Write-Log "Restore-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Invoke-Build
{
    $timer = Start-Timer
    Write-Log "Invoke-Build: Start build."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Build: Source: $TPB_Solution"
    Write-Verbose "$dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:TestPlatform.binlog
    Write-Log ".. .. Build: Complete."

    Set-ScriptFailedOnError

    Write-Log "Invoke-Build: Complete. {$(Get-ElapsedTime($timer))}"
}

function Invoke-TestAssetsBuild
{
    $timer = Start-Timer
    Write-Log "Invoke-TestAssetsBuild: Start test assets build."
    $dotnetExe = Get-DotNetPath

    
    Write-Log ".. .. Build: Source: $TPB_TestAssets_Solution"
    Write-Verbose "$dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild"
    & $dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:"$($env:TP_ROOT_DIR)\TestAssets.binlog"
    Write-Log ".. .. Build: Complete."

    Set-ScriptFailedOnError

    Write-Log "Invoke-TestAssetsBuild: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-PackageIntoStaticDirectory { 
    # packages are published into folder based on configuration, but 
    # nuget does not understand that, and does not support wildcards in paths 
    # in order to be able to use the produced packages for acceptance tests we
    # need to put them in folder that is not changing it's name based on config
    $tpPackagesPath = "$env:TP_OUT_DIR\$TPB_Configuration\packages\"
    $tpPackagesDestination = "$env:TP_TESTARTIFACTS"
    Copy-Item $tpPackagesPath $tpPackagesDestination -Force -Filter *.nupkg -Verbose -Recurse
}

function Publish-PatchedDotnet { 
    Write-Log "Publish-PatchedDotnet: Copy local dotnet installation to testArtifacts"
    $dotnetPath = "$env:TP_TOOLS_DIR\dotnet\"

    $dotnetTestArtifactsPath = "$env:TP_TESTARTIFACTS\dotnet\" 
    
    if (Test-Path $dotnetTestArtifactsPath) { 
        Remove-Item -Force -Recurse $dotnetTestArtifactsPath
    }

    $dotnetTestArtifactsSdkPath = "$env:TP_TESTARTIFACTS\dotnet\sdk\$env:DOTNET_CLI_VERSION\"
    Copy-Item $dotnetPath $dotnetTestArtifactsPath -Force -Recurse

    Write-Log "Publish-PatchedDotnet: Copy VSTest task artifacts to local dotnet installation to allow `dotnet test` to run with it"
    $buildArtifactsPath = "$env:TP_ROOT_DIR\src\Microsoft.TestPlatform.Build\bin\$TPB_Configuration\$TPB_TargetFrameworkNS2_0\*"
    Copy-Item $buildArtifactsPath $dotnetTestArtifactsSdkPath -Force
}

function Publish-Package
{
    $timer = Start-Timer
    Write-Log "Publish-Package: Started."
    $dotnetExe = Get-DotNetPath
    $fullCLRPackageDir = Get-FullCLRPackageDirectory
    $coreCLR20PackageDir = Get-CoreCLR20PackageDirectory
    $coreCLR20TestHostPackageDir = Get-CoreCLR20TestHostPackageDirectory
    $packageProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "package\package.csproj"
    $testHostProject = Join-Path $env:TP_ROOT_DIR "src\testhost\testhost.csproj"
    $testHostx86Project = Join-Path $env:TP_ROOT_DIR "src\testhost.x86\testhost.x86.csproj"
    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework\$TPB_TargetRuntime")
    $testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20")
    $testhostCorePackageX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCorePackageX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    $testhostCorePackageTempX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCorePackageTempX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    $testhostUapPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkUap")
    $vstestConsoleProject = Join-Path $env:TP_ROOT_DIR "src\vstest.console\vstest.console.csproj"
    $settingsMigratorProject = Join-Path $env:TP_ROOT_DIR "src\SettingsMigrator\SettingsMigrator.csproj"
    $dataCollectorProject = Join-Path $env:TP_ROOT_DIR "src\datacollector\datacollector.csproj"

    Write-Log "Package: Publish src\package\package\package.csproj"

    Publish-PackageInternal $packageProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-PackageInternal $packageProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.
    
    Write-Log "Package: Publish src\vstest.console\vstest.console.csproj"
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir
	
    Write-Log "Package: Publish src\SettingsMigrator\SettingsMigrator.csproj"
    Publish-PackageInternal $settingsMigratorProject $TPB_TargetFramework $fullCLRPackageDir

    Write-Log "Package: Publish src\datacollector\datacollector.csproj"
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework472 $fullCLRPackageDir
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    # Publish testhost
    
    Write-Log "Package: Publish testhost\testhost.csproj"
    Publish-PackageInternal $testHostProject $TPB_TargetFramework $testhostFullPackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore20 $testhostCorePackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore20 $testhostUapPackageDir
    Publish-PackageWithRuntimeInternal $testHostProject $TPB_TargetFrameworkCore20 $TPB_X64_Runtime false $testhostCorePackageTempX64Dir

    Write-Log "Package: Publish testhost.x86\testhost.x86.csproj"
    Publish-PackageInternal $testHostx86Project $TPB_TargetFramework $testhostFullPackageDir
    Publish-PackageWithRuntimeInternal $testHostx86Project $TPB_TargetFrameworkCore20 $TPB_X86_Runtime false $testhostCorePackageTempX86Dir

    # Copy the .NET core x86 and x64 testhost exes from tempPublish to required folder
    New-Item -ItemType directory -Path $testhostCorePackageX64Dir -Force | Out-Null
    Copy-Item $testhostCorePackageTempX64Dir\testhost* $testhostCorePackageX64Dir -Force -recurse
    New-Item -ItemType directory -Path $testhostCorePackageX86Dir -Force | Out-Null
    Copy-Item $testhostCorePackageTempX86Dir\testhost.x86* $testhostCorePackageX86Dir -Force -recurse

    # Copy over the Full CLR built testhost package assemblies to the Core CLR and Full CLR package folder.
    $coreCLRFull_Dir = "TestHost"
    $fullDestDir = Join-Path $coreCLR20PackageDir $coreCLRFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force -recurse

    Set-ScriptFailedOnError

    # Copy over the Full CLR built datacollector package assemblies to the Core CLR package folder along with testhost
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework472 $fullDestDir
    
    New-Item -ItemType directory -Path $fullCLRPackageDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force -recurse

    Set-ScriptFailedOnError

    # Publish platform abstractions
    $platformAbstraction = Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.PlatformAbstractions\bin\$TPB_Configuration"
    $platformAbstractionNetFull = Join-Path $platformAbstraction $TPB_TargetFramework
    $platformAbstractionNetCore = Join-Path $platformAbstraction $TPB_TargetFrameworkCore20
    $platformAbstractionUap = Join-Path $platformAbstraction $TPB_TargetFrameworkUap
    Copy-Item $platformAbstractionNetFull\* $fullCLRPackageDir -Force
    Copy-Item $platformAbstractionNetCore\* $coreCLR20PackageDir -Force
    Copy-Item $platformAbstractionUap\* $testhostUapPackageDir -Force
    
    # Publish msdia
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any\ComComponents"
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCorePackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostFullPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostUapPackageDir -Force
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
    $loggers = @("Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.pdb", "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.pdb")
    foreach($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackageDir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move logger resource dlls
    if($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackageDir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $fullCLRPackageDir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.resources.dll"
    }

    # Copy Blame Datacollector to Extensions folder.
    $TPB_TargetFrameworkStandard = "netstandard2.0"
    $blameDataCollector = Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.Extensions.BlameDataCollector\bin\$TPB_Configuration"
    $blameDataCollectorNetFull = Join-Path $blameDataCollector $TPB_TargetFramework472
    $blameDataCollectorNetStandard = Join-Path $blameDataCollector $TPB_TargetFrameworkStandard
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $coreCLRExtensionsDir -Force
    # we use this to dump processes on netcore
    Copy-Item $blameDataCollectorNetStandard\Microsoft.Diagnostics.NETCore.Client.dll $coreCLRExtensionsDir -Force

    # $null = New-Item -Force "$fullCLRExtensionsDir\procdump" -ItemType Directory
    # $null = New-Item -Force "$coreCLRExtensionsDir\procdump" -ItemType Directory
    # Copy-Item $blameDataCollectorNetFull\procdump.exe $fullCLRExtensionsDir\procdump -Force
    # Copy-Item $blameDataCollectorNetFull\procdump64.exe $fullCLRExtensionsDir\procdump -Force
    # Copy-Item $blameDataCollectorNetStandard\procdump.exe $coreCLRExtensionsDir\procdump -Force
    # Copy-Item $blameDataCollectorNetStandard\procdump64.exe $coreCLRExtensionsDir\procdump -Force
    # Copy-Item $blameDataCollectorNetStandard\procdump $coreCLRExtensionsDir\procdump -Force

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
    if ($TPB_LocalizedBuild) {
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
    $testPlatformExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformExternalsVersion
    $intellitraceSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Intellitrace\$testPlatformExternalsVersion\tools"
    $intellitraceTargetDirectory = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Intellitrace"

    if (-not (Test-Path $intellitraceTargetDirectory)) {
        New-Item $intellitraceTargetDirectory -Type Directory -Force | Out-Null
    }

    Copy-Item -Recurse $intellitraceSourceDirectory\* $intellitraceTargetDirectory -Force
    
    # Copy Microsoft.VisualStudio.Telemetry APIs
    $testPlatformDirectory = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Intellitrace\Common7\IDE\Extensions\TestPlatform"
    
    if (-not (Test-Path $testPlatformDirectory)) {
        New-Item $testPlatformDirectory -Type Directory -Force | Out-Null
    }

    $visualStudioTelemetryDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Telemetry\16.3.58\lib\net45"
    $visualStudioRemoteControl = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.RemoteControl\16.3.23\lib\net45"
    $visualStudioUtilitiesDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Utilities.Internal\16.3.23\lib\net45"

    Copy-Item "$visualStudioTelemetryDirectory\Microsoft.VisualStudio.Telemetry.dll" $testPlatformDirectory -Force
    Copy-Item "$visualStudioRemoteControl\Microsoft.VisualStudio.RemoteControl.dll" $testPlatformDirectory -Force
    Copy-Item "$visualStudioUtilitiesDirectory\Microsoft.VisualStudio.Utilities.Internal.dll" $testPlatformDirectory -Force

    Copy-CodeCoverage-Package-Artifacts

    Write-Log "Publish-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Publish-Tests
{
	if($TPB_PublishTests)	
	{
		$dotnetExe = Get-DotNetPath
		Write-Log "Publish-Tests: Started."

		# Adding only Perf project for now
		$fullCLRTestDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework"
		$fullCLRPerfTestAssetDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework\TestAssets\PerfAssets"
    
		$mstest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "MSTestAdapterPerfTestProject"
		$mstest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\MSTestAdapterPerfTestProject"
		Publish-PackageInternal $mstest10kPerfProject $TPB_TargetFramework $mstest10kPerfProjectDir

		$nunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "NUnitAdapterPerfTestProject"
		$nunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\NUnitAdapterPerfTestProject"
		Publish-PackageInternal $nunittest10kPerfProject $TPB_TargetFramework $nunittest10kPerfProjectDir

		$xunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "XUnitAdapterPerfTestProject"
		$xunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\XUnitAdapterPerfTestProject"
		Publish-PackageInternal $xunittest10kPerfProject $TPB_TargetFramework $xunittest10kPerfProjectDir

		$testPerfProject = Join-Path $env:TP_ROOT_DIR "test\Microsoft.TestPlatform.PerformanceTests"
		Publish-PackageInternal $testPerfProject $TPB_TargetFramework $fullCLRTestDir
	}
}

function Publish-PackageInternal($packagename, $framework, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild

    Set-ScriptFailedOnError
}

function Publish-PackageWithRuntimeInternal($packagename, $framework, $runtime, $selfcontained, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --runtime $runtime --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --runtime $runtime --self-contained $selfcontained --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild

    Set-ScriptFailedOnError
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

    $testPlatformExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformExternalsVersion

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\$testPlatformExternalsVersion\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Microsoft.VisualStudio.ArchitectureTools.PEReader to Extensions
    Copy-Item $legacyDir\Microsoft.VisualStudio.ArchitectureTools.PEReader.dll $extensionsPackageDir -Force

    # Copy QtAgent Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools\$testPlatformExternalsVersion\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Legacy data collectors Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools.DataCollectors\$testPlatformExternalsVersion\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

	# Copy CUIT Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.CUIT\$testPlatformExternalsVersion\contentFiles\any\any"
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
        Write-Verbose "$msbuildPath $vsixProjectDir\TestPlatform.csproj -p:Configuration=$Configuration"
        & $msbuildPath "$vsixProjectDir\TestPlatform.csproj" -p:Configuration=$Configuration
    }
    else
    { 
        throw ".. Create-VsixPackage: Cannot generate vsix as msbuild.exe not found at '$msbuildPath'."
    }

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-NugetPackages
{
    $timer = Start-Timer

    Write-Log "Create-NugetPackages: Started."
    $stagingDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration
	$packageOutputDir = $TPB_PackageOutDir

    if (-not (Test-Path $packageOutputDir)) {
        New-Item $packageOutputDir -type directory -Force
    }

    $tpNuspecDir = Join-Path $env:TP_PACKAGE_PROJ_DIR "nuspec"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec",
                     "TestPlatform.ObjectModel.nuspec",
                     "TestPlatform.TestHost.nuspec",
                     "TestPlatform.CLI.nuspec",
                     "TestPlatform.Build.nuspec",
                     "TestPlatform.Extensions.TrxLogger.nuspec", 
                     "Microsoft.NET.Test.Sdk.nuspec",
                     "Microsoft.TestPlatform.nuspec",
                     "Microsoft.TestPlatform.Portable.nuspec",
                     "Microsoft.CodeCoverage.nuspec")

    $targetFiles = @("Microsoft.CodeCoverage.targets")
    $propFiles = @("Microsoft.NET.Test.Sdk.props", "Microsoft.CodeCoverage.props")
    $contentDirs = @("netcoreapp", "netfx")

    # Nuget pack analysis emits warnings if binaries are packaged as content. It is intentional for the below packages.
    $skipAnalysis = @("TestPlatform.CLI.nuspec")
    foreach ($item in $nuspecFiles + $targetFiles + $propFiles + $contentDirs) {
        Copy-Item $tpNuspecDir\$item $stagingDir -Force -Recurse
    }

    # Copy empty and third patry notice file
    Copy-Item $tpNuspecDir\"_._" $stagingDir -Force
    Copy-Item $tpNuspecDir\..\"ThirdPartyNotices.txt" $stagingDir -Force

    #Copy Uap target, & props
    $testhostUapPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkUap")
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.props" $testhostUapPackageDir\Microsoft.TestPlatform.TestHost.props -Force
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.targets" $testhostUapPackageDir\Microsoft.TestPlatform.TestHost.targets -Force
	
	$testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20")
    Copy-Item $tpNuspecDir\"Microsoft.TestPlatform.TestHost.NetCore.props" $testhostCorePackageDir\Microsoft.TestPlatform.TestHost.props -Force
    
    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    # Pass Newtonsoft.Json version to nuget pack to keep the version consistent across all nuget packages.
    $JsonNetVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.JsonNetVersion

    # Additional external dependency folders
    $microsoftFakesVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.MicrosoftFakesVersion
    $FakesPackageDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.TestPlatform.Fakes\$microsoftFakesVersion\lib"

    # package them from stagingDir
    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version $additionalArgs"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version`;JsonNetVersion=$JsonNetVersion`;Runtime=$TPB_TargetRuntime`;NetCoreTargetFramework=$TPB_TargetFrameworkCore20`;FakesPackageDir=$FakesPackageDir $additionalArgs

        Set-ScriptFailedOnError
    }

    # Verifies that expected number of files gets shipped in nuget packages.
    # Few nuspec uses wildcard characters.
    Verify-Nuget-Packages $packageOutputDir

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-CodeCoverage-Package-Artifacts
{
    # Copy TraceDataCollector to Microsoft.CodeCoverage folder.
    $microsoftCodeCoveragePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.CodeCoverage\")

    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir -Force | Out-Null

    $traceDataCollectorOutDir = Join-Path $env:TP_ROOT_DIR "src\DataCollectors\TraceDataCollector\bin\$TPB_Configuration\$TPB_TargetFrameworkNS2_0"

    Copy-Item $traceDataCollectorOutDir\Microsoft.VisualStudio.TraceDataCollector.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorOutDir\Microsoft.VisualStudio.TraceDataCollector.pdb $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorOutDir\CodeCoverage $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $traceDataCollectorOutDir\Shim $microsoftCodeCoveragePackageDir -Force -Recurse

    # Copy TraceDataCollector resource dlls
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $traceDataCollectorOutDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
        Copy-Loc-Files $traceDataCollectorOutDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
    }
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
    Write-Verbose "& $dotnetExe msbuild $localizationProject -m -nologo -v:minimal -t:Localize -p:LocalizeResources=true -nodeReuse:False"
    & $dotnetExe msbuild $localizationProject -m -nologo -v:minimal -t:Localize -p:LocalizeResources=true -nodeReuse:False

    Set-ScriptFailedOnError

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

function Set-ScriptFailedOnError
{
    if ($lastExitCode -eq 0) {
        return
    }

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
    $msbuildPath = Get-ChildItem (Join-Path -path $vsInstallPath -childPath "MSBuild\*\Bin\MSBuild.exe")

    Write-Verbose "found msbuild : '$($msbuildPath -join "','")'"
    $msBuild = $msBuildPath | Select-Object -First 1
    Write-Verbose "msbuildPath is : '$($msbuildPath -join "','")'"
    if ($null -eq $msBuild -or 0 -eq $msBuild.Count) { 
        throw "MSBuild not found."
    }

    return $msBuild.FullName
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
       if ($TPB_CIBuild) {
           Write-Verbose "VSWhere command line: $vswhere -version '(15.0' -products * -requires $requiredPackageIds -property installationPath"
           $vsInstallPath = & $vswhere -version '(15.0' -products * -requires $requiredPackageIds -property installationPath
       }
       else {
           # Allow using pre release versions of VS for dev builds
           Write-Verbose "VSWhere command line: $vswhere -version '(15.0' -prerelease -products * -requires $requiredPackageIds -property installationPath"
           $vsInstallPath = & $vswhere -version '(15.0' -prerelease -products * -requires $requiredPackageIds -property installationPath
       }
   }
   Catch [System.Management.Automation.MethodInvocationException]
   {
       throw "Failed to find VS installation with requirements: $requiredPackageIds"
   }

   if ($null -eq $vsInstallPath -or 0 -eq @($vsInstallPath).Count) {
        throw "Failed to find VS installation with requirements: $requiredPackageIds"
   }
   else { 
        Write-Verbose "Found VS installation with requirements '$($requiredPackageIds -join "','")'  : '$($vsInstallPath -join "','")'."
   }

   $vsPath = $vsInstallPath | Select-Object -First 1
   Write-Verbose "VSInstallPath is : $vsPath"
   return $vsPath
}

function Update-VsixVersion($vsixProjectDir)
{
    Write-Log "Update-VsixVersion: Started."

    $packageDir = Get-FullCLRPackageDirectory
    $vsixVersion = $Version

    # Build number comes in the form 20170111-01(yyyymmdd-buildNoOfThatDay)
    # So Version of the vsix will be 15.1.0.2017011101
    $vsixVersionSuffix = $BuildNumber.Split("-");
    if($vsixVersionSuffix.Length -ige 2) {
        $vsixVersion = "$vsixVersion.$($vsixVersionSuffix[0])$($vsixVersionSuffix[1])"
    }

    $manifestContentWithVersion = Get-Content "$vsixProjectDir\source.extension.vsixmanifest" -raw | % {$_.ToString().Replace("`$version`$", "$vsixVersion") } 
    Set-Content -path "$vsixProjectDir\source.extension.vsixmanifest" -value $manifestContentWithVersion

    Write-Log "Update-VsixVersion: Completed."
}

function Generate-Manifest
{
    Write-Log "Generate-Manifest: Started."

    $sdkTaskPath = Join-Path $env:TP_ROOT_DIR "eng\common\sdk-task.ps1"
    & $sdkTaskPath -restore -task GenerateBuildManifest /p:PackagesToPublishPattern=$TPB_PackageOutDir\*.nupkg /p:AssetManifestFilePath=$TPB_PackageOutDir\manifest\manifest.xml /p:ManifestBuildData="Location=https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json" /p:BUILD_BUILDNUMBER=$BuildNumber

    Write-Log "Generate-Manifest: Completed."
}

function Build-SpecificProjects
{
    Write-Log "Build-SpecificProjects: Started for pattern: $ProjectNamePatterns"
    # FrameworksAndOutDirs format ("<target_framework>", "<output_dir>").
    $FrameworksAndOutDirs =( ("net451", "net451\win7-x64"), ("netstandard2.0", "netcoreapp2.1"), ("netcoreapp2.1", "netcoreapp2.1"))
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
                Write-Log "Copying artifacts from $fromDir to $toDir"
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
Clear-Package
Restore-Package
Update-LocalizedResources
Invoke-Build
Publish-Package
Create-VsixPackage
Create-NugetPackages
Generate-Manifest
Publish-PatchedDotnet
Copy-PackageIntoStaticDirectory
Invoke-TestAssetsBuild
Publish-Tests
 
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
 
