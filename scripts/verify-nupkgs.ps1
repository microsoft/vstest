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
        "Microsoft.CodeCoverage" = 53;
        "Microsoft.NET.Test.Sdk" = 27;
        "Microsoft.TestPlatform" = 612;
        "Microsoft.TestPlatform.Build" = 21;
        "Microsoft.TestPlatform.CLI" = 425;
        "Microsoft.TestPlatform.Extensions.TrxLogger" = 35;
        "Microsoft.TestPlatform.ObjectModel" = 238;
        "Microsoft.TestPlatform.AdapterUtilities" = 62;
        "Microsoft.TestPlatform.Portable" = 644;
        "Microsoft.TestPlatform.TestHost" = 214;
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
    foreach($unzipNugetPackageDir in $unzipNugetPackageDirs)
    {
        $actualNumOfFiles = (Get-ChildItem -Recurse -File -Path $unzipNugetPackageDir).Count
        $versionLen = $version.Length + 1  # +1 for dot
        $packageKey = (Get-Item $unzipNugetPackageDir).BaseName -replace ".{$versionLen}$"
        Write-VerboseLog "verifying package $packageKey."

        if($expectedNumOfFiles[$packageKey] -ne $actualNumOfFiles)
        {
            Write-Error "Number of files are not equal $unzipNugetPackageDir, expected: $($expectedNumOfFiles[$packageKey]) actual: $actualNumOfFiles"
        }

        # Don't remove the directories after you unpacked them
        # they are useful for reviewing what is in the package.
        # Remove-Item -Force -Recurse $unzipNugetPackageDir | Out-Null
    }

    Write-Log "Completed Verify-Nuget-Packages."
}
