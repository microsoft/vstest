[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [ValidateSet("Debug", "Release")]
    [string] $configuration,

    [Parameter(Mandatory)]
    [string[]] $extractedPackageDirs
)

$ErrorActionPreference = 'Stop'

$isCI = $env:TF_BUILD -eq 'true' -or $env:CI -eq 'true'
$repoRoot = Resolve-Path "$PSScriptRoot/.."

# Each app.config maps to a specific exe that ships in the packages.
# We find the exe in the extracted packages and resolve assemblies from that directory.
$appConfigs = @(
    @{ Config = "src/vstest.console/app.config";  ExeName = "vstest.console.exe" }
    @{ Config = "src/testhost.x86/app.config";    ExeName = "testhost.x86.exe" }
    @{ Config = "src/datacollector/app.config";    ExeName = "datacollector.exe" }
)

function Get-ManagedAssemblyVersion {
    param([string] $dllPath)

    try {
        return [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString()
    }
    catch {
        return $null
    }
}

function Find-ExeInPackages {
    param(
        [string] $exeName,
        [string[]] $packageDirs
    )

    # Search extracted packages for the exe. Prefer the VSIX (most complete layout),
    # then the main Microsoft.TestPlatform nupkg.
    foreach ($dir in $packageDirs) {
        $found = Get-ChildItem $dir -Recurse -Filter $exeName -File -ErrorAction SilentlyContinue
        if ($found) {
            # Prefer the one closest to a net462 or root layout (not nested in TestHostNetFramework).
            $preferred = $found | Where-Object { $_.FullName -notlike "*TestHostNetFramework*" } | Select-Object -First 1
            if ($preferred) {
                return $preferred.DirectoryName
            }

            return $found[0].DirectoryName
        }
    }

    return $null
}

$errors = @()
$configsToFix = @{}

foreach ($entry in $appConfigs) {
    $configPath = Join-Path $repoRoot $entry.Config
    if (-not (Test-Path $configPath)) {
        Write-Host "Skipping $($entry.ExeName): config '$configPath' not found."
        continue
    }

    $deployDir = Find-ExeInPackages -exeName $entry.ExeName -packageDirs $extractedPackageDirs
    if (-not $deployDir) {
        Write-Host "Skipping $($entry.ExeName): not found in any extracted package."
        continue
    }

    Write-Host "Checking assembly redirects for $($entry.ExeName) (from '$deployDir')..."

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

        # Look for the assembly in the same directory as the exe, then in Extensions subfolder.
        $dllPath = Join-Path $deployDir "$assemblyName.dll"
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $deployDir "Extensions/$assemblyName.dll"
        }

        if (-not (Test-Path $dllPath)) {
            Write-Host "  $assemblyName - not found in package layout, skipping."
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

        $errors += "$($entry.ExeName): $assemblyName redirect newVersion is '$currentNewVersion' but actual assembly version is '$actualVersion'"

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
        $message += "`n`nTo fix this, run the following command locally after building and packing:`n"
        $message += "  .\build.cmd -c $configuration`n"
        $message += "This will rebuild, pack, and auto-update the app.config files with the correct versions.`n"
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
