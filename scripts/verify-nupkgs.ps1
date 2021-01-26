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
        "Microsoft.CodeCoverage" = 60;
        "Microsoft.NET.Test.Sdk" = 26;
        "Microsoft.TestPlatform" = 514;
        "Microsoft.TestPlatform.Build" = 20;
        "Microsoft.TestPlatform.CLI" = 380;
        "Microsoft.TestPlatform.Extensions.TrxLogger" = 34;
        "Microsoft.TestPlatform.ObjectModel" = 179;
        "Microsoft.TestPlatform.ManagedNames" = 47;
        "Microsoft.TestPlatform.Portable" = 569;
        "Microsoft.TestPlatform.TestHost" = 213;
        "Microsoft.TestPlatform.TranslationLayer" = 122;
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