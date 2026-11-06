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
    [System.String] $Version = "", # Will set this later by reading TestPlatform.Settings.targets file.

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
    [System.String[]] $ProjectNamePatterns = @(),

    # Which of the build actions to run. Defaults to all of them, in the order below, which is
    # exactly what this script did before this parameter existed - passing nothing is unchanged.
    #
    # Added so a pipeline that already builds through another build system can invoke only the
    # packaging actions without redoing restore and build, and specifically so UpdateLocalization
    # can be left out: it runs xlftool.exe update, which rewrites the checked-in .xlf files from
    # the neutral .resx. For a servicing build we need the translations that shipped.
    #
    # Step names match the equivalent parameter on the 16.11 branch where the actions correspond,
    # so the two scripts converge rather than drift.
    [Parameter(Mandatory=$false)]
    [Alias("s")]
    [ValidateSet("InstallDotnet", "Restore", "UpdateLocalization", "Build", "Publish", "PublishTests", "Vsix", "NugetPackages")]
    [System.String[]] $Steps = @("InstallDotnet", "Restore", "UpdateLocalization", "Build", "Publish", "PublishTests", "Vsix", "NugetPackages")
)

$ErrorActionPreference = "Stop"

#
# Variables
#
Write-Verbose "Setup environment variables."
$CurrentScriptDir = (Get-Item (Split-Path $MyInvocation.MyCommand.Path))
$env:TP_ROOT_DIR = $CurrentScriptDir.Parent.FullName
$env:TP_TOOLS_DIR = Join-Path $env:TP_ROOT_DIR "tools"
# The third-party payload directory. Historically this was <repo>\packages, filled by this script's
# own nuget restore. Under Arcade that restore does not happen, and restoring these packages by
# identity would be wrong anyway - the shipped msdia140.dll is 14.20.27106.0 while the restorable
# package carries 14.26.28619.0, so a version-matched restore injects different bytes.
#
# The pipeline sets this to a payload directory staged from the shipped artifacts by
# scripts\build\New-StagedPayload.ps1. Respect an existing value rather than overwriting it, so the
# historic default still applies to anyone running this script standalone.
if (-not $env:TP_PACKAGES_DIR) {
    $env:TP_PACKAGES_DIR = Join-Path $env:TP_ROOT_DIR "packages"
}
$env:TP_OUT_DIR = Join-Path $env:TP_ROOT_DIR "artifacts"
$env:TP_TESTARTIFACTS = Join-Path $env:TP_OUT_DIR "testArtifacts"
$env:TP_PACKAGE_PROJ_DIR = Join-Path $env:TP_ROOT_DIR "src\package"

# Set Version from scripts/build/TestPlatform.Settings.targets
if([string]::IsNullOrWhiteSpace($Version))
{
$Version = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Settings.targets)).Project.PropertyGroup.TPVersionPrefix
$Version = ($Version).Trim()
}

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
# NUGET_PACKAGES is deliberately NOT derived from TP_PACKAGES_DIR.
#
# It used to be, with the comment "Dotnet build doesn't support --packages yet", which pointed NuGet
# at <repo>\packages - the folder this script's own restore filled. Two things make that wrong now:
#
#   1. TP_PACKAGES_DIR no longer names a restore folder. It names the staged third-party payload
#      extracted from the shipped artifacts, because feed copies are not the shipped bytes: the
#      shipped msdia140.dll is 14.20.27106.0 and the restorable package carries 14.26.28619.0.
#      Pointing NuGet at that directory makes it restore packages into the payload tree.
#   2. Arcade restores to the NuGet global packages folder and builds against it. Overriding
#      NUGET_PACKAGES here makes the packaging steps resolve tasks from a different, empty location
#      than the build did, which surfaced as XliffTasks MSB4018 "TranslateSource task failed" while
#      the same build succeeded through Arcade.
#
# Leaving it unset lets NuGet use the same folder Arcade already restored into. Measured: setting it
# created <repo>\packages with 159 package directories, which is also the untracked folder that
# produced 45 false findings in the shared delivery guard on another line.
$env:NUGET_EXE_Version = "3.4.3"
$env:DOTNET_CLI_VERSION = "LATEST"
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
$TPB_TargetFrameworkNS2_0 = "netstandard2.0"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime
# Version suffix is empty for RTM releases
$TPB_Version = if ($VersionSuffix -ne '') { $Version + "-" + $VersionSuffix } else { $Version }
$TPB_CIBuild = $CIBuild
$TPB_PublishTests = $PublishTestArtifacts
$TPB_LocalizedBuild = !$DisableLocalizedBuild

# $TPB_LocalizedBuild was doing two different jobs, and under Arcade they conflict.
#
#   1. Passed to MSBuild as -p:LocalizedBuild, it activates this repo's own localization ItemGroup
#      in scripts\build\TestPlatform.Localization.targets, which adds culture-specific .resx to
#      @(EmbeddedResource). Arcade's XliffTasks then treats Resources.cs.resx as a neutral file and
#      looks for Resources\xlf\Resources.cs.cs.xlf - a doubled language code - and the build fails.
#      Arcade already produces every satellite from the checked-in .xlf, verified by comparing
#      5,018 resource entries against the shipped assemblies, so this must stay OFF.
#
#   2. Used as a PowerShell guard, it enables the Copy-Loc-Files and Move-Loc-Files calls that
#      relocate extension satellites into the Extensions subfolder. That work is not done by
#      anything else, so this must stay ON. Without it the satellites are packaged at
#      contentFiles/any/netcoreapp2.0/<locale>/ where the shipped package has them at
#      contentFiles/any/netcoreapp2.0/Extensions/<locale>/ - measured against the shipped
#      Microsoft.TestPlatform.CLI 15.9.2 as 54 missing and 50 added paths.
#
# Split into two variables so each can take the value its consumer needs. -noloc still turns both
# off, which is what a caller asking for no localized build means.
$TPB_LocalizedResourceRelocation = $TPB_LocalizedBuild
$TPB_LocalizedBuild = $false

$language = @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant")

# Capture error state in any step globally to modify return code
$Script:ScriptFailed = $false

Import-Module "$($CurrentScriptDir.FullName)\verify-nupkgs.ps1"

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
    & $dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
    Write-Log ".. .. Build: Complete."

    Set-ScriptFailedOnError

    Write-Log ".. .. Build: Source: $TPB_TestAssets_Solution"
    Write-Verbose "$dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild"
    & $dotnetExe build $TPB_TestAssets_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
    Write-Log ".. .. Build: Complete."

    Set-ScriptFailedOnError

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

    Set-ScriptFailedOnError

    # Copy over the Full CLR built datacollector package assemblies to the Core CLR package folder along with testhost
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework $fullDestDir
    
    New-Item -ItemType directory -Path $fullCLRPackageDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force -recurse

    Set-ScriptFailedOnError

    # Publish platform abstractions
    # Arcade puts build output in artifacts\bin\<ProjectName>\<Configuration>\<tfm>, not in
    # src\<Project>\bin\<Configuration>\<tfm>. The structure below the configuration is unchanged -
    # the same net451, netcoreapp1.0, netstandard1.0 and uap10.0 subdirectories - so only the root
    # moves and the copy sites below need no edits.
    $platformAbstraction = Join-Path $env:TP_OUT_DIR "bin\Microsoft.TestPlatform.PlatformAbstractions\$TPB_Configuration"
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
    if($TPB_LocalizedResourceRelocation) {
        Move-Loc-Files $fullCLRPackageDir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
    }

    # Copy Blame Datacollector to Extensions folder.
    $TPB_TargetFrameworkStandard = "netstandard1.5"
    $blameDataCollector = Join-Path $env:TP_OUT_DIR "bin\Microsoft.TestPlatform.Extensions.BlameDataCollector\$TPB_Configuration"
    $blameDataCollectorNetFull = Join-Path $blameDataCollector $TPB_TargetFramework
    $blameDataCollectorNetStandard = Join-Path $blameDataCollector $TPB_TargetFrameworkStandard
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $coreCLRExtensionsDir -Force

    # Copy blame data collector resource dlls
    if($TPB_LocalizedResourceRelocation) {
        Copy-Loc-Files $blameDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
        Copy-Loc-Files $blameDataCollectorNetStandard $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
    }

    # Copy Event Log Datacollector to Extensions folder.
    $eventLogDataCollector = Join-Path $env:TP_OUT_DIR "bin\Microsoft.TestPlatform.Extensions.EventLogCollector\$TPB_Configuration"
    $eventLogDataCollectorNetFull = Join-Path $eventLogDataCollector $TPB_TargetFramework
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $coreCLRExtensionsDir -Force

    # Copy EventLogCollector resource dlls
    if($TPB_LocalizedResourceRelocation) {
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
    if($TPB_LocalizedResourceRelocation) {
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
    $intellitraceSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Intellitrace\15.9.0-preview-20180924-02\tools"
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

    $visualStudioTelemetryDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Telemetry\15.6.815-master284DF69C\lib\net45"
    $visualStudioUtilitiesDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Utilities.Internal\14.0.74-masterCEEA65A3\lib\net45"

    Copy-Item "$visualStudioTelemetryDirectory\Microsoft.VisualStudio.Telemetry.dll" $testPlatformDirectory -Force
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

	}
}

function Publish-PackageInternal($packagename, $framework, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild

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
        #
        # TargetFrameworkRootPath is supplied from a restored package rather than relying on an
        # installed targeting pack. The project sets <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
        # and modern machines no longer carry the 4.6 pack, so msbuild failed with MSB3644. The
        # leftover Reference Assemblies\.NETFramework\v4.6 directory holds 126 files and no
        # mscorlib.dll; the package holds 259 files WITH mscorlib.dll, which is the profile of a
        # real pack. Supplying the reference assemblies is infrastructure - it does not retarget
        # the project and does not change what the VSIX contains. Retargeting would.
        # Same resolution as build-time tooling elsewhere in this script: prefer the package cache,
        # fall back to TP_PACKAGES_DIR. $toolRoot is a local in the two functions that already use
        # it, so it is recomputed here rather than assumed to be in scope.
        $toolRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { $env:TP_PACKAGES_DIR }
        $refAsmRoot = Join-Path $toolRoot 'microsoft.netframework.referenceassemblies.net46\1.0.3\build'
        $msbuildArgs = @("$vsixProjectDir\TestPlatform.csproj", "-p:Configuration=$Configuration")
        if (Test-Path -path (Join-Path $refAsmRoot '.NETFramework\v4.6\mscorlib.dll')) {
            $msbuildArgs += "-p:TargetFrameworkRootPath=$refAsmRoot"
            Write-Host "... .. Create-VsixPackage: using reference assemblies from '$refAsmRoot'."
        }
        else {
            Write-Host "... .. Create-VsixPackage: no restored 4.6 reference assemblies at '$refAsmRoot'; relying on an installed targeting pack. Expect MSB3644 if none is present."
        }
        # The vsix project resolves Microsoft.VSSDK.BuildTools from $(TestPlatformRoot)packages\,
        # the historic <repo>\packages layout, NOT from $toolRoot. TP_PACKAGES_DIR now points at
        # the staged runtime payload, so that folder no longer exists and the project failed with
        # "This project references NuGet package(s) that are missing on this computer."
        # Place the restored package where the 2018 project expects it. <repo>\packages is
        # gitignored, so nothing is committed. This is a THIRD build-time tool: the earlier
        # statement that tooling is exactly two entries was scoped to $toolRoot resolution and
        # missed this one, which resolves through an MSBuild property instead.
        $vssdkVersion = '15.8.3247'
        $vssdkSource = Join-Path $toolRoot "microsoft.vssdk.buildtools\$vssdkVersion"
        $vssdkTarget = Join-Path $env:TP_ROOT_DIR "packages\microsoft.vssdk.buildtools\$vssdkVersion"
        if ((Test-Path -path $vssdkSource) -and -not (Test-Path -path (Join-Path $vssdkTarget 'build\Microsoft.VSSDK.BuildTools.targets'))) {
            Write-Host "... .. Create-VsixPackage: staging Microsoft.VSSDK.BuildTools $vssdkVersion from '$vssdkSource' to '$vssdkTarget'."
            New-Item -ItemType Directory -Force -Path $vssdkTarget | Out-Null
            Copy-Item -Path (Join-Path $vssdkSource '*') -Destination $vssdkTarget -Recurse -Force
        }
        if (-not (Test-Path -path (Join-Path $vssdkTarget 'build\Microsoft.VSSDK.BuildTools.targets'))) {
            Write-Host "... .. Create-VsixPackage: Microsoft.VSSDK.BuildTools $vssdkVersion is not at '$vssdkTarget' and was not found at '$vssdkSource'. The vsix project will report missing NuGet packages."
        }

        Write-Verbose "$msbuildPath\msbuild.exe $($msbuildArgs -join ' ')"
        & $msbuildPath\msbuild.exe @msbuildArgs
        # The exit code of this invocation was never checked, so a FAILED vsix build still printed
        # "Create-VsixPackage: Complete" and the script exited 0. Measured: msbuild reported
        # "Build FAILED" with error MSB3644 and the run still exited 0. Same shape as the missing
        # MSBuild branch below - the VSIX is the artifact VS inserts, so a build that did not
        # produce one must not report success. Deferred rather than thrown so the remaining
        # packaging steps still run.
        Set-ScriptFailedOnError
        $vsixProduced = @(Get-ChildItem -Path $vsixProjectDir -Recurse -Filter '*.vsix' -ErrorAction SilentlyContinue)
        if ($vsixProduced.Count -eq 0) {
            Write-Error ".. Create-VsixPackage: msbuild completed but produced no .vsix under '$vsixProjectDir'. Checking the exit code alone is not enough: assert the artifact exists, because the whole point of this step is the file."
            $Script:ScriptFailed = $true
        }
        else {
            Write-Host "... .. Create-VsixPackage: produced $($vsixProduced.Count) vsix: $(($vsixProduced | ForEach-Object { $_.Name }) -join ', ')."
        }
    }
    else
    {
        # This was a Write-Log, which meant a missing MSBuild SKIPPED VSIX generation silently and
        # the step still reported success with exit 0. Measured: zero .vsix in the null-build
        # reference (10,523 files), zero in artifacts, and the local run printed
        # "Create-VsixPackage: Complete" followed by "Build complete" having produced nothing.
        #
        # The VSIX is the artifact VS inserts - the component pin
        # Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI.vsman resolves to it, and
        # VisualStudio.External.vsmanproj sets PreSignManifest=false, so nothing downstream
        # re-creates or re-signs it. A drop built from a run with no VSIX is an empty insertion:
        # every step green, nothing to insert. Fail instead, so the absence is visible where it
        # happens rather than at the point of consumption.
        # DEFERRED, not thrown. Create-VsixPackage runs BEFORE Create-NugetPackages (see the step
        # list at the end of this script), so throwing here would short-circuit the nine nuspecs
        # and hide whatever the packaging and signing path does next. That is exactly the defect
        # fixed in the servicing delivery guard, where one route's throw short-circuited a later
        # route: record the failure, let the remaining work run, and fail at the end. The script
        # already has that mechanism - $Script:ScriptFailed, checked by the final Exit.
        Write-Error ".. Create-VsixPackage: Cannot generate vsix as msbuild.exe was not found at '$msbuildPath'. Locate-VsInstallPath found no Visual Studio instance satisfying Microsoft.Component.MSBuild and Microsoft.VisualStudio.Component.VSSDK. A build that produces no VSIX cannot be inserted, so this is an error rather than a skip. The remaining packaging steps still run so this build reports everything that is wrong with it, not just the first thing."
        $Script:ScriptFailed = $true
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
    $nuspecFiles = @("TestPlatform.TranslationLayer.nuspec",
                     "TestPlatform.ObjectModel.nuspec",
                     "TestPlatform.TestHost.nuspec",
                     "TestPlatform.CLI.nuspec",
                     "TestPlatform.Build.nuspec",
                     "Microsoft.Net.Test.Sdk.nuspec",
                     "Microsoft.TestPlatform.nuspec",
                     "Microsoft.TestPlatform.Portable.nuspec",
                     "Microsoft.CodeCoverage.nuspec")

    $targetFiles = @("Microsoft.Net.Test.Sdk.targets", "Microsoft.CodeCoverage.targets")
    $propFiles = @("Microsoft.Net.Test.Sdk.props", "Microsoft.CodeCoverage.props")
    # Nuget pack analysis emits warnings if binaries are packaged as content. It is intentional for the below packages.
    $skipAnalysis = @("TestPlatform.CLI.nuspec")
    foreach ($file in $nuspecFiles + $targetFiles + $propFiles) {
        Copy-Item $tpNuspecDir\$file $stagingDir -Force
    }

    # Copy empty and third patry notice file
    Copy-Item $tpNuspecDir\"_._" $stagingDir -Force
    Copy-Item $tpNuspecDir\..\"ThirdPartyNotices.txt" $stagingDir -Force

    #Copy Uap target, & props
    $testhostNS1_4PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkNS1_4")
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.props" $testhostNS1_4PackageDir\Microsoft.TestPlatform.TestHost.props -Force
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.targets" $testhostNS1_4PackageDir\Microsoft.TestPlatform.TestHost.targets -Force

    # Call nuget pack on these components.
    # nuget.exe and vswhere are build-time TOOLING, not payload. They do not ship, so they come from
    # the NuGet package cache rather than from TP_PACKAGES_DIR, which now names the staged payload
    # extracted from the shipped artifacts. Fall back to TP_PACKAGES_DIR so a standalone run against
    # the historic <repo>\packages layout still works.
    $toolRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { $env:TP_PACKAGES_DIR }
    $nugetExe = Join-Path $toolRoot -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    # Pass Newtonsoft.Json version to nuget pack to keep the version consistent across all nuget packages.
    $JsonNetVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.JsonNetVersion

    # package them from stagingDir
    #
    # The arguments are built as an array and splatted rather than appended as a string, because
    # passing a variable that holds an empty string to a native command is host-dependent:
    # Windows PowerShell 5.1 drops it, PowerShell 7 passes it as a literal "". This loop used to
    # append $additionalArgs, which is empty for every package except the one in $skipAnalysis.
    # Under PowerShell 7 that stray empty argument made nuget reject its own command line for the
    # other eight, so one package of nine was produced. An array omits the entry entirely, which
    # behaves identically on both hosts and does not depend on which one runs the script.
    foreach ($file in $nuspecFiles) {
        $packArgs = @(
            "pack", (Join-Path $stagingDir $file),
            "-OutputDirectory", $packageOutputDir,
            "-Version", $TPB_Version,
            "-Properties", "Version=$TPB_Version;JsonNetVersion=$JsonNetVersion;Runtime=$TPB_TargetRuntime;NetCoreTargetFramework=$TPB_TargetFrameworkCore20"
        )

        if ($skipAnalysis -contains $file) {
            $packArgs += "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe $($packArgs -join ' ')"
        & $nugetExe @packArgs

        Set-ScriptFailedOnError
    }

    # Verifies that every nuspec that went in came out as a package.
    #
    # nuget.exe 3.4.3 prints "pack: invalid arguments." with its usage banner and then returns exit
    # code 0, so Set-ScriptFailedOnError above cannot see a pack that produced nothing. The exit
    # code of a tool of this vintage is not a contract; the product is. Verify-Nuget-Packages below
    # cannot cover this either, because it enumerates the packages that exist and a package that was
    # never produced is outside the space it searches.
    Verify-Nuget-Packages-Produced $stagingDir $nuspecFiles $packageOutputDir $TPB_Version

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

    $traceDataCollectorOutDir = Join-Path $env:TP_OUT_DIR "bin\Microsoft.VisualStudio.TraceDataCollector\$TPB_Configuration\$TPB_TargetFrameworkNS2_0"

    Copy-Item $traceDataCollectorOutDir\Microsoft.VisualStudio.TraceDataCollector.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorOutDir\Microsoft.VisualStudio.TraceDataCollector.pdb $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorOutDir\CodeCoverage $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $traceDataCollectorOutDir\Shim $microsoftCodeCoveragePackageDir -Force -Recurse

    # Copy TraceDataCollector resource dlls
    if($TPB_LocalizedResourceRelocation) {
        Copy-Loc-Files $traceDataCollectorOutDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
        Copy-Loc-Files $traceDataCollectorOutDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
    }
}

function Copy-PackageItems($packageName)
{
    # Packages published separately are copied into their own artifacts directory.
    # Arcade builds to artifacts\bin\<PackageName>\<Configuration>\<tfm>, so that is the source;
    # the destination layout artifacts\<Configuration>\<PackageName>\<tfm> is what the nuspecs read
    # through basePath and is unchanged.
    $binariesDirectory = [System.IO.Path]::Combine($env:TP_OUT_DIR, "bin", "$packageName", "$TPB_Configuration")
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

    Set-ScriptFailedOnError

    Write-Log ".. Update-LocalizedResources: Complete. {$(Get-ElapsedTime($timer))}"
}

#
# Helper functions
#
function Get-DotNetPath
{
    # Arcade installs the SDK to .dotnet\ at the repo root and pins its version in global.json.
    # Install-DotNetCli in this script installs to tools\dotnet\ instead, and the pipeline does not
    # run that step - it builds through Arcade. Prefer Arcade's copy so the packaging steps use the
    # same SDK that produced the binaries they are packaging, and fall back to the historic location
    # so a developer running this script standalone is unaffected.
    $dotnetPath = Join-Path $env:TP_ROOT_DIR ".dotnet\dotnet.exe"
    if (-not (Test-Path $dotnetPath)) {
        $dotnetPath = Join-Path $env:TP_TOOLS_DIR "dotnet\dotnet.exe"
    }

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

    if([string]::IsNullOrEmpty($vsInstallPath))
    {
        return $null
    }

    $vsInstallPath = Resolve-Path -path $vsInstallPath
    $msbuildPath = Join-Path -path $vsInstallPath -childPath "MSBuild\$env:MSBUILD_VERSION\Bin"

    # MSBUILD_VERSION is pinned to "15.0", which is the VS 2017 layout. Visual Studio 2019 and later
    # install MSBuild under "Current" instead, so the pinned path does not exist on a modern agent
    # and the build failed with "The term '...\MSBuild\15.0\Bin\msbuild.exe' is not recognized".
    # Measured on the located instance: MSBuild\Current\Bin\msbuild.exe PRESENT,
    # MSBuild\15.0 absent. Prefer the pin when it is really there, so a genuine VS 2017 machine
    # keeps the original behaviour, and fall back to Current otherwise. Report which one was taken
    # rather than silently substituting - a substituted toolchain is exactly the kind of change
    # that must not be invisible.
    if (-not (Test-Path -path (Join-Path $msbuildPath 'msbuild.exe'))) {
        $currentPath = Join-Path -path $vsInstallPath -childPath "MSBuild\Current\Bin"
        if (Test-Path -path (Join-Path $currentPath 'msbuild.exe')) {
            Write-Host "... .. Locate-MSBuildPath: MSBuild $env:MSBUILD_VERSION is not present at '$msbuildPath'. Using '$currentPath' instead."
            $msbuildPath = $currentPath
        }
        else {
            Write-Host "... .. Locate-MSBuildPath: neither MSBuild $env:MSBUILD_VERSION nor Current holds msbuild.exe under '$vsInstallPath'. Subdirectories present: $((Get-ChildItem (Join-Path $vsInstallPath 'MSBuild') -Directory -ErrorAction SilentlyContinue | ForEach-Object { $_.Name }) -join ', ')."
        }
    }

    Write-Verbose "msbuildPath is : $msbuildPath"
    return $msbuildPath
}

function Locate-VsInstallPath
{
   # vswhere is build-time TOOLING, not payload. It does not ship, so it is not present in
   # TP_PACKAGES_DIR, which now names the staged payload extracted from the shipped artifacts
   # rather than a restore folder. Resolve it from the package cache and fall back to
   # TP_PACKAGES_DIR so a standalone run against the historic <repo>\packages layout still works.
   # Same distinction as nuget.exe in Create-NugetPackages: rebuilt / carried / build-time tooling,
   # and the third must never be sourced from the second's tree.
   $toolRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { $env:TP_PACKAGES_DIR }
   $vswhere = Join-Path -path $toolRoot -ChildPath "vswhere\$env:VSWHERE_VERSION\tools\vswhere.exe"
   if (!(Test-Path -path $vswhere)) {
       throw "Unable to locate vswhere in path '$vswhere'."
   }

   Write-Verbose "Using '$vswhere' to locate VS installation path."

   $requiredPackageIds = @("Microsoft.Component.MSBuild", "Microsoft.VisualStudio.Component.VSSDK")
   # Microsoft.Net.Component.4.6.TargetingPack was in this list and has been REMOVED. A selector
   # should select. Requiring it made vswhere return nothing on every current machine and on the
   # build agent, and the resulting message named all three component ids without saying which one
   # was missing or what to do about it. 16.11 measured the underlying cause: the
   # Reference Assemblies .NETFramework\v4.6 directory still EXISTS but holds 126 files and no
   # mscorlib.dll, while v4.6.2, v4.7.2 and v4.8 hold 258-260 files with mscorlib.dll. The 4.6
   # targeting pack really is unavailable; testing that directory for EXISTENCE rather than for
   # CONTENTS is what made the component id look like a false positive.
   #
   # Dropping it lets vswhere find the instance and moves the failure to reference resolution,
   # where MSBuild says MSB3644 and names the missing pack and the remedy. Both stop the build at
   # the same point, so nothing is lost by preferring the message that names the missing thing.
   Write-Verbose "VSInstallation requirements : $requiredPackageIds"

   Try
   {
       Write-Verbose "VSWhere command line: $vswhere -latest -prerelease -products * -requires $requiredPackageIds -property installationPath"
       if ($TPB_CIBuild) {
           # -prerelease is required on modern CI. The 2018 code deliberately omitted it here so
           # that CI used a stable VS while dev boxes could use previews. That premise no longer
           # holds: the agent image for this definition is windows.vs2026preview.scout.amd64, and
           # vswhere reports every preview install with isPrerelease=True, so a query that omits
           # -prerelease returns NOTHING on a preview-only image. Measured on a preview-only box:
           # no -requires and no -prerelease returns 0 instances; no -requires WITH -prerelease
           # returns 4. This changes whether the VSIX can be produced at all, not what it contains.
           $vsInstallPath = & $vswhere -latest -prerelease -products * -requires $requiredPackageIds -property installationPath
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

   # When the query comes back empty, report WHICH requirement failed rather than only that the
   # whole query matched nothing. A single bit ("no instance") costs one CI run per candidate
   # cause; this names the culprit in the run that first hits it. Each component is counted
   # separately, and the unfiltered population is printed beside them so a zero everywhere is
   # distinguishable from a zero on one component.
   # NOTE: Write-Host, not Write-Log. Write-Log is a Write-Output wrapper, so it emits to the
   # PIPELINE - and this function returns its value by pipeline. Using Write-Log here appended the
   # diagnostic lines to $vsInstallPath, and the caller then failed with
   # "Cannot find drive. A drive with the name '... .. Locate-VsInstallPath' does not exist."
   # Caught by running this locally; in CI it would have looked like a path defect, not a
   # logging defect. Any diagnostic added inside a value-returning function in this script has
   # the same hazard.
   if ([string]::IsNullOrEmpty($vsInstallPath)) {
       $anyPrerelease = @(& $vswhere -all -prerelease -products * -property installationPath 2>$null).Count
       $anyStable     = @(& $vswhere -all -products * -property installationPath 2>$null).Count
       Write-Host "... .. Locate-VsInstallPath: no instance matched. VS instances visible: $anyPrerelease including prerelease, $anyStable excluding prerelease."
       foreach ($component in $requiredPackageIds) {
           $matched = @(& $vswhere -all -prerelease -products * -requires $component -property installationPath 2>$null).Count
           Write-Host "... .. Locate-VsInstallPath: $component matched $matched of $anyPrerelease instance(s)."
       }
   }

   return $vsInstallPath
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
Write-Log "Build steps: $($Steps -join ', ')"
if ($Steps -contains "InstallDotnet")      { Install-DotNetCli }
if ($Steps -contains "Restore")            { Restore-Package }
if ($Steps -contains "UpdateLocalization") { Update-LocalizedResources }
if ($Steps -contains "Build")              { Invoke-Build }
if ($Steps -contains "Publish")            { Publish-Package }
if ($Steps -contains "PublishTests")       { Publish-Tests }
if ($Steps -contains "Vsix")               { Create-VsixPackage }
if ($Steps -contains "NugetPackages")      { Create-NugetPackages }
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
