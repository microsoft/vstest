# Verifies that binding redirects in .exe.config files match the actual assembly
# versions of the DLLs shipped in the same package directory.

function Verify-BindingRedirects {
    param(
        [Parameter(Mandatory)]
        [string[]]$PackageDirs
    )

    $errors = @()

    # Step 1: Find ALL DLLs across all package dirs
    $allDlls = @()
    foreach ($packageDir in $PackageDirs) {
        if (Test-Path $packageDir) {
            $allDlls += Get-ChildItem -Recurse $packageDir -Filter "*.dll"
        }
    }

    if ($allDlls.Count -eq 0) {
        return $errors
    }

    # Step 2: Get assembly versions for ALL DLLs in ONE subprocess
    Write-Host "Getting assembly versions for $($allDlls.Count) DLLs..."
    $dllPaths = $allDlls | ForEach-Object { $_.FullName }
    $pathsArg = $dllPaths -join "|"

    $versionOutput = & pwsh -NoProfile -Command "
        `$paths = '$pathsArg' -split '\|'
        foreach (`$p in `$paths) {
            try {
                `$asm = [System.Reflection.Assembly]::LoadFile(`$p)
                `$n = `$asm.GetName()
                Write-Output `"`$p|`$(`$n.Name)|`$(`$n.Version)`"
            } catch {
                Write-Output `"`$p|ERROR|0.0.0.0`"
            }
        }
    "

    # Step 3: Build lookup — array of objects with path, name, version
    $dllVersions = @()
    foreach ($line in $versionOutput) {
        if ($line -and $line.Contains("|")) {
            $parts = $line -split '\|', 3
            $dllVersions += [PSCustomObject]@{
                Path = $parts[0]
                Name = $parts[1]
                Version = $parts[2]
            }
        }
    }
    Write-Host "Got versions for $($dllVersions.Count) assemblies."

    # Step 4: Process each config file and compare (no more subprocess calls)
    foreach ($packageDir in $PackageDirs) {
        if (-not (Test-Path $packageDir)) { continue }

        $configFiles = @(Get-ChildItem -Recurse $packageDir -Filter "*.config" | Where-Object {
            $_.Extension -eq ".config" -and (Get-Content $_.FullName -Raw) -match "bindingRedirect"
        })

        if ($configFiles.Count -eq 0) { continue }

        $packageName = Split-Path $packageDir -Leaf
        Write-Host "Verifying binding redirects in '$packageName' ($($configFiles.Count) config file(s))."

        foreach ($configFile in $configFiles) {
            [xml]$config = Get-Content $configFile.FullName

            $xmlNsMgr = New-Object System.Xml.XmlNamespaceManager($config.NameTable)
            $xmlNsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

            $dependentAssemblies = $config.SelectNodes("//asm:dependentAssembly", $xmlNsMgr)
            if ($null -eq $dependentAssemblies -or $dependentAssemblies.Count -eq 0) { continue }

            # Determine DLL search directories: config file's directory + probing paths.
            $configDir = Split-Path $configFile.FullName
            $searchDirs = @($configDir)

            $probingNodes = $config.SelectNodes("//asm:probing", $xmlNsMgr)
            foreach ($probing in $probingNodes) {
                $privatePath = $probing.GetAttribute("privatePath")
                if ($privatePath) {
                    foreach ($subPath in $privatePath -split ";") {
                        $probingDir = Join-Path $configDir $subPath.Trim()
                        if (Test-Path $probingDir) { $searchDirs += $probingDir }
                    }
                }
            }

            foreach ($dep in $dependentAssemblies) {
                $identityNode = $dep.SelectSingleNode("asm:assemblyIdentity", $xmlNsMgr)
                $redirectNode = $dep.SelectSingleNode("asm:bindingRedirect", $xmlNsMgr)

                if ($null -eq $identityNode -or $null -eq $redirectNode) { continue }

                $assemblyName = $identityNode.GetAttribute("name")
                $expectedVersion = $redirectNode.GetAttribute("newVersion")

                # Find matching DLL from our pre-loaded version data
                $matchingDll = $null
                foreach ($dir in $searchDirs) {
                    $candidate = Join-Path $dir "$assemblyName.dll"
                    $matchingDll = $dllVersions | Where-Object { $_.Path -eq $candidate } | Select-Object -First 1
                    if ($matchingDll) { break }
                }

                if (-not $matchingDll) {
                    Write-Host "  INFO: $assemblyName referenced in $($configFile.Name) not found in package (may be runtime-provided)"
                    continue
                }

                if ($matchingDll.Name -eq "ERROR") {
                    Write-Host "  WARN: Could not read assembly version for $assemblyName"
                    continue
                }

                $normalizedExpected = Normalize-Version $expectedVersion
                $normalizedActual   = Normalize-Version $matchingDll.Version

                if ($normalizedExpected -ne $normalizedActual) {
                    $msg = "MISMATCH: $assemblyName in $($configFile.Name): redirect says $expectedVersion but DLL is $($matchingDll.Version)"
                    $errors += $msg
                    Write-Host "  ERROR: $msg"
                } else {
                    Write-Host "  OK: $assemblyName $($matchingDll.Version)"
                }
            }
        }
    }

    return $errors
}

function Normalize-Version {
    param([string]$v)
    try {
        $ver = [Version]$v.Trim()
        $rev = if ($ver.Revision -lt 0) { 0 } else { $ver.Revision }
        $bld = if ($ver.Build   -lt 0) { 0 } else { $ver.Build }
        return "$($ver.Major).$($ver.Minor).$bld.$rev"
    } catch {
        return $v.Trim()
    }
}
