Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)

    Write-VerboseLog "Unzip $zipfile to $outpath."

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}


# Verifies that every nuspec handed to nuget pack came out as a package.
#
# nuget.exe 3.4.3 prints "pack: invalid arguments." followed by its usage banner and then returns
# exit code 0, so a pack that produced nothing is indistinguishable from one that succeeded if you
# only read the exit code. The exit code of a tool of this vintage is not a contract; the product is.
#
# Verify-Nuget-Packages cannot cover this. It enumerates the packages that exist, so a package that
# was never produced is outside the space it searches - the check is green because it never reached
# the missing thing.
#
# The join key is the id declared inside the nuspec, never the file name. TestPlatform.ObjectModel.nuspec
# declares Microsoft.TestPlatform.ObjectModel, and build.ps1 asks for Microsoft.Net.Test.Sdk.nuspec
# where the file on disk is Microsoft.NET.Test.Sdk.nuspec. A file name is not an identity.
#
# Both directions are reported separately and never netted. Two errors in opposite directions cancel
# in a count, and the closer a count looks to correct the more thoroughly it hides them.
function Verify-Nuget-Packages-Produced($nuspecDirectory, $nuspecFiles, $packageDirectory, $version)
{
    Write-Log "Starting Verify-Nuget-Packages-Produced."

    $expected = @{}
    foreach ($file in $nuspecFiles)
    {
        $nuspecPath = Join-Path $nuspecDirectory $file
        if (-not (Test-Path $nuspecPath))
        {
            Write-Error "Nuspec '$nuspecPath' was handed to nuget pack but is not present."
            continue
        }

        $id = ([xml](Get-Content $nuspecPath)).package.metadata.id
        if ([string]::IsNullOrWhiteSpace($id))
        {
            Write-Error "Nuspec '$nuspecPath' declares no package id, so the package it produces cannot be identified."
            continue
        }

        $expected[$id.Trim()] = $file
    }

    $producedNames = @(Get-ChildItem -Path $packageDirectory -Filter "*.nupkg" -File | ForEach-Object { $_.BaseName })
    $expectedNames = @($expected.Keys | ForEach-Object { "$_.$version" })

    $missing = @($expected.Keys | Where-Object { $producedNames -notcontains "$_.$version" } | Sort-Object)
    $added = @($producedNames | Where-Object { $expectedNames -notcontains $_ } | Sort-Object)

    Write-Log "Nuspecs packed: $($expected.Count). Packages produced: $($producedNames.Count). Missing: $($missing.Count). Added: $($added.Count)."

    foreach ($id in $missing)
    {
        Write-Error "Package '$id' was not produced. Its nuspec '$($expected[$id])' was handed to nuget pack but no '$id.$version.nupkg' exists in '$packageDirectory'."
    }

    foreach ($name in $added)
    {
        Write-Error "Package '$name' is present in '$packageDirectory' but no nuspec handed to nuget pack declares it."
    }

    Write-Log "Completed Verify-Nuget-Packages-Produced."
}

function Verify-Nuget-Packages($packageDirectory)
{
    Write-Log "Starting Verify-Nuget-Packages."
    $expectedNumOfFiles = @{"Microsoft.CodeCoverage" = 29;
                     "Microsoft.NET.Test.Sdk" = 10;
                     "Microsoft.TestPlatform" = 421;
                     "Microsoft.TestPlatform.Build" = 19;
                     "Microsoft.TestPlatform.CLI" = 302;
                     "Microsoft.TestPlatform.ObjectModel" = 65;
                     "Microsoft.TestPlatform.Portable" = 472;
                     "Microsoft.TestPlatform.TestHost" = 139;
                     "Microsoft.TestPlatform.TranslationLayer" = 121}

    $nugetPackages = Get-ChildItem -Filter "*.nupkg" $packageDirectory | % { $_.FullName}

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
    foreach($unzipNugetPackageDir in $unzipNugetPackageDirs)
    {
        $actualNumOfFiles = (Get-ChildItem -Recurse -File -Path $unzipNugetPackageDir).Count
        $versionLen = $TPB_Version.Length + 1  # +1 for dot
        $packageKey = (Get-Item $unzipNugetPackageDir).BaseName -replace ".{$versionLen}$"

        Write-VerboseLog "verifying package $packageKey."

        if( $expectedNumOfFiles[$packageKey] -ne $actualNumOfFiles)
        {
            Write-Error "Number of files are not equal $unzipNugetPackageDir, expected: $($expectedNumOfFiles[$packageKey]) actual: $actualNumOfFiles"
        }

        Remove-Item -Force -Recurse $unzipNugetPackageDir | Out-Null
    }

    Write-Log "Completed Verify-Nuget-Packages."
}
