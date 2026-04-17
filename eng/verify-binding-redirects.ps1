$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Verifies that binding redirects in source app.config files match the actual
# assembly versions of the DLLs shipped in the extracted nupkg packages.
#
# In CI: validates and fails with instructions to run locally.
# Locally: auto-fixes the source app.config files with the correct versions.

# Each source app.config maps to a specific exe that ships in the packages.
$script:AppConfigs = @(
    @{ Config = "src/vstest.console/app.config";  ExeName = "vstest.console.exe" }
    @{ Config = "src/testhost.x86/app.config";    ExeName = "testhost.x86.exe" }
    @{ Config = "src/datacollector/app.config";    ExeName = "datacollector.exe" }
)

function Find-ExeInPackages {
    param(
        [string] $ExeName,
        [string[]] $PackageDirs
    )

    foreach ($dir in $PackageDirs) {
        $found = Get-ChildItem $dir -Recurse -Filter $ExeName -File -ErrorAction SilentlyContinue
        if ($found) {
            # Prefer the one closest to a net462 or root layout (not nested in TestHostNetFramework).
            $preferred = $found | Where-Object { $_.FullName -notlike "*TestHostNetFramework*" } | Select-Object -First 1
            if ($preferred) { return $preferred.DirectoryName }
            return $found[0].DirectoryName
        }
    }

    return $null
}

function Get-ManagedAssemblyVersion {
    param([string] $DllPath)

    try {
        return [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Version.ToString()
    }
    catch {
        return $null
    }
}

function Verify-BindingRedirects {
    param(
        [Parameter(Mandatory)]
        [string[]]$PackageDirs,

        [Parameter(Mandatory)]
        [ValidateSet("Debug", "Release")]
        [string]$Configuration
    )

    $repoRoot = Resolve-Path "$PSScriptRoot/.."
    $errors = @()
    $configsToFix = @{}

    foreach ($entry in $script:AppConfigs) {
        $configPath = Join-Path $repoRoot $entry.Config
        if (-not (Test-Path $configPath)) {
            Write-Host "Skipping $($entry.ExeName): config '$configPath' not found."
            continue
        }

        $deployDir = Find-ExeInPackages -ExeName $entry.ExeName -PackageDirs $PackageDirs
        if (-not $deployDir) {
            Write-Host "Skipping $($entry.ExeName): not found in any extracted package."
            continue
        }

        Write-Host "Checking assembly redirects for $($entry.ExeName) (from '$deployDir')..."

        [xml]$xml = Get-Content $configPath -Raw
        $nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $nsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

        # Build search directories from probing paths in the config.
        $searchDirs = @($deployDir)
        $probingNodes = $xml.SelectNodes("//asm:probing", $nsMgr)
        foreach ($probing in $probingNodes) {
            $privatePath = $probing.GetAttribute("privatePath")
            if ($privatePath) {
                foreach ($subPath in $privatePath -split ";") {
                    $probingDir = Join-Path $deployDir $subPath.Trim()
                    if (Test-Path $probingDir) { $searchDirs += $probingDir }
                }
            }
        }

        $dependentAssemblies = $xml.SelectNodes("//asm:dependentAssembly", $nsMgr)
        foreach ($dep in $dependentAssemblies) {
            $identity = $dep.SelectSingleNode("asm:assemblyIdentity", $nsMgr)
            $redirect = $dep.SelectSingleNode("asm:bindingRedirect", $nsMgr)
            if (-not $identity -or -not $redirect) { continue }

            $assemblyName = $identity.GetAttribute("name")
            $currentNewVersion = $redirect.GetAttribute("newVersion")
            $currentOldVersion = $redirect.GetAttribute("oldVersion")

            # Look for the assembly DLL in each search directory.
            $dllPath = $null
            foreach ($dir in $searchDirs) {
                $candidate = Join-Path $dir "$assemblyName.dll"
                if (Test-Path $candidate) { $dllPath = $candidate; break }
            }

            if (-not $dllPath) {
                Write-Host "  $assemblyName - not found in package layout, skipping."
                continue
            }

            $actualVersion = Get-ManagedAssemblyVersion -DllPath $dllPath
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
            $message += "  .\build.cmd -c $Configuration`n"
            $message += "This will rebuild, pack, and auto-update the app.config files with the correct versions.`n"
            $message += "Then commit the updated app.config files."
            Write-Error $message
        }
        else {
            Write-Host "`nFixed $($errors.Count) binding redirect(s). Please commit the updated app.config files." -ForegroundColor Green
        }
    }
    else {
        Write-Host "All binding redirects match their DLL versions."
    }
}
