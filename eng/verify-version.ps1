[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [string] $NugetDir
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$Script:RootDir = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName

function Find-NugetPackage {
    param (
        [string]$NugetDir
    )

    Write-Host "Searching in nuget dir: $NugetDir"
    $nuget = Get-ChildItem -Path $NugetDir |
        Where-Object { $_.Name -match "Microsoft.TestPlatform\.\d+\.\d+\.\d+.*.nupkg" } |
        Where-Object { $_.Name -notMatch ".*symbols.*" }

    Write-Host "Found nuget: $nuget"

    return $nuget
}

function Create-TemporaryDirectory {
    param (
        [string]$TmpDir
    )

    if (!(Test-Path $tmpDir)) {
        Write-Host "Creating temporary directory ..."
        New-Item -ItemType Directory -Force -Path $tmpDir
    }
}

function Delete-TemporaryDirectory {
    param (
        [string]$TmpDir
    )

    if ($null -ne $TmpDir -and (Test-Path $TmpDir)) {
        Remove-Item -Force -Recurse $TmpDir | Out-Null
    }
}

function Unzip-NugetPackage {
    param (
        [string]$NugetPackage,
        [string]$TmpDir
    )

    Write-Host "Unzipping file `"$NugetPackage`" to path `"$TmpDir`""
    [System.IO.Compression.ZipFile]::ExtractToDirectory($NugetPackage, $TmpDir)
}

function Match-VersionAgainstBranch {
    param (
        [string]$VSTestVersion,
        [string]$BranchName
    )

    # Output useful info.
    Write-Host "VSTest Product Version: `"$VSTestVersion`""
    Write-Host "Current Branch: `"$BranchName`""

    $versionIsRTM = $VSTestVersion -match "^\d+\.\d+\.\d+$"
    $versionIsRelease = $VSTestVersion -match "^\d+\.\d+\.\d+\-release\-\d{8}\-\d{2}$"
    $versionIsPreview = $VSTestVersion -match "^\d+\.\d+\.\d+\-preview\-\d{8}\-\d{2}$"

    $isReleaseBranch = $BranchName -like "rel/*"
    $isPreviewBranch = $BranchName -like "main"

    if (!$isReleaseBranch -and !$isPreviewBranch)
    {
        Write-Host "Skipping check since branch is neither `"release`" nor `"preview`""
        return
    }

    Write-Host "Matching branch against product version ... "
    if ($isReleaseBranch -and !$versionIsRTM -and !$versionIsRelease)
    {
        Write-Error "Release version `"$VSTestVersion`" should either be RTM, or contain a `"release`" suffix."
        Exit 1
    }
    if ($isPreviewBranch -and !$versionIsPreview)
    {
        Write-Error "Preview version `"$VSTestVersion`" should contain a `"preview`" suffix."
        Exit 2
    }

    Write-Host "Branch and version check was successful."
    Exit 0
}

function Verify-Version {
    param (
        [string]$NugetDir
    )

    $tmpDir = Join-Path $RootDir "tmp"
    try {
        $nuget = Find-NugetPackage -NugetDir $NugetDir
        $nugetFullPath = Join-Path $NugetDir $nuget

        Create-TemporaryDirectory -TmpDir $tmpDir
        Unzip-NugetPackage -NugetPackage $nugetFullPath -TmpDir $tmpDir

        $vsTestExe = [IO.Path]::Combine($tmpDir, 'tools', 'net462', 'Common7', 'IDE', 'Extensions', 'TestPlatform', 'vstest.console.exe')
        $vsTestProductVersion = (Get-Item $vsTestExe).VersionInfo.ProductVersion
        $currentBranch = git branch --show-current

        Match-VersionAgainstBranch -VSTestVersion $vsTestProductVersion -BranchName $currentBranch
    }
    finally {
        Delete-TemporaryDirectory -TmpDir $tmpDir
    }
}

Verify-Version -NugetDir $NugetDir
