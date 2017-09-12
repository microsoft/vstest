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
TARGET_RUNTIME="ubuntu.16.04-x64"
VERSION="15.5.0"
VERSION_SUFFIX="dev"
FAIL_FAST=false
DISABLE_LOCALIZED_BUILD=false
CI_BUILD=false
VERBOSE=false
PROJECT_NAME_PATTERNS=

while [ $# -gt 0 ]; do
    lowerI="$(echo ${1:-} | awk '{print tolower($0)}')"
    case $lowerI in
        -h | --help)
            usage
            exit
            ;;
        -c)
            CONFIGURATION=$2
            ;;
        -r)
            TARGET_RUNTIME=$2
            ;;
        -v)
            VERSION=$2
            ;;
        -vs)
            VERSION_SUFFIX=$2
            ;;
        -noloc)
            DISABLE_LOCALIZED_BUILD=$2
            ;;
        -ci)
            CI_BUILD=$2
            ;;
        -p)
            PROJECT_NAME_PATTERNS=$2
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
TP_PACKAGES_DIR="$TP_ROOT_DIR/packages"
TP_OUT_DIR="$TP_ROOT_DIR/artifacts"
TP_PACKAGE_PROJ_DIR="$TP_ROOT_DIR/src/package/package"
TP_PACKAGE_NUSPEC_DIR="$TP_ROOT_DIR/src/package/nuspec"
TP_SRC_DIR="$TP_ROOT_DIR/src"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# Dotnet build doesnt support --packages yet. See https://github.com/dotnet/cli/issues/2712
export NUGET_PACKAGES=$TP_PACKAGES_DIR
DOTNET_CLI_VERSION="LATEST"
DOTNET_RUNTIME_VERSION="LATEST"

#
# Build configuration
#
TPB_Solution="TestPlatform.sln"
TPB_TargetFramework="net451"
TPB_TargetFrameworkCore="netcoreapp2.0"
TPB_TargetFrameworkCore10="netcoreapp1.0"
TPB_Configuration=$CONFIGURATION
TPB_TargetRuntime=$TARGET_RUNTIME
TPB_Version=$(test -z $VERSION_SUFFIX && echo $VERSION || echo $VERSION-$VERSION_SUFFIX)
TPB_CIBuild=$CI_BUILD
TPB_LocalizedBuild=$DISABLE_LOCALIZED_BUILD
TPB_Verbose=$VERBOSE
TPB_HasMono=$(command -v mono > /dev/null && echo true || echo false)

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

    log "install_cli: Get the latest dotnet cli toolset..."
    $install_script --install-dir "$TP_TOOLS_DIR/dotnet" --no-path --channel "master" --version $DOTNET_CLI_VERSION

    # Get netcoreapp1.1 shared components
    log "install_cli: Get the shared netcoreapp1.0 runtime..."
    $install_script --install-dir "$TP_TOOLS_DIR/dotnet" --no-path --channel "preview" --version "1.0.5" --shared-runtime
    log "install_cli: Get the shared netcoreapp1.1 runtime..."
    $install_script --install-dir "$TP_TOOLS_DIR/dotnet" --no-path --channel "release/1.1.0" --version "1.1.2" --shared-runtime
    log "install_cli: Get the shared netcoreapp2.0 runtime..."
    $install_script --install-dir "$TP_TOOLS_DIR/dotnet" --no-path --channel "release/2.0.0" --version "2.0.0" --shared-runtime
    #log "install_cli: Get shared components which is compatible with dotnet cli version $DOTNET_CLI_VERSION..."
    #$install_script --install-dir "$TP_TOOLS_DIR/dotnet" --no-path --channel "master" --version $DOTNET_RUNTIME_VERSION --shared-runtime

    log "install_cli: Complete. Elapsed $(( SECONDS - start ))s."
    return 0
}


function restore_package()
{
    local failed=false
    local dotnet=$(_get_dotnet_path)

    log "restore_package: Start restoring packages to $TP_PACKAGES_DIR."
    local start=$SECONDS

    log ".. .. Restore: Source: $TP_ROOT_DIR/src/package/external/external.csproj"
    $dotnet restore $TP_ROOT_DIR/src/package/external/external.csproj --packages $TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version || failed=true
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
    
    if $TPB_HasMono; then
        if [ -z "$PROJECT_NAME_PATTERNS" ]
        then
            $dotnet build $TPB_Solution --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild || failed=true
        else
            find . -name "$PROJECT_NAME_PATTERNS" | xargs -L 1 $dotnet build --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -f netcoreapp1.0
        fi
    else
        # Need to target the appropriate targetframework for each project until netstandard2.0 ships
        PROJECTFRAMEWORKMAP=( \
            Microsoft.TestPlatform.CrossPlatEngine/Microsoft.TestPlatform.CrossPlatEngine:netstandard1.4 \
            testhost.x86/testhost.x86:netcoreapp1.0 \
            Microsoft.TestPlatform.PlatformAbstractions/Microsoft.TestPlatform.PlatformAbstractions:netcoreapp1.0 \
            Microsoft.TestPlatform.PlatformAbstractions/Microsoft.TestPlatform.PlatformAbstractions:netstandard1.0 \
            package/package/package:netcoreapp1.0 \
            Microsoft.TestPlatform.ObjectModel/Microsoft.TestPlatform.ObjectModel:netstandard1.4 \
            Microsoft.TestPlatform.VsTestConsole.TranslationLayer/Microsoft.TestPlatform.VsTestConsole.TranslationLayer:netstandard1.5 \
            datacollector/datacollector:netcoreapp2.0 \
            vstest.console/vstest.console:netcoreapp2.0 \
            Microsoft.TestPlatform.Common/Microsoft.TestPlatform.Common:netstandard1.4 \
            Microsoft.TestPlatform.Client/Microsoft.TestPlatform.Client:netstandard1.4 \
            Microsoft.TestPlatform.Extensions.TrxLogger/Microsoft.TestPlatform.Extensions.TrxLogger:netstandard1.5 \
            Microsoft.TestPlatform.Utilities/Microsoft.TestPlatform.Utilities:netstandard1.4 \
            Microsoft.TestPlatform.CommunicationUtilities/Microsoft.TestPlatform.CommunicationUtilities:netstandard1.4 \
            Microsoft.TestPlatform.Build/Microsoft.TestPlatform.Build:netstandard1.3 \
            testhost/testhost:netcoreapp1.0 \
            Microsoft.TestPlatform.CoreUtilities/Microsoft.TestPlatform.CoreUtilities:netstandard1.4
        )
        
        for item in "${PROJECTFRAMEWORKMAP[@]}" ;
        do
            projectToBuild="${item%%:*}"
            framework="${item##*:}"
            verbose "$dotnet build src/$projectToBuild.csproj --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:TargetFramework=$framework"
            $dotnet build src/$projectToBuild.csproj --configuration $TPB_Configuration -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild -p:TargetFramework=$framework
        done
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
        $TPB_TargetFrameworkCore:$coreCLRPackageDir \
        $TPB_TargetFramework:$packageDir
    )
    
    for fxpkg in "${frameworkPackageDirMap[@]}" ;
    do
        local framework="${fxpkg%%:*}"
        local packageDir="${fxpkg##*:}"
        local projects=( \
            $TP_PACKAGE_PROJ_DIR/package.csproj \
            $TP_ROOT_DIR/src/vstest.console/vstest.console.csproj \
            $TP_ROOT_DIR/src/datacollector/datacollector.csproj
        )

        if [ "$framework" == "net451" ] && ! $TPB_HasMono; then
            # Skip publish if mono is not available
            continue
        fi

        log "Package: Publish projects for $framework"
        for project in "${projects[@]}" ;
        do
            log ".. Package: Publish $project"
            $dotnet publish $project --configuration $TPB_Configuration --framework $framework --output $packageDir -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild
        done

        # Copy TestHost for desktop targets if we've built net451
        # packages with mono
        if $TPB_HasMono; then
            local testhost=$packageDir/TestHost
            mkdir -p $testhost
            cp -r src/testhost/bin/$TPB_Configuration/net451/win7-x64/* $testhost
            cp -r src/testhost.x86/bin/$TPB_Configuration/net451/win7-x64/* $testhost
        fi
        
        # Copy over the logger assemblies to the Extensions folder.
        local extensionsDir="$packageDir/Extensions"
        # Create an extensions directory.
        mkdir -p $extensionsDir

        # Note Note: If there are some dependencies for the logger assemblies, those need to be moved too. 
        # Ideally we should just be publishing the loggers to the Extensions folder.
        loggers=("Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll" "Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.pdb")
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

    # Publish TestHost for netcoreapp1.0 target
    log ".. Package: Publish testhost.csproj"
    local projectToPackage=$TP_ROOT_DIR/src/testhost/testhost.csproj
    local packageOutputPath=$TP_OUT_DIR/$TPB_Configuration/Microsoft.TestPlatform.TestHost/$TPB_TargetFrameworkCore10
    $dotnet publish $projectToPackage --configuration $TPB_Configuration --framework $TPB_TargetFrameworkCore10 --output $packageOutputPath -v:minimal -p:Version=$TPB_Version -p:CIBuild=$TPB_CIBuild -p:LocalizedBuild=$TPB_LocalizedBuild

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
    local coreCLRPackageDir=$TP_OUT_DIR/$TPB_Configuration/$TPB_TargetFrameworkCore10
    
    local platformAbstraction="$TP_ROOT_DIR/src/Microsoft.TestPlatform.PlatformAbstractions/bin/$TPB_Configuration"
    local platformAbstractionNetCore=$platformAbstraction/$TPB_TargetFrameworkCore10
    
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

    DOTNET_PATH="$TP_TOOLS_DIR/dotnet/dotnet"
    if [[ ! -e $DOTNET_PATH ]]; then
        log "dotnet not found at $DOTNET_PATH. Did the dotnet cli installation succeed?"
    fi

    nuspecFiles=("TestPlatform.TranslationLayer.nuspec" "TestPlatform.ObjectModel.nuspec" "TestPlatform.TestHost.nuspec" "TestPlatform.nuspec" "TestPlatform.CLI.nuspec" "TestPlatform.Build.nuspec" "Microsoft.NET.Test.Sdk.nuspec")
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
        log "$DOTNET_PATH pack --no-build $stagingDir/${i} -o $packageOutputDir -p:Version=$TPB_Version" \
        && $DOTNET_PATH restore $stagingDir/${i} \
        && $DOTNET_PATH pack --no-build $stagingDir/${i} -o $packageOutputDir -p:Version=$TPB_Version
    done

    log "Create-NugetPackages: Elapsed $(( SECONDS - start ))s."
}

#
# Privates
#
_get_dotnet_path()
{
    echo "$TP_TOOLS_DIR/dotnet/dotnet"
}

# Execute build
start=$SECONDS
log "Build started"
log "Test platform environment variables: "
(set | grep ^TP_)

log "Test platform build variables: "
(set | grep ^TPB_)

if $TPB_HasMono; then
    # Workaround for https://github.com/dotnet/sdk/issues/335
    export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/
fi

if [ -z "$PROJECT_NAME_PATTERNS" ]
then
    install_cli && restore_package && invoke_build && publish_package && create_package
else
    invoke_build
fi

log "Build complete. Elapsed $(( SECONDS - start ))s."

if [[ $? -ne 0 ]]; then
    exit 1
fi
