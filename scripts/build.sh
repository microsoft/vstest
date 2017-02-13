#!/usr/bin/env/ bash

function usage()
{
    echo " Usage: ./build.sh [Options]"
    echo ""
    echo " -c <CONFIGURATION>             Build the specified Configuration (Debug or Release, default: Debug)"
    echo " -r <TARGET_RUNTIME>            Build for the specified runtime moniker (ubuntu.14.04-x64)"
    echo " -v <VERSION>                   Version number for the package generated (15.0.0)"
    echo " -vs <VERSION_SUFFIX>           Version suffix for package generated (dev)"
    echo " -loc <DISABLE_LOCALIZED_BUILD> Disable Localized builds (true,false)"
    echo " -ci <CI_BUILD>                 Declares if this is a CI_BUILD or not"
    echo " -p <PROJECT_NAME_PATTERNS>     Pattern to build specific projects"
}

CONFIGURATION="Debug"
TARGET_RUNTIME="osx.10.11-x64"
VERSION="15.0.0"
VERSION_SUFFIX="DEV"
FAIL_FAST=false
SYNC_XLF=false
DISABLE_LOCALIZED_BUILD=false
CI_BUILD=false
PROJECT_NAME_PATTERNS=

while [ "$1" != "" ]; do
    PARAM=`echo $1 | awk -F= '{print $1}'`
    VALUE=`echo $1 | awk -F= '{print $2}'`
    case $PARAM in
            -h | --help)
                    usage
                    exit
                    ;;
                -c)
            CONFIGURATION=$VALUE
            ;;
        -r)
            TARGET_RUNTIME=$VALUE
            ;;
        -v)
            VERSION=$VALUE
            ;;
        -vs)
            VERSION_SUFFIX=$VALUE
            ;;
        -loc)
            DISABLE_LOCALIZED_BUILD=$VALUE
            ;;
        -ci)
                    CI_BUILD=$VALUE
                    ;;
                -p)
            PROJECT_NAME_PATTERNS=$VALUE
            ;;
    esac
done

#
# Variables
#
echo "Setup environment variables."
TP_ROOT_DIR=$(cd "$(dirname "$0")"; pwd -P)
TP_TOOLS_DIR="$TP_ROOT_DIR/tools"
TP_PACKAGES_DIR="$TP_ROOT_DIR/packages"
TP_OUT_DIR="$TP_ROOT_DIR/artifacts"
TP_PACKAGE_PROJ_DIR="$TP_ROOT_DIR/src/package/package"
TP_PACKAGE_NUSPEC_DIR="$TP_ROOT_DIR/src/package/nuspec"
TP_SRC_DIR="$TP_ROOT_DIR/src"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
echo "Setup dotnet configuration."
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# Dotnet build doesnt support --packages yet. See https://github.com/dotnet/cli/issues/2712
export NUGET_PACKAGES=$TP_PACKAGES_DIR
DOTNET_CLI_VERSION="latest"

#
# Build configuration
#
echo "Setup build configuration."
TPB_Solution="TestPlatform.sln"
TPB_TargetFrameworkCore="netcoreapp1.0"
TPB_Configuration=$CONFIGURATION
TPB_TargetRuntime=$TARGET_RUNTIME
TPB_Version=$VERSION
TPB_VersionSuffix=$VERSION_SUFFIX
TPB_CIBuild=$CI_BUILD

function installdotnetcli()
{
    echo "Installing dotnet cli..."
    start=$SECONDS

    [[ -z "$DOTNET_INSTALL_DIR" ]] && export DOTNET_INSTALL_DIR="$TP_TOOLS_DIR/dotnet"
    [[ -d "$DOTNET_INSTALL_DIR" ]] || mkdir -p $DOTNET_INSTALL_DIR

    DOTNET_INSTALL_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ ! -e $DOTNET_INSTALL_PATH ]]; then

        # Install a stage 0
        DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh"
        curl -sSL "$DOTNET_INSTALL_SCRIPT_URL" | bash /dev/stdin  --version $DOTNET_CLI_VERSION --verbose
    fi

    echo "installdotnetcli: Complete. Elapsed $(( SECONDS - start ))"
}


function restorepackage()
{
    echo "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    start=$SECONDS
    DOTNET_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ -e $DOTNET_PATH ]]; then
        echo "dotnet not found at $DOTNET_PATH. Did the dotnet cli installation succeed?"
    fi

    echo ".. .. Restore-Package: Source: $TPB_Solution" \
    && $DOTNET_PATH restore $TPB_Solution --packages $TP_PACKAGES_DIR -v:minimal
    echo ".. .. Restore-Package: Source: $TP_ROOT_DIR/src/package/external/external.csproj" \
    && $DOTNET_PATH restore $TP_ROOT_DIR/src/package/external/external.csproj --packages $TP_PACKAGES_DIR -v:minimal
    echo ".. .. Restore-Package: Complete."

    if [[ $? -ne 0 ]]; then
        echo "Restore Package failed."
        exit
    fi

    echo "Restore-Package: Complete. Elapsed $(( SECONDS - start ))"
}

function invokebuild()
{
    echo "Invoke-Build: Start build."
    start=$SECONDS
    DOTNET_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ -e $DOTNET_PATH ]]; then
        echo "dotnet not found at $DOTNET_PATH. Did the dotnet cli installation succeed?"
    fi

    echo ".. .. Build: Source: $TPB_Solution"
    
    #Need to target the appropriate targetframework for each project until netstandard2.0 ships
    $DOTNET_PATH build ./src/Microsoft.TestPlatform.CrossPlatEngine/Microsoft.TestPlatform.CrossPlatEngine.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/testhost.x86/testhost.x86.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.PlatformAbstractions/Microsoft.TestPlatform.PlatformAbstractions.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.PlatformAbstractions/Microsoft.TestPlatform.PlatformAbstractions.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.0

    $DOTNET_PATH build ./src/package/package/package.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.ObjectModel/Microsoft.TestPlatform.ObjectModel.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.VsTestConsole.TranslationLayer/Microsoft.TestPlatform.VsTestConsole.TranslationLayer.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/datacollector/datacollector.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/vstest.console/vstest.console.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.Common/Microsoft.TestPlatform.Common.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.Client/Microsoft.TestPlatform.Client.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.Extensions.TrxLogger/Microsoft.TestPlatform.Extensions.TrxLogger.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.Utilities/Microsoft.TestPlatform.Utilities.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.CommunicationUtilities/Microsoft.TestPlatform.CommunicationUtilities.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.5

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.Build/Microsoft.TestPlatform.Build.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.3

    $DOTNET_PATH build ./src/testhost/testhost.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netcoreapp1.0

    $DOTNET_PATH build ./src/Microsoft.TestPlatform.CoreUtilities/Microsoft.TestPlatform.CoreUtilities.csproj --configuration $TPB_Configuration --version-suffix $TPB_VersionSuffix -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:SyncXlf=$SYNC_XLF -p:TargetFramework=netstandard1.4

    echo ".. .. Build: Complete."
    if [[ $? -ne 0 ]]; then
        echo "Invoke Build failed."
        exit
    fi

    echo "Invoke-Build: Complete. Elapsed $(( SECONDS - start ))"
}

function publishpackage()
{
    echo "Publish-Package: Started."
    start=$SECONDS
    DOTNET_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ -e $DOTNET_PATH ]]; then
        echo "dotnet not found at $DOTNET_PATH. Did the dotnet cli installation succeed?"
    fi
    
    coreCLRPackageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFrameworkCore
    packageProject=$TP_PACKAGE_PROJ_DIR/package.csproj
    testHostProject=$TP_ROOT_DIR/src/testhost/testhost.csproj
    testHostx86Project=$TP_ROOT_DIR/src/testhost.x86/testhost.x86.csproj
    testhostCorePackageDir=$TP_OUT_DIR/$TPB_Configuration/Microsoft.TestPlatform.TestHost/$TPB_TargetFrameworkCore
    vstestConsoleProject=$TP_ROOT_DIR/src/vstest.console/vstest.console.csproj
    dataCollectorProject=$TP_ROOT_DIR/src/datacollector/datacollector.csproj

    echo "Package: Publish package\*.csproj"

    echo "$DOTNET_PATH publish $packageProject --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $coreCLRPackageDir -v:minimal -p:SyncXlf=$SYNC_XLF -p:LocalizedBuild=$TPB_LocalizedBuild"
     $DOTNET_PATH publish $packageProject --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $coreCLRPackageDir -v:minimal -p:SyncXlf=$SYNC_XLF -p:LocalizedBuild=$TPB_LocalizedBuild

    # Publish vstest.console and datacollector exclusively because *.config/*.deps.json file is not getting publish when we are publishing aforementioned project through dependency.    
    echo "Package: Publish src\vstest.console\vstest.console.csproj"
    $DOTNET_PATH publish $vstestConsoleProject --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $coreCLRPackageDir -v:minimal -p:SyncXlf=$SYNC_XLF -p:LocalizedBuild=$TPB_LocalizedBuild

    echo "Package: Publish src\datacollector\datacollector.csproj"
    $DOTNET_PATH publish $dataCollectorProject --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $coreCLRPackageDir -v:minimal -p:SyncXlf=$SYNC_XLF -p:LocalizedBuild=$TPB_LocalizedBuild

    # Publish testhost
    
    echo "Package: Publish testhost\testhost.csproj"
    $DOTNET_PATH publish $testHostProject --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $testhostCorePackageDir -v:minimal -p:SyncXlf=$SYNC_XLF -p:LocalizedBuild=$TPB_LocalizedBuild

    # Copy over the logger assemblies to the Extensions folder.
    coreCLRExtensionsDir="$coreCLRPackageDir/Extensions"
    # Create an extensions directory.
    mkdir -p $coreCLRExtensionsDir

    # Note Note: If there are some dependencies for the logger assemblies, those need to be moved too. 
    # Ideally we should just be publishing the loggers to the Extensions folder.
    loggers=("Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll" "Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.pdb")
    for i in ${loggers[@]}; do
        mv $coreCLRPackageDir/${i} $coreCLRExtensionsDir
    done

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    packageName="Microsoft.TestPlatform.Build"
    binariesDirectory="src/$packageName/bin/$TPB_Configuration"
    publishDirectory="$TP_OUT_DIR/$TPB_Configuration/$packageName"
    mkdir -p $publishDirectory
    cp -r $binariesDirectory $publishDirectory

    echo "Publish-Package: Complete. Elapsed $(( SECONDS - start ))"
    
    publishplatformatbstractions
}

function publishplatformatbstractions()
{
    echo "Publish-PlatfromAbstractions-Internal: Started."
    
    start=$SECONDS
    coreCLRPackageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFrameworkCore
    
    platformAbstraction="$TP_ROOT_DIR/src/Microsoft.TestPlatform.PlatformAbstractions/bin/$TPB_Configuration"
    platformAbstractionNetCore=$platformAbstraction/$TPB_TargetFrameworkCore
    
    cp -r $platformAbstractionNetCore $coreCLRPackageDir
    
    echo "Publish-PlatfromAbstractions-Internal: Complete. Elapsed $(( SECONDS - start ))"
}

function createnugetpackages()
{
    start=$SECONDS
    echo "Create-NugetPackages: Started."
    stagingDir="$TP_OUT_DIR/$TPB_Configuration"

    DOTNET_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ -e $DOTNET_PATH ]]; then
        echo "dotnet not found at $DOTNET_PATH. Did the dotnet cli installation succeed?"
    fi

    nuspecFiles=("TestPlatform.TranslationLayer.nuspec" "TestPlatform.ObjectModel.nuspec" "TestPlatform.TestHost.nuspec" "TestPlatform.nuspec" "TestPlatform.CLI.nuspec" "TestPlatform.Build.nuspec" Microsoft.NET.Test.Sdk.nuspec)
    projectFiles=("Microsoft.TestPlatform.CLI.csproj" "Microsoft.TestPlatform.Build.csproj")
    binDir="$TP_ROOT_DIR/bin/packages"

    for i in ${nuspecFiles[@]}; do
        cp $TP_PACKAGE_NUSPEC_DIR/${i} $stagingDir
    done
    for i in ${projectFiles[@]}; do
        cp $TP_PACKAGE_NUSPEC_DIR/${i} $stagingDir
    done

    # Copy and rename props file.
    cp "$TP_PACKAGE_NUSPEC_DIR/Microsoft.NET.Test.Sdk.props" $stagingDir

    # Copy over empty and third patry notice file
    cp "$TP_PACKAGE_NUSPEC_DIR/_._" $stagingDir
    cp "$TP_PACKAGE_NUSPEC_DIR/../ThirdPartyNotices.txt" $stagingDir


    for i in ${projectFiles[@]}; do
        echo "$DOTNET_PATH pack --no-build $stagingDir/${i} -o $binDir --version-suffix $TPB_VersionSuffix" /p:Version=$TPB_Version \
        && $DOTNET_PATH pack --no-build $stagingDir/${i} -o $binDir --version-suffix $TPB_VersionSuffix /p:Version=$TPB_Version
    done

    echo "Create-NugetPackages: Elapsed $(( SECONDS - start ))"
}

# Execute build
start=$SECONDS
echo "Build started"
echo "Test platform environment variables: "
compgen -A variable | grep "TP_"

echo "Test platform build variables: "
compgen -A variable | grep "TPB_"

installdotnetcli
restorepackage
invokebuild
publishpackage
createnugetpackages

echo "Build complete. $(( SECONDS - start ))"

if [[ $? -ne 0 ]]; then
    exit 1
fi
