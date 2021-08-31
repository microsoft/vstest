[CmdletBinding()]
param
( 
   [string] $Path = ".",
   # if this is a pre-release or stable version
   [switch] $Stable,
   # externally provide the version number we will use
   [string] $PackageVersion,
   # in CI we don't know the end tag, so we diff till the current commit
   [switch] $EndWithLatestCommit
)

if ($EndWithLatestCommit -and [string]::IsNullOrWhiteSpace($PackageVersion)) { 
    throw "EndWithLatestCommit was enabled, provide PackageVersion in this format 16.8.0-preview-20200924-01, or this format 16.8.0."
}

$repoUrl = $(if ((git -C $Path remote -v) -match "upstream") {
        git -C $Path remote get-url --push upstream
    }
    else {  
        git -C $Path remote get-url --push origin
    })-replace "\.git$" 

# list all tags on this branch ordered by creator date to get the latest, stable or pre-release tag. 
# For stable release we choose only tags without any dash, for pre-release we choose all tags.
$tags = git -C $Path tag -l --sort=creatordate | Where-Object { $_ -match "v\d+\.\d+\.\d+.*" -and (-not $Stable -or $_ -notlike '*-*') }

if ($EndWithLatestCommit) { 
    # in CI we don't have the tag yet, so we show changes between the most recent tag, and this commit
    # we figure out the tag from the package version that is set by vsts-prebuild
    $start = $tags | Select-Object -Last 1
    $end = git -C $Path rev-parse HEAD
    $tag = "v$PackageVersion"
}
else { 
    # normally we show changes between the latest two tags
    $start, $end = $tags | Select-Object -Last 2
    $tag = $end
}

# # override the tags to use if you need
# $start = "v16.8.0-preview-20200812-03"
# $end = $tag = "v16.8.0-preview-20200921-01"


Write-Host "Generating release notes for $start..$end$(if ($EndWithLatestCommit) { " (expected tag: $tag)" })"

$sourceBranch = $branch = git -C $Path rev-parse --abbrev-ref HEAD
if ($sourceBranch -eq "HEAD") { 
    # when CI checks out just the single commit, https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
    $sourceBranch = $env:BUILD_SOURCEBRANCH -replace "^refs/heads/"  
}

if ([string]::IsNullOrWhiteSpace($branch)) { 
    throw "Branch is null or empty!"
}

if ([string]::IsNullOrWhiteSpace($sourceBranch)) { 
    throw "SourceBranch is null or empty!"
}

Write-Host "Branch is $branch"
Write-Host "SourceBranch is $sourceBranch"
$branchesWithStartTag = git -C $Path branch --contains tags/$start

if (-not $branchesWithStartTag -or -not ($branchesWithStartTag -match $branch)) { 
    Write-Host "This branch $branch$(if($branch -ne $sourceBranch){" ($sourceBranch)"}), does not contain the starting tag $start. Skipping generating release notes."
    if ($branchesWithStartTag) {
        Write-Host "The tag is present on branches:`n$($branchesWithStartTag)."
    }
    return 
}
else { 
    Write-Host "Branch $branch$(if($branch -ne $sourceBranch){" ($sourceBranch)"}) has tag $start, getting log since that."
}

$prUrl = "$repoUrl/pull/"
$v = $tag -replace '^v'
$b = if ($Stable) { $v } else { $tag -replace '.*?(\d+-\d+)$', '$1' }
# using .. because I want to know the changes that are on this branch, but don't care about the changes that I don't have https://stackoverflow.com/a/24186641/3065397
$log = (git -C $Path log "$start..$end" --oneline --pretty="format:%s" --first-parent)
$issues = $log | ForEach-Object {
    if ($_ -match '^(?<message>.+)\s\(#(?<pr>\d+)\)?$') {
        $message = "* $($matches.message)"
        if ($matches.pr) {
            $pr = $matches.pr
            $message += " [#$pr]($prUrl$pr)"
        }

        $message
    }
    else
    {
        "* $_"
    }
}

$output = @"

See the release notes [here](https://github.com/microsoft/vstest-docs/blob/main/docs/releases.md#$($v -replace '\.')).

-------------------------------

## $v

### $(if ($issues.Length -eq 1) { 'Issue' } else { 'Issues' }) Fixed
$($issues -join "`n")

See full log [here]($repoUrl/compare/$start...$tag)

### Drops

* TestPlatform vsix: [$v](https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/microsoft/vstest/$sourceBranch/$b;/TestPlatform.vsix)
* Microsoft.TestPlatform.ObjectModel : [$v](https://www.nuget.org/packages/Microsoft.TestPlatform.ObjectModel/$v)
"@


$output
$output | clip
