[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [ValidateSet("Debug", "Release")]
    [string] $configuration,

    [Parameter(Mandatory)]
    [string] $versionPrefix,

    [Parameter(Mandatory)]
    [string] $currentBranch
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Verify-Nuget-Packages {
    Write-Host "Starting Verify-Nuget-Packages."
    $expectedNumOfFiles = @{
        "Microsoft.CodeCoverage"                      = 59;
        "Microsoft.NET.Test.Sdk"                      = 15;
        "Microsoft.TestPlatform"                      = 607;
        "Microsoft.TestPlatform.Build"                = 20;
        "Microsoft.TestPlatform.CLI"                  = 470;
        "Microsoft.TestPlatform.Extensions.TrxLogger" = 34;
        "Microsoft.TestPlatform.ObjectModel"          = 92;
        "Microsoft.TestPlatform.AdapterUtilities"     = 75;
        "Microsoft.TestPlatform.Portable"             = 592;
        "Microsoft.TestPlatform.TestHost"             = 62;
        "Microsoft.TestPlatform.TranslationLayer"     = 122;
        "Microsoft.TestPlatform.Internal.Uwp"         = 38;
    }

    $packageDirectory = Resolve-Path "$PSScriptRoot/../artifacts/packages/$configuration"
    $tmpDirectory = Resolve-Path "$PSScriptRoot/../artifacts/tmp/$configuration"

    $pattern = "*.$versionPrefix*.nupkg"
    $nugetPackages = @(Get-ChildItem $packageDirectory -Filter $pattern -Recurse -File | Where-Object { $_.Name -notLike "*.symbols.nupkg"})

    if (0 -eq $nugetPackages.Length) {
        throw "No nuget packages matching $pattern were found in '$packageDirectory'."
    }

    $suffixes = @($nugetPackages -replace ".*?$([regex]::Escape($versionPrefix))(.*)\.nupkg", '$1' | Sort-Object -Unique)
    if (1 -lt $suffixes.Length) {
        Write-Host "There are two different suffixes matching the same version prefix: '$($suffixes -join "', '")'".

        $latestNuget = $nugetPackages |
            Where-Object { $_.Name -like "Microsoft.TestPlatform.ObjectModel.*" } |
            Sort-Object -Property LastWriteTime -Descending |
            Select-Object -First 1

        $suffix = $suffixes | Where { $latestNuget.Name.Contains("$versionPrefix$_.nupkg") }
        $version = "$versionPrefix$suffix"
        Write-Host "The most recently written Microsoft.TestPlatform.ObjectModel.* nuget, is $($latestNuget.Name), which has '$suffix' suffix. Selecting only packages with that suffix."

        $nugetPackages = $nugetPackages | Where-Object { $_.Name -like "*$version.nupkg" }
    }
    else {
        $suffix = $suffixes[0]
        $version = "$versionPrefix$suffix"
    }


    Write-Host "Found $(@($nugetPackages).Count) nuget packages:`n    $($nugetPackages.FullName -join "`n    ")"
    Write-Host "Unzipping NuGet packages."
    $unzipNugetPackageDirs = @()
    foreach ($nugetPackage in $nugetPackages) {
        $unzipNugetPackageDir = Join-Path $tmpDirectory $nugetPackage.BaseName
        $unzipNugetPackageDirs += $unzipNugetPackageDir

        if (Test-Path -Path $unzipNugetPackageDir) {
            Remove-Item -Force -Recurse $unzipNugetPackageDir
        }

        Unzip $nugetPackage.FullName $unzipNugetPackageDir
    }

    Write-Host "Verify NuGet packages files."
    $errors = @()
    foreach ($unzipNugetPackageDir in $unzipNugetPackageDirs) {
        try {
            $packageBaseName = (Get-Item $unzipNugetPackageDir).BaseName
            $packageKey = $packageBaseName.Replace([string]".$version", [string]"")
            Write-Host "Verifying package '$packageBaseName'."

            $actualNumOfFiles = (Get-ChildItem -Recurse -File -Path $unzipNugetPackageDir).Count
            if (-not $expectedNumOfFiles.ContainsKey($packageKey)) {
                $errors += "Package '$packageKey' is not present in file expectedNumOfFiles table. Is that package known?"
                continue
            }
            if ($expectedNumOfFiles[$packageKey] -ne $actualNumOfFiles) {
                $errors += "Number of files are not equal for '$packageBaseName', expected: $($expectedNumOfFiles[$packageKey]) actual: $actualNumOfFiles"
            }

            if ($packageKey -eq "Microsoft.TestPlatform") {
                Verify-Version -nugetDir $unzipNugetPackageDir -errors $errors
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

function Unzip {
    param([string]$zipfile, [string]$outpath)

    Write-Verbose "Unzipping '$zipfile' to '$outpath'."

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function Match-VersionAgainstBranch {
    param ([string]$vsTestVersion, [string]$branchName,  [string[]]$errors)

    # Output useful info.
    Write-Host "VSTest Product Version: `"$vsTestVersion`""
    Write-Host "Current Branch: `"$branchName`""

    $versionIsRTM = $vsTestVersion -match "^\d+\.\d+\.\d+$"
    $versionIsRelease = $vsTestVersion -match "^\d+\.\d+\.\d+\-release\-\d{8}\-\d{2}$"
    $versionIsPreview = $vsTestVersion -match "^\d+\.\d+\.\d+\-preview\-\d{8}\-\d{2}$"

    $isReleaseBranch = $branchName -like "rel/*"
    $isPreviewBranch = $branchName -like "main"

    if (!$isReleaseBranch -and !$isPreviewBranch) {
        Write-Host "Skipping check since branch is neither `"release`" nor `"preview`""
        return
    }

    Write-Host "Matching branch against product version ... "
    if ($isReleaseBranch -and !$versionIsRTM -and !$versionIsRelease) {
        $errors += "Release version `"$vsTestVersion`" should either be RTM, or contain a `"release`" suffix."
    }
    if ($isPreviewBranch -and !$versionIsPreview) {
        $errors += "Preview version `"$vsTestVersion`" should contain a `"preview`" suffix."
    }
}

function Verify-Version {
    param ([string]$nugetDir, [string[]] $errors)

    $vsTestExe = "$nugetDir/tools/net462/Common7/IDE/Extensions/TestPlatform/vstest.console.exe"
    $vsTestProductVersion = (Get-Item $vsTestExe).VersionInfo.ProductVersion

    Match-VersionAgainstBranch -vsTestVersion $vsTestProductVersion -branchName $currentBranch -errors $errors
}

Verify-Nuget-Packages
