$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Verifies that no binding redirects exist in source app.config files.
# Binding redirects are a legacy mechanism that causes version mismatches
# and should not be used. See https://github.com/microsoft/vstest/issues/15765.

# Each source app.config maps to a specific exe that ships in the packages.
$script:AppConfigs = @(
    @{ Config = "src/vstest.console/app.config";  ExeName = "vstest.console.exe" }
    @{ Config = "src/testhost.x86/app.config";    ExeName = "testhost.x86.exe" }
    @{ Config = "src/datacollector/app.config";    ExeName = "datacollector.exe" }
)

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

    foreach ($entry in $script:AppConfigs) {
        $configPath = Join-Path $repoRoot $entry.Config
        if (-not (Test-Path $configPath)) {
            Write-Host "Skipping $($entry.ExeName): config '$configPath' not found."
            continue
        }

        Write-Host "Checking $($entry.Config) has no binding redirects..."

        [xml]$xml = Get-Content $configPath -Raw
        $nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $nsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

        $dependentAssemblies = $xml.SelectNodes("//asm:dependentAssembly", $nsMgr)
        foreach ($dep in $dependentAssemblies) {
            $identity = $dep.SelectSingleNode("asm:assemblyIdentity", $nsMgr)
            $redirect = $dep.SelectSingleNode("asm:bindingRedirect", $nsMgr)
            if (-not $identity -or -not $redirect) { continue }

            $assemblyName = $identity.GetAttribute("name")
            $errors += "$($entry.Config): has binding redirect for '$assemblyName'. Remove it."
            Write-Host "  FAIL: found binding redirect for '$assemblyName'" -ForegroundColor Red
        }

        if (-not $errors) {
            Write-Host "  OK - no binding redirects."
        }
    }

    if ($errors) {
        $message = "Binding redirects are not allowed in app.config files:`n"
        $message += ($errors -join "`n")
        $message += "`n`nSee https://github.com/microsoft/vstest/issues/15765 for details."
        Write-Error $message
    }
    else {
        Write-Host "No binding redirects found in any app.config - good."
    }
}
