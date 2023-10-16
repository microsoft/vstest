[CmdletBinding()]
Param(
    [Parameter(Mandatory)]
    [string] $VSTestExe
)

$Script:RootDir = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName

function Extract-VSTestVersion {
    param (
        [string]$VSTestExe
    )

    $outputFile = Join-Path $RootDir "output.txt"

    Invoke-Expression $VSTestExe > $outputFile 2>&1
    $firstLine = Get-Content -Path $outputFile -TotalCount 1
    Remove-Item $outputFile

    $version = ""
    $matchFound = $firstLine -match "(?<=Version\s+).*"
    if ($matchFound) {
        $version = $matches[0]
    }

    return $version
}

function Match-VersionAgainstBranch {
    param (
        [string]$VSTestVersion,
        [string]$BranchName
    )

    $versionIsRTM = $VSTestVersion -match "^\d+\.\d+\.\d+( \(x\d+\))?$"
    $versionIsRelease = $VSTestVersion -match "^\d+\.\d+\.\d+\-release\-\d{8}\-\d{2}( \(x\d+\))?$"
    $versionIsPreview = $VSTestVersion -match "^\d+\.\d+\.\d+\-preview\-\d{8}\-\d{2}( \(x\d+\))?$"

    $isReleaseBranch = $BranchName -like "rel/*"
    $isPreviewBranch = $BranchName -like "main"

    if (!$isReleaseBranch -and !$isPreviewBranch)
    {
        Write-Error "Branch `"$CurrentBranch`" is neither a `"release`" branch, nor a `"preview`" branch (main)."
        Exit 1
    }
    if ($isReleaseBranch -and !$versionIsRTM -and !$versionIsRelease)
    {
        Write-Error "Release version `"$VSTestVersion`" should either be RTM, or contain a `"release`" suffix."
        Exit 2
    }
    if ($isPreviewBranch -and !$versionIsPreview)
    {
        Write-Error "Preview version `"$VSTestVersion`" should contain a `"preview`" suffix."
        Exit 3
    }
}

$Script:VSTestOutputVersion = Extract-VSTestVersion -VSTestExe $VSTestExe
$Script:VSTestProductVersion = (Get-Item $VSTestExe).VersionInfo.ProductVersion
$Script:CurrentBranch = git branch --show-current

# Output useful info.
Write-Host "VSTest Output Version: `"$VSTestOutputVersion`""
Write-Host "VSTest Product Version: `"$VSTestProductVersion`""
Write-Host "Current Branch: `"$CurrentBranch`""

Write-Host "Matching branch against output version ... "
Match-VersionAgainstBranch -VSTestVersion $VSTestOutputVersion -BranchName $CurrentBranch

Write-Host "Matching branch against product version ... "
Match-VersionAgainstBranch -VSTestVersion $VSTestProductVersion -BranchName $CurrentBranch

Write-Host "Branch and version check was successful."
Exit 0
