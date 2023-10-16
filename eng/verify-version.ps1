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

# vstest.console.exe version info.
$Script:VSTestVersion = Extract-VSTestVersion -VSTestExe $VSTestExe
$Script:VersionIsPreview = $VSTestVersion -like "*preview*"
$Script:VersionIsRelease = $VSTestVersion -like "*release*"
$Script:VersionIsRTM = $VSTestVersion -match "^\d+.\d+.\d+( \(x\d+\))?$"

# Branch info.
$Script:CurrentBranch = git branch --show-current
$Script:IsReleaseBranch = $CurrentBranch -like "rel/*"
$Script:IsPreviewBranch = $CurrentBranch -like "main"

# Output useful info.
Write-Host "VSTest Version: `"$VSTestVersion`""
Write-Host "Current Branch: `"$CurrentBranch`""

# Branch checks.
if (!$IsReleaseBranch -and !$IsPreviewBranch)
{
    Write-Error "Branch `"$CurrentBranch`" is neither a `"release`" branch, nor a `"preview`" branch (main)."
    Exit 1
}
if ($IsReleaseBranch -and !($VersionIsRTM) -and !($VersionIsRelease))
{
    Write-Error "Release version `"$VSTestVersion`" should either be RTM, or contain a `"release`" suffix."
    Exit 2
}
if ($IsPreviewBranch -and !($VersionIsPreview))
{
    Write-Error "Preview version `"$VSTestVersion`" should contain a `"preview`" suffix."
    Exit 3
}

Write-Host "Branch and suffix check was successful."
Exit 0