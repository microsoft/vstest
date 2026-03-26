# Verifies that binding redirects in .exe.config files match the actual assembly
# versions of the DLLs shipped in the same package directory.

function Verify-BindingRedirects {
    param(
        [Parameter(Mandatory)]
        [string[]]$PackageDirs
    )

    $errors = @()

    foreach($packageDir in $PackageDirs) {
        if (-not (Test-Path $packageDir)) {
            continue
        }

        $configFiles = @(Get-ChildItem -Recurse $packageDir -Filter "*.config" | Where-Object {
            $_.Extension -eq ".config" -and (Get-Content $_.FullName -Raw) -match "bindingRedirect"
        })

        if ($configFiles.Count -eq 0) {
            continue
        }

        $packageName = Split-Path $packageDir -Leaf
        Write-Host "Verifying binding redirects in '$packageName' ($($configFiles.Count) config file(s))."

        foreach ($configFile in $configFiles) {
            [xml]$config = Get-Content $configFile.FullName

            $xmlNsMgr = New-Object System.Xml.XmlNamespaceManager($config.NameTable)
            $xmlNsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

            $dependentAssemblies = $config.SelectNodes("//asm:dependentAssembly", $xmlNsMgr)
            if ($null -eq $dependentAssemblies -or $dependentAssemblies.Count -eq 0) {
                continue
            }

            # Determine DLL search directories: config file's directory + probing paths.
            $configDir = Split-Path $configFile.FullName
            $searchDirs = @($configDir)

            $probingNodes = $config.SelectNodes("//asm:probing", $xmlNsMgr)
            foreach ($probing in $probingNodes) {
                $privatePath = $probing.GetAttribute("privatePath")
                if ($privatePath) {
                    foreach ($subPath in $privatePath -split ";") {
                        $probingDir = Join-Path $configDir $subPath.Trim()
                        if (Test-Path $probingDir) {
                            $searchDirs += $probingDir
                        }
                    }
                }
            }

            foreach ($dep in $dependentAssemblies) {
                $identityNode = $dep.SelectSingleNode("asm:assemblyIdentity", $xmlNsMgr)
                $redirectNode = $dep.SelectSingleNode("asm:bindingRedirect", $xmlNsMgr)

                if ($null -eq $identityNode -or $null -eq $redirectNode) {
                    continue
                }

                $assemblyName = $identityNode.GetAttribute("name")
                $expectedVersion = $redirectNode.GetAttribute("newVersion")

                # Search for the DLL in config directory and probing paths.
                $dllPath = $null
                foreach ($dir in $searchDirs) {
                    $candidate = Join-Path $dir "$assemblyName.dll"
                    if (Test-Path $candidate) {
                        $dllPath = $candidate
                        break
                    }
                }

                if (-not $dllPath) {
                    Write-Host "  INFO: $assemblyName referenced in $($configFile.Name) not found in package (may be runtime-provided)"
                    continue
                }

                # Load assembly in a subprocess to avoid locking the DLL in the current process.
                $actualVersion = & pwsh -NoProfile -Command "[System.Reflection.Assembly]::LoadFile('$dllPath').GetName().Version.ToString()"

                if ($LASTEXITCODE -ne 0) {
                    Write-Host "  WARN: Could not read assembly version for $assemblyName ($dllPath)"
                    continue
                }

                # Assembly versions are 4-part (Major.Minor.Build.Revision); normalize both to
                # 4-part for comparison so "15.0.0.0" equals "15.0.0.0" and "15.0.0" matches "15.0.0.0".
                $normalizedExpected = Normalize-Version $expectedVersion
                $normalizedActual   = Normalize-Version $actualVersion

                if ($normalizedExpected -ne $normalizedActual) {
                    $msg = "MISMATCH: $assemblyName in $($configFile.Name): redirect says $expectedVersion but DLL is $actualVersion"
                    $errors += $msg
                    Write-Host "  ERROR: $msg"
                } else {
                    Write-Host "  OK: $assemblyName $actualVersion"
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
