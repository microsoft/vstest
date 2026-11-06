[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [string] $ArtifactsBinDirectory,
    [string] $NuGetPackagesDirectory = $env:NUGET_PACKAGES,
    [string] $DotNetPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

if (-not $ArtifactsBinDirectory) {
    $ArtifactsBinDirectory = Join-Path $PSScriptRoot '..\artifacts\bin'
}
if (-not $DotNetPath) {
    $DotNetPath = Join-Path $PSScriptRoot '..\.dotnet\dotnet.exe'
}
if (-not $NuGetPackagesDirectory) {
    $NuGetPackagesDirectory = Join-Path $env:USERPROFILE '.nuget\packages'
}

$consoleDirectory = Join-Path $ArtifactsBinDirectory "vstest.console\$Configuration\netcoreapp2.0"
$console = Join-Path $consoleDirectory 'vstest.console.dll'
$extensionsDirectory = Join-Path $consoleDirectory 'Extensions'
$extensions = @(
    @{ Project = 'Microsoft.TestPlatform.TestHostProvider'; Assembly = 'Microsoft.TestPlatform.TestHostRuntimeProvider.dll' },
    @{ Project = 'Microsoft.TestPlatform.Extensions.TrxLogger'; Assembly = 'Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll' },
    @{ Project = 'Microsoft.TestPlatform.Extensions.BlameDataCollector'; Assembly = 'Microsoft.TestPlatform.Extensions.BlameDataCollector.dll' }
)
$copies = @(
    foreach ($extension in $extensions) {
        @{
            Source = Join-Path $ArtifactsBinDirectory "$($extension.Project)\$Configuration\netstandard1.5\$($extension.Assembly)"
            Destination = $extensionsDirectory
        }
    }
    @{
        # Keep aligned with Microsoft.TestPlatform.TestHostProvider.csproj.
        Source = Join-Path $NuGetPackagesDirectory 'microsoft.extensions.dependencymodel\1.0.3\lib\netstandard1.3\Microsoft.Extensions.DependencyModel.dll'
        Destination = $consoleDirectory
    }
)

# Validate sources even when a previous run left a usable destination behind.
foreach ($path in @($DotNetPath, $console) + @($copies | ForEach-Object { $_.Source })) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "SETUP FAILURE: required test runner input not found: $path. Build the solution and restore its packages before preparing the runner."
    }
}

New-Item -ItemType Directory -Path $extensionsDirectory -Force | Out-Null
foreach ($copy in $copies) {
    Copy-Item -LiteralPath $copy.Source -Destination $copy.Destination -Force
    Write-Host "staged  : $($copy.Source) -> $($copy.Destination)"
}

$previousDotNetRoot = $env:DOTNET_ROOT
try {
    $env:DOTNET_ROOT = Split-Path -Parent (Resolve-Path -LiteralPath $DotNetPath).Path
    $loggers = & $DotNetPath exec $console /ListLoggers 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SETUP FAILURE: the console could not enumerate loggers (exit $LASTEXITCODE): $($loggers -join [Environment]::NewLine)"
    }

    # --help starts the console without loading any extensions.
    if (($loggers -join [Environment]::NewLine) -notmatch 'Microsoft\.VisualStudio\.TestPlatform\.Extensions\.TrxLogger\.TrxLogger') {
        throw "SETUP FAILURE: the console did not discover the staged TRX logger: $($loggers -join [Environment]::NewLine)"
    }
    Write-Host 'warm-up : console discovered the TRX logger'
}
finally {
    $env:DOTNET_ROOT = $previousDotNetRoot
}
