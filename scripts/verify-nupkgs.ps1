Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)

    Write-VerboseLog "Unzip $zipfile to $outpath."

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function Verify-Nuget-Packages($packageDirectory, $version)
{
    Write-Log "Starting Verify-Nuget-Packages."
    $expectedNumOfFiles = @{
        "Microsoft.CodeCoverage" = 57;
        "Microsoft.NET.Test.Sdk" = 27;
        "Microsoft.TestPlatform" = 624;
        "Microsoft.TestPlatform.Build" = 21;
        "Microsoft.TestPlatform.CLI" = 427;
        "Microsoft.TestPlatform.Extensions.TrxLogger" = 35;
        "Microsoft.TestPlatform.ObjectModel" = 238;
        "Microsoft.TestPlatform.AdapterUtilities" = 62;
        "Microsoft.TestPlatform.Portable" = 648;
        "Microsoft.TestPlatform.TestHost" = 208;
        "Microsoft.TestPlatform.TranslationLayer" = 123;
        "Microsoft.TestPlatform.Internal.Uwp" = 86;
    }

    $nugetPackages = Get-ChildItem -Filter "*$version*.nupkg" $packageDirectory | % { $_.FullName }

    Write-VerboseLog "Unzip NuGet packages."
    $unzipNugetPackageDirs =  New-Object System.Collections.Generic.List[System.Object]
    foreach($nugetPackage in $nugetPackages)
    {
        $unzipNugetPackageDir = $(Join-Path $packageDirectory $(Get-Item $nugetPackage).BaseName)
        $unzipNugetPackageDirs.Add($unzipNugetPackageDir)

        if(Test-Path -Path $unzipNugetPackageDir)
        {
            Remove-Item -Force -Recurse $unzipNugetPackageDir
        }

        Unzip $nugetPackage $unzipNugetPackageDir
    }

    Write-VerboseLog "Verify NuGet packages files."
    $errors = @()
    $seenPackageKeys = New-Object System.Collections.Generic.List[string]
    foreach($unzipNugetPackageDir in $unzipNugetPackageDirs)
    {
        try {
            $actualNumOfFiles = (Get-ChildItem -Recurse -File -Path $unzipNugetPackageDir).Count
            $versionLen = $version.Length + 1  # +1 for dot
            $packageKey = (Get-Item $unzipNugetPackageDir).BaseName -replace ".{$versionLen}$"
            $seenPackageKeys.Add($packageKey)
            Write-VerboseLog "verifying package $packageKey."

            if ($expectedNumOfFiles[$packageKey] -ne $actualNumOfFiles) {
                $errors += "Number of files are not equal $unzipNugetPackageDir, expected: $($expectedNumOfFiles[$packageKey]) actual: $actualNumOfFiles"
            }
        }
        finally {
            if ($null -ne $unzipNugetPackageDir -and (Test-Path $unzipNugetPackageDir)) {
                Remove-Item -Force -Recurse $unzipNugetPackageDir | Out-Null
            }
        }
    }

    # A CHECK THAT ONLY LOOKS AT WHAT IT FOUND CANNOT REPORT WHAT IS MISSING.
    #
    # Everything above iterates $unzipNugetPackageDirs, which comes from Get-ChildItem of the
    # output folder. An expected package that was never produced contributes no directory, so it
    # is compared against nothing and the function returns clean. This is not hypothetical: builds
    # 3067129 and a local Release run both produced 10 of these 12 - Microsoft.TestPlatform.CLI and
    # Microsoft.TestPlatform.Build were absent because Pack was a silent no-op - and this function
    # passed on both. $expectedNumOfFiles was being read as a manifest of what ships when it only
    # ever constrained what happened to be there.
    #
    # The two directions are reported separately and never netted: a missing package and an
    # unexpected package are two findings, not zero.
    $expectedPackageKeys = $expectedNumOfFiles.Keys
    $missingPackages = @($expectedPackageKeys | Where-Object { $seenPackageKeys -notcontains $_ })
    $unexpectedPackages = @($seenPackageKeys | Where-Object { $expectedPackageKeys -notcontains $_ })

    if ($missingPackages.Count -gt 0) {
        $errors += "Expected packages were NOT produced in ${packageDirectory}: $($missingPackages -join ', '). Expected $($expectedPackageKeys.Count) packages, found $($seenPackageKeys.Count)."
    }

    if ($unexpectedPackages.Count -gt 0) {
        $errors += "Packages were produced that are not in the expected set: $($unexpectedPackages -join ', '). Either the package is new and belongs in `$expectedNumOfFiles, or it is stale output from an earlier run."
    }

    if ($errors) { 
        Write-Error "There are $($errors.Count) errors:`n$($errors -join "`n")"
    }

    Write-Log "Completed Verify-Nuget-Packages. Verified $($seenPackageKeys.Count) of $($expectedPackageKeys.Count) expected packages: $(($seenPackageKeys | Sort-Object) -join ', ')"
}
