<#
.SYNOPSIS
    Detects projects with implicit Newtonsoft.Json dependencies that don't ship the DLL.

.DESCRIPTION
    Scans all csproj files for direct and transitive Newtonsoft.Json references,
    checks which packages ship the DLL, and reports mismatches.

    This is a safety net to catch accidental re-introduction of Newtonsoft.Json
    dependencies after the STJ/Jsonite migration.

.PARAMETER repoRoot
    Root of the vstest repository. Defaults to the repo root.
#>
param(
    [string]$repoRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"

Write-Host "=== Newtonsoft.Json Dependency Detection ===" -ForegroundColor Cyan
Write-Host "Repo root: $repoRoot"
Write-Host ""

$errors = @()

# 1. Find direct PackageReference to Newtonsoft.Json in src/ projects
Write-Host "1. Checking direct PackageReference in src/ projects..." -ForegroundColor Yellow
$srcProjects = Get-ChildItem "$repoRoot/src" -Recurse -Filter "*.csproj"
$directRefs = @()
foreach ($proj in $srcProjects) {
    $content = Get-Content $proj.FullName -Raw
    if ($content -match 'PackageReference.*Include="Newtonsoft\.Json"') {
        $hasPrivateAssets = $content -match 'Newtonsoft\.Json.*PrivateAssets.*All'
        $directRefs += [PSCustomObject]@{
            Project = $proj.Name
            Path = $proj.FullName.Replace($repoRoot, "").TrimStart("\", "/")
            PrivateAssets = $hasPrivateAssets
        }
        if ($hasPrivateAssets) {
            Write-Host "  [OK] $($proj.Name) - PrivateAssets=All (internal only)" -ForegroundColor Green
        } else {
            Write-Host "  [WARN] $($proj.Name) - Newtonsoft.Json exposed to consumers!" -ForegroundColor Red
            $errors += "Project $($proj.Name) references Newtonsoft.Json without PrivateAssets=All"
        }
    }
}
if ($directRefs.Count -eq 0) {
    Write-Host "  No direct Newtonsoft.Json references in src/ projects." -ForegroundColor Green
}
Write-Host ""

# 2. Check for using Newtonsoft.Json in source code (excluding Legacy/ folder)
Write-Host "2. Checking source code for Newtonsoft.Json usage (excluding Legacy/)..." -ForegroundColor Yellow
$srcFiles = Get-ChildItem "$repoRoot/src" -Recurse -Include "*.cs" | Where-Object {
    $_.FullName -notmatch "\\Legacy\\" -and
    $_.Name -notmatch "^Legacy" -and
    $_.FullName -notmatch "\\obj\\" -and
    $_.FullName -notmatch "\\bin\\"
}
$newtonsoftUsage = @()
foreach ($file in $srcFiles) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -match 'using Newtonsoft\.Json') {
        $newtonsoftUsage += $file.FullName.Replace($repoRoot, "").TrimStart("\", "/")
    }
}
if ($newtonsoftUsage.Count -gt 0) {
    foreach ($f in $newtonsoftUsage) {
        Write-Host "  [WARN] $f" -ForegroundColor Red
        $errors += "Source file uses Newtonsoft.Json outside Legacy/: $f"
    }
} else {
    Write-Host "  No Newtonsoft.Json usage outside Legacy/ folder." -ForegroundColor Green
}
Write-Host ""

# 3. Check shipped packages for Newtonsoft.Json DLL
Write-Host "3. Checking shipped packages for Newtonsoft.Json.dll..." -ForegroundColor Yellow
$nuspecs = Get-ChildItem "$repoRoot/src/package" -Recurse -Filter "*.nuspec" | Where-Object {
    $_.Name -notmatch "sourcebuild"
}
$shippedNewtonsoft = @()
foreach ($nuspec in $nuspecs) {
    $content = Get-Content $nuspec.FullName -Raw
    if ($content -match 'Newtonsoft\.Json\.dll') {
        $shippedNewtonsoft += $nuspec.Name
        Write-Host "  [INFO] $($nuspec.Name) ships Newtonsoft.Json.dll" -ForegroundColor Yellow
    }
}
if ($shippedNewtonsoft.Count -eq 0) {
    Write-Host "  No packages ship Newtonsoft.Json.dll." -ForegroundColor Green
}
Write-Host ""

# 4. Check for System.Text.Json in net462/netstandard2.0 builds
Write-Host "4. Checking for System.Text.Json references in .NET Framework builds..." -ForegroundColor Yellow
foreach ($proj in $srcProjects) {
    $content = Get-Content $proj.FullName -Raw
    # Look for unconditional System.Text.Json PackageReference
    if ($content -match 'PackageReference.*Include="System\.Text\.Json"' -and
        $content -notmatch 'Condition.*NETCOREAPP.*System\.Text\.Json' -and
        $content -notmatch 'System\.Text\.Json.*Condition.*NETCOREAPP') {
        # Check if the project targets net462 or netstandard
        if ($content -match 'net462|netstandard|NET_FRAMEWORK') {
            Write-Host "  [WARN] $($proj.Name) - unconditional STJ ref may leak to .NET Framework!" -ForegroundColor Red
            $errors += "Project $($proj.Name) has unconditional System.Text.Json reference"
        }
    }
}
Write-Host "  Check complete." -ForegroundColor Green
Write-Host ""

# 5. Check binding redirects for Newtonsoft.Json
Write-Host "5. Checking binding redirects for Newtonsoft.Json..." -ForegroundColor Yellow
$configs = Get-ChildItem "$repoRoot/src" -Recurse -Filter "*.config" | Where-Object {
    $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\"
}
foreach ($config in $configs) {
    $content = Get-Content $config.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -match 'Newtonsoft\.Json') {
        Write-Host "  [INFO] $($config.FullName.Replace($repoRoot, '').TrimStart('\', '/')) has Newtonsoft.Json binding redirect" -ForegroundColor Yellow
    }
}
Write-Host ""

# 6. Summary
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Direct references in src/: $($directRefs.Count)"
Write-Host "Source files using Newtonsoft (non-Legacy): $($newtonsoftUsage.Count)"
Write-Host "Packages shipping Newtonsoft.Json.dll: $($shippedNewtonsoft.Count)"
Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "ERRORS ($($errors.Count)):" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  - $e" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "All checks passed. Newtonsoft.Json is properly contained." -ForegroundColor Green
    exit 0
}
