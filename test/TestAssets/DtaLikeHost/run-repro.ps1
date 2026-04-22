#Requires -Version 7
[CmdletBinding()]
param(
    # Folder containing locally-built NuGet packages (must have Microsoft.TestPlatform.18.5.0-dev.nupkg).
    [string] $LocalPackageSource = "$PSScriptRoot/../../../artifacts/packages/Debug/Shipping"
)

$ErrorActionPreference = 'Stop'

$repoRoot       = Resolve-Path "$PSScriptRoot/../../../"
$assetDir       = Resolve-Path "$PSScriptRoot"
$localFeed      = Resolve-Path $LocalPackageSource
$testAssetsRoot = Resolve-Path "$repoRoot/test/TestAssets"

# Stage into a temp dir the same way test/Microsoft.TestPlatform.Acceptance.IntegrationTests/AcceptanceTestBase.cs does.
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("DtaLikeHost-" + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

# Copy asset files.
Copy-Item -Path (Join-Path $assetDir '*') -Destination $tempRoot -Recurse

# Directory.Build.props with $(RepoRoot) rewritten to the temp dir.
$dbpSrc = Join-Path $testAssetsRoot 'Directory.Build.props'
$dbp    = (Get-Content -Raw $dbpSrc) -replace [regex]::Escape('$(RepoRoot)'), ($tempRoot + '/')
Set-Content -Path (Join-Path $tempRoot 'Directory.Build.props') -Value $dbp -Encoding UTF8

# Copy Directory.Build.targets if present.
$dbtSrc = Join-Path $testAssetsRoot 'Directory.Build.targets'
if (Test-Path $dbtSrc) { Copy-Item $dbtSrc (Join-Path $tempRoot 'Directory.Build.targets') }

# Copy eng/Versions.props + Version.Details.props.
New-Item -ItemType Directory -Path (Join-Path $tempRoot 'eng') | Out-Null
Copy-Item (Join-Path $repoRoot 'eng/Versions.props')        (Join-Path $tempRoot 'eng/Versions.props')
Copy-Item (Join-Path $repoRoot 'eng/Version.Details.props') (Join-Path $tempRoot 'eng/Version.Details.props')

# Copy NuGet.config with localy-built-packages source inserted (note: typo "localy" matches existing convention).
$nugetSrc = Get-Content -Raw (Join-Path $repoRoot 'NuGet.config')
$nugetSrc = $nugetSrc -replace '"\.packages"', ('"' + (Join-Path $repoRoot '.packages') + '"')
$nugetSrc = $nugetSrc -replace '</packageSources>', ('<add key="localy-built-packages" value="' + $localFeed + '" /></packageSources>')
Set-Content -Path (Join-Path $tempRoot 'NuGet.config') -Value $nugetSrc -Encoding UTF8

Write-Host "== Staged test asset at $tempRoot"
Write-Host "== Local package source: $localFeed"

Push-Location $tempRoot
try {
    & dotnet restore DtaLikeHost.csproj /nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw "restore failed" }

    & dotnet build DtaLikeHost.csproj -c Debug --no-restore /nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
}
finally {
    Pop-Location
}

$outDir = Join-Path $tempRoot 'artifacts/bin/TestAssets/DtaLikeHost/Debug/net472'
$exe    = Join-Path $outDir 'DtaLikeHost.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Exe not found at $exe"
    exit 2
}

Write-Host ""
Write-Host "== Output folder contents (the key DLLs next to the exe):"
Get-ChildItem $outDir -Filter '*.dll' | Where-Object { $_.Name -match 'Immutable|Metadata|TestPlatform\.(Common|ObjectModel)' } | ForEach-Object {
    $v = (Get-Item $_.FullName).VersionInfo.FileVersion
    "  {0,-55} file v{1}" -f $_.Name, $v
}
Get-ChildItem $outDir -Filter '*.config' | ForEach-Object { "  app.config: $($_.Name)" }

Write-Host ""
Write-Host "== Running $exe"
& $exe
$code = $LASTEXITCODE
Write-Host ""
Write-Host "== Exit code: $code"
exit $code
