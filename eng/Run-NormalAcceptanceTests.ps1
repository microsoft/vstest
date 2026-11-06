[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [Parameter(Mandatory = $true)]
    [string] $LocalVSTestConsolePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'AcceptanceTestResults.ps1')
$root = Split-Path $PSScriptRoot -Parent
$dotnetRoot = Join-Path $root '.dotnet'
$dotnet = Join-Path $dotnetRoot 'dotnet.exe'
$resultsDirectory = Join-Path $root "artifacts\AcceptanceTestResults\$Configuration"
$assemblyName = 'Microsoft.TestPlatform.AcceptanceTests'
$runs = @(
    @{ TargetFramework = 'net451'; Framework = '.NETFramework,Version=v4.5.1' },
    @{ TargetFramework = 'netcoreapp2.1'; Framework = '.NETCoreApp,Version=v2.1' }
)

foreach ($path in @($dotnet, $LocalVSTestConsolePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Acceptance test prerequisite is missing: $path"
    }
}

# Acceptance tests launch tools\dotnet and exercise a separate SDK with this build's VSTest task.
# Never overlay the bootstrapped SDK itself.
$sdkVersion = (Get-Content -LiteralPath (Join-Path $root 'global.json') -Raw | ConvertFrom-Json).sdk.version
$taskDirectory = Join-Path $root "artifacts\bin\Microsoft.TestPlatform.Build\$Configuration\netstandard2.0"
$toolsDotnet = Join-Path $root 'tools\dotnet'
$patchedDotnet = Join-Path $root 'artifacts\testArtifacts\dotnet'
if (-not (Test-Path -LiteralPath (Join-Path $taskDirectory 'Microsoft.TestPlatform.Build.dll'))) {
    throw "The locally built VSTest task is missing: $taskDirectory"
}
foreach ($destination in @($toolsDotnet, $patchedDotnet)) {
    if ((Test-Path -LiteralPath $destination) -and ((Get-Item -LiteralPath $destination).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing to overwrite an SDK through a reparse point: $destination"
    }
    $null = New-Item -ItemType Directory -Path $destination -Force
    Get-ChildItem -LiteralPath $dotnetRoot -Force | Copy-Item -Destination $destination -Recurse -Force
}
$patchedSdk = Join-Path $patchedDotnet "sdk\$sdkVersion"
if (-not (Test-Path -LiteralPath (Join-Path $patchedSdk 'MSBuild.dll'))) {
    throw "The pinned SDK was not copied to $patchedSdk"
}
Get-ChildItem -LiteralPath $taskDirectory -File | Copy-Item -Destination $patchedSdk -Force

$null = New-Item -ItemType Directory -Path $resultsDirectory -Force
$stagingRoot = Join-Path $root "artifacts\ta\$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
foreach ($run in $runs) {
    $sourceAssembly = Join-Path $root "artifacts\bin\$assemblyName\$Configuration\$($run.TargetFramework)\$assemblyName.dll"
    $run.Results = Join-Path $resultsDirectory "$assemblyName.$($run.TargetFramework).normal.trx"
    if (-not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)) {
        throw "Acceptance test assembly is missing: $sourceAssembly"
    }
    # .NET Framework's file APIs cannot inspect dump attachments under the long build-output path.
    $stage = Join-Path $stagingRoot $run.TargetFramework
    & $dotnet publish (Join-Path $root "test\$assemblyName\$assemblyName.csproj") `
        -c $Configuration -f $run.TargetFramework --no-build --no-restore -o $stage -v quiet -nr:false
    if ($LASTEXITCODE -ne 0) { throw "Acceptance layout preparation failed for $($run.TargetFramework)." }
    $run.Assembly = Join-Path $stage "$assemblyName.dll"
    if ((Get-FileHash -LiteralPath $sourceAssembly).Hash -ne (Get-FileHash -LiteralPath $run.Assembly).Hash) {
        throw "Staged acceptance assembly differs from the build: $($run.TargetFramework)."
    }
    if (Test-Path -LiteralPath $run.Results) {
        Remove-Item -LiteralPath $run.Results
    }
}

$environment = @{}
foreach ($name in @('TP_ROOT_DIR', 'DOTNET_ROOT', 'DOTNET_ROOT(x86)', 'DOTNET_MULTILEVEL_LOOKUP', 'VSTEST_ACCEPTANCE_DOTNET_SDK', 'VSTEST_ACCEPTANCE_DIAGNOSTICS_DIRECTORY', 'VSTEST_DISABLE_ARTIFACTS_POSTPROCESSING', 'TPT_TargetFramework', 'TPT_TargetRuntime', 'PATH')) {
    $environment[$name] = [Environment]::GetEnvironmentVariable($name)
}

try {
    $env:TP_ROOT_DIR = $root
    $env:DOTNET_ROOT = $dotnetRoot
    ${env:DOTNET_ROOT(x86)} = Join-Path $dotnetRoot 'x86'
    $env:DOTNET_MULTILEVEL_LOOKUP = '0'
    $env:VSTEST_ACCEPTANCE_DOTNET_SDK = Join-Path $patchedSdk 'dotnet.dll'
    $env:VSTEST_ACCEPTANCE_DIAGNOSTICS_DIRECTORY = Join-Path $resultsDirectory 'diagnostics'
    # The pinned SDK otherwise adds a modern-only switch when dotnet test receives a DLL.
    $env:VSTEST_DISABLE_ARTIFACTS_POSTPROCESSING = '1'
    $env:PATH = "$dotnetRoot;$env:PATH"
    foreach ($run in $runs) {
        $env:TPT_TargetFramework = $run.TargetFramework
        $env:TPT_TargetRuntime = if ($run.TargetFramework -eq 'net451') { 'win7-x64' } else { '' }
        $arguments = @(
            'exec', $LocalVSTestConsolePath, $run.Assembly,
            "/Framework:$($run.Framework)", '/Platform:x64',
            "/TestAdapterPath:$(Split-Path $run.Assembly -Parent)",
            "/ResultsDirectory:$resultsDirectory",
            "/Logger:trx;LogFileName=$([IO.Path]::GetFileName($run.Results))",
            '/Logger:console;verbosity=normal',
            '/TestCaseFilter:TestCategory!=Compatibility&TestCategory!=TelemetryPerf&TestCategory!=Performance'
        )

        Write-Host "Running normal acceptance tests: $($run.TargetFramework)"
        & $dotnet @arguments
        $run.ExitCode = $LASTEXITCODE
    }

    Assert-NormalAcceptanceTestResults -Runs $runs
}
finally {
    foreach ($name in $environment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $environment[$name])
    }
}
