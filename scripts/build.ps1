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
    [Switch] $DisableLocalizedBuild,

    [Parameter(Mandatory=$false)]
    [Alias("ci")]
    [Switch] $CIBuild,

    [Parameter(Mandatory=$false)]
    [Alias("pt")]
    [Switch] $PublishTestArtifacts,

    # Build specific projects
    [Parameter(Mandatory=$false)]
    [Alias("p")]
    [System.String[]] $ProjectNamePatterns = @(),

    [Alias("f")]
    [Switch] $Force, 

    [Alias("s")]
    [String[]] $Steps = @("InstallDotnet", "Restore", "UpdateLocalization", "Build", "Publish", "PrepareAcceptanceTests")
)

. $PSScriptRoot\common.lib.ps1

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
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_Solution = "TestPlatform.sln"
$TPB_TestAssets_Solution = Join-Path $env:TP_ROOT_DIR "test\TestAssets\TestAssets.sln"
$TPB_TestAssets_CILAssets = Join-Path $env:TP_ROOT_DIR "test\TestAssets\CILProject\CILProject.proj"
$TPB_TargetFramework45 = "net45"
$TPB_TargetFramework451 = "net451"
$TPB_TargetFramework472 = "net472"
$TPB_TargetFrameworkCore10 = "netcoreapp1.0"
$TPB_TargetFrameworkCore20 = "netcoreapp2.1"
$TPB_TargetFrameworkUap100 = "uap10.0"
$TPB_TargetFrameworkNS10 = "netstandard1.0"
$TPB_TargetFrameworkNS13 = "netstandard1.3"
$TPB_TargetFrameworkNS20 = "netstandard2.0"
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

Import-Module -Name "$($CurrentScriptDir.FullName)\verify-nupkgs.ps1" -Scope Local

# Update the version in the dependencies props to be the TPB_version version, this is not ideal but because changing how this is resolved would 
# mean that we need to change the whole build process this is a solution with the least amount of impact, that does not require us to keep track of 
# the version in multiple places
$dependenciesPath = "$env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props"
$dependencies = Get-Content -Raw -Encoding UTF8 $dependenciesPath
$updatedDependencies = $dependencies -replace "<NETTestSdkVersion>.*?</NETTestSdkVersion>", "<NETTestSdkVersion>$TPB_Version</NETTestSdkVersion>"
$updatedDependencies | Set-Content -Encoding UTF8 $dependenciesPath -NoNewline

function Invoke-Build
{
    $timer = Start-Timer
    Write-Log "Invoke-Build: Start build."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Build: Source: $TPB_Solution"
    Write-Verbose "$dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:TestPlatform.binlog
    Write-Log ".. .. Build: Complete."

    Write-Log ".. .. Build: Source: $TPB_TestAssets_CILAssets"
    Write-Verbose "$dotnetExe build $TPB_TestAssets_CILAssets --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild"
    & $dotnetExe build $TPB_TestAssets_CILAssets --configuration $TPB_Configuration -v:minimal -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:"$($env:TP_ROOT_DIR)\CILAssets.binlog"
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
    New-Item -ItemType Directory -Force $tpPackagesDestination | Out-Null
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
    $buildArtifactsPath = "$env:TP_ROOT_DIR\src\Microsoft.TestPlatform.Build\bin\$TPB_Configuration\$TPB_TargetFrameworkNS20\*"
    Copy-Item $buildArtifactsPath $dotnetTestArtifactsSdkPath -Force
}

function Publish-Package
{
    $timer = Start-Timer
    Write-Log "Publish-Package: Started."
    $dotnetExe = Get-DotNetPath
    $fullCLRPackage451Dir = Get-FullCLRPackageDirectory
    $fullCLRPackage45Dir = Get-FullCLRPackageDirectory45
    $uap100PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkUap100");
    $net45PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\net45");
    $netstandard10PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkNS10");
    $netstandard13PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkNS13");
    $netstandard20PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkNS20");
    $coreCLR20PackageDir = Get-CoreCLR20PackageDirectory
    $coreCLR10PackageDir = Get-CoreCLR10PackageDirectory
    $coreCLR20TestHostPackageDir = Get-CoreCLR20TestHostPackageDirectory
    $packageProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "package\package.csproj"
    $testHostProject = Join-Path $env:TP_ROOT_DIR "src\testhost\testhost.csproj"
    $testHostx86Project = Join-Path $env:TP_ROOT_DIR "src\testhost.x86\testhost.x86.csproj"

    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework451\$TPB_TargetRuntime")
    $testhostCore20PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20")
    $testhostCore20PackageX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCore20PackageX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    $testhostCore20PackageTempX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCore20PackageTempX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    
    $testhostCore10PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10")
    $testhostCore10PackageX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X64_Runtime")
    $testhostCore10PackageX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X86_Runtime")
    $testhostCore10PackageTempX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X64_Runtime")
    $testhostCore10PackageTempX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X86_Runtime")
    
    $testhostUapPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkUap100")
    $vstestConsoleProject = Join-Path $env:TP_ROOT_DIR "src\vstest.console\vstest.console.csproj"
    $settingsMigratorProject = Join-Path $env:TP_ROOT_DIR "src\SettingsMigrator\SettingsMigrator.csproj"
    $dataCollectorProject = Join-Path $env:TP_ROOT_DIR "src\datacollector\datacollector.csproj"

    Write-Log "Package: Publish src\package\package\package.csproj"
    Publish-PackageInternal $packageProject $TPB_TargetFramework451 $fullCLRPackage451Dir
    Publish-PackageInternal $packageProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    
    ################################################################################
    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.
    Write-Log "Package: Publish src\vstest.console\vstest.console.csproj"
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFramework451 $fullCLRPackage451Dir
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir
    
    Write-Log "Package: Publish src\SettingsMigrator\SettingsMigrator.csproj"
    Publish-PackageInternal $settingsMigratorProject $TPB_TargetFramework451 $fullCLRPackage451Dir

    Write-Log "Package: Publish src\datacollector\datacollector.csproj"
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework472 $fullCLRPackage451Dir
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    ################################################################################
    # Publish testhost
    Write-Log "Package: Publish testhost\testhost.csproj"
    Publish-PackageInternal $testHostProject $TPB_TargetFramework451 $testhostFullPackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore20 $testhostCore20PackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore10 $testhostCore10PackageDir
    Publish-PackageInternal $testHostProject $TPB_TargetFrameworkCore20 $testhostUapPackageDir
    Publish-PackageWithRuntimeInternal $testHostProject $TPB_TargetFrameworkCore20 $TPB_X64_Runtime false $testhostCore20PackageTempX64Dir
    Publish-PackageWithRuntimeInternal $testHostProject $TPB_TargetFrameworkCore10 $TPB_X64_Runtime true $testhostCore10PackageTempX64Dir
    
    Write-Log "Package: Publish testhost.x86\testhost.x86.csproj"
    Publish-PackageInternal $testHostx86Project $TPB_TargetFramework451 $testhostFullPackageDir
    Publish-PackageWithRuntimeInternal $testHostx86Project $TPB_TargetFrameworkCore20 $TPB_X86_Runtime false $testhostCore20PackageTempX86Dir
    Publish-PackageWithRuntimeInternal $testHostx86Project $TPB_TargetFrameworkCore10 $TPB_X86_Runtime true $testhostCore10PackageTempX86Dir
    
    # Copy the .NET multitarget testhost exes to destination folder (except for net451 which is the default)
    foreach ($tfm in "net452;net46;net461;net462;net47;net471;net472;net48" -split ";") {
        Copy-Item "$(Split-Path $testHostProject)\bin\$TPB_Configuration\$tfm\$TPB_X64_Runtime\testhost.$tfm.exe" $testhostFullPackageDir\testhost.$tfm.exe -Force 
        Copy-Item "$(Split-Path $testHostProject)\bin\$TPB_Configuration\$tfm\$TPB_X64_Runtime\testhost.$tfm.pdb" $testhostFullPackageDir\testhost.$tfm.pdb -Force 
        Copy-Item "$(Split-Path $testHostProject)\bin\$TPB_Configuration\$tfm\$TPB_X64_Runtime\testhost.$tfm.exe.config" $testhostFullPackageDir\testhost.$tfm.exe.config -Force 
    }

    # Copy the .NET multitarget testhost.x86 exes to destination folder (except for net451 which is the default)
    foreach ($tfm in "net452;net46;net461;net462;net47;net471;net472;net48" -split ";") {
        Copy-Item "$(Split-Path $testHostx86Project)\bin\$TPB_Configuration\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.exe" $testhostFullPackageDir\testhost.$tfm.x86.exe -Force 
        Copy-Item "$(Split-Path $testHostx86Project)\bin\$TPB_Configuration\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.pdb" $testhostFullPackageDir\testhost.$tfm.x86.pdb -Force 
        Copy-Item "$(Split-Path $testHostx86Project)\bin\$TPB_Configuration\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.exe.config" $testhostFullPackageDir\testhost.$tfm.x86.exe.config -Force 
    }

    # Copy the .NET core x86 and x64 testhost exes from tempPublish to required folder
    New-Item -ItemType directory -Path $testhostCore20PackageX64Dir -Force | Out-Null
    Copy-Item $testhostCore20PackageTempX64Dir\testhost* $testhostCore20PackageX64Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore20PackageX64Dir -Force
    
    New-Item -ItemType directory -Path $testhostCore20PackageX86Dir -Force | Out-Null
    Copy-Item $testhostCore20PackageTempX86Dir\testhost.x86* $testhostCore20PackageX86Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX86Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore20PackageX86Dir -Force
 
    New-Item -ItemType directory -Path $testhostCore10PackageX64Dir -Force | Out-Null
    Copy-Item $testhostCore10PackageTempX64Dir\testhost* $testhostCore10PackageX64Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore10PackageX64Dir -Force

    New-Item -ItemType directory -Path $testhostCore10PackageX86Dir -Force | Out-Null
    Copy-Item $testhostCore10PackageTempX86Dir\testhost.x86* $testhostCore10PackageX86Dir -Force -Recurse
    Copy-Item $testhostCore10PackageTempX86Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore10PackageX86Dir -Force
    
    # Copy over the Full CLR built testhost package assemblies to the Core CLR and Full CLR package folder.
    $coreCLRFull_Dir = "TestHost"
    $fullDestDir = Join-Path $coreCLR20PackageDir $coreCLRFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force -Recurse

    Set-ScriptFailedOnError

    # Copy over the Full CLR built datacollector package assemblies to the Core CLR package folder along with testhost
    Publish-PackageInternal $dataCollectorProject $TPB_TargetFramework472 $fullDestDir
    
    New-Item -ItemType directory -Path $fullCLRPackage451Dir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullCLRPackage451Dir -Force -Recurse

    Set-ScriptFailedOnError

    ################################################################################
    # Publish Microsoft.TestPlatform.PlatformAbstractions
    Copy-Bulk -root (Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.PlatformAbstractions\bin\$TPB_Configuration") `
              -files @{
                $TPB_TargetFramework45     = $fullCLRPackage45Dir          # net45
                $TPB_TargetFramework451    = $fullCLRPackage451Dir         # net451
                $TPB_TargetFrameworkCore20 = $coreCLR20PackageDir          # netcoreapp2.1
                $TPB_TargetFrameworkNS10   = $netstandard10PackageDir      # netstandard1_0
                $TPB_TargetFrameworkNS13   = $netstandard13PackageDir      # netstandard1_3
                $TPB_TargetFrameworkNS20   = $netstandard20PackageDir      # netstandard2_0
                $TPB_TargetFrameworkUap100 = $testhostUapPackageDir        # uap10.0
              }

    ################################################################################
    # Publish Microsoft.TestPlatform.CoreUtilities
    Copy-Bulk -root (Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.CoreUtilities\bin\$TPB_Configuration") `
              -files @{
                $TPB_TargetFramework45      = $fullCLRPackage45Dir           # net45
                $TPB_TargetFramework451     = $fullCLRPackage451Dir          # net451
                $TPB_TargetFrameworkNS10    = $netstandard10PackageDir       # netstandard1_0
                $TPB_TargetFrameworkNS13    = $netstandard13PackageDir       # netstandard1_3
                $TPB_TargetFrameworkNS20    = $netstandard20PackageDir       # netstandard2_0
                $TPB_TargetFrameworkUap100  = $testhostUapPackageDir         # uap10.0
              }

    ################################################################################
    # Publish Microsoft.TestPlatform.ObjectModel
    Copy-Bulk -root (Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.ObjectModel\bin\$TPB_Configuration") `
              -files @{
                $TPB_TargetFramework45      = $fullCLRPackage45Dir           # net45
                $TPB_TargetFramework451     = $fullCLRPackage451Dir          # net451
                $TPB_TargetFrameworkCore10  = $coreCLR10PackageDir           # netcoreapp1.0
                $TPB_TargetFrameworkCore20  = $coreCLR20PackageDir           # netcoreapp2.1
                $TPB_TargetFrameworkNS10    = $netstandard10PackageDir       # netstandard1_0
                $TPB_TargetFrameworkNS13    = $netstandard13PackageDir       # netstandard1_3
                $TPB_TargetFrameworkNS20    = $netstandard20PackageDir       # netstandard2_0
                $TPB_TargetFrameworkUap100  = $testhostUapPackageDir         # uap10.0
              }

    ################################################################################
    # Publish Microsoft.TestPlatform.AdapterUtilities
    Copy-Bulk -root (Join-Path $env:TP_ROOT_DIR "src\Microsoft.TestPlatform.AdapterUtilities\bin\$TPB_Configuration") `
            -files @{
              # "net20"                     = $net20PackageDir               # net20
                "net45/any"                 = $net45PackageDir               # $net4
                $TPB_TargetFrameworkNS10    = $netstandard10PackageDir       # netstandard1_0
                $TPB_TargetFrameworkNS20    = $netstandard20PackageDir       # netstandard2_0
                $TPB_TargetFrameworkUap100  = $uap100PackageDir              # uap10.0
            }

    ################################################################################
    # Publish msdia
    $testPlatformExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformExternalsVersion
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCore20PackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCore10PackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostFullPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostUapPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $coreCLR20TestHostPackageDir -Force

    $microsoftInternalDiaInterop = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia.Interop\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $microsoftInternalDiaInterop\* $coreCLR20TestHostPackageDir -Force

    # Copy over the logger assemblies to the Extensions folder.
    $extensions_Dir = "Extensions"
    $fullCLRExtensionsDir = Join-Path $fullCLRPackage451Dir $extensions_Dir
    $coreCLRExtensionsDir = Join-Path $coreCLR20PackageDir $extensions_Dir

    # Create an extensions directory.
    New-Item -ItemType directory -Path $fullCLRExtensionsDir -Force | Out-Null
    New-Item -ItemType directory -Path $coreCLRExtensionsDir -Force | Out-Null

    # If there are some dependencies for the logger assemblies, those need to be moved too.
    # Ideally we should just be publishing the loggers to the Extensions folder.
    $loggers = @(
        "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.pdb", 
        "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.dll", "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.pdb"
    )

    foreach($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move logger resource dlls
    if($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackage451Dir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $fullCLRPackage451Dir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.resources.dll"
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
    $eventLogDataCollectorNetFull = Join-Path $eventLogDataCollector $TPB_TargetFramework451
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $coreCLRExtensionsDir -Force

    # Copy EventLogCollector resource dlls
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $eventLogDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
        Copy-Loc-Files $eventLogDataCollectorNetFull $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
    }

    # Copy Microsoft.VisualStudio.Coverage.IO dlls 
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.CodeCoverageExternalsVersion
    $codeCoverageIOPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.coverage.io\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkStandard"
    Copy-Item $codeCoverageIOPackagesDir\Microsoft.VisualStudio.Coverage.IO.dll $coreCLR20PackageDir -Force
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $codeCoverageIOPackagesDir $coreCLR20PackageDir "Microsoft.VisualStudio.Coverage.IO.resources.dll"
    }

    # If there are some dependencies for the TestHostRuntimeProvider assemblies, those need to be moved too.
    $runtimeproviders = @("Microsoft.TestPlatform.TestHostRuntimeProvider.dll", "Microsoft.TestPlatform.TestHostRuntimeProvider.pdb")
    foreach($file in $runtimeproviders) {
        Write-Verbose "Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force
        
        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move TestHostRuntimeProvider resource dlls
    if ($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackage451Dir $fullCLRExtensionsDir "Microsoft.TestPlatform.TestHostRuntimeProvider.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.TestPlatform.TestHostRuntimeProvider.resources.dll"
    }

    # Copy dependency of Microsoft.TestPlatform.TestHostRuntimeProvider
    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\9.0.1\lib\net45\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $fullCLRPackage451Dir -Force"
    Copy-Item $newtonsoft $fullCLRPackage451Dir -Force

    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $coreCLR20PackageDir -Force"
    Copy-Item $newtonsoft $coreCLR20PackageDir -Force

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    Copy-PackageItems "Microsoft.TestPlatform.Build"

    # Copy IntelliTrace components.
    $testPlatformExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformExternalsVersion
    $intellitraceSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Intellitrace\$testPlatformExternalsVersion\tools\net451"
    $intellitraceTargetDirectory = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Intellitrace"

    if (-not (Test-Path $intellitraceTargetDirectory)) {
        New-Item $intellitraceTargetDirectory -Type Directory -Force | Out-Null
    }

    Copy-Item -Recurse $intellitraceSourceDirectory\* $intellitraceTargetDirectory -Force

    # Copy IntelliTrace Extensions components.
    $intellitraceExtensionsSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Intellitrace.Extensions\$testPlatformExternalsVersion\tools\net451"

    if (-not (Test-Path $intellitraceExtensionsSourceDirectory)) {
        New-Item $intellitraceExtensionsSourceDirectory -Type Directory -Force | Out-Null
    }

    Copy-Item -Recurse $intellitraceExtensionsSourceDirectory\* $intellitraceTargetDirectory -Force

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
        $fullCLRTestDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework451"
        $fullCLRPerfTestAssetDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework451\TestAssets\PerfAssets"
    
        $mstest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "MSTestAdapterPerfTestProject"
        $mstest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\MSTestAdapterPerfTestProject"
        Publish-PackageInternal $mstest10kPerfProject $TPB_TargetFramework451 $mstest10kPerfProjectDir

        $nunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "NUnitAdapterPerfTestProject"
        $nunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\NUnitAdapterPerfTestProject"
        Publish-PackageInternal $nunittest10kPerfProject $TPB_TargetFramework451 $nunittest10kPerfProjectDir

        $xunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "XUnitAdapterPerfTestProject"
        $xunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\XUnitAdapterPerfTestProject"
        Publish-PackageInternal $xunittest10kPerfProject $TPB_TargetFramework451 $xunittest10kPerfProjectDir

        $testPerfProject = Join-Path $env:TP_ROOT_DIR "test\Microsoft.TestPlatform.PerformanceTests"
        Publish-PackageInternal $testPerfProject $TPB_TargetFramework451 $fullCLRTestDir
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
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.CodeCoverageExternalsVersion
    $interopExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.InteropExternalsVersion

    # Copy Microsoft.VisualStudio.TraceDataCollector to Extensions
    $traceDataCollectorPackageDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.TraceDataCollector\$codeCoverageExternalsVersion\lib\$TPB_TargetFramework472"
    Copy-Item $traceDataCollectorPackageDirectory\Microsoft.VisualStudio.TraceDataCollector.dll $extensionsPackageDir -Force
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $traceDataCollectorPackageDirectory $extensionsPackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
    }
	
	# Copy Microsoft.VisualStudio.Core to Extensions
    $codeCoverageCorePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.coverage.core\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    Copy-Item $codeCoverageCorePackagesDir\Microsoft.VisualStudio.Coverage.Core.dll $extensionsPackageDir -Force

    # Copy Microsoft.VisualStudio.Interprocess to Extensions
    $codeCoverageInterprocessPackageDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Coverage.Interprocess\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    Copy-Item $codeCoverageInterprocessPackageDirectory\Microsoft.VisualStudio.Coverage.Interprocess.dll $extensionsPackageDir -Force

    # Copy Microsoft.VisualStudio.Instrumentation to Extensions
    $codeCoverageInstrumentationPackageDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Coverage.Instrumentation\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    Copy-Item $codeCoverageInstrumentationPackageDirectory\Microsoft.VisualStudio.Coverage.Instrumentation.dll $extensionsPackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackageDirectory\Mono.Cecil.dll $extensionsPackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackageDirectory\Mono.Cecil.Pdb.dll $extensionsPackageDir -Force

    # Copy Microsoft.VisualStudio.IO to root
    $codeCoverageIOPackageDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Coverage.IO\$codeCoverageExternalsVersion\lib\$TPB_TargetFramework451"
    Copy-Item $codeCoverageIOPackageDirectory\Microsoft.VisualStudio.Coverage.IO.dll $packageDir -Force
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $codeCoverageIOPackageDirectory $packageDir "Microsoft.VisualStudio.Coverage.IO.resources.dll"
    }

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Microsoft.VisualStudio.ArchitectureTools.PEReader to Extensions
    Copy-Item $legacyDir\Microsoft.VisualStudio.ArchitectureTools.PEReader.dll $extensionsPackageDir -Force

    # Copy QtAgent Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Legacy data collectors Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.QualityTools.DataCollectors\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy CUIT Related depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.CUIT\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy Interop depedencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.Interop\$interopExternalsVersion\lib\net45"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy COM Components and their manifests over
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force

    # Copy Microsoft.Internal.Dia.Interop
    $internalDiaInterop = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia.Interop\$testPlatformExternalsVersion\tools\net451"
    Copy-Item -Recurse $internalDiaInterop\* $packageDir -Force

    # Copy COM Components and their manifests over to Extensions Test Impact directory
    $comComponentsDirectoryTIA = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformExternalsVersion\tools\net451"
    if (-not (Test-Path $testImpactComComponentsDir)) {
        New-Item $testImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $testImpactComComponentsDir -Force

    if (-not (Test-Path $legacyTestImpactComComponentsDir)) {
        New-Item $legacyTestImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $legacyTestImpactComComponentsDir -Force
    
    Copy-Item (Join-Path $env:TP_PACKAGE_PROJ_DIR "ThirdPartyNotices.txt") $packageDir -Force

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
    
    Copy-Item (Join-Path $env:TP_PACKAGE_PROJ_DIR "Icon.png") $stagingDir -Force


    if (-not (Test-Path $packageOutputDir)) {
        New-Item $packageOutputDir -type directory -Force
    }

    $tpNuspecDir = Join-Path $env:TP_PACKAGE_PROJ_DIR "nuspec"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @(
        "Microsoft.CodeCoverage.nuspec",
        "Microsoft.NET.Test.Sdk.nuspec",
        "Microsoft.TestPlatform.AdapterUtilities.nuspec",
        "Microsoft.TestPlatform.nuspec",
        "Microsoft.TestPlatform.Portable.nuspec",
        "TestPlatform.Build.nuspec",
        "TestPlatform.CLI.nuspec",
        "TestPlatform.Extensions.TrxLogger.nuspec",
        "TestPlatform.ObjectModel.nuspec",
        "TestPlatform.TestHost.nuspec",
        "TestPlatform.TranslationLayer.nuspec"
    )

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
    Copy-Item $tpNuspecDir\..\"ThirdPartyNoticesCodeCoverage.txt" $stagingDir -Force

    # Copy licenses folder
    Copy-Item (Join-Path $env:TP_PACKAGE_PROJ_DIR "licenses") $stagingDir -Force -Recurse

    # Copy Uap target, & props
    $testhostUapPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkUap100")
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.props" $testhostUapPackageDir\Microsoft.TestPlatform.TestHost.props -Force
    Copy-Item $tpNuspecDir\uap\"Microsoft.TestPlatform.TestHost.Uap.targets" $testhostUapPackageDir\Microsoft.TestPlatform.TestHost.targets -Force
    
    $testhostCore20PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20")
    Copy-Item $tpNuspecDir\"Microsoft.TestPlatform.TestHost.NetCore.props" $testhostCore20PackageDir\Microsoft.TestPlatform.TestHost.props -Force
    
    $testhostCore10PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10")
    Copy-Item $tpNuspecDir\"Microsoft.TestPlatform.TestHost.NetCore.props" $testhostCore10PackageDir\Microsoft.TestPlatform.TestHost.props -Force
        
    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    # Pass Newtonsoft.Json version to nuget pack to keep the version consistent across all nuget packages.
    $JsonNetVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.JsonNetVersion

    # Additional external dependency folders
    $microsoftFakesVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.MicrosoftFakesVersion
    $FakesPackageDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.QualityTools.Testing.Fakes.TestRunnerHarness\$microsoftFakesVersion\contentFiles"

    # package them from stagingDir
    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version $additionalArgs"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version`;JsonNetVersion=$JsonNetVersion`;Runtime=$TPB_TargetRuntime`;NetCoreTargetFramework=$TPB_TargetFrameworkCore20`;FakesPackageDir=$FakesPackageDir`;NetStandard10Framework=$TPB_TargetFrameworkNS10`;NetStandard13Framework=$TPB_TargetFrameworkNS13`;NetStandard20Framework=$TPB_TargetFrameworkNS20`;Uap10Framework=$testhostUapPackageDir`;BranchName=$TPB_BRANCH`;CommitId=$TPB_COMMIT $additionalArgs

        Set-ScriptFailedOnError
    }

    # Verifies that expected number of files gets shipped in nuget packages.
    # Few nuspec uses wildcard characters.
    Verify-Nuget-Packages $packageOutputDir $TPB_Version

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-CodeCoverage-Package-Artifacts
{
    # Copy TraceDataCollector to Microsoft.CodeCoverage folder.
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.CodeCoverageExternalsVersion
    $traceDataCollectorPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.tracedatacollector\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $internalCodeCoveragePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage\$codeCoverageExternalsVersion\contentFiles\any\any\"
    $codeCoverageCorePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.coverage.core\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageInterprocessPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.coverage.interprocess\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageInstrumentationPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.coverage.instrumentation\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageImUbuntuPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage.instrumentationmethod.ubuntu\$codeCoverageExternalsVersion\content\native"
    $codeCoverageImAlpinePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage.instrumentationmethod.alpine\$codeCoverageExternalsVersion\content\native"

    $microsoftCodeCoveragePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.CodeCoverage\")

    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir -Force | Out-Null

    Copy-Item $traceDataCollectorPackagesDir\Microsoft.VisualStudio.TraceDataCollector.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorPackagesDir\Microsoft.VisualStudio.TraceDataCollector.pdb $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageCorePackagesDir\Microsoft.VisualStudio.Coverage.Core.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInterprocessPackagesDir\Microsoft.VisualStudio.Coverage.Interprocess.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Microsoft.VisualStudio.Coverage.Instrumentation.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Mono.Cecil.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Mono.Cecil.Pdb.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $internalCodeCoveragePackagesDir\CodeCoverage $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $internalCodeCoveragePackagesDir\InstrumentationEngine $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $internalCodeCoveragePackagesDir\Shim $microsoftCodeCoveragePackageDir -Force -Recurse

    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir\InstrumentationEngine\ubuntu\ -Force | Out-Null
    Copy-Item $codeCoverageImUbuntuPackagesDir\x64 $microsoftCodeCoveragePackageDir\InstrumentationEngine\ubuntu\ -Force -Recurse
    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir\InstrumentationEngine\alpine\ -Force | Out-Null
    Copy-Item $codeCoverageImAlpinePackagesDir\x64 $microsoftCodeCoveragePackageDir\InstrumentationEngine\alpine\ -Force -Recurse

    # Copy TraceDataCollector resource dlls
    if($TPB_LocalizedBuild) {
        Copy-Loc-Files $traceDataCollectorPackagesDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
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
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFramework451\$TPB_TargetRuntime")
}

function Get-FullCLRPackageDirectory45
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFramework45\$TPB_TargetRuntime")
}

function Get-CoreCLR20PackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20")
}

function Get-CoreCLR10PackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore10")
}

function Get-CoreCLR20TestHostPackageDirectory
{
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20\TestHost")
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
    & $sdkTaskPath -restore -task GenerateBuildManifest /p:PackagesToPublishPattern=$TPB_PackageOutDir\*.nupkg /p:AssetManifestFilePath=$TPB_PackageOutDir\manifest\manifest.xml /p:ManifestBuildData="Location=https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" /p:BUILD_BUILDNUMBER=$BuildNumber

    Write-Log "Generate-Manifest: Completed."
}

function Build-SpecificProjects
{
    Write-Log "Build-SpecificProjects: Started for pattern: $ProjectNamePatterns"
    # FrameworksAndOutDirs format ("<target_framework>", "<output_dir>").
    $FrameworksAndOutDirs = ( 
        ("net451", "net451\win7-x64"), 
        ("netstandard1.0", "netstandard1.0"), 
        ("netstandard1.3", "netstandard1.3"), 
        ("netstandard2.0", "netcoreapp2.1"), 
        ("netcoreapp2.1", "netcoreapp2.1")
    )
    
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

if ($Force -or $Steps -contains "InstallDotnet") {
    Install-DotNetCli
}

if ($Force -or $Steps -contains "Restore") {
    Clear-Package
    Restore-Package    
}

if ($Force -or $Steps -contains "UpdateLocalization") {
    Update-LocalizedResources
}

if ($Force -or $Steps -contains "Build") {
    Invoke-Build
}

if ($Force -or $Steps -contains "Publish") {
    Publish-Package
    Create-VsixPackage
    Create-NugetPackages
    Generate-Manifest
    Copy-PackageIntoStaticDirectory
}

if ($Force -or $Steps -contains "PrepareAcceptanceTests") {
    Publish-PatchedDotnet
    Invoke-TestAssetsBuild
    Publish-Tests
}
 
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
