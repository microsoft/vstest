[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packages = @(Get-ChildItem (Join-Path $repoRoot "artifacts/packages/$Configuration/Shipping") -Filter 'Microsoft.TestPlatform.CLI.*.nupkg' |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' })
if ($packages.Count -ne 1) {
    throw "Expected one CLI package from this build, found $($packages.Count)."
}

$destination = Join-Path $repoRoot "artifacts/tmp/$Configuration/localConsole-$([Guid]::NewGuid().ToString('N'))"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($packages[0].FullName, $destination)
$consoles = @(Get-ChildItem (Join-Path $destination 'contentFiles/any') -Recurse -Filter 'vstest.console.dll' -File)
if ($consoles.Count -ne 1) {
    throw "Expected one packaged console, found $($consoles.Count)."
}

$console = $consoles[0]
$logger = Join-Path $console.Directory.FullName 'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll'
if (-not (Test-Path $logger -PathType Leaf)) {
    throw "The packaged console is missing the TRX logger at $logger."
}

$dotnetName = if ($env:OS -eq 'Windows_NT') { 'dotnet.exe' } else { 'dotnet' }
$dotnet = Join-Path $repoRoot ".dotnet/$dotnetName"
# Check the actual package, without overlaying build outputs that could conceal missing assets.
$help = & $dotnet exec $console.FullName --help 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "The packaged console failed to start (exit $LASTEXITCODE): $($help -join [Environment]::NewLine)"
}

Write-Host "Package: $($packages[0].Name)"
Write-Host "Console: $($console.FullName)"
Write-Host "Console startup succeeded."
Write-Host "##vso[task.setvariable variable=_LocalVSTestConsolePath]$($console.FullName)"
Write-Output $console.FullName
