[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [System.String] $configuration
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Unzip {
    param([string]$zipfile, [string]$outpath)

    Write-Verbose "Unzipping '$zipfile' to '$outpath'."

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function Verify-Nuget-Packages {
    Write-Host "Starting Verify-Nuget-Packages."
    $expectedNumOfFiles = @{
        "Microsoft.CodeCoverage"                      = 59;
        "Microsoft.NET.Test.Sdk"                      = 16;
        "Microsoft.TestPlatform"                      = 608;
        "Microsoft.TestPlatform.Build"                = 21;
        "Microsoft.TestPlatform.CLI"                  = 493;
        "Microsoft.TestPlatform.Extensions.TrxLogger" = 35;
        "Microsoft.TestPlatform.ObjectModel"          = 93;
        "Microsoft.TestPlatform.AdapterUtilities"     = 34;
        "Microsoft.TestPlatform.Portable"             = 595;
        "Microsoft.TestPlatform.TestHost"             = 63;
        "Microsoft.TestPlatform.TranslationLayer"     = 123;
        "Microsoft.TestPlatform.Internal.Uwp"         = 39;
    }

    $packageDirectory = Resolve-Path (Join-Path $PSScriptRoot "../artifacts/packages/$configuration")
    $tmpDirectory = Resolve-Path (Join-Path $PSScriptRoot "../artifacts/tmp/$configuration")
    $nugetPackages = Get-ChildItem -Filter "*.nupkg" $packageDirectory -Recurse -Exclude "*.symbols.nupkg" | ForEach-Object { $_.FullName }

    Write-Verbose "Unzip NuGet packages."
    $unzipNugetPackageDirs = New-Object System.Collections.Generic.List[System.Object]
    foreach ($nugetPackage in $nugetPackages) {
        $unzipNugetPackageDir = $(Join-Path $tmpDirectory $(Get-Item $nugetPackage).BaseName)
        $unzipNugetPackageDirs.Add($unzipNugetPackageDir)

        if (Test-Path -Path $unzipNugetPackageDir) {
            Remove-Item -Force -Recurse $unzipNugetPackageDir
        }

        Unzip $nugetPackage $unzipNugetPackageDir
    }

    $versionPropsXml = [xml](Get-Content $PSScriptRoot\Versions.props)
    $version = $versionPropsXml.Project.PropertyGroup.VersionPrefix | Where-Object { $null -ne $_ } | Select-Object -First 1
    if ($null -eq $version) {
        throw "version is null"
    }

    Write-Verbose "Verify NuGet packages files."
    $errors = @()
    foreach ($unzipNugetPackageDir in $unzipNugetPackageDirs) {
        try {
            $packageFullName = (Get-Item $unzipNugetPackageDir).BaseName
            $versionIndex = $packageFullName.LastIndexOf($version)
            $packageKey = $packageFullName.Substring(0, $versionIndex - 1) # Remove last dot
            Write-Verbose "verifying package '$packageKey'."

            $actualNumOfFiles = (Get-ChildItem -Recurse -File -Path $unzipNugetPackageDir).Count
            if ($expectedNumOfFiles[$packageKey] -ne $actualNumOfFiles) {
                $errors += "Number of files are not equal for '$packageKey', expected: $($expectedNumOfFiles[$packageKey]) actual: $actualNumOfFiles"
            }
        }
        finally {
            if ($null -ne $unzipNugetPackageDir -and (Test-Path $unzipNugetPackageDir)) {
                Remove-Item -Force -Recurse $unzipNugetPackageDir | Out-Null
            }
        }
    }

    if ($errors) {
        Write-Error "There are $($errors.Count) errors:`n$($errors -join "`n")"
    }

    Write-Host "Completed Verify-Nuget-Packages."
}

Verify-Nuget-Packages
