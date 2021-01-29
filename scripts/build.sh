#!/usr/bin/env/ bash
# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

set -o nounset  # Fail on uninitialized variables.
set -e          # Fail on non-zero exit code.

# ANSI color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NOCOLOR='\033[0m'

#
# Parse options
#
CONFIGURATION="Debug"
TARGET_RUNTIME="ubuntu.18.04-x64"
VERSION="" # Will set this later by reading TestPlatform.Settings.targets file.
VERSION_SUFFIX="dev"
FAIL_FAST=false
DISABLE_LOCALIZED_BUILD=false
CI_BUILD=false
VERBOSE=false
PROJECT_NAME_PATTERNS=

#
# Source build repo api
# See https://github.com/dotnet/source-build/blob/dev/release/2.0/Documentation/RepoApi.md
#
DOTNET_BUILD_FROM_SOURCE=0
DOTNET_CORE_SDK_DIR=
DOTNET_BUILD_TOOLS_DIR=

while [ $# -gt 0 ]; do
    lowerI="$(echo ${1:-} | awk '{print tolower($0)}')"
    case $lowerI in
        -h | --help)
            usage
            exit
            ;;
        -c)
            CONFIGURATION=$2
            shift
            ;;
        -r)
            TARGET_RUNTIME=$2
            shift
            ;;
        -v)
            VERSION=$2
            shift
            ;;
        -vs)
            VERSION_SUFFIX=$2
            shift
            ;;
        -noloc)
            DISABLE_LOCALIZED_BUILD=$2
            shift
            ;;
        -ci)
            CI_BUILD=$2
            shift
            ;;
        -p)
            PROJECT_NAME_PATTERNS=$2
            shift
            ;;
        -dotnetbuildfromsource)
            DOTNET_BUILD_FROM_SOURCE=1
            ;;
        -dotnetcoresdkdir)
            DOTNET_CORE_SDK_DIR=$2
            shift
            ;;
        -dotnetbuildtoolsdir)
            DOTNET_BUILD_TOOLS_DIR=$2
            shift
            ;;
        -verbose)
            VERBOSE=true
            ;;
        *)
            break
            ;;
   esac
   shift
done

#
# Variables
#
TP_ROOT_DIR=$(cd "$(dirname "$0")"; pwd -P)
TP_TOOLS_DIR="$TP_ROOT_DIR/tools"
TP_DOTNET_DIR="${DOTNET_CORE_SDK_DIR:-${TP_TOOLS_DIR}/dotnet-linux}"
TP_PACKAGES_DIR="${NUGET_PACKAGES:-${TP_ROOT_DIR}/packages}"
TP_OUT_DIR="$TP_ROOT_DIR/artifacts"
TP_PACKAGE_PROJ_DIR="$TP_ROOT_DIR/src/package/package"
TP_PACKAGE_NUSPEC_DIR="$TP_ROOT_DIR/src/package/nuspec"
TP_SRC_DIR="$TP_ROOT_DIR/src"
TP_USE_REPO_API=$DOTNET_BUILD_FROM_SOURCE

# Set VERSION from scripts/build/TestPlatform.Settings.targets
VERSION=$(test -z $VERSION && grep TPVersionPrefix $TP_ROOT_DIR/scripts/build/TestPlatform.Settings.targets  | head -1 | cut -d'>' -f2 | cut -d'<' -f1 || echo $VERSION)

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# Dotnet build doesnt support --packages yet. See https://github.com/dotnet/cli/issues/2712
export NUGET_PACKAGES=$TP_PACKAGES_DIR
DOTNET_CLI_VERSION="6.0.100-alpha.1.21067.8"
#DOTNET_RUNTIME_VERSION="LATEST"

#
# Build configuration
#
TPB_Solution="TestPlatform.sln"
TPB_Build_From_Source_Solution="TestPlatform_BuildFromSource.sln"
TPB_TargetFramework="net451"
TPB_TargetFrameworkCore="netcoreapp2.1"
TPB_Configuration=$CONFIGURATION
TPB_TargetRuntime=$TARGET_RUNTIME
TPB_Version=$(test -z $VERSION_SUFFIX && echo $VERSION || echo $VERSION-$VERSION_SUFFIX)
TPB_CIBuild=$CI_BUILD
TPB_LocalizedBuild=$DISABLE_LOCALIZED_BUILD
TPB_Verbose=$VERBOSE

#
# Logging
#
log()
{
    printf "${GREEN}... $@${NOCOLOR}\n"
}

verbose()
{
    if [ ${TPB_Verbose-false} ]
    then
        printf "${YELLOW}... $@${NOCOLOR}\n" >&2
    fi
}

error()
{
    printf "${RED}... $@${NOCOLOR}\n" >&2
}

function usage()
{
    log " Usage: ./build.sh [Options]"
    log ""
    log " -c <CONFIGURATION>                Build the specified Configuration (Debug or Release, default: Debug)"
    log " -r <TARGET_RUNTIME>               Build for the specified runtime moniker (ubuntu.14.04-x64)"
    log " -v <VERSION>                      Version number for the package generated (15.0.0)"
    log " -vs <VERSION_SUFFIX>              Version suffix for package generated (dev)"
    log " -noloc <DISABLE_LOCALIZED_BUILD>  Disable Localized builds (true,false)"
    log " -ci <CI_BUILD>                    Declares if this is a CI_BUILD or not"
    log " -p <PROJECT_NAME_PATTERNS>        Pattern to build specific projects"
    log " -verbose <VERBOSE>                Enable verbose logging (true, false)"
}

#
# Build steps
#
function install_cli()
{
    if [[ $TP_USE_REPO_API = 0 ]]; then
        # Skip download of dotnet toolset if REPO API is enabled
        local failed=false
        local install_script="$TP_TOOLS_DIR/dotnet-install.sh"
        local remote_path="https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.sh"

        log "Installing dotnet cli..."
        local start=$SECONDS

        # Install the latest version of dotnet-cli
        curl --retry 10 -sSL --create-dirs -o $install_script $remote_path || failed=true
        if [ "$failed" = true ]; then
            error "Failed to download dotnet-install.sh script."
            return 1
        fi
        chmod u+x $install_script
        # Get netcoreapp1.1 shared components
        $install_script  --runtime dotnet --version "2.1.0" --channel "release/2.1.0" --install-dir "$TP_DOTNET_DIR" --no-path --architecture x64
        $install_script  --runtime dotnet --version "3.1.0" --channel "release/3.1.0" --install-dir "$TP_DOTNET_DIR" --no-path --architecture x64
        $install_script  --runtime dotnet --version "5.0.1" --channel "release/5.0.1" --install-dir "$TP_DOTNET_DIR" --no-path --architecture x64

        log "install_cli: Get the latest dotnet cli toolset..."
        $install_script --install-dir "$TP_DOTNET_DIR" --no-path --channel "master" --version $DOTNET_CLI_VERSION


        log " ---- dotnet x64"
        "$TP_DOTNET_DIR/dotnet" --info
    fi

    local dotnet_path=$(_get_dotnet_path)
    if [[ ! -e $dotnet_path ]]; then
        log "dotnet not found at $dotnet_path. Did the dotnet cli installation succeed?"
        return 1
    fi

    log "install_cli: Complete. Elapsed $(( SECONDS - start ))s."
    return 0
}

function restore_package()
{
    local failed=false
    local dotnet=$(_get_dotnet_path)

    log "restore_package: Start restoring packages to $TP_PACKAGES_DIR."
    local start=$SECONDS

    if [[ $TP_USE_REPO_API = 0 ]]; then
        log ".. .. Restore: Source: $TP_ROOT_DIR/src/package/external/external.csproj"
        $dotnet restore $TP_ROOT_DIR/src/package/external/external.csproj --packages $TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version || failed=true
    else
        log ".. .. Restore: Source: $TP_ROOT_DIR/src/package/external/external_BuildFromSource.csproj"
        $dotnet restore $TP_ROOT_DIR/src/package/external/external.csproj --packages $TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version  -p:DotNetBuildFromSource=true || failed=true
    fi

    if [ "$failed" = true ]; then
        error "Failed to restore packages."
        return 2
    fi

    log "restore_package: Complete. Elapsed $(( SECONDS - start ))s."
}

function invoke_build()
{
    local failed=false
    local dotnet=$(_get_dotnet_path)

    log "invoke_build: Start build."
    local start=$SECONDS
    log ".. .. Build: Source: $TPB_Solution"
    
    # Workaround for https://github.com/dotnet/sdk/issues/335
    export FrameworkPathOverride=$TP_PACKAGES_DIR/microsoft.targetingpack.netframework.v4.7.2/1.0.0/lib/net472/
    if [ -z "$PROJECT_NAME_PATTERNS" ]
    then
        if [[ $TP_USE_REPO_API = 0 ]]; then
            $dotnet build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -bl:TestPlatform.binlog || failed=true
        else
            $dotnet build $TPB_Build_From_Source_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:DotNetBuildFromSource=true -bl:TestPlatform.binlog || failed=true
       fi
    else
        find . -name "$PROJECT_NAME_PATTERNS" | xargs -L 1 $dotnet build --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
    fi

    log ".. .. Build: Complete."
    if [ "$failed" = true ]; then
        error "Failed to build solution."
        return 2
    fi

    log "invoke_build: Complete. Elapsed $(( SECONDS - start ))s."
}

function publish_package()
{
    local failed=false
    local dotnet=$(_get_dotnet_path)

    log "publish_package: Started."
    local start=$SECONDS
    
    local packageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFramework/$TPB_TargetRuntime
    local coreCLRPackageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFrameworkCore
    local frameworkPackageDirMap=( \
        $TPB_TargetFrameworkCore:$coreCLRPackageDir
    )

    if [[ $DOTNET_BUILD_FROM_SOURCE = 0 ]]; then
       frameworkPackageDirMap+=( \
           $TPB_TargetFramework:$packageDir
       )
    fi

    for fxpkg in "${frameworkPackageDirMap[@]}" ;
    do
        local framework="${fxpkg%%:*}"
        local packageDir="${fxpkg##*:}"
        local projects=( \
            $TP_PACKAGE_PROJ_DIR/package.csproj \
            $TP_ROOT_DIR/src/vstest.console/vstest.console.csproj \
            $TP_ROOT_DIR/src/datacollector/datacollector.csproj
        )

        log "Package: Publish projects for $framework"
        for project in "${projects[@]}" ;
        do
            log ".. Package: Publish $project"
            $dotnet publish $project --configuration $TPB_Configuration --framework $framework --output $packageDir -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
        done

        # Copy TestHost for desktop targets
        local testhost=$packageDir/TestHost
        mkdir -p $testhost
        cp -r src/testhost/bin/$TPB_Configuration/net451/win7-x64/* $testhost
        cp -r src/testhost.x86/bin/$TPB_Configuration/net451/win7-x86/* $testhost

        # Copy over the logger assemblies to the Extensions folder.
        local extensionsDir="$packageDir/Extensions"
        # Create an extensions directory.
        mkdir -p $extensionsDir

        # Note Note: If there are some dependencies for the logger assemblies, those need to be moved too. 
        # Ideally we should just be publishing the loggers to the Extensions folder.
        loggers=("Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll" "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.pdb" "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.dll" "Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.pdb")
        for i in ${loggers[@]}; do
            mv $packageDir/${i} $extensionsDir
        done

        # Note Note: If there are some dependencies for the TestHostRuntimeProvider assemblies, those need to be moved too.
        runtimeproviders=("Microsoft.TestPlatform.TestHostRuntimeProvider.dll" "Microsoft.TestPlatform.TestHostRuntimeProvider.pdb")
        for i in ${runtimeproviders[@]}; do
            mv $packageDir/${i} $extensionsDir
        done
        newtonsoft=$TP_PACKAGES_DIR/newtonsoft.json/9.0.1/lib/netstandard1.0/Newtonsoft.Json.dll
        cp $newtonsoft $packageDir
    done

    # Publish TestHost for netcoreapp2.1 target
    log ".. Package: Publish testhost.csproj"
    local projectToPackage=$TP_ROOT_DIR/src/testhost/testhost.csproj
    local packageOutputPath=$TP_OUT_DIR/$TPB_Configuration/Microsoft.TestPlatform.TestHost/$TPB_TargetFrameworkCore
    $dotnet publish $projectToPackage --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore --output $packageOutputPath -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild

    # For libraries that are externally published, copy the output into artifacts. These will be signed and packaged independently.
    packageName="Microsoft.TestPlatform.Build"
    binariesDirectory="src/$packageName/bin/$TPB_Configuration/**"
    publishDirectory="$TP_OUT_DIR/$TPB_Configuration/$packageName"
    mkdir -p $publishDirectory
    cp -r $binariesDirectory $publishDirectory

    log "publish_package: Complete. Elapsed $(( SECONDS - start ))s."
    
    publishplatformatbstractions
}

function publishplatformatbstractions()
{
    log "Publish-PlatfromAbstractions-Internal: Started."
    
    local start=$SECONDS
    local coreCLRPackageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFrameworkCore
    
    local platformAbstraction="$TP_ROOT_DIR/src/Microsoft.TestPlatform.PlatformAbstractions/bin/$TPB_Configuration"
    local platformAbstractionNetCore=$platformAbstraction/$TPB_TargetFrameworkCore
    
    cp -r $platformAbstractionNetCore $coreCLRPackageDir
    
    log "Publish-PlatfromAbstractions-Internal: Complete. Elapsed $(( SECONDS - start ))"
}

function create_package()
{
    local failed=false
    local dotnet=$(_get_dotnet_path)

    local start=$SECONDS
    log "Create-NugetPackages: Started."
    stagingDir="$TP_OUT_DIR/$TPB_Configuration"
    packageOutputDir="$TP_OUT_DIR/$TPB_Configuration/packages"
    mkdir -p $packageOutputDir

    nuspecFiles=("TestPlatform.TranslationLayer.nuspec" "TestPlatform.ObjectModel.nuspec" "TestPlatform.ObjectModel.nuspec" "TestPlatform.TestHost.nuspec"\
        "Microsoft.TestPlatform.nuspec" "Microsoft.TestPlatform.Portable.nuspec" "TestPlatform.CLI.nuspec" "TestPlatform.Build.nuspec" "Microsoft.NET.Test.Sdk.nuspec"\
        "Microsoft.CodeCoverage.nuspec" "Microsoft.TestPlatform.AdapterUtilities.nuspec" "TestPlatform.Extensions.TrxLogger.nuspec")
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
    cp "$TP_PACKAGE_NUSPEC_DIR/../Icon.png" $stagingDir

    for i in ${projectFiles[@]}; do
        log "$dotnet pack --no-build $stagingDir/${i} -o $packageOutputDir -p:Version=$TPB_Version" \
        && $dotnet restore $stagingDir/${i} \
        && $dotnet pack --no-build $stagingDir/${i} -o $packageOutputDir -p:Version=$TPB_Version
    done

    log "Create-NugetPackages: Elapsed $(( SECONDS - start ))s."
}

#
# Privates
#
_get_dotnet_path()
{
    echo "$TP_DOTNET_DIR/dotnet"
}

# Execute build
start=$SECONDS
log "Build started"
log "Test platform environment variables: "
(set | grep ^TP_)

log "Test platform build variables: "
(set | grep ^TPB_)

if [ -z "$PROJECT_NAME_PATTERNS" ]
then
    install_cli && restore_package && invoke_build && publish_package && create_package
else
    invoke_build
fi

if [[ $? -ne 0 ]]; then
    log "Build failed. Elapsed $(( SECONDS - start ))s."
    exit 1
fi

log "Build complete. Elapsed $(( SECONDS - start ))s."
