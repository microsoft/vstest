$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-Producer([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
$yaml = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\..\azure-pipelines-official.yml')).Replace("`r`n", "`n")
foreach ($pair in @(@('runPackagingAndSigning', 'true'), @('uploadVsDrop', 'true'), @('publishArtifacts', 'false'))) {
    Assert-Producer ($yaml.Contains("- name: $($pair[0])`n  type: boolean`n  default: $($pair[1])`n")) "Unexpected producer default: $($pair[0])"
}
Assert-Producer ($yaml.Contains('enablePublishBuildAssets: false') -and $yaml.Contains('enablePublishUsingPipelines: false')) 'BAR/Darc must remain disabled.'
Write-Host 'PASS Producer: signed servicing VS drop is enabled by default independently of disabled feed/BAR publishing.'

$uploadCondition = '${{ if and(eq(parameters.uploadVsDrop, true), eq(parameters.runPackagingAndSigning, true), ne(variables[''Build.Reason''], ''PullRequest''), eq(variables[''Build.SourceBranch''], ''refs/heads/internal/rel/15.9'')) }}'
$feedCondition = '${{ if and(eq(parameters.publishArtifacts, true), ne(variables[''Build.Reason''], ''PullRequest'')) }}'
$blocks = @([regex]::Matches($yaml, '(?ms)^            - (?<condition>\$\{\{ if .*?\}\}):\n(?<body>.*?)(?=^            - |\z)'))
$upload = @($blocks | Where-Object { $_.Groups['condition'].Value -ceq $uploadCondition })
$feed = @($blocks | Where-Object { $_.Groups['condition'].Value -ceq $feedCondition })
Assert-Producer ($upload.Count -eq 1 -and $feed.Count -eq 1) 'Expected separate VS drop and feed conditions at job-step scope.'
$body = $upload[0].Groups['body'].Value
Assert-Producer ($body.Contains('Copy-InsertionDrop') -and $body.Contains('task: 1ES.MicroBuildVstsDrop@1') -and
    -not $body.Contains('1ES.PublishNuget') -and -not $body.Contains('parameters.publishArtifacts')) 'VS drop upload is coupled to feed publishing.'
Assert-Producer ($feed[0].Groups['body'].Value.Contains('task: 1ES.PublishNuget@1') -and
    -not $feed[0].Groups['body'].Value.Contains('MicroBuildVstsDrop') -and
    -not $feed[0].Groups['body'].Value.Contains('parameters.uploadVsDrop')) 'Feed opt-in is coupled to VS drop upload.'
Assert-Producer (@([regex]::Matches($body, '(?m)^                condition: ')).Count -eq 4) 'Unexpected number of VS drop tasks.'
foreach ($condition in [regex]::Matches($body, '(?m)^                condition: (?<value>.*)$')) {
    Assert-Producer ($condition.Groups['value'].Value.StartsWith("and(succeeded(), eq(variables['Agent.JobStatus'], 'Succeeded'), eq(variables['ArtifactsSigned'], 'true')", [StringComparison]::Ordinal)) 'VS drop task lost success/signing prerequisites.'
}
Assert-Producer ($body.IndexOf('Mint DevDiv drop access token', [StringComparison]::Ordinal) -lt
    $body.IndexOf('Stage the drop folder', [StringComparison]::Ordinal)) 'VS drop credential acquisition must precede staging/upload.'
Assert-Producer ($body.Contains("azureSubscription: 'dnceng-devdiv-drop-rw-code-rw-wif'") -and
    $body.Contains('dropName: $(VisualStudioDropName)')) 'Existing VS drop identity or destination changed.'
Write-Host 'PASS Producer: only signed non-PR servicing builds enter the independent VS drop block; every task retains its success/signing gate.'

$match = [regex]::Match($body, '(?ms)^                  inlineScript: \|\n(?<script>.*?)(?=^                condition:)')
Assert-Producer $match.Success 'Could not find the production VS drop identity script.'
$script = [scriptblock]::Create(($match.Groups['script'].Value -replace '(?m)^ {20}', ''))
$environment = @{
    TF_BUILD = 'True'; SYSTEM_DEFINITIONID = '1222'
    SYSTEM_TEAMPROJECTID = '7ea9116e-9fac-403d-b258-b31fcf1bb293'
    BUILD_REPOSITORY_ID = 'ef257391-4d76-4240-8649-3f21ac49c392'
    BUILD_SOURCEBRANCH = 'refs/heads/internal/rel/15.9'
    BUILD_SOURCEVERSION = 'a' * 40; BUILD_REASON = 'IndividualCI'
}
$cases = @(
    @{ Name = 'servicing producer'; Changes = @{}; Expected = 'Mock token boundary reached.' }
    @{ Name = 'non-pipeline process'; Changes = @{ TF_BUILD = 'False' } }
    @{ Name = 'wrong pipeline'; Changes = @{ SYSTEM_DEFINITIONID = '1595' } }
    @{ Name = 'wrong project'; Changes = @{ SYSTEM_TEAMPROJECTID = 'public' } }
    @{ Name = 'wrong repository'; Changes = @{ BUILD_REPOSITORY_ID = 'public' } }
    @{ Name = 'experiment source'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/internal/experiments/15.9-vs-insertion' } }
    @{ Name = 'public source'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/rel/15.9' } }
    @{ Name = 'other servicing source'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/internal/rel/17.11' } }
    @{ Name = 'PR ref'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/pull/1/merge' } }
    @{ Name = 'PR reason'; Changes = @{ BUILD_REASON = 'PullRequest' } }
    @{ Name = 'missing reason'; Changes = @{ BUILD_REASON = $null } }
    @{ Name = 'missing immutable source'; Changes = @{ BUILD_SOURCEVERSION = $null } }
)
$saved = @{}
foreach ($name in $environment.Keys) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    foreach ($case in $cases) {
        foreach ($name in $environment.Keys) { [Environment]::SetEnvironmentVariable($name, $environment[$name]) }
        foreach ($name in $case.Changes.Keys) { [Environment]::SetEnvironmentVariable($name, $case.Changes[$name]) }
        $expected = if ($case.ContainsKey('Expected')) { $case.Expected } else { 'VS drop upload requires the private official 15.9 servicing producer outside a PR.' }
        $message = ''
        try {
            & {
                function az { throw 'Mock token boundary reached.' }
                & $script
            }
        }
        catch { $message = $_.Exception.Message }
        Assert-Producer ($message -ceq $expected) "Unexpected production guard result for $($case.Name): $message"
    }
}
finally {
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
}
Write-Host "PASS Producer: $($cases.Count) production identity cases execute with a mocked token boundary and no network."
$insertion = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\..\azure-pipelines-insertion.yml')).Replace("`r`n", "`n")
$artifact = [regex]::Match($insertion, '(?m)^\s+artifactName: (?<name>InsertionEvidence[^\r\n]*)$')
Assert-Producer ($artifact.Success -and $artifact.Groups['name'].Value -ceq
    'InsertionEvidence_$(System.StageAttempt)_$(System.JobAttempt)') 'Insertion evidence must distinguish stage and job retries.'
$names = foreach ($attempt in @(@('1', '1'), @('1', '2'), @('2', '1'))) {
    $artifact.Groups['name'].Value.Replace('$(System.StageAttempt)', $attempt[0]).Replace('$(System.JobAttempt)', $attempt[1])
}
Assert-Producer (@($names | Sort-Object -Unique).Count -eq 3) 'Insertion retries collide with existing evidence artifacts.'
Write-Host 'PASS Producer: initial insertion, job retry and stage retry retain distinct evidence artifacts.'
Write-Host 'Insertion producer self-tests passed: 4.'
