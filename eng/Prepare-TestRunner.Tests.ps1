[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$root = Join-Path ([IO.Path]::GetTempPath()) ("vstest-runner-" + [Guid]::NewGuid().ToString('N'))
$bin = Join-Path $root 'bin'
$packages = Join-Path $root 'packages'
$dotnet = Join-Path $root 'dotnet.cmd'
$consoleDirectory = Join-Path $bin 'vstest.console\Release\netcoreapp2.0'
$console = Join-Path $consoleDirectory 'vstest.console.dll'
$inputs = @(
    @{
        Source = Join-Path $bin 'Microsoft.TestPlatform.TestHostProvider\Release\netstandard1.5\Microsoft.TestPlatform.TestHostRuntimeProvider.dll'
        Destination = Join-Path $consoleDirectory 'Extensions\Microsoft.TestPlatform.TestHostRuntimeProvider.dll'
    },
    @{
        Source = Join-Path $bin 'Microsoft.TestPlatform.Extensions.TrxLogger\Release\netstandard1.5\Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll'
        Destination = Join-Path $consoleDirectory 'Extensions\Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll'
    },
    @{
        Source = Join-Path $bin 'Microsoft.TestPlatform.Extensions.BlameDataCollector\Release\netstandard1.5\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll'
        Destination = Join-Path $consoleDirectory 'Extensions\Microsoft.TestPlatform.Extensions.BlameDataCollector.dll'
    },
    @{
        Source = Join-Path $packages 'microsoft.extensions.dependencymodel\1.0.3\lib\netstandard1.3\Microsoft.Extensions.DependencyModel.dll'
        Destination = Join-Path $consoleDirectory 'Microsoft.Extensions.DependencyModel.dll'
    }
)
$arguments = @{
    Configuration = 'Release'
    ArtifactsBinDirectory = $bin
    NuGetPackagesDirectory = $packages
    DotNetPath = $dotnet
}
$prepare = Join-Path $PSScriptRoot 'Prepare-TestRunner.ps1'

function Assert-SetupFailure([string] $Expected) {
    $failure = $null
    try {
        & $prepare @arguments
    }
    catch {
        $failure = $_.Exception.Message
    }
    if (-not $failure -or -not $failure.Contains($Expected)) {
        throw "Expected setup failure containing '$Expected', got '$failure'."
    }
}

try {
    foreach ($path in @($console, $dotnet) + @($inputs | ForEach-Object { $_.Source })) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        Set-Content -LiteralPath $path -Value "fixture: $path" -Encoding ASCII
    }
    $workingHost = "@echo off`r`necho Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger`r`nexit /b 0"
    Set-Content -LiteralPath $dotnet -Value $workingHost -Encoding ASCII
    $previousDotNetRoot = $env:DOTNET_ROOT
    & $prepare @arguments
    if ($env:DOTNET_ROOT -ne $previousDotNetRoot) {
        throw 'Runner preparation did not restore DOTNET_ROOT.'
    }
    foreach ($input in $inputs) {
        if ((Get-FileHash -LiteralPath $input.Source).Hash -ne (Get-FileHash -LiteralPath $input.Destination).Hash) {
            throw "Staged bytes differ for $($input.Source)."
        }
    }

    # Each source must exist even after a successful staging left all destinations present.
    foreach ($path in @($console, $dotnet) + @($inputs | ForEach-Object { $_.Source })) {
        Move-Item -LiteralPath $path -Destination "$path.saved"
        try {
            Assert-SetupFailure "required test runner input not found: $path"
        }
        finally {
            Move-Item -LiteralPath "$path.saved" -Destination $path
        }
    }

    Set-Content -LiteralPath $dotnet -Value "@echo off`r`nexit /b 0" -Encoding ASCII
    Assert-SetupFailure 'did not discover the staged TRX logger'
    Set-Content -LiteralPath $dotnet -Value "@echo off`r`nexit /b 7" -Encoding ASCII
    Assert-SetupFailure 'could not enumerate loggers (exit 7)'
    if ($env:DOTNET_ROOT -ne $previousDotNetRoot) {
        throw 'Failed runner preparation did not restore DOTNET_ROOT.'
    }

    Set-Content -LiteralPath $dotnet -Value $workingHost -Encoding ASCII
    & $prepare @arguments
    Write-Host 'Runner setup tests passed: exact copies, six missing inputs, missing logger, failed startup, environment restoration.'
}
finally {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
