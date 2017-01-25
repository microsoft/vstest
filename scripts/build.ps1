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

    # Versioning scheme = Major(15).Minor(RTW, Updates).SubUpdates(preview4, preview5, RC etc)
    # E.g. VS 2017 Update 1 Preview will have version 15.1.1
    [Parameter(Mandatory=$false)]
    [Alias("v")]
    [System.String] $Version = "15.0.0",

    [Parameter(Mandatory=$false)]
    [Alias("vs")]
    [System.String] $VersionSuffix = "dev",

    [Parameter(Mandatory=$false)]
    [Alias("ff")]
    [System.Boolean] $FailFast = $true,

    [Parameter(Mandatory=$false)]
    [Alias("xlf")]
    [Switch] $SyncXlf = $false,

    [Parameter(Mandatory=$false)]
    [Alias("loc")]
    [Switch] $DisableLocalizedBuild = $false,

    [Parameter(Mandatory=$false)]
    [Alias("ci")]
    [Switch] $CIBuild = $false
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
$env:DOTNET_CLI_VERSION = "latest"

#
# Build configuration
#
Write-Verbose "Setup build configuration."
$TPB_Solution = "TestPlatform.sln"
$TPB_TargetFramework = "net46"
$TPB_TargetFrameworkCore = "netcoreapp1.0"
$TPB_Configuration = $Configuration
$TPB_TargetRuntime = $TargetRuntime
$TPB_Version = $Version
$TPB_VersionSuffix = $VersionSuffix
$TPB_CIBuild = $CIBuild
$TPB_LocalizedBuild = !$DisableLocalizedBuild
$TPB_VSIX_DIR = Join-Path $env:TP_ROOT_DIR "src\VSIX"

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
    & $dotnetInstallScript -InstallDir $dotnetInstallPath -NoPath -Version $env:DOTNET_CLI_VERSION

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
    & $dotnetExe restore $TPB_Solution --packages $env:TP_PACKAGES_DIR -v:minimal
    Write-Log ".. .. Restore-Package: Source: $env:TP_ROOT_DIR\src\package\external\external.csproj"
    & $dotnetExe restore $env:TP_ROOT_DIR\src\package\external\external.csproj --packages $env:TP_PACKAGES_DIR -v:minimal
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
    Write-Verbose "$dotnetExe build $TPB_Solution --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SyncXlf"
    & $dotnetExe build $TPB_Solution --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SyncXlf
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
    $packageProject = Join-Path $env:TP_PACKAGE_PROJ_DIR "package.csproj"
    $testHostProject = Join-Path $env:TP_ROOT_DIR "src\testhost\testhost.csproj"
    $testHostx86Project = Join-Path $env:TP_ROOT_DIR "src\testhost.x86\testhost.x86.csproj"
    $testhostFullPackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFramework\$TPB_TargetRuntime")
    $testhostCorePackageDir = $(Join-Path $env:TP_OUT_DIR "$TPB_Configuration\Microsoft.TestPlatform.TestHost\$TPB_TargetFrameworkCore")
    $vstestConsoleProject = Join-Path $env:TP_ROOT_DIR "src\vstest.console\vstest.console.csproj"
    $dataCollectorProject = Join-Path $env:TP_ROOT_DIR "src\datacollector\datacollector.csproj"
    $dataCollectorx86Project = Join-Path $env:TP_ROOT_DIR "src\datacollector.x86\datacollector.x86.csproj"

    Write-Log "Package: Publish package\*.csproj"
	
    Publish-Package-Internal $packageProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $packageProject $TPB_TargetFrameworkCore $coreCLRPackageDir

    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.
    
    Write-Log "Package: Publish src\vstest.console\vstest.console.csproj"
    Publish-Package-Internal $vstestConsoleProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $vstestConsoleProject $TPB_TargetFrameworkCore $coreCLRPackageDir

    Write-Log "Package: Publish src\datacollector\datacollector.csproj"
    Publish-Package-Internal $dataCollectorProject $TPB_TargetFramework $fullCLRPackageDir
    Publish-Package-Internal $dataCollectorProject $TPB_TargetFrameworkCore $coreCLRPackageDir

    Write-Log "Package: Publish src\datacollector.x86\datacollector.x86.csproj"
    Publish-Package-Internal $dataCollectorx86Project $TPB_TargetFramework $fullCLRPackageDir

    # Publish testhost
    
    Write-Log "Package: Publish testhost\testhost.csproj"
    Publish-Package-Internal $testHostProject $TPB_TargetFramework $testhostFullPackageDir
    Publish-Package-Internal $testHostProject $TPB_TargetFrameworkCore $testhostCorePackageDir

    Write-Log "Package: Publish testhost.x86\testhost.x86.csproj"
    Publish-Package-Internal $testHostx86Project $TPB_TargetFramework $testhostFullPackageDir

    # Copy over the Full CLR built testhost package assemblies to the $fullCLRPackageDir
    Copy-Item $testhostFullPackageDir\* $fullCLRPackageDir -Force

    # Copy over the Full CLR built testhost package assemblies to the Core CLR package folder.
    $netFull_Dir = "TestHost"
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


function Publish-Package-Internal($packagename, $framework, $output)
{
    Write-Verbose "$dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:SyncXlf=$SyncXlf -p:LocalizedBuild=$TPB_LocalizedBuild"
    & $dotnetExe publish $packagename --configuration $TPB_Configuration --framework $framework --output $output -v:minimal -p:SyncXlf=$SyncXlf -p:LocalizedBuild=$TPB_LocalizedBuild
}

function Create-VsixPackage
{
    $timer = Start-Timer

    Write-Log "Create-VsixPackage: Started."
    $packageDir = Get-FullCLRPackageDirectory

    # Copy legacy dependencies
    $legacyDir = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.TestPlatform.Extensions\15.0.0\contentFiles\any\any"
    Copy-Item -Recurse $legacyDir\* $packageDir -Force

    # Copy COM Components and their manifests over
    $comComponentsDirectory = Join-Path $env:TP_PACKAGES_DIR "Microsoft.Internal.Dia\14.0.0\contentFiles\any\any"
    Copy-Item -Recurse $comComponentsDirectory\* $packageDir -Force

    #Copy [Content_Types].xml and License.rtf
    Copy-Item $TPB_VSIX_DIR\*.xml $packageDir -Force

    $fileToCopy = Join-Path $TPB_VSIX_DIR "License.rtf"
    Copy-Item $fileToCopy $packageDir -Force

    $fileToCopy = Join-Path $env:TP_PACKAGE_PROJ_DIR "ThirdPartyNotices.txt"
    Copy-Item $fileToCopy $packageDir -Force

    #update version of VSIX
    Update-VsixVersion

    # Zip the folder
    # TODO remove vsix creator
    $dotnetExe = Get-DotNetPath
    & $dotnetExe restore src\Microsoft.TestPlatform.VSIXCreator\Microsoft.TestPlatform.VSIXCreator.csproj
    & $dotnetExe build src\Microsoft.TestPlatform.VSIXCreator\Microsoft.TestPlatform.VSIXCreator.csproj
    & src\Microsoft.TestPlatform.VSIXCreator\bin\$TPB_Configuration\net46\Microsoft.TestPlatform.VSIXCreator.exe $packageDir $env:TP_OUT_DIR\$TPB_Configuration

    Write-Log "Create-VsixPackage: Complete. {$(Get-ElapsedTime($timer))}"
}

function Update-VsixVersion
{
    Write-Log "Update-VsixVersion: Started."

    $packageDir = Get-FullCLRPackageDirectory
    $vsixVersion = "15.0.3" # Hardcode since we want to keep 15.0.0 for other assemblies.

    # VersionSuffix in microbuild comes in the form preview-20170111-01(preview-yyyymmdd-buildNoOfThatDay)
    # So Version of the vsix will be 15.0.3.2017011101
    $vsixVersionSuffix = $VersionSuffix.Split("-");
    if($vsixVersionSuffix.Length -ige 2) {
        $vsixVersion = "$vsixVersion.$($vsixVersionSuffix[1])$($vsixVersionSuffix[2])"
    }

    $filesToUpdate = @("extension.vsixmanifest",
        "manifest.json",
        "catalog.json")

    foreach ($file in $filesToUpdate) {
        Get-Content "$TPB_VSIX_DIR\$file" -raw | % {$_.ToString().Replace("`$version`$", "$vsixVersion") } | Set-Content "$packageDir\$file"
    }

    $fileToUpdate = Join-Path $env:TP_ROOT_DIR "artifacts\$TPB_Configuration\Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI.json"
    Get-Content "$TPB_VSIX_DIR\Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI.json" -raw | % {$_.ToString().Replace("`$version`$", "$vsixVersion") } | Set-Content $fileToUpdate

    Write-Log "Update-VsixVersion: Completed."
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

    # Copy and rename props file.
    Copy-Item $tpSrcDir\"Microsoft.Net.Test.Sdk_props" $stagingDir\"Microsoft.Net.Test.Sdk.props" -Force

    # Copy over empty and third patry notice file
    Copy-Item $tpSrcDir\package\"_._" $stagingDir -Force
    Copy-Item $tpSrcDir\package\"ThirdPartyNotices.txt" $stagingDir -Force

    # Call nuget pack on these components.
    $nugetExe = Join-Path $env:TP_PACKAGES_DIR -ChildPath "Nuget.CommandLine" | Join-Path -ChildPath $env:NUGET_EXE_Version | Join-Path -ChildPath "tools\NuGet.exe"

    foreach ($file in $nuspecFiles) {
        $additionalArgs = ""
        if ($skipAnalysis -contains $file) {
            $additionalArgs = "-NoPackageAnalysis"
        }

        Write-Verbose "$nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir -Version=$Version-$VersionSuffix -Properties Version=$Version-$VersionSuffix $additionalArgs"
        & $nugetExe pack $stagingDir\$file -OutputDirectory $stagingDir -Version $Version-$VersionSuffix -Properties Version=$Version-$VersionSuffix`;Runtime=$TPB_TargetRuntime $additionalArgs
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

function Update-LocalizedResources
{
    $timer = Start-Timer

    Write-Log "Update-LocalizedResources: Started."

    # For each resx file, file the xlf files in all languages
    # Sync the resx to xlf to ensure all new resources are added
    $xlfTool = Join-Path $env:TP_PACKAGES_DIR "fmdev.xlftool\0.1.2\tools\xlftool.exe"
    $resxFiles = Get-ChildItem -Recurse -Include *.resx "$env:TP_ROOT_DIR\src"

    foreach ($resxFile in $resxFiles) {
        Write-Log "... Resource: $resxFile"

        foreach ($lang in @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant")) {
            $xlfFile = Join-Path $($resxFile.Directory.FullName) "xlf\$($resxFile.BaseName).$lang.xlf"

            Write-VerboseLog "$xlfTool update -resx $($resxFile.FullName) -xlf $xlfFile -verbose"
            & $xlfTool update -resx $resxFile.FullName -xlf $xlfFile -verbose
        }
    }

    Write-Log "Update-LocalizedResources: Complete. {$(Get-ElapsedTime($timer))}"
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
#Update-LocalizedResources
Invoke-Build
Publish-Package
Create-VsixPackage
Create-NugetPackages

Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"

if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
