[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Select', 'Validate', 'Insert')][string] $Phase,
    [Parameter(Mandatory)][string] $WorkDirectory,
    [string] $ArtifactDirectory,
    [string] $ComponentBranchName = 'internal/rel/15.9',
    [string] $VisualStudioBranchName = 'rel/d15.9',
    [string] $OptionalTitlePrefix = 'DO NOT MERGE',
    [ValidateSet('false', 'true')][string] $CreateDraftPR = 'false'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking
if ($ComponentBranchName -cne 'internal/rel/15.9' -or $VisualStudioBranchName -cne 'rel/d15.9') {
    throw 'This driver only supports internal/rel/15.9 -> rel/d15.9.'
}
if ($Phase -ceq 'Insert' -and ($env:TF_BUILD -ine 'true' -or $env:SYSTEM_DEFINITIONID -cne '1595' -or
    $env:SYSTEM_TEAMPROJECTID -cne '7ea9116e-9fac-403d-b258-b31fcf1bb293' -or
    $env:BUILD_REPOSITORY_ID -cne 'ef257391-4d76-4240-8649-3f21ac49c392' -or
    [string]::IsNullOrWhiteSpace($env:BUILD_REASON) -or $env:BUILD_REASON -ieq 'PullRequest' -or
    $env:BUILD_SOURCEVERSION -cnotmatch '^[a-f0-9]{40}$' -or
    $env:BUILD_SOURCEBRANCH -cne 'refs/heads/internal/rel/15.9')) {
    throw 'Writes require private pipeline 1595 running the merged internal/rel/15.9 insertion YAML outside a PR.'
}
if ([string]::IsNullOrWhiteSpace($env:INSERTION_TOKEN)) { throw 'INSERTION_TOKEN is required.' }
$headers = @{ Authorization = "Bearer $env:INSERTION_TOKEN" }
$invokeRequest = Get-Command Invoke-InsertionRequest
$request = {
    param($Method, $Uri, $Body)
    & $invokeRequest $Method $Uri $headers $Body
}.GetNewClosure()
$download = $null
if ($Phase -cne 'Select') {
    $producer = Get-InsertionProducer $request
    if ($Phase -ceq 'Insert' -and $env:BUILD_SOURCEVERSION -cne $producer.sourceVersion) {
        throw 'Insertion YAML and selected producer must come from the same current merged servicing source commit.'
    }
    $download = New-InsertionDropDownloader $producer.buildNumber $headers
}
Invoke-ComponentInsertion -Request $request -Download $download -Phase $Phase `
    -WorkDirectory $WorkDirectory -ArtifactDirectory $ArtifactDirectory `
    -TitlePrefix $OptionalTitlePrefix -CreateDraftPR ($CreateDraftPR -ceq 'true')
