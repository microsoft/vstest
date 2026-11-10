#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
original_global_hash="$(sha256sum "$repo_root/global.json" | cut -d' ' -f1)"
original_details_hash="$(sha256sum "$repo_root/eng/Version.Details.xml" | cut -d' ' -f1)"
mkdir -p "$repo_root/artifacts/tmp"
fixture="$(mktemp -d "$repo_root/artifacts/tmp/legacy-inner-XXXXXX")"
if [[ "$fixture" != "$repo_root/artifacts/tmp/legacy-inner-"* ]]; then
  echo "Unexpected source-build test directory." >&2
  exit 1
fi
trap 'rm -rf -- "$fixture"' EXIT

inner="$fixture/artifacts/sourcebuild-intermediate/artifacts/sb/src"
mkdir -p "$inner/eng/common" "$fixture/.dotnet/sdk/11.0.100-preview.6.26359.118/Sdks"
printf '%s\n' '{"sdk":{"version":"11.0.100-preview.6.26359.118"},"msbuild-sdks":{"Microsoft.DotNet.Arcade.Sdk":"9.0.0-beta.25164.2"}}' \
  > "$fixture/compat-global-before.json"
cp "$fixture/compat-global-before.json" "$fixture/artifacts/sourcebuild-intermediate/global.json"
cp "$repo_root/global.json" "$inner/global.json"
cp "$repo_root/eng/Version.Details.xml" "$inner/eng/Version.Details.xml"
cp "$repo_root/eng/BuildSourceBuildIntermediate.sh" "$inner/eng/BuildSourceBuildIntermediate.sh"
cat > "$fixture/.dotnet/dotnet" <<'EOF'
#!/usr/bin/env bash
if [[ "${1:-}" == "--version" ]]; then
  echo 9.0.104
  exit 0
fi
printf '%s\n' "$@" > "$PILOT_ARGS"
printf '%s\n' "$MSBuildSDKsPath" > "$PILOT_MSBUILD_SDKS"
EOF
chmod +x "$fixture/.dotnet/dotnet"
cat > "$inner/eng/common/dotnet-install.sh" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$@" > "$PILOT_INSTALL_ARGS"
mkdir -p "$BUILD_SOURCESDIRECTORY/.dotnet/sdk/9.0.104/Sdks"
EOF

BUILD_SOURCESDIRECTORY="$fixture" \
BUILD_REPOSITORY_ID=ef257391-4d76-4240-8649-3f21ac49c392 \
BUILD_BUILDNUMBER=20260930.99 \
PILOT_ARGS="$fixture/args.txt" \
PILOT_INSTALL_ARGS="$fixture/install-args.txt" \
PILOT_MSBUILD_SDKS="$fixture/sdk-path.txt" \
MSBuildSDKsPath="$fixture/.dotnet/sdk/11.0.100-preview.6.26359.118/Sdks" \
  bash "$inner/eng/BuildSourceBuildIntermediate.sh" --inner-legacy \
    "$fixture/.dotnet/dotnet" \
    "$fixture/tool-packages/microsoft.dotnet.arcade.sdk/9.0.0-beta.25164.2/tools" \
    17.11.2-hotfix-26480-99

[[ $(grep -Fc '9.0.104' "$inner/global.json") -eq 2 ]]
[[ $(grep -Fc '9.0.0-beta.25164.2' "$inner/global.json") -eq 1 ]]
[[ $(grep -Fc '9.0.104' "$fixture/artifacts/sourcebuild-intermediate/global.json") -eq 1 ]]
cmp <(sed -e 's/11\.0\.100-preview\.6\.26359\.118/9.0.104/g' \
          -e 's/11\.0\.0-beta\.26421\.2/9.0.0-beta.25164.2/g' "$repo_root/global.json") \
  "$inner/global.json"
cmp <(sed -e 's/9\.0\.0-beta\.24429\.5/9.0.0-beta.25164.2/g' \
          -e 's/d21db44e84b9038ea7b2add139adee2303d46800/5ba9ca776c1d0bb72b2791591e54cf51fc52dfee/g' \
          "$repo_root/eng/Version.Details.xml") "$inner/eng/Version.Details.xml"
[[ $(grep -Fc '9.0.0-beta.25164.2' "$inner/eng/Version.Details.xml") -eq 3 ]]
[[ $(grep -Fc '5ba9ca776c1d0bb72b2791591e54cf51fc52dfee' "$inner/eng/Version.Details.xml") -eq 3 ]]
grep -Fxq -- '-p:Publish=false' "$fixture/args.txt"
grep -Fxq -- '-p:ArcadeBuildFromSource=true' "$fixture/args.txt"
grep -Fxq -- '-p:DotNetBuildRepo=true' "$fixture/args.txt"
grep -Fxq -- '-p:Version=17.11.2-hotfix-26480-99' "$fixture/args.txt"
grep -Fxq -- '-p:Configuration=Release' "$fixture/args.txt"
grep -Fxq -- '-p:ArcadeSdkVersion=9.0.0-beta.25164.2' "$fixture/args.txt"
grep -Fxq -- 'private-copy=verified' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- 'sdk9-install=started' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- 'sdk9-install=finished' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- 'compat-sdk=9.0.104' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- 'sdk-selected=9.0.104' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- "$fixture/.dotnet/sdk/9.0.104/Sdks" "$fixture/sdk-path.txt"
grep -Fxq -- 'inner-build=starting' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"
grep -Fxq -- '-version' "$fixture/install-args.txt"
grep -Fxq -- '9.0.104' "$fixture/install-args.txt"
grep -Fxq -- '-dotnetpath' "$fixture/install-args.txt"
grep -Fxq -- "$fixture/.dotnet" "$fixture/install-args.txt"
grep -Fxq -- '-runtime' "$fixture/install-args.txt"
grep -Fxq -- 'sdk' "$fixture/install-args.txt"
if grep -Fq -- '-p:NuGetPackageRoot=' "$fixture/args.txt"; then
  echo "The inner source build must resolve from its own package cache." >&2
  exit 1
fi
[[ "$(sha256sum "$repo_root/global.json" | cut -d' ' -f1)" == "$original_global_hash" ]]
[[ "$(sha256sum "$repo_root/eng/Version.Details.xml" | cut -d' ' -f1)" == "$original_details_hash" ]]

rmdir "$fixture/.dotnet/sdk/9.0.104/Sdks" "$fixture/.dotnet/sdk/9.0.104"
cp "$repo_root/global.json" "$inner/global.json"
cp "$repo_root/eng/Version.Details.xml" "$inner/eng/Version.Details.xml"
cp "$fixture/compat-global-before.json" "$fixture/artifacts/sourcebuild-intermediate/global.json"
cat > "$inner/eng/common/dotnet-install.sh" <<'EOF'
#!/usr/bin/env bash
echo "Simulated SDK installation failure" >&2
exit 23
EOF
if BUILD_SOURCESDIRECTORY="$fixture" \
    BUILD_REPOSITORY_ID=ef257391-4d76-4240-8649-3f21ac49c392 \
    BUILD_BUILDNUMBER=20260930.99 \
    bash "$inner/eng/BuildSourceBuildIntermediate.sh" --inner-legacy \
      "$fixture/.dotnet/dotnet" \
      "$fixture/tool-packages/microsoft.dotnet.arcade.sdk/9.0.0-beta.25164.2/tools" \
      17.11.2-hotfix-26480-99 > "$fixture/install-failure.log" 2>&1; then
  echo "The source build ignored the SDK installation failure." >&2
  exit 1
else
  [[ "$?" -eq 23 ]]
fi
grep -Fq 'Simulated SDK installation failure' "$fixture/install-failure.log"
grep -Fxq 'sdk9-install=failed' "$fixture/artifacts/log/Release/SourceBuildIntermediate/inner-toolchain-check.txt"

cp "$repo_root/global.json" "$inner/global.json"
cp "$repo_root/eng/Version.Details.xml" "$inner/eng/Version.Details.xml"
if BUILD_SOURCESDIRECTORY="$fixture" BUILD_REPOSITORY_ID=wrong-repository \
    BUILD_BUILDNUMBER=20260930.99 \
    bash "$inner/eng/BuildSourceBuildIntermediate.sh" --inner-legacy \
      "$fixture/.dotnet/dotnet" \
      "$fixture/tool-packages/microsoft.dotnet.arcade.sdk/9.0.0-beta.25164.2/tools" \
      17.11.2-hotfix-26480-99 >/dev/null 2>&1; then
  echo "The inner-only guard accepted a different repository." >&2
  exit 1
fi
cmp "$repo_root/global.json" "$inner/global.json"
cmp "$repo_root/eng/Version.Details.xml" "$inner/eng/Version.Details.xml"

verify_package_version() {
  local name="$1" build_version="$2" cli_version="$3" expected="$4"
  shift 4
  local root="$fixture/$name"
  mkdir -p "$root/eng" "$root/.dotnet" "$root/artifacts/packages/Release/Shipping"
  cp "$repo_root/eng/BuildSourceBuildIntermediate.sh" "$root/eng/BuildSourceBuildIntermediate.sh"
  touch "$root/artifacts/packages/Release/Shipping/Microsoft.TestPlatform.Build.$build_version.nupkg" \
        "$root/artifacts/packages/Release/Shipping/Microsoft.TestPlatform.CLI.$cli_version.nupkg"
  cat > "$root/build.sh" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$@" > "$PILOT_BUILD_ARGS"
EOF
  cat > "$root/.dotnet/dotnet" <<'EOF'
#!/usr/bin/env bash
if [[ "${1:-}" == "--version" ]]; then
  echo 11.0.100-preview.6.26359.118
  exit 0
fi
printf '%s\n' "$@" > "$PILOT_RESTORE_ARGS"
exit 29
EOF
  chmod +x "$root/build.sh" "$root/.dotnet/dotnet"
  local status=0
  BUILD_REASON=PullRequest \
  BUILD_BUILDNUMBER=20260930.99 \
  BUILD_SOURCEVERSION=0123456789abcdef0123456789abcdef01234567 \
  PILOT_BUILD_ARGS="$root/build-args.txt" \
  PILOT_RESTORE_ARGS="$root/restore-args.txt" \
    bash "$root/eng/BuildSourceBuildIntermediate.sh" "$@" > "$root/output.log" 2>&1 || status=$?
  if [[ "$status" -ne "$expected" ]]; then
    cat "$root/output.log" >&2
    echo "Package version case '$name': expected exit $expected, got $status." >&2
    return 1
  fi
  grep -Fxq -- "$1" "$root/build-args.txt"
  if [[ "$expected" -eq 29 ]]; then
    grep -Fxq -- restore "$root/restore-args.txt"
  else
    grep -Fq 'Source-built package versions do not match' "$root/output.log"
    [[ ! -f "$root/restore-args.txt" ]]
  fi
}

verify_package_version hotfix 17.11.2-hotfix-26480-99 17.11.2-hotfix-26480-99 29 \
  /p:DotNetFinalVersionKind=
verify_package_version rtm 17.11.2 17.11.2 29 \
  /p:DotNetFinalVersionKind=release
verify_package_version wrong-version 0.0.0 0.0.0 1 \
  /p:DotNetFinalVersionKind=release
verify_package_version mismatched-packages 17.11.2 17.11.2-hotfix-26480-99 1 \
  /p:DotNetFinalVersionKind=release
printf '%s\n' "Legacy source-build helper kept toolchain edits in the private inner copy."
