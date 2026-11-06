# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [Alias("c")]
    [System.String] $Configuration = "Debug",

    [Parameter(Mandatory = $false)]
    [Alias("r")]
    [System.String] $TargetRuntime = "win7-x64",

    # Versioning scheme = Major(15).Minor(RTW, Updates).SubUpdates(preview4, preview5, RC etc)
    # E.g. VS 2017 Update 1 Preview will have version 15.1.1
    [Parameter(Mandatory = $false)]
    [Alias("v")]
    [System.String] $Version, # Will set this later by reading TestPlatform.Settings.targets file.

    [Parameter(Mandatory = $false)]
    [Alias("vs")]
    [System.String] $VersionSuffix = "dev",

    [Parameter(Mandatory = $false)]
    [Alias("bn")]
    [System.String] $BuildNumber = "20991231-99",

    [Parameter(Mandatory = $false)]
    [Alias("ff")]
    [System.Boolean] $FailFast = $true,

    [Parameter(Mandatory = $false)]
    [Alias("noloc")]
    [Switch] $DisableLocalizedBuild,

    [Parameter(Mandatory = $false)]
    [Alias("ci")]
    [Switch] $CIBuild,

    [Parameter(Mandatory = $false)]
    [Alias("pt")]
    [Switch] $PublishTestArtifacts,

    # Build specific projects
    [Parameter(Mandatory = $false)]
    [Alias("p")]
    [System.String[]] $ProjectNamePatterns = @(),

    [Alias("f")]
    [Switch] $Force,

    [Alias("s")]
    [ValidateSet("InstallDotnet", "Restore", "UpdateLocalization", "Build", "Publish", "Pack", "Manifest", "PrepareAcceptanceTests")]
    [String[]] $Steps = @("InstallDotnet", "Restore", "UpdateLocalization", "Build", "Publish", "Pack", "Manifest", "PrepareAcceptanceTests"),
    [Switch] $BuildCompatibilityTests
)

$ErrorActionPreference = 'Stop'
$ErrorView = 'Normal'

. $PSScriptRoot\common.lib.ps1

# Set Version from scripts/build/TestPlatform.Settings.targets, when we are running locally and not providing the version as the parameter
# or when the build is done directly in VS
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Settings.targets)).Project.PropertyGroup[0].TPVersionPrefix |
    ForEach-Object { $_.Trim() } |
    Select-Object -First 1

    Write-Verbose "Version was not provided using version '$Version' from TestPlatform.Settings.targets"
}


#
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_TestAssets = Join-Path $env:TP_ROOT_DIR "test\TestAssets\"
$TPB_Solution = Join-Path $env:TP_ROOT_DIR "TestPlatform.sln"
$TPB_TestAssets_Solution = Join-Path $TPB_TestAssets "TestAssets.sln"
$TPB_TestAssets_CILAssets = Join-Path $TPB_TestAssets "CILProject\CILProject.proj"
$TPB_TargetFramework45 = "net45"
$TPB_TargetFramework451 = "net451"
$TPB_TargetFramework472 = "net472"
$TPB_TargetFramework48 = "net48"
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
$TPB_ARM64_Runtime = "win10-arm64"

# Version suffix is empty for RTM release
$TPB_Version = if ($VersionSuffix -ne '') { $Version + "-" + $VersionSuffix } else { $Version }
$TPB_CIBuild = $CIBuild
$TPB_PublishTests = $PublishTestArtifacts
$TPB_LocalizedBuild = !$DisableLocalizedBuild
$TPB_PackageOutDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration\packages
$TPB_SourceBuildPackageOutDir = Join-Path $TPB_PackageOutDir "source-build"

$language = @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant")

. "$($CurrentScriptDir.FullName)\verify-nupkgs.ps1"

# Update the version in the dependencies props to be the TPB_version version, this is not ideal but because changing how this is resolved would
# mean that we need to change the whole build process this is a solution with the least amount of impact, that does not require us to keep track of
# the version in multiple places
$dependenciesPath = "$env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props"
$dependencies = Get-Content -Raw -Encoding UTF8 $dependenciesPath
$updatedDependencies = $dependencies -replace "<NETTestSdkVersion>.*?</NETTestSdkVersion>", "<NETTestSdkVersion>$TPB_Version</NETTestSdkVersion>"
# PS7 considers utf8 to not have BOM, and utf8BOM needs to be used instead, while earlier versions use BOM with utf8 encoding
$encoding = if ($PSVersionTable.PSVersion.Major -ge 7) { "utf8BOM" } else { "utf8" }
$updatedDependencies | Set-Content -Encoding $encoding $dependenciesPath -NoNewline

function Get-ProjectOutputRoot {
    <#
      Translates a project name to the directory Arcade writes its build output to.

      THE ERA A STAGING TRANSLATION, in one place. Before Arcade this script read
        $env:TP_ROOT_DIR\src\<Project>\bin\<Configuration>\<TFM>\
      because that is where the pre-Arcade build put output. Arcade writes to
        $env:TP_ROOT_DIR\artifacts\bin\<Project>\<Configuration>\<TFM>\
      preserving the <TFM>[\<RID>] structure underneath, so only the ROOT changes.

      ONE definition referenced at every call site, deliberately, rather than eleven edited
      literals. The failure mode this avoids is a PARTIAL translation: ten corrected paths and one
      missed still works on an incremental tree, because the stale pre-Arcade directory is still
      sitting there from an earlier build. That defect cannot fail locally and breaks the first
      clean CI build. Verify by deleting artifacts\bin and rebuilding, never by counting the diff.

      Note this does NOT relocate the build. An earlier attempt set BaseOutputPath to move Arcade's
      output back to the historic layout; measured, it changes BaseOutputPath while leaving
      OutputPath untouched, because Arcade's RepoLayout.props computes OutputPath from it at import
      time - before this file's assignment runs. The result was a SPLIT layout, which is worse than
      either consistent option. Translate the readers instead; do not move the writer.

      DEFINED HERE, NOT IN THE HELPER SECTION AT THE BOTTOM, AND THAT IS LOAD-BEARING. Ten of the
      eleven call sites are inside functions, so they run long after the whole file has been read
      and the definition's position does not matter for them. The eleventh - the AttachVS PATH
      prepend a few lines below - is TOP-LEVEL code, so it executes the moment the interpreter
      reaches it. With the definition 968 lines further down, that call raised
        The term 'Get-ProjectOutputRoot' is not recognized
      and the Package step exited 1 in 13 seconds having done nothing (build 3066763). Moving the
      DEFINITION rather than the AttachVS block keeps every side effect in its original order,
      because defining a function has no side effect at all. Do not move this back down.
    #>
    param (
        [Parameter(Mandatory = $true)][string] $Project,
        [string] $Configuration = $TPB_Configuration
    )

    return (Join-Path $env:TP_ROOT_DIR "artifacts\bin\$Project\$Configuration")
}

# AttachVS is a debugging helper put on PATH; it is not shipped.
#
# The configuration was hardcoded to Debug here before Arcade, and that is a PRE-EXISTING
# LATENT DEFECT rather than something the port introduced: a Release build never produced
# src\AttachVS\bin\Debug\net472, so the path did not exist and AttachVS was silently absent from
# PATH on every Release build. Nothing failed, because nothing checks.
#
# Translating it faithfully would have carried the defect into the Arcade layout - measured:
# artifacts\bin\AttachVS\ contains Release and no Debug. Using $TPB_Configuration makes it
# resolve for whichever configuration was actually built, which is what the line was always
# trying to express.
#
# Found by verifying every translated path against real build output rather than by counting
# the edits. A path that only feeds a PATH prepend has no error to search for.
$attachVsPath = "$(Get-ProjectOutputRoot -Project 'AttachVS')\net472"

if ($env:PATH -notlike "*$attachVsPath") {
    Write-Log "Adding AttachVS to PATH"
    $env:PATH = "$attachVsPath;$env:PATH"
}

# VsixUtil gets regularly eaten by antivirus or something. Remove the package dir if it gets broken
# so nuget restores it correctly.
$vsSdkBuildToolsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.VSSdkBuildToolsVersion
$vsixUtilDir = "$env:TP_ROOT_DIR\packages\microsoft.vssdk.buildtools"
if ((Test-Path $vsixUtilDir) -and -not (Test-Path "$vsixUtilDir\$vsSdkBuildToolsVersion\tools\vssdk\bin\VsixUtil.exe")) {
    Remove-Item -Recurse -Force $vsixUtilDir
}

function Invoke-Build {
    $timer = Start-Timer
    Write-Log "Invoke-Build: Start build."
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Build: Source: $TPB_Solution"
    Invoke-Exe $dotnetExe -Arguments "build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:""$env:TP_OUT_DIR\log\$Configuration\TestPlatform.binlog"""
    Write-Log ".. .. Build: Complete."

    Write-Log ".. .. Build: Source: $TPB_TestAssets_CILAssets"
    Invoke-Exe $dotnetExe -Arguments "build $TPB_TestAssets_CILAssets --configuration $TPB_Configuration -v:minimal -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:""$env:TP_OUT_DIR\log\$Configuration\CILAssets.binlog"""
    Write-Log ".. .. Build: Complete."
    Write-Log "Invoke-Build: Complete. {$(Get-ElapsedTime($timer))}"
}

function Invoke-TestAssetsBuild {
    $timer = Start-Timer
    Write-Log "Invoke-TestAssetsBuild: Start test assets build."
    $dotnetExe = Get-DotNetPath
    Invoke-Exe $dotnetExe -Arguments "restore ""$env:TP_ROOT_DIR\test\Microsoft.TestPlatform.AcceptanceTests\Microsoft.TestPlatform.AcceptanceTests.csproj"" --packages ""$env:TP_PACKAGES_DIR"""
    Write-Log ".. .. Build: Source: $TPB_TestAssets_Solution"
    $projects = [regex]::Matches((Get-Content $TPB_TestAssets_Solution -Raw), '"([^"]+\.csproj)"') |
        ForEach-Object { $_.Groups[1].Value } |
        Where-Object { $BuildCompatibilityTests -or ($_ -notmatch '^performance\\|^Tools\\|^SampleProjectWithOldTestHost\\') }
    $filterPath = Join-Path $env:TP_OUT_DIR "tmp\TestAssets.$TPB_Configuration.slnf"
    New-Item (Split-Path $filterPath) -ItemType Directory -Force | Out-Null
    @{ solution = @{ path = $TPB_TestAssets_Solution; projects = @($projects) } } |
        ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 $filterPath
    Invoke-Exe $dotnetExe -Arguments "build ""$filterPath"" --configuration $TPB_Configuration -v:minimal -nr:false -p:Version=$TPB_Version -p:NETTestSdkVersion=$TPB_Version -p:PackageVersion=$TPB_Version -p:RestoreAdditionalProjectSources=""$TPB_PackageOutDir"" -bl:""$env:TP_OUT_DIR\log\$Configuration\TestAssets.binlog"""

    if ($BuildCompatibilityTests) {
        # Compatibility matrix build.
        $dependenciesPath = "$env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props"
        $dependenciesXml = [xml](Get-Content -Raw -Encoding UTF8 $dependenciesPath)

        # Restore previous versions of TestPlatform (for vstest.console.exe), and TestPlatform.CLI (for vstest.console.dll).
        # These properties are coming from TestPlatform.Dependencies.props.
        $vstestConsoleVersionProperties = @(
            "VSTestConsoleLatestVersion"
            "VSTestConsoleLatestPreviewVersion"
            "VSTestConsoleLatestStableVersion" 
            "VSTestConsoleRecentStableVersion" 
            "VSTestConsoleMostDownloadedVersion"
            "VSTestConsolePreviousStableVersion"
            "VSTestConsoleLegacyStableVersion"
        )

        foreach ($propertyName in $vstestConsoleVersionProperties) {
            if ("VSTestConsoleLatestVersion" -eq $propertyName) { 
                # NETTestSdkVersion has the version of the locally built package.
                $vsTestConsoleVersion = $dependenciesXml.Project.PropertyGroup."NETTestSdkVersion"
            }
            else { 
                $vsTestConsoleVersion = $dependenciesXml.Project.PropertyGroup.$propertyName
            }
            
            # The command line tool does not like the package ranges.
            $vsTestConsoleVersion = $vsTestConsoleVersion -replace "(\[|\])"
            if (-not $vsTestConsoleVersion) {
                throw "VSTestConsoleVersion for $propertyName is empty."
            }

            # We don't use the results of this build anywhere, we just use them to restore the packages to nuget cache
            # because using nuget.exe install errors out in various weird ways.
            Invoke-Exe $dotnetExe -Arguments "build $env:TP_ROOT_DIR\test\TestAssets\Tools\Tools.csproj --configuration $TPB_Configuration -v:minimal -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:NETTestSdkVersion=$vsTestConsoleVersion -p:RestoreAdditionalProjectSources=""$TPB_PackageOutDir"""
        }

        # Build with multiple versions of MSTest. The projects are directly in the root.
        # The folder structure in VS is not echoed in the TestAssets directory.
        $projects = @(
            "$env:TP_ROOT_DIR\test\TestAssets\MSTestProject1\MSTestProject1.csproj"
            "$env:TP_ROOT_DIR\test\TestAssets\MSTestProject2\MSTestProject2.csproj"
            # Don't use this one, it does not use the variables for mstest and test sdk.
            # "$env:TP_ROOT_DIR\test\TestAssets\SimpleTestProject2\SimpleTestProject2.csproj"
        )

        $msTestVersionProperties = @(
            "MSTestFrameworkLatestPreviewVersion"
            "MSTestFrameworkLatestStableVersion"
            "MSTestFrameworkRecentStableVersion"
            "MSTestFrameworkMostDownloadedVersion"
            "MSTestFrameworkPreviousStableVersion"
            "MSTestFrameworkLegacyStableVersion"
        )
        
        foreach ($project in $projects) {
            # We use the same version properties for NET.Test.Sdk as for VSTestConsole, for now.
            foreach ($sdkPropertyName in $vstestConsoleVersionProperties) {
                if ("VSTestConsoleLatestVersion" -eq $sdkPropertyName) { 
                    # NETTestSdkVersion has the version of the locally built package.
                    $netTestSdkVersion = $dependenciesXml.Project.PropertyGroup."NETTestSdkVersion"
                }
                else { 
                    $netTestSdkVersion = $dependenciesXml.Project.PropertyGroup.$sdkPropertyName
                }
            
                if (-not $netTestSdkVersion) {
                    throw "NetTestSdkVersion for $sdkPropertyName is empty."
                }
            
                $dirNetTestSdkVersion = $netTestSdkVersion -replace "\[|\]" 
                $dirNetTestSdkPropertyName = $sdkPropertyName -replace "Framework" -replace "Version" -replace "VSTestConsole", "NETTestSdk"
                
                foreach ($propertyName in $msTestVersionProperties) {  
                    $mstestVersion = $dependenciesXml.Project.PropertyGroup.$propertyName

                    if (-not $mstestVersion) {
                        throw "MSTestVersion for $propertyName is empty."
                    }
                
                    $dirMSTestVersion = $mstestVersion -replace "\[|\]" 
                    $dirMSTestPropertyName = $propertyName -replace "Framework" -replace "Version"
                    Invoke-Exe $dotnetExe -Arguments "build $project --configuration $TPB_Configuration -v:minimal -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:MSTestFrameworkVersion=$mstestVersion -p:MSTestAdapterVersion=$mstestVersion -p:NETTestSdkVersion=$netTestSdkVersion -p:RestoreAdditionalProjectSources=""$TPB_PackageOutDir"" -p:BaseOutputPath=""bin\$dirNetTestSdkPropertyName-$dirNetTestSdkVersion\$dirMSTestPropertyName-$dirMSTestVersion\\"" -bl:""$env:TP_OUT_DIR\log\$Configuration\perm.binlog"""
                }
            }
        }

        # end 
    }
    Write-Log ".. .. Build: Complete."
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
    $dotnetPath = Split-Path (Get-DotNetPath)

    $dotnetTestArtifactsPath = "$env:TP_TESTARTIFACTS\dotnet\"

    $dotnetTestArtifactsSdkPath = "$env:TP_TESTARTIFACTS\dotnet\sdk\$env:DOTNET_CLI_VERSION\"
    New-Item $dotnetTestArtifactsPath -ItemType Directory -Force | Out-Null
    Copy-Item (Join-Path $dotnetPath '*') $dotnetTestArtifactsPath -Force -Recurse

    Write-Log "Publish-PatchedDotnet: Copy VSTest task artifacts to local dotnet installation to allow `dotnet test` to run with it"
    $buildArtifactsPath = "$(Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.Build')\$TPB_TargetFrameworkNS20\*"
    Copy-Item $buildArtifactsPath $dotnetTestArtifactsSdkPath -Force
}

function Publish-Package {
    $timer = Start-Timer
    Write-Log "Publish-Package: Started."
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
    $testHostarm64Project = Join-Path $env:TP_ROOT_DIR "src\testhost.arm64\testhost.arm64.csproj"

    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework451\$TPB_TargetRuntime")
    $testhostCore20PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20")
    $testhostCore20PackageX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCore20PackageX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    $testhostCore20PackageARM64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_ARM64_Runtime")
    $testhostCore20PackageTempX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X64_Runtime")
    $testhostCore20PackageTempX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_X86_Runtime")
    $testhostCore20PackageTempARM64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore20\$TPB_ARM64_Runtime")

    $testhostCore10PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10")
    $testhostCore10PackageX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X64_Runtime")
    $testhostCore10PackageX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X86_Runtime")
    $testhostCore10PackageARM64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_ARM64_Runtime")
    $testhostCore10PackageTempX64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X64_Runtime")
    $testhostCore10PackageTempX86Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_X86_Runtime")
    $testhostCore10PackageTempARM64Dir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\publishTemp\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore10\$TPB_ARM64_Runtime")

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

    # We build vstest.console.arm64.exe before building vstest.console.exe and we put it in the same folder, so they end up shipping together.
    Publish-PackageWithRuntimeInternal $vstestConsoleProject $TPB_TargetFramework451 $TPB_ARM64_Runtime false $fullCLRPackage451Dir
    Publish-PackageWithRuntimeInternal $vstestConsoleProject $TPB_TargetFramework451 $TPB_X64_Runtime false $fullCLRPackage451Dir
    Publish-PackageInternal $vstestConsoleProject $TPB_TargetFrameworkCore20 $coreCLR20PackageDir

    Write-Log "Package: Publish src\SettingsMigrator\SettingsMigrator.csproj"
    Publish-PackageInternal $settingsMigratorProject $TPB_TargetFramework451 $fullCLRPackage451Dir

    Write-Log "Package: Publish src\datacollector\datacollector.csproj"
    # We build datacollector.arm64.exe before building datacollector.exe and we put it in the same folder, so they end up shipping together.
    Publish-PackageWithRuntimeInternal $dataCollectorProject $TPB_TargetFramework472 $TPB_ARM64_Runtime false $fullCLRPackage451Dir
    Publish-PackageWithRuntimeInternal $dataCollectorProject $TPB_TargetFramework472 $TPB_X64_Runtime false $fullCLRPackage451Dir
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

    Write-Log "Package: Publish testhost.arm64\testhost.arm64.csproj"
    Publish-PackageInternal $testHostarm64Project $TPB_TargetFramework451 $testhostFullPackageDir
    Publish-PackageWithRuntimeInternal $testHostarm64Project $TPB_TargetFrameworkCore20 $TPB_ARM64_Runtime false $testhostCore20PackageTempARM64Dir
    Publish-PackageWithRuntimeInternal $testHostarm64Project $TPB_TargetFrameworkCore10 $TPB_ARM64_Runtime true $testhostCore10PackageTempARM64Dir

    # THE ERA A STAGING TRANSLATION, continued. These three loops read build output, so they are
    # readers and must go through Get-ProjectOutputRoot like every other reader in this file.
    #
    # They were MISSED by the first translation pass, which found eleven call sites and corrected
    # all eleven. The miss was not carelessness about a number; it was that the enumeration matched
    # on the shape "Join-Path $env:TP_ROOT_DIR src\<Project>\bin\..." and these lines are written as
    # "$(Split-Path $testHostProject)\bin\...", deriving the same stale root from the PROJECT FILE
    # PATH instead of spelling it out. Same defect, different spelling, so a shape-matched search
    # cannot see it.
    #
    # 🛑 THE ENUMERATION RULE THIS COMMENT USED TO GIVE - "search for \bin\ across the whole file" -
    # IS WRONG AND WAS DISPROVEN BY BUILD 3066979. That search returned zero stale readers while TWO
    # were still stale, because they are assembled as [IO.Path]::Combine(..., "bin", ...), where the
    # literal is "bin" with NO separator on either side. That is a THIRD spelling of the same root,
    # after the Join-Path one and the Split-Path one. Any search keyed on punctuation will keep
    # missing a spelling nobody has thought of yet.
    # SEARCH FOR THE BARE WORD bin, case-insensitive, AND TRIAGE EVERY HIT. On this file that is ~32
    # hits, most of them legitimately unrelated (VsixUtil, MSBuild\Bin, procdump, the BaseOutputPath
    # WRITER at the perf-asset build). Triaging thirty-two lines by hand is cheap; a clean search
    # result that is clean only because of how the path was punctuated costs a whole CI round trip.
    # Better still: the only complete enumeration is RUNNING the step. Three separate passes each
    # believed they were exhaustive, and each was corrected by the next build, not by a grep.
    #
    # This is exactly the PARTIAL TRANSLATION that Get-ProjectOutputRoot's own comment warns about,
    # and it failed the way that comment predicts: invisibly on an incremental tree, and on the
    # first clean CI build (3066895) as
    #   Copy-Item : Cannot find path 'D:\a\_work\1\s\src\testhost\bin\Release\net452\win7-x64\testhost.net452.exe'
    #
    # The three roots below are MEASURED from 3066895's own Build log, not inferred from the helper:
    #   artifacts\bin\testhost\Release\<tfm>\win7-x64\testhost.<tfm>.exe
    #   artifacts\bin\testhost.x86\Release\<tfm>\win7-x86\testhost.<tfm>.x86.exe
    #   artifacts\bin\testhost.arm64\Release\<tfm>\win10-arm64\testhost.<tfm>.arm64.exe
    # All nine TFMs are present under each of the three roots, so the eight the loops ask for
    # (all but the default net451) exist. The <TFM>\<RID> structure below the root is unchanged,
    # which is why only the root is being translated.
    $testhostOutputRoot = Get-ProjectOutputRoot -Project 'testhost'
    $testhostx86OutputRoot = Get-ProjectOutputRoot -Project 'testhost.x86'
    $testhostarm64OutputRoot = Get-ProjectOutputRoot -Project 'testhost.arm64'

    # Copy the .NET multitarget testhost exes to destination folder (except for net451 which is the default)
    foreach ($tfm in "net452;net46;net461;net462;net47;net471;net472;net48" -split ";") {
        Copy-Item "$testhostOutputRoot\$tfm\$TPB_X64_Runtime\testhost.$tfm.exe" $testhostFullPackageDir\testhost.$tfm.exe -Force
        Copy-Item "$testhostOutputRoot\$tfm\$TPB_X64_Runtime\testhost.$tfm.pdb" $testhostFullPackageDir\testhost.$tfm.pdb -Force
        Copy-Item "$testhostOutputRoot\$tfm\$TPB_X64_Runtime\testhost.$tfm.exe.config" $testhostFullPackageDir\testhost.$tfm.exe.config -Force
    }

    # Copy the .NET multitarget testhost.x86 exes to destination folder (except for net451 which is the default)
    foreach ($tfm in "net452;net46;net461;net462;net47;net471;net472;net48" -split ";") {
        Copy-Item "$testhostx86OutputRoot\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.exe" $testhostFullPackageDir\testhost.$tfm.x86.exe -Force
        Copy-Item "$testhostx86OutputRoot\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.pdb" $testhostFullPackageDir\testhost.$tfm.x86.pdb -Force
        Copy-Item "$testhostx86OutputRoot\$tfm\$TPB_X86_Runtime\testhost.$tfm.x86.exe.config" $testhostFullPackageDir\testhost.$tfm.x86.exe.config -Force
    }

    # Copy the .NET multitarget testhost.arm64 exes to destination folder (except for net451 which is the default)
    foreach ($tfm in "net452;net46;net461;net462;net47;net471;net472;net48" -split ";") {
        Copy-Item "$testhostarm64OutputRoot\$tfm\$TPB_ARM64_Runtime\testhost.$tfm.arm64.exe" $testhostFullPackageDir\testhost.$tfm.arm64.exe -Force
        Copy-Item "$testhostarm64OutputRoot\$tfm\$TPB_ARM64_Runtime\testhost.$tfm.arm64.pdb" $testhostFullPackageDir\testhost.$tfm.arm64.pdb -Force
        Copy-Item "$testhostarm64OutputRoot\$tfm\$TPB_ARM64_Runtime\testhost.$tfm.arm64.exe.config" $testhostFullPackageDir\testhost.$tfm.arm64.exe.config -Force
    }

    # Copy the .NET core x86, x64 and arm64 testhost exes from tempPublish to required folder
    New-Item -ItemType directory -Path $testhostCore20PackageX64Dir -Force | Out-Null
    Copy-Item $testhostCore20PackageTempX64Dir\testhost* $testhostCore20PackageX64Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore20PackageX64Dir -Force

    New-Item -ItemType directory -Path $testhostCore20PackageX86Dir -Force | Out-Null
    Copy-Item $testhostCore20PackageTempX86Dir\testhost.x86* $testhostCore20PackageX86Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX86Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore20PackageX86Dir -Force

    New-Item -ItemType directory -Path $testhostCore20PackageARM64Dir -Force | Out-Null
    Copy-Item $testhostCore20PackageTempARM64Dir\testhost.arm64* $testhostCore20PackageARM64Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempARM64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore20PackageARM64Dir -Force

    New-Item -ItemType directory -Path $testhostCore10PackageX64Dir -Force | Out-Null
    Copy-Item $testhostCore10PackageTempX64Dir\testhost* $testhostCore10PackageX64Dir -Force -Recurse
    Copy-Item $testhostCore20PackageTempX64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore10PackageX64Dir -Force

    New-Item -ItemType directory -Path $testhostCore10PackageX86Dir -Force | Out-Null
    Copy-Item $testhostCore10PackageTempX86Dir\testhost.x86* $testhostCore10PackageX86Dir -Force -Recurse
    Copy-Item $testhostCore10PackageTempX86Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore10PackageX86Dir -Force

    New-Item -ItemType directory -Path $testhostCore10PackageARM64Dir -Force | Out-Null
    Copy-Item $testhostCore10PackageTempARM64Dir\testhost.arm64* $testhostCore10PackageARM64Dir -Force -Recurse
    Copy-Item $testhostCore10PackageTempARM64Dir\Microsoft.TestPlatform.PlatformAbstractions.dll $testhostCore10PackageARM64Dir -Force

    # Copy over the Full CLR built testhost package assemblies to the Core CLR and Full CLR package folder.
    $coreCLRFull_Dir = "TestHost"
    $fullDestDir = Join-Path $coreCLR20PackageDir $coreCLRFull_Dir
    New-Item -ItemType directory -Path $fullDestDir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullDestDir -Force -Recurse

    # Copy over the Full CLR built datacollector package assemblies to the Core CLR package folder along with testhost
    Publish-PackageWithRuntimeInternal $dataCollectorProject $TPB_TargetFramework472 $TPB_ARM64_Runtime false $fullDestDir
    Publish-PackageWithRuntimeInternal $dataCollectorProject $TPB_TargetFramework472 $TPB_X64_Runtime false $fullDestDir
    
    New-Item -ItemType directory -Path $fullCLRPackage451Dir -Force | Out-Null
    Copy-Item $testhostFullPackageDir\* $fullCLRPackage451Dir -Force -Recurse

    ################################################################################
    # Publish Microsoft.TestPlatform.ObjectModel

    # Copy this first because for ObjectModel it puts platform abstractions NS1.3 version
    # into the output folder (for some reason), and we overwrite it with actual uap10.0 version below

    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.ObjectModel') `
        -files @{
        $TPB_TargetFramework45     = $fullCLRPackage45Dir           # net45
        $TPB_TargetFramework451    = $fullCLRPackage451Dir          # net451
        $TPB_TargetFrameworkCore10 = $coreCLR10PackageDir           # netcoreapp1.0
        $TPB_TargetFrameworkCore20 = $coreCLR20PackageDir           # netcoreapp2.1
        $TPB_TargetFrameworkNS10   = $netstandard10PackageDir       # netstandard1_0
        $TPB_TargetFrameworkNS13   = $netstandard13PackageDir       # netstandard1_3
        $TPB_TargetFrameworkNS20   = $netstandard20PackageDir       # netstandard2_0
        $TPB_TargetFrameworkUap100 = $uap100PackageDir              # uap10.0
    }

    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.ObjectModel') `
        -files @{
        $TPB_TargetFrameworkUap100 = $testhostUapPackageDir         # uap10.0 - testhost
    }

    ################################################################################
    # Publish Microsoft.TestPlatform.PlatformAbstractions
    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.PlatformAbstractions') `
        -files @{
        $TPB_TargetFramework45     = $fullCLRPackage45Dir          # net45
        $TPB_TargetFramework451    = $fullCLRPackage451Dir         # net451
        $TPB_TargetFrameworkCore20 = $coreCLR20PackageDir          # netcoreapp2.1
        $TPB_TargetFrameworkNS10   = $netstandard10PackageDir      # netstandard1_0
        $TPB_TargetFrameworkNS13   = $netstandard13PackageDir      # netstandard1_3
        $TPB_TargetFrameworkNS20   = $netstandard20PackageDir      # netstandard2_0
        $TPB_TargetFrameworkUap100 = $uap100PackageDir             # uap10.0
    }
    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.PlatformAbstractions') `
        -files @{
        $TPB_TargetFrameworkUap100 = $testhostUapPackageDir        # uap10.0 - testhost
    }

    ################################################################################
    # Publish Microsoft.TestPlatform.CoreUtilities
    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.CoreUtilities') `
        -files @{
        $TPB_TargetFramework45     = $fullCLRPackage45Dir           # net45
        $TPB_TargetFramework451    = $fullCLRPackage451Dir          # net451
        $TPB_TargetFrameworkNS10   = $netstandard10PackageDir       # netstandard1_0
        $TPB_TargetFrameworkNS13   = $netstandard13PackageDir       # netstandard1_3
        $TPB_TargetFrameworkNS20   = $netstandard20PackageDir       # netstandard2_0
        $TPB_TargetFrameworkUap100 = $uap100PackageDir              # uap10.0
    }

    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.CoreUtilities') `
        -files @{
        $TPB_TargetFrameworkUap100 = $testhostUapPackageDir         # uap10.0 - testhost
    }

    ################################################################################
    # Publish Microsoft.TestPlatform.AdapterUtilities
    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.AdapterUtilities') `
        -files @{
        # "net20"                     = $net20PackageDir               # net20
        "net45/any"                = $net45PackageDir               # $net4
        $TPB_TargetFrameworkNS10   = $netstandard10PackageDir       # netstandard1_0
        $TPB_TargetFrameworkNS20   = $netstandard20PackageDir       # netstandard2_0
        $TPB_TargetFrameworkUap100 = $uap100PackageDir              # uap10.0
    }

    ################################################################################
    # Publish Microsoft.TestPlatform.CrossPlatEngine
    Copy-Bulk -root (Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.CrossPlatEngine') `
        -files @{
        $TPB_TargetFrameworkNS13 = $netstandard13PackageDir       # netstandard1_3
    }

    ################################################################################
    # Publish msdia
    $testPlatformMsDiaVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformMSDiaVersion
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformMsDiaVersion\tools\net451"
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCore20PackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostCore10PackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostFullPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $testhostUapPackageDir -Force
    Copy-Item -Recurse $comComponentsDirectory\* $coreCLR20TestHostPackageDir -Force

    $microsoftInternalDiaInterop = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia.Interop\$testPlatformMsDiaVersion\tools\net451"
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

    foreach ($file in $loggers) {
        Write-Verbose "Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force"
        Move-Item $fullCLRPackage451Dir\$file $fullCLRExtensionsDir -Force

        Write-Verbose "Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force"
        Move-Item $coreCLR20PackageDir\$file $coreCLRExtensionsDir -Force
    }

    # Move logger resource dlls
    if ($TPB_LocalizedBuild) {
        Move-Loc-Files $fullCLRPackage451Dir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.resources.dll"
        Move-Loc-Files $fullCLRPackage451Dir $fullCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.resources.dll"
        Move-Loc-Files $coreCLR20PackageDir $coreCLRExtensionsDir "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.resources.dll"
    }

    # Copy Blame Datacollector to Extensions folder.
    $TPB_TargetFrameworkStandard = "netstandard2.0"
    $blameDataCollector = Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.Extensions.BlameDataCollector'
    $blameDataCollectorNetFull = Join-Path $blameDataCollector $TPB_TargetFramework472
    $blameDataCollectorNetStandard = Join-Path $blameDataCollector $TPB_TargetFrameworkStandard
    New-Item -ItemType Directory "$fullCLRExtensionsDir/dump" -Force | Out-Null
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetFull\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.exe "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.pdb "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.exe.config "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.x86.exe "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.x86.pdb "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.x86.exe.config "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.arm64.exe "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.arm64.pdb "$fullCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetFull\DumpMinitool.arm64.exe.config "$fullCLRExtensionsDir/dump" -Force

    New-Item -ItemType Directory "$coreCLRExtensionsDir/dump" -Force | Out-Null
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\Microsoft.TestPlatform.Extensions.BlameDataCollector.pdb $coreCLRExtensionsDir -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.exe "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.pdb "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.exe.config "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.x86.exe "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.x86.pdb "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.x86.exe.config "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.arm64.exe "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.arm64.pdb "$coreCLRExtensionsDir/dump" -Force
    Copy-Item $blameDataCollectorNetStandard\DumpMinitool.arm64.exe.config "$coreCLRExtensionsDir/dump" -Force

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
    if ($TPB_LocalizedBuild) {
        Copy-Loc-Files $blameDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
        Copy-Loc-Files $blameDataCollectorNetStandard $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.BlameDataCollector.resources.dll"
    }

    # Copy Event Log Datacollector to Extensions folder.
    #
    # Translated for the same reason as $blameDataCollector 47 lines above, and it is worth noting
    # WHY only one of the pair was corrected the first time: the blame line was already written as
    # a single Join-Path off $env:TP_ROOT_DIR, which the shape-matched enumeration saw, while this
    # one is identical in meaning. Adjacent, same idiom, one translated and one not.
    #
    # This would NOT have been reached in 3066895 - the testhost loops above die first - so fixing
    # only those and pushing would have produced a fresh failure on the same step an hour later.
    # Measured from 3066895's Build log, the output root really is the project name:
    #   artifacts\bin\Microsoft.TestPlatform.Extensions.EventLogCollector\Release\net451
    # and net451 is $TPB_TargetFramework451, which is the TFM the next line asks for.
    $eventLogDataCollector = Get-ProjectOutputRoot -Project 'Microsoft.TestPlatform.Extensions.EventLogCollector'
    $eventLogDataCollectorNetFull = Join-Path $eventLogDataCollector $TPB_TargetFramework451
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $fullCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.dll $coreCLRExtensionsDir -Force
    Copy-Item $eventLogDataCollectorNetFull\Microsoft.TestPlatform.Extensions.EventLogCollector.pdb $coreCLRExtensionsDir -Force

    # Copy EventLogCollector resource dlls
    if ($TPB_LocalizedBuild) {
        Copy-Loc-Files $eventLogDataCollectorNetFull $fullCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
        Copy-Loc-Files $eventLogDataCollectorNetFull $coreCLRExtensionsDir "Microsoft.TestPlatform.Extensions.EventLogCollector.resources.dll"
    }

    # Copy Microsoft.CodeCoverage.IO dlls
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\eng\Versions.props)).Project.PropertyGroup.MicrosoftInternalCodeCoverageVersion
    $codeCoverageIOPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.codecoverage.io\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkStandard"
    Copy-Item $codeCoverageIOPackagesDir\Microsoft.CodeCoverage.IO.dll $coreCLR20PackageDir -Force
    if ($TPB_LocalizedBuild) {
        Copy-Loc-Files $codeCoverageIOPackagesDir $coreCLR20PackageDir "Microsoft.CodeCoverage.IO.resources.dll"
    }

    # If there are some dependencies for the TestHostRuntimeProvider assemblies, those need to be moved too.
    $runtimeproviders = @("Microsoft.TestPlatform.TestHostRuntimeProvider.dll", "Microsoft.TestPlatform.TestHostRuntimeProvider.pdb")
    foreach ($file in $runtimeproviders) {
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
    $newtonsoftJsonVersion = ([xml](Get-Content $env:TP_ROOT_DIR\eng\Versions.props)).Project.PropertyGroup.NewtonsoftJsonVersion
    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\$newtonsoftJsonVersion\lib\net45\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $fullCLRPackage451Dir -Force"
    Copy-Item $newtonsoft $fullCLRPackage451Dir -Force

    $newtonsoft = Join-Path $env:TP_PACKAGES_DIR "newtonsoft.json\$newtonsoftJsonVersion\lib\netstandard1.0\Newtonsoft.Json.dll"
    Write-Verbose "Copy-Item $newtonsoft $coreCLR20PackageDir -Force"
    Copy-Item $newtonsoft $coreCLR20PackageDir -Force

    # Copy .NET Standard CPP Test adapter
    New-Item "$fullCLRPackage451Dir\TestHost" -ItemType Directory -Force | Out-Null
    $fullCLRTestHostDir = "$fullCLRPackage451Dir\TestHost"

    $testPlatformRemoteExternalsVersion = ([xml](Get-Content "$env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props")).Project.PropertyGroup.TestPlatformRemoteExternalsVersion
    $testPlatformRemoteExternalsSourceDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Remote\$testPlatformRemoteExternalsVersion\tools\netstandard\Extensions\*"
    Copy-Item $testPlatformRemoteExternalsSourceDirectory $coreCLR20PackageDir -Force -Recurse
    Copy-Item $testPlatformRemoteExternalsSourceDirectory $fullCLRTestHostDir -Force -Recurse

    # Copy standalone testhost
    $standaloneTesthost = Join-Path $env:TP_ROOT_DIR "temp\testhost\*"
    Copy-Item $standaloneTesthost $coreCLR20PackageDir -Force
    Copy-Item $testhostCore20PackageDir\testhost.dll $coreCLR20PackageDir -Force
    Copy-Item $testhostCore20PackageDir\testhost.pdb $coreCLR20PackageDir -Force

    Get-Item "$testhostCore20PackageDir\*" |
    Where-Object { $_.Name -notin ("x64", "x86", "win7-x64", "win7-x86", "testhost.deps.json", "testhost.runtimeconfig.json") } |
    Copy-Item -Recurse -Destination $fullCLRTestHostDir -Force
    Copy-Item $standaloneTesthost $fullCLRTestHostDir -Force

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

function Publish-Tests {
    if ($TPB_PublishTests) {
        Write-Log "Publish-Tests: Started."

        # Adding only Perf project for now
        $fullCLRTestDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework451"
        $fullCLRPerfTestAssetDir = Join-Path $env:TP_TESTARTIFACTS "$TPB_Configuration\$TPB_TargetFramework451\TestAssets\PerfAssets"

        $mstest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "MSTest10kPassing"
        $mstest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\MSTest10kPassing"
        Publish-PackageInternal $mstest10kPerfProject $TPB_TargetFramework451 $mstest10kPerfProjectDir

        $nunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "NUnit10kPassing"
        $nunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\NUnit10kPassing"
        Publish-PackageInternal $nunittest10kPerfProject $TPB_TargetFramework451 $nunittest10kPerfProjectDir

        $xunittest10kPerfProjectDir = Join-Path $fullCLRPerfTestAssetDir "XUnit10kPassing"
        $xunittest10kPerfProject = Join-Path $env:TP_ROOT_DIR "test\TestAssets\PerfAssets\XUnit10kPassing"
        Publish-PackageInternal $xunittest10kPerfProject $TPB_TargetFramework451 $xunittest10kPerfProjectDir

        $testPerfProject = Join-Path $env:TP_ROOT_DIR "test\Microsoft.TestPlatform.PerformanceTests"
        Publish-PackageInternal $testPerfProject $TPB_TargetFramework48 $fullCLRTestDir
    }
}

function Publish-PackageInternal($packagename, $framework, $output) {
    $dotnetExe = Get-DotNetPath
    Invoke-Exe $dotnetExe -Arguments "publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
}

function Publish-PackageWithRuntimeInternal($packagename, $framework, $runtime, $selfcontained, $output) {
    $dotnetExe = Get-DotNetPath
    Invoke-Exe $dotnetExe -Arguments "publish $packagename --configuration $TPB_Configuration --framework $framework --runtime $runtime --self-contained $selfcontained --output $output -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild"
}

function Copy-Loc-Files($sourceDir, $destinationDir, $dllName) {
    foreach ($lang in $language) {
        $dllToCopy = Join-Path $sourceDir\$lang $dllName
        $destinationFolder = Join-Path $destinationDir $lang
        if (-not (Test-Path $destinationFolder)) {
            New-Item $destinationFolder -Type Directory -Force | Out-Null
        }
        Copy-Item $dllToCopy $destinationFolder -Force
    }
}

function Move-Loc-Files($sourceDir, $destinationDir, $dllName) {
    foreach ($lang in $language) {
        $dllToCopy = Join-Path $sourceDir\$lang $dllName
        $destinationFolder = Join-Path $destinationDir $lang
        if (-not (Test-Path $destinationFolder)) {
            New-Item $destinationFolder -Type Directory -Force | Out-Null
        }
        Move-Item $dllToCopy $destinationFolder -Force
    }
}

function Publish-VsixPackage {
    Write-Log "Publish-VsixPackage: Started."
    $timer = Start-Timer

    $packageDir = Get-FullCLRPackageDirectory
    $extensionsPackageDir = Join-Path $packageDir "Extensions"
    $testImpactComComponentsDir = Join-Path $extensionsPackageDir "TestImpact"
    $legacyTestImpactComComponentsDir = Join-Path $extensionsPackageDir "V1\TestImpact"

    $testPlatformExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformExternalsVersion
    $testPlatformMsDiaVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.TestPlatformMSDiaVersion
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\eng\Versions.props)).Project.PropertyGroup.MicrosoftInternalCodeCoverageVersion
    $interopExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.InteropExternalsVersion

    # Copy Microsoft.VisualStudio.IO to root
    $codeCoverageIOPackageDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.CodeCoverage.IO\$codeCoverageExternalsVersion\lib\$TPB_TargetFramework451"
    Copy-Item $codeCoverageIOPackageDirectory\Microsoft.CodeCoverage.IO.dll $packageDir -Force
    if ($TPB_LocalizedBuild) {
        Copy-Loc-Files $codeCoverageIOPackageDirectory $packageDir "Microsoft.CodeCoverage.IO.resources.dll"
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
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformMsDiaVersion\tools\net451"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force

    # Copy Microsoft.Internal.Dia.Interop
    $internalDiaInterop = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia.Interop\$testPlatformMsDiaVersion\tools\net451"
    Copy-Item -Recurse $internalDiaInterop\* $packageDir -Force

    # Copy COM Components and their manifests over to Extensions Test Impact directory
    $comComponentsDirectoryTIA = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\$testPlatformMsDiaVersion\tools\net451"
    if (-not (Test-Path $testImpactComComponentsDir)) {
        New-Item $testImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $testImpactComComponentsDir -Force

    if (-not (Test-Path $legacyTestImpactComComponentsDir)) {
        New-Item $legacyTestImpactComComponentsDir -Type Directory -Force | Out-Null
    }
    Copy-Item -Recurse $comComponentsDirectoryTIA\* $legacyTestImpactComComponentsDir -Force

    Copy-Item (Join-Path $env:TP_PACKAGE_PROJ_DIR "ThirdPartyNotices.txt") $packageDir -Force
    
    Write-Log "Publish-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-VsixPackage {
    Write-Log "Create-VsixPackage: Started."
    $timer = Start-Timer

    Write-Verbose "Locating MSBuild install path..."
    $msbuildPath = Locate-MSBuildPath

    $vsixSourceDir = Join-Path $env:TP_ROOT_DIR "src\package\VSIXProject"
    $vsixProjectDir = Join-Path $env:TP_OUT_DIR "$TPB_Configuration\VSIX"

    # Create vsix only when msbuild is installed.
    if (![string]::IsNullOrEmpty($msbuildPath)) {
        # Copy the vsix project to artifacts directory to modify manifest
        New-Item $vsixProjectDir -Type Directory -Force
        Copy-Item -Recurse $vsixSourceDir\* $vsixProjectDir -Force

        # Update version of VSIX
        Update-VsixVersion $vsixProjectDir

        # Build vsix project to get TestPlatform.vsix
        Invoke-Exe $msbuildPath -Arguments """$vsixProjectDir\TestPlatform.csproj"" -p:Configuration=$Configuration"

        # Assert the VSIX, not the step. Invoke-Exe already fails the script on a non-zero msbuild
        # exit, but a msbuild that exits 0 without writing a .vsix leaves this function logging
        # "Complete" and a caller cannot tell that from success. This is the step whose whole purpose
        # is to produce one file, so check for the file.
        $producedVsix = @(Get-ChildItem $vsixProjectDir -Recurse -Filter "*.vsix" -ErrorAction SilentlyContinue)
        if ($producedVsix.Count -eq 0) {
            throw (".. Create-VsixPackage: msbuild exited 0 but produced no .vsix under '$vsixProjectDir'. " +
                   "The VSIX is the entire output of this step, so a green msbuild with no file is a failure.")
        }
        Write-Log ".. Create-VsixPackage: produced $($producedVsix.Count) vsix: $(($producedVsix | ForEach-Object { $_.Name }) -join ', ')"
    }
    else {
        throw ".. Create-VsixPackage: Cannot generate vsix as msbuild.exe not found at '$msbuildPath'."
    }

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Create-NugetPackages {
    $timer = Start-Timer

    Write-Log "Create-NugetPackages: Started."
    $stagingDir = Join-Path $env:TP_OUT_DIR $TPB_Configuration
    $packageOutputDir = $TPB_PackageOutDir

    Copy-Item (Join-Path $env:TP_PACKAGE_PROJ_DIR "Icon.png") $stagingDir -Force

    # STOP THE STAGED PROJECTS INHERITING THE REPO'S ARCADE PROPS. Without these two files
    # Microsoft.TestPlatform.CLI and Microsoft.TestPlatform.Build are SILENTLY NOT PRODUCED.
    #
    # $stagingDir is artifacts\<Config>, which is INSIDE the repository, so MSBuild's upward walk
    # from a staged .csproj reaches the repo root Directory.Build.props and imports the Arcade SDK.
    # Arcade sets IsPackable=false by default, and Pack on a non-packable project is a NO-OP that
    # reports "Build succeeded, 0 Error(s)" and exits 0. Nothing anywhere reports the absence:
    # Verify-Nuget-Packages below only iterates the packages it FINDS, so a missing package cannot
    # fail it, and the 12 keys of $expectedNumOfFiles pass while only 10 packages exist.
    #
    # MEASURED on this branch, not inferred:
    #   dotnet msbuild <staged>.csproj -getProperty:IsPackable            -> false
    #   dotnet pack --no-build <staged>.csproj                            -> exit 0, no package
    #   dotnet pack --no-build <staged>.csproj -p:IsPackable=true         -> exit 1,
    #     Workarounds.targets(74,5): Either PackageLicenseExpression or PackageLicenseFile must be
    #     specified  - so forcing IsPackable is the WRONG fix; it swaps a silent miss for an Arcade
    #     packaging rule these nuspec-driven projects were never meant to satisfy.
    #   with the two files below                                          -> exit 0, and
    #     Microsoft.TestPlatform.Build = 21 files, Microsoft.TestPlatform.CLI = 427 files, which
    #     are EXACTLY the counts scripts\verify-nupkgs.ps1 expects. Producing the right packages,
    #     not merely producing something.
    #
    # This is the repository's own established remedy rather than a new idea: test\TestAssets
    # carries the same pair for the same reason - those projects must not be treated as Arcade
    # projects either.
    #
    # The 10 nuspec packages are packed by nuget.exe, which does not evaluate MSBuild, so they were
    # never affected and their content does not change.
    '<Project />' | Set-Content (Join-Path $stagingDir "Directory.Build.props") -NoNewline
    '<Project />' | Set-Content (Join-Path $stagingDir "Directory.Build.targets") -NoNewline

    # Packages folder should not be cleared on CI.
    # Artifacts from source-build are downloaded into this directory before the build starts, and this would remove them.
    if (-not $TPB_CIBuild) {
        # Remove all locally built nuget packages before we start creating them
        # we are leaving them in the folder after uzipping them for easier review.
        if (Test-Path $packageOutputDir) {
            Remove-Item $packageOutputDir -Recurse -Force
        }
    }

    if (-not (Test-Path $packageOutputDir)) {
        New-Item $packageOutputDir -Type directory -Force
    }

    $tpNuspecDir = Join-Path $env:TP_PACKAGE_PROJ_DIR "nuspec"

    # Copy over the nuspecs to the staging directory
    $nuspecFiles = @(
        "Microsoft.CodeCoverage.nuspec",
        "Microsoft.NET.Test.Sdk.nuspec",
        "Microsoft.TestPlatform.AdapterUtilities.nuspec",
        "Microsoft.TestPlatform.nuspec",
        "Microsoft.TestPlatform.Portable.nuspec",
        "TestPlatform.Extensions.TrxLogger.nuspec",
        "TestPlatform.ObjectModel.nuspec",
        "TestPlatform.TestHost.nuspec",
        "TestPlatform.TranslationLayer.nuspec"
        "TestPlatform.Internal.Uwp.nuspec"
    )

    $projectFiles = @(
        "Microsoft.TestPlatform.CLI.csproj",
        "Microsoft.TestPlatform.Build.csproj"
    )

    $dependencies = @(   
        "TestPlatform.Build.nuspec",
        "TestPlatform.CLI.nuspec",

        ## .target and .props Files
        "Microsoft.NET.Test.Sdk.props", 
        "Microsoft.CodeCoverage.props",
        "Microsoft.CodeCoverage.targets",
        
        ## Content Directories
        "netcoreapp", 
        "netfx"
    )

    # Nuget pack analysis emits warnings if binaries are packaged as content. It is intentional for the below packages.
    $skipAnalysis = @(
        "TestPlatform.CLI.nuspec",
        "Microsoft.TestPlatform.CLI.csproj"
    )


    foreach ($item in $nuspecFiles + $projectFiles + $dependencies) {
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
    $dotnetExe = Get-DotNetPath

    # Pass Newtonsoft.Json version to nuget pack to keep the version consistent across all nuget packages.
    $JsonNetVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.JsonNetVersion

    # Additional external dependency folders
    $microsoftFakesVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.MicrosoftFakesVersion
    $FakesPackageDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.QualityTools.Testing.Fakes.TestRunnerHarness\$microsoftFakesVersion\contentFiles"

    $uap100PackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkUap100");

    # package them from stagingDir
    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        if ($file -eq "TestPlatform.Internal.Uwp.nuspec") {
            # this directory is mostly the same as the testhost10 dir, but has less libraries
            # and does not have netstandard2.0 versions, so I don't copy them by mistake
            $uap10Nuget = $uap100PackageDir
        }
        else {
            $uap10Nuget = $testhostUapPackageDir
        }

        Invoke-Exe $nugetExe -Arguments "pack $stagingDir\$file -OutputDirectory $packageOutputDir -Version $TPB_Version -Properties Version=$TPB_Version;JsonNetVersion=$JsonNetVersion;Runtime=$TPB_TargetRuntime;NetCoreTargetFramework=$TPB_TargetFrameworkCore20;FakesPackageDir=$FakesPackageDir;NetStandard10Framework=$TPB_TargetFrameworkNS10;NetStandard13Framework=$TPB_TargetFrameworkNS13;NetStandard20Framework=$TPB_TargetFrameworkNS20;Uap10Framework=$uap10Nuget;BranchName=$TPB_BRANCH;CommitId=$TPB_COMMIT $additionalArgs"
    }

    foreach ($file in $projectFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Host "Attempting to build package from '$file'."
        Invoke-Exe $dotnetExe -Arguments "restore $stagingDir\$file" -CaptureOutput | Out-Null
        Invoke-Exe $dotnetExe -Arguments "pack --no-build  $stagingDir\$file -o $packageOutputDir -p:Version=$TPB_Version -p:BranchName=`"$TPB_BRANCH`" -p:CommitId=`"$TPB_COMMIT`" /bl:pack_$file.binlog"
    }

    # Verifies that expected number of files gets shipped in nuget packages.
    # Few nuspec uses wildcard characters.
    Verify-Nuget-Packages $packageOutputDir $TPB_Version

    Write-Log "Create-NugetPackages: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-CodeCoverage-Package-Artifacts {
    # Copy TraceDataCollector to Microsoft.CodeCoverage folder.
    $codeCoverageExternalsVersion = ([xml](Get-Content $env:TP_ROOT_DIR\eng\Versions.props)).Project.PropertyGroup.MicrosoftInternalCodeCoverageVersion
    $traceDataCollectorPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.visualstudio.tracedatacollector\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $traceDataCollectorPackagesNetFxDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.VisualStudio.TraceDataCollector\$codeCoverageExternalsVersion\lib\$TPB_TargetFramework472"
    $internalCodeCoveragePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage\$codeCoverageExternalsVersion\contentFiles\any\any\"
    $codeCoverageCorePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.codecoverage.core\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageInterprocessPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.codecoverage.interprocess\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageInstrumentationPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.codecoverage.instrumentation\$codeCoverageExternalsVersion\lib\$TPB_TargetFrameworkNS20"
    $codeCoverageImUbuntuPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage\$codeCoverageExternalsVersion\contentFiles\any\any\InstrumentationEngine\ubuntu"
    $codeCoverageImAlpinePackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage\$codeCoverageExternalsVersion\contentFiles\any\any\InstrumentationEngine\alpine"
    $codeCoverageImMacosPackagesDir = Join-Path $env:TP_PACKAGES_DIR "microsoft.internal.codecoverage\$codeCoverageExternalsVersion\contentFiles\any\any\InstrumentationEngine\macos"

    $microsoftCodeCoveragePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.CodeCoverage\")
    $microsoftCodeCoverageExtensionsPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.CodeCoverage.Extensions\")

    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir -Force | Out-Null
    New-Item -ItemType directory -Path $microsoftCodeCoverageExtensionsPackageDir -Force | Out-Null

    Copy-Item $traceDataCollectorPackagesDir\Microsoft.VisualStudio.TraceDataCollector.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $traceDataCollectorPackagesDir\Microsoft.VisualStudio.TraceDataCollector.pdb $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageCorePackagesDir\Microsoft.CodeCoverage.Core.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInterprocessPackagesDir\Microsoft.CodeCoverage.Interprocess.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Microsoft.CodeCoverage.Instrumentation.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Mono.Cecil.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $codeCoverageInstrumentationPackagesDir\Mono.Cecil.Pdb.dll $microsoftCodeCoveragePackageDir -Force
    Copy-Item $internalCodeCoveragePackagesDir\CodeCoverage $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $internalCodeCoveragePackagesDir\InstrumentationEngine $microsoftCodeCoveragePackageDir -Force -Recurse
    Copy-Item $internalCodeCoveragePackagesDir\Shim $microsoftCodeCoveragePackageDir -Force -Recurse

    Copy-Item $traceDataCollectorPackagesNetFxDir\Microsoft.VisualStudio.TraceDataCollector.dll $microsoftCodeCoverageExtensionsPackageDir -Force

    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir\InstrumentationEngine\ubuntu\ -Force | Out-Null
    Copy-Item $codeCoverageImUbuntuPackagesDir\x64 $microsoftCodeCoveragePackageDir\InstrumentationEngine\ubuntu\ -Force -Recurse
    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir\InstrumentationEngine\alpine\ -Force | Out-Null
    Copy-Item $codeCoverageImAlpinePackagesDir\x64 $microsoftCodeCoveragePackageDir\InstrumentationEngine\alpine\ -Force -Recurse
    New-Item -ItemType directory -Path $microsoftCodeCoveragePackageDir\InstrumentationEngine\macos\ -Force | Out-Null
    Copy-Item $codeCoverageImMacosPackagesDir\x64 $microsoftCodeCoveragePackageDir\InstrumentationEngine\macos\ -Force -Recurse

    # Copy TraceDataCollector resource dlls
    if ($TPB_LocalizedBuild) {
        Copy-Loc-Files $traceDataCollectorPackagesDir $microsoftCodeCoveragePackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
        Copy-Loc-Files $traceDataCollectorPackagesNetFxDir $microsoftCodeCoverageExtensionsPackageDir "Microsoft.VisualStudio.TraceDataCollector.resources.dll"
    }
}

function Copy-PackageItems($packageName) {
    # Packages published separately are copied into their own artifacts directory
    # E.g. artifacts\bin\Microsoft.TestPlatform.ObjectModel\Debug\net451\* is copied
    # to artifacts\Debug\Microsoft.TestPlatform.ObjectModel\net451
    #
    # This reader was stale until build 3066979 and the "\bin\" search could not see it: the path was
    # assembled with [IO.Path]::Combine(..., "bin", ...), where the literal is "bin" with no separator
    # on either side. Measured from 3066979's Build log, not inferred:
    #   Microsoft.TestPlatform.Build -> artifacts\bin\Microsoft.TestPlatform.Build\Release\netstandard2.0\
    # so the ROOT moves and the <TFM> level below it is unchanged, which is what the trailing \* copies.
    $binariesDirectory = Get-ProjectOutputRoot -Project $packageName -Configuration $TPB_Configuration
    $binariesDirectory = $(Join-Path $binariesDirectory "*")
    $publishDirectory = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$packageName")
    Write-Log "Copy-PackageItems: Package: $packageName"
    Write-Verbose "Create $publishDirectory"
    New-Item -ItemType directory -Path $publishDirectory -Force | Out-Null

    Write-Log "Copy binaries for package '$packageName' from '$binariesDirectory' to '$publishDirectory'"
    Copy-Item -Path $binariesDirectory -Destination $publishDirectory -Recurse -Force
}

function Update-LocalizedResources {
    $timer = Start-Timer
    $dotnetExe = Get-DotNetPath

    Write-Log ".. Update-LocalizedResources: Started: $TPB_Solution"
    if (!$TPB_LocalizedBuild) {
        Write-Log ".. Update-LocalizedResources: Skipped based on user setting."
        return
    }

    $localizationProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "Localize\Localize.proj"
    Invoke-Exe $dotnetExe -Arguments "msbuild $localizationProject -m -nologo -v:minimal -t:Localize -p:LocalizeResources=true -nodeReuse:False"
    Write-Log ".. Update-LocalizedResources: Complete. {$(Get-ElapsedTime($timer))}"
}

#
# Helper functions
#
function Get-DotNetPath {
    # RESOLVE the SDK rather than hardcoding one location, for the same reason XlfToolPath is
    # resolved in TestPlatform.Localization.targets: under Arcade the tool is somewhere else.
    #
    # This function used to return $env:TP_TOOLS_DIR\dotnet\dotnet.exe unconditionally, because
    # this script's own InstallDotnet step put it there. Under Arcade the SDK is bootstrapped to
    # <root>\.dotnet instead, and the official pipeline deliberately omits InstallDotnet from
    # -Steps because Arcade has already done it. The result was that packaging died on its FIRST
    # action, before any of the layout or payload work could even be reached:
    #
    #   Get-DotNetPath : Dotnet.exe not found at D:\a\_work\1\s\tools\dotnet\dotnet.exe.
    #                    Did the dotnet cli installation succeed?
    #   at scripts\common.lib.ps1:153        build 3066271
    #
    # The message asks whether the install succeeded, which sends the reader to the install step.
    # The install succeeded; it just happened somewhere else.
    #
    # Order matters. DOTNET_ROOT is what Arcade's eng\common\tools.ps1 exports after bootstrapping,
    # so it is the most specific and most current answer. The repo-relative .dotnet is the same
    # location by another name, kept for a local build that never exported the variable. The
    # historic tools\dotnet path stays LAST rather than being deleted, so a pre-Arcade invocation
    # of this script - which is still a supported way to build this branch - keeps working.
    $candidates = @()
    if ($env:DOTNET_ROOT)   { $candidates += (Join-Path $env:DOTNET_ROOT "dotnet.exe") }
    if ($env:TP_ROOT_DIR)   { $candidates += (Join-Path $env:TP_ROOT_DIR ".dotnet\dotnet.exe") }
    if ($env:TP_TOOLS_DIR)  { $candidates += (Join-Path $env:TP_TOOLS_DIR "dotnet\dotnet.exe") }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            Write-Verbose "Get-DotNetPath: using $candidate"
            return $candidate
        }
    }

    # Name every location that was tried. The original error named one path and asked whether the
    # install had succeeded, which is the wrong question when the install succeeded elsewhere.
    Write-Error ("Dotnet.exe not found. Looked in order at:`n  " + ($candidates -join "`n  ") +
                 "`nUnder Arcade the SDK is bootstrapped to <root>\.dotnet by eng\common\tools.ps1, " +
                 "and the official pipeline omits this script's own InstallDotnet step because of that. " +
                 "Run restore first, or pass -Steps InstallDotnet to use the historic tools\dotnet location.")
}

function Get-FullCLRPackageDirectory {
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFramework451\$TPB_TargetRuntime")
}

function Get-FullCLRPackageDirectory45 {
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFramework45\$TPB_TargetRuntime")
}

function Get-CoreCLR20PackageDirectory {
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20")
}

function Get-CoreCLR10PackageDirectory {
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore10")
}

function Get-CoreCLR20TestHostPackageDirectory {
    return $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\$TPB_TargetFrameworkCore20\TestHost")
}

function Locate-MSBuildPath {
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

function Locate-VsInstallPath {
    $vswhere = Join-Path -path $env:TP_PACKAGES_DIR -ChildPath "vswhere\$env:VSWHERE_VERSION\tools\vswhere.exe"
    if (!(Test-Path -path $vswhere)) {
        throw "Unable to locate vswhere in path '$vswhere'."
    }

    Write-Verbose "Using '$vswhere' to locate VS installation path."

    # Two lists, and the split is deliberate. This is the same selector 16.11 already corrected, and
    # the reasoning is copied here rather than referenced so a later reader does not have to find it.
    #
    # Microsoft.Net.Component.4.6.TargetingPack used to be required here, and it selects ZERO
    # instances on the build agent. It is worth recording what that measurement does and does not
    # mean, because the first reading of it was wrong:
    #
    #   Reference Assemblies\...\.NETFramework\v4.6 EXISTS   -> looks like the pack is installed
    #   it holds 126 files and NO mscorlib.dll               -> it is a leftover shell, not a pack
    #   v4.6.2, v4.7.2 and v4.8 hold 258-260 files WITH mscorlib.dll
    #
    # So the component id was telling the truth: 4.6 targeting really is unavailable on a current
    # machine. Testing a DIRECTORY for existence rather than for contents is what made it look like a
    # false positive.
    #
    # It is still dropped from the SELECTOR, for a different and narrower reason: a selector should
    # select. Requiring it makes vswhere return nothing and the run stops with "Failed to find VS
    # installation with requirements: <three component ids>", which does not say which of the three
    # is missing or what to do. Letting the selector find the instance moves the failure to reference
    # resolution, where MSBuild says exactly what is wrong and how to fix it:
    #
    #   error MSB3644: The reference assemblies for .NETFramework,Version=v4.6 were not found.
    #                  ... install the Developer Pack (SDK/Targeting Pack) for this framework version
    #                  or retarget your application.
    #
    # Both stop the build at the same point - Locate-MSBuildPath runs after all the payload copying
    # either way - so nothing is lost by preferring the message that names the missing thing.
    $requiredPackageIds = @("Microsoft.Component.MSBuild", "Microsoft.VisualStudio.Component.VSSDK")
    Write-Verbose "VSInstallation requirements : $requiredPackageIds"

    Try {
        # -prerelease on both branches now. Without it, vswhere finds NOTHING on a machine whose only
        # installations are preview or internal channels, and the CI image this line builds on is
        # windows.vs2026preview. The flag was omitted under CIBuild on the assumption that a build
        # agent carries a released VS, and that assumption no longer holds.
        #
        # Called directly rather than through Invoke-Exe: vswhere exits 0 and prints nothing when it
        # matches no instance, so Invoke-Exe cannot report this as a failure either way, and the
        # direct call passes '(15.0' as one argument instead of splitting a string.
        Write-Verbose "VSWhere command line: $vswhere -version '(15.0' -prerelease -products * -requires $requiredPackageIds -property installationPath"
        $vsInstallPath = & $vswhere -version '(15.0' -prerelease -products * -requires $requiredPackageIds -property installationPath
    }
    Catch [System.Management.Automation.MethodInvocationException] {
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

function Update-VsixVersion($vsixProjectDir) {
    Write-Log "Update-VsixVersion: Started."
    $vsixVersion = $Version

    # Build number comes in the form 20170111-01(yyyymmdd-buildNoOfThatDay)
    # So Version of the vsix will be 15.1.0.2017011101
    $vsixVersionSuffix = $BuildNumber.Split("-");
    if ($vsixVersionSuffix.Length -ige 2) {
        $vsixVersion = "$vsixVersion.$($vsixVersionSuffix[0])$($vsixVersionSuffix[1])"
    }

    $manifestContentWithVersion = Get-Content "$vsixProjectDir\source.extension.vsixmanifest" -raw | ForEach-Object { $_.ToString().Replace("`$version`$", "$vsixVersion") }
    Set-Content -path "$vsixProjectDir\source.extension.vsixmanifest" -value $manifestContentWithVersion

    Write-Log "Update-VsixVersion: Completed."
}

function Generate-Manifest ($PackageFolder) {
    $packagesFolderName = [System.IO.Path]::GetFileName($PackageFolder)
    Write-Log "Generate-Manifest ($packagesFolderName): Started."

    $generateManifestPath = Join-Path $env:TP_ROOT_DIR "scripts\build\GenerateManifest.proj"
    $msbuildPath = Locate-MSBuildPath

    Invoke-Exe $msbuildPath -Arguments "$generateManifestPath /t:PublishToBuildAssetRegistry /p:PackagesToPublishPattern=$PackageFolder\*.nupkg /p:BUILD_BUILDNUMBER=$BuildNumber /p:PackagesPath=""$PackageFolder"" /p:Configuration=$TPB_Configuration /bl:""$env:TP_OUT_DIR\log\$Configuration\manifest-generation-$packagesFolderName.binlog"""

    Write-Log "Generate-Manifest ($packagesFolderName): Completed."
}

function Build-SpecificProjects {
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
            if ($_.FullName -match $ProjectNamePattern) {
                $ProjectsToBuild += , "$_"
            }
        }
    }

    if ( $null -eq $ProjectsToBuild) {
        Write-Error "No csproj name match for given pattern: $ProjectNamePatterns"
    }

    # Build Projects.
    foreach ($ProjectToBuild in $ProjectsToBuild) {
        Write-Log "Building Project $ProjectToBuild"
        # Restore and Build
        $output = Invoke-Exe $dotnetPath -Arguments "restore $ProjectToBuild"
        PrintAndExit-OnError $output
        $output = Invoke-Exe $dotnetPath -Arguments "build $ProjectToBuild"
        PrintAndExit-OnError $output

        if (-Not ($ProjectToBuild.FullName -contains "$($env:TP_ROOT_DIR)$([IO.Path]::DirectorySeparatorChar)src")) {
            # Don't copy artifacts for non src folders.
            continue;
        }

        # Copy artifacts
        $ProjectDir = [System.IO.Path]::GetDirectoryName($ProjectToBuild)
        foreach ($FrameworkAndOutDir in $FrameworksAndOutDirs) {
            # Same Era A staging translation as every other reader. This one is only reached via
            # -ProjectNamePatterns, a developer convenience path that CI never takes, so it could not
            # have failed the pipeline - but it is the same defect and it is corrected here so a later
            # enumeration does not have to rediscover it.
            $fromDir = $(Join-Path (Get-ProjectOutputRoot -Project ([System.IO.Path]::GetFileName($ProjectDir)) -Configuration $TPB_Configuration) $FrameworkAndOutDir[0])
            $toDir = $([System.IO.Path]::Combine($env:TP_OUT_DIR, $TPB_Configuration, $FrameworkAndOutDir[1]))
            if ( Test-Path $fromDir) {
                Write-Log "Copying artifacts from $fromDir to $toDir"
                Get-ChildItem $fromDir | ForEach-Object {
                    if (-not ($_.PSIsContainer)) {
                        Copy-Item $_.FullName $toDir
                    }
                }
            }
        }
    }
}

if ($ProjectNamePatterns.Count -ne 0) {
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
    # Must follow Restore-Package: it reads the shipped Microsoft.TestPlatform 17.3.0 that
    # Restore-Package downloads, and it must precede Pack, which consumes what it stages.
    Restore-FakesPayload
}

if ($Force -or $Steps -contains "UpdateLocalization") {
    Update-LocalizedResources
}

if ($Force -or $Steps -contains "Build") {
    Invoke-Build
}

if ($Force -or $Steps -contains "Publish") {
    Publish-Package
    Publish-VsixPackage
}

if ($Force -or $Steps -contains "Pack") { 
    Create-VsixPackage
    Create-NugetPackages
}

if ($Force -or $Steps -contains "Manifest") {
    Generate-Manifest -PackageFolder $TPB_PackageOutDir
    if (Test-Path $TPB_SourceBuildPackageOutDir)
    {
        Generate-Manifest -PackageFolder $TPB_SourceBuildPackageOutDir
    }
    Copy-PackageIntoStaticDirectory
}

if ($Force -or $Steps -contains "PrepareAcceptanceTests") {
    $shell = (Get-Process -Id $PID).Path
    Invoke-Exe $shell -Arguments "-NoProfile -File ""$env:TP_ROOT_DIR\eng\Install-TestSdks.ps1"""
    Copy-PackageIntoStaticDirectory
    Publish-PatchedDotnet
    Invoke-TestAssetsBuild
    Publish-Tests
}

if ($Script:ScriptFailed) {
    Write-Log "Build failed. {$(Get-ElapsedTime($timer))}" -Level "Error"
    Exit 1
}
else {
    Write-Log "Build succeeded. {$(Get-ElapsedTime($timer))}"
    Exit 0
}
