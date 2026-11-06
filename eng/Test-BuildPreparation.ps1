[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$fixture = Join-Path $repo "artifacts\tmp\build-preparation-test-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path (Join-Path $fixture 'scripts\build') -Force
$fixture = (Resolve-Path -LiteralPath $fixture).Path
$shell = (Get-Process -Id $PID).Path

try {
    foreach ($file in 'build.ps1', 'common.lib.ps1', 'verify-nupkgs.ps1') {
        Copy-Item (Join-Path $repo "scripts\$file") (Join-Path $fixture "scripts\$file")
    }
    foreach ($file in 'TestPlatform.Settings.targets', 'TestPlatform.Dependencies.props') {
        Copy-Item (Join-Path $repo "scripts\build\$file") (Join-Path $fixture "scripts\build\$file")
    }
    Copy-Item (Join-Path $repo 'global.json') $fixture
    $bin = Join-Path $fixture 'packages\procdump\0.0.1\bin'
    $null = New-Item -ItemType Directory -Path $bin -Force
    foreach ($name in 'procdump.exe', 'procdump64.exe') {
        Set-Content -LiteralPath (Join-Path $bin $name) -Value "Fixture $name; never executed."
    }
    $before = @(Get-ChildItem -LiteralPath $bin -File | Get-FileHash)

    $output = & $shell -NoProfile -Command "& '$fixture\scripts\build.ps1' -Version 17.3.4 -VersionSuffix fixture -Steps @()" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Build initialization failed:`n$($output -join "`n")"
    }
    foreach ($file in $before) {
        if (-not (Test-Path -LiteralPath $file.Path) -or (Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) {
            throw "Build initialization deleted or changed a healthy dependency: $($file.Path)"
        }
    }
    Write-Host 'Build initialization preserves both existing ProcDump files.'
}
finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force
}

exit 0
