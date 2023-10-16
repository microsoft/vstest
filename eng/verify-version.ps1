[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [string] $VSTestExe
)

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

$Script:VSTestProductVersion = (Get-Item $VSTestExe).VersionInfo.ProductVersion
$Script:CurrentBranch = git branch --show-current

Match-VersionAgainstBranch -VSTestVersion $VSTestProductVersion -BranchName $CurrentBranch
