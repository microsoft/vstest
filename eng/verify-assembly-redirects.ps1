[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [ValidateSet("Debug", "Release")]
    [string] $configuration
)

$ErrorActionPreference = 'Stop'

$isCI = $env:TF_BUILD -eq 'true' -or $env:CI -eq 'true'
$repoRoot = Resolve-Path "$PSScriptRoot/.."

# Map each project to its app.config and the TFM used for .NET Framework builds.
$projects = @(
    @{ Name = "vstest.console";  Config = "src/vstest.console/app.config";  Tfm = "net48" }
    @{ Name = "testhost.x86";    Config = "src/testhost.x86/app.config";    Tfm = "net462" }
    @{ Name = "datacollector";   Config = "src/datacollector/app.config";   Tfm = "net48" }
)

function Get-ManagedAssemblyVersion {
    param([string] $dllPath)

    try {
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath)

        return $assemblyName.Version.ToString()
    }
    catch {
        return $null
    }
}

function Resolve-AssemblyPath {
    param(
        [string] $assemblyName,
        [string] $binDir
    )

    # Check directly in binDir first, then in RID subdirectories (e.g., win7-x64, win-x86).
    $dllName = "$assemblyName.dll"
    $dllPath = Join-Path $binDir $dllName
    if (Test-Path $dllPath) {
        return $dllPath
    }

    $ridDirs = Get-ChildItem $binDir -Directory -ErrorAction SilentlyContinue
    foreach ($ridDir in $ridDirs) {
        $dllPath = Join-Path $ridDir.FullName $dllName
        if (Test-Path $dllPath) {
            return $dllPath
        }
    }

    return $null
}

$errors = @()
$fixes = @()
$configsToFix = @{}

foreach ($project in $projects) {
    $configPath = Join-Path $repoRoot $project.Config
    if (-not (Test-Path $configPath)) {
        Write-Host "Skipping $($project.Name): '$configPath' not found."
        continue
    }

    $binDir = Join-Path $repoRoot "artifacts/bin/$($project.Name)/$configuration/$($project.Tfm)"
    if (-not (Test-Path $binDir)) {
        Write-Host "Skipping $($project.Name): build output '$binDir' not found. Build the project first."
        continue
    }

    Write-Host "Checking assembly redirects for $($project.Name) ($($project.Tfm))..."

    [xml]$xml = Get-Content $configPath -Raw
    $nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $nsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

    $dependentAssemblies = $xml.SelectNodes("//asm:dependentAssembly", $nsMgr)
    foreach ($dep in $dependentAssemblies) {
        $identity = $dep.SelectSingleNode("asm:assemblyIdentity", $nsMgr)
        $redirect = $dep.SelectSingleNode("asm:bindingRedirect", $nsMgr)
        if (-not $identity -or -not $redirect) {
            continue
        }

        $assemblyName = $identity.GetAttribute("name")
        $currentNewVersion = $redirect.GetAttribute("newVersion")
        $currentOldVersion = $redirect.GetAttribute("oldVersion")

        $dllPath = Resolve-AssemblyPath -assemblyName $assemblyName -binDir $binDir
        if (-not $dllPath) {
            Write-Host "  $assemblyName - assembly not found in '$binDir', skipping."
            continue
        }

        $actualVersion = Get-ManagedAssemblyVersion -dllPath $dllPath
        if (-not $actualVersion) {
            Write-Host "  $assemblyName - could not read version (native or corrupt?), skipping."
            continue
        }

        if ($currentNewVersion -eq $actualVersion) {
            Write-Host "  $assemblyName - OK ($actualVersion)"
            continue
        }

        # newVersion needs updating, and the upper bound of oldVersion range too.
        $newOldVersion = $currentOldVersion
        if ($currentOldVersion -match '^(.*)-(.*)$') {
            $newOldVersion = "$($Matches[1])-$actualVersion"
        }

        $errors += "$($project.Name): $assemblyName redirect newVersion is '$currentNewVersion' but actual assembly version is '$actualVersion'"

        if (-not $configsToFix.ContainsKey($configPath)) {
            $configsToFix[$configPath] = @()
        }

        $configsToFix[$configPath] += @{
            AssemblyName  = $assemblyName
            OldNewVersion = $currentNewVersion
            NewNewVersion = $actualVersion
            OldOldVersion = $currentOldVersion
            NewOldVersion = $newOldVersion
        }

        if ($isCI) {
            Write-Host "  $assemblyName - MISMATCH: expected $actualVersion, found $currentNewVersion" -ForegroundColor Red
        }
        else {
            Write-Host "  $assemblyName - FIXING: $currentNewVersion -> $actualVersion (oldVersion: $currentOldVersion -> $newOldVersion)" -ForegroundColor Yellow
        }
    }
}

# Apply fixes by string replacement to preserve original formatting.
if (-not $isCI) {
    foreach ($configPath in $configsToFix.Keys) {
        $content = [System.IO.File]::ReadAllText($configPath)
        foreach ($fix in $configsToFix[$configPath]) {
            $content = $content -replace [regex]::Escape("oldVersion=""$($fix.OldOldVersion)"" newVersion=""$($fix.OldNewVersion)"""), "oldVersion=""$($fix.NewOldVersion)"" newVersion=""$($fix.NewNewVersion)"""
        }

        # Preserve the original BOM if present.
        $bom = [System.IO.File]::ReadAllBytes($configPath)
        $hasBom = $bom.Length -ge 3 -and $bom[0] -eq 0xEF -and $bom[1] -eq 0xBB -and $bom[2] -eq 0xBF
        $encoding = if ($hasBom) { New-Object System.Text.UTF8Encoding($true) } else { New-Object System.Text.UTF8Encoding($false) }
        [System.IO.File]::WriteAllText($configPath, $content, $encoding)
        Write-Host "Updated '$configPath'." -ForegroundColor Green
    }
}

if ($errors) {
    if ($isCI) {
        $message = "Assembly binding redirect mismatches detected:`n"
        $message += ($errors -join "`n")
        $message += "`n`nTo fix this, run the following command locally after building:`n"
        $message += "  .\build.cmd -c $configuration`n"
        $message += "This will rebuild and auto-update the app.config files with the correct versions.`n"
        $message += "Then commit the updated app.config files."
        Write-Error $message
    }
    else {
        Write-Host "`nFixed $($configsToFix.Values.Count) binding redirect(s). Please commit the updated app.config files." -ForegroundColor Green
    }
}
else {
    Write-Host "`nAll assembly binding redirects are up to date." -ForegroundColor Green
}
