$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking

function Assert-Workflow([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-WorkflowFailure([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Expected workflow failure containing '$Message'."
}

$root = Join-Path ([IO.Path]::GetTempPath()) ('vstest-insertion-workflow-' + [Guid]::NewGuid().ToString('N'))
try {
    $artifact = New-Item -ItemType Directory -Path (Join-Path $root 'VSSetup\Insertion')
    $work = Join-Path $root 'Work'
    $component = 'Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI'
    $componentPath = '/.corext/Configs/components.json'
    $appPath = '/src/vset/Agile/TestPlatform/RocksteadyCLI/App.config'
    $revisionPath = '/src/SetupPackages/TestTools/TestPlatform/V1/CLI/core/revision.txt'
    $dropTaskName = 'Drop signed artifacts to the internal validation namespace'
    $protectedDropTaskName = [char]::ConvertFromUtf32(0x1F512) + ' ' + $dropTaskName
    $build = @{
        id = 1234; buildNumber = '20260922.1'; status = 'completed'; result = 'succeeded'; reason = 'manual'
        sourceVersion = 'a' * 40; sourceBranch = 'refs/heads/internal/rel/15.9'
        definition = @{ id = 1222 }; repository = @{ id = 'ef257391-4d76-4240-8649-3f21ac49c392' }
    }
    $drop = 'https://vsdrop.corp.microsoft.com/file/v1/Products/internal/microsoft-vstest/realsigned-DO-NOT-SHIP-15.9/20260922.1'
    $payloadPath = Join-Path $artifact 'TestPlatform.vsix'
    [IO.File]::WriteAllText($payloadPath, 'synthetic fixture bytes')
    $manifest = @{
        manifestVersion = '1.1'; info = @{ manifestName = $component; buildVersion = '15.9.3.2026092201' }
        signers = @(@{ '$id' = '1' })
        packages = @(@{
            id = $component; version = '15.9.3.2026092201'; type = 'Vsix'
            payloads = @(@{
                fileName = 'TestPlatform.vsix'; sha256 = (Get-FileHash $payloadPath).Hash
                size = (Get-Item $payloadPath).Length; url = "$drop;/TestPlatform.vsix"; signer = @{ '$ref' = '1' }
            })
        })
    }
    $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $artifact "$component.vsman")
    $baseFiles = @{
        $componentPath = '{"Components":{"Microsoft.VisualStudio.TestTools.TestPlatform.V2.CLI":{"fileName":"old.vsman","url":"https://old/"}}}'
        $appPath = [Text.Encoding]::UTF8.GetBytes('<configuration><runtime /></configuration>')
        $revisionPath = "180`n"
    }
    $state = @{
        Build = $build; SourceHead = 'a' * 40; Target = 'b' * 40; Commit = 'c' * 40
        SourceConfig = [byte[]](@(0xEF, 0xBB, 0xBF) + [Text.Encoding]::UTF8.GetBytes("<?xml version=`"1.0`" encoding=`"utf-8`"?><configuration />`n"))
        BaseFiles = $baseFiles; InsertedFiles = @{}; ChangedPaths = @(); Blobs = @{}
        Writes = 0; Pushes = 0; CreatedPrs = 0; BranchExists = $false; Pr = $null
        TargetReads = 0; ChangeTargetAtRead = 0; FailPrCreation = $false; ExtraCandidate = $false
        FailDownload = $false; FailTask = $false; InsertionHead = 'c' * 40
        AdvanceSourceDuringDiff = $false; AdvanceTargetDuringDiff = $false; RemoveHoldDuringDiff = $false
        EnableAutoCompleteDuringDiff = $false; CorruptBlobResponse = $false
        LastDiffSource = $null; LastDiffTarget = $null
        CandidateReads = 0; ExperimentCandidateAtRead = 0
        ClosedPr = $null; BranchReads = 0; ChangeBranchAtRead = 0
        Drop = $drop; DropResult = 'succeeded'; OmitDropTask = $false
        DropTaskNames = @($protectedDropTaskName); DropState = 'completed'; DropType = 'Task'
        RetentionLeases = @(); FailRetention = $false
        ExperimentPr = @{
            pullRequestId = 456; sourceRefName = 'refs/heads/dev/dotnet-bot/insertions/vstest15.9-experiment'
            title = "DO NOT MERGE VS Test Platform 'internal/experiments/15.9-vs-insertion/20260921.9' Insertion into rel/d15.9"
        }
    }
    $assert = Get-Command Assert-Workflow
    $blobId = Get-Command Get-InsertionBlobId
    $request = {
        param($Method, $Uri, $Body)
        $parsed = [Uri]$Uri
        $query = @{}
        foreach ($part in $parsed.Query.TrimStart('?').Split('&')) {
            $pair = $part.Split('=', 2)
            if ($pair.Count -eq 2) { $query[[Uri]::UnescapeDataString($pair[0])] = [Uri]::UnescapeDataString($pair[1]) }
        }
        if ($Method -ne 'GET') { $state.Writes++ }
        if ($parsed.AbsolutePath -like '*/git/repositories/*/refs') {
            $filter = $query.filter
            if ($filter -ceq 'heads/internal/rel/15.9') {
                return @{ value = @(@{ name = 'refs/heads/internal/rel/15.9'; objectId = $state.SourceHead }) }
            }
            if ($filter -ceq 'heads/rel/d15.9') {
                $state.TargetReads++
                $sha = if ($state.ChangeTargetAtRead -gt 0 -and $state.TargetReads -ge $state.ChangeTargetAtRead) { 'd' * 40 } else { $state.Target }
                return @{ value = @(@{ name = 'refs/heads/rel/d15.9'; objectId = $sha }) }
            }
            $state.BranchReads++
            if ($state.ChangeBranchAtRead -gt 0 -and $state.BranchReads -ge $state.ChangeBranchAtRead) {
                return @{ value = @(@{ name = "refs/$filter"; objectId = '9' * 40 }) }
            }
            $refs = if ($state.BranchExists) { @(@{ name = "refs/$filter"; objectId = $state.InsertionHead }) } else { @() }
            return @{ value = @($refs) }
        }
        if ($parsed.AbsolutePath.EndsWith('/build/builds') -and $Method -eq 'GET') {
            & $assert ($query.branchName -ceq 'refs/heads/internal/rel/15.9' -and $query.definitions -ceq '1222') 'Producer selection escaped the private servicing route.'
            return @{ value = @($state.Build) }
        }
        if ($parsed.AbsolutePath.EndsWith('/artifacts')) {
            return @{ value = @(@{ name = 'VSSetup'; resource = @{ type = 'PipelineArtifact' } }) }
        }
        if ($parsed.AbsolutePath.EndsWith('/timeline')) {
            $names = @('Unit Test', 'Integration Test', 'Require passing tests and declared skips before signing', 'Sign', 'Validate the insertion manifest')
            $records = @($names | ForEach-Object {
                @{ name = $_; type = 'Task'; state = 'completed'; result = $(if ($state.FailTask) { 'failed' } else { 'succeeded' }) }
            })
            if (-not $state.OmitDropTask) {
                $records += @($state.DropTaskNames | ForEach-Object {
                    @{ name = $_; type = $state.DropType; state = $state.DropState; result = $state.DropResult }
                })
            }
            return @{ records = $records }
        }
        if ($parsed.AbsolutePath.EndsWith('/items')) {
            if ($query.ContainsKey('scopePath')) { return @{ value = @(@{ path = $revisionPath }) } }
            $path = $query.path
            if ($parsed.AbsolutePath.Contains('/dnceng/')) {
                & $assert ($query.'versionDescriptor.version' -ceq $state.Build.sourceVersion) 'App.config source lookup is not immutable.'
                & $assert (-not $query.ContainsKey('includeContent')) 'App.config was requested as decoded JSON content.'
                $id = & $blobId $state.SourceConfig
                $state.Blobs[$id] = $state.SourceConfig
                return @{ path = $path; gitObjectType = 'blob'; objectId = $id }
            }
            $files = if ($query.'versionDescriptor.version' -cin @($state.Commit, $state.InsertionHead)) { $state.InsertedFiles } else { $state.BaseFiles }
            if (-not $files.ContainsKey($path)) { throw "TF401174: missing $path" }
            if ($path -ceq $appPath) {
                & $assert (-not $query.ContainsKey('includeContent')) 'VS App.config was requested as decoded JSON content.'
                $id = & $blobId $files[$path]
                $state.Blobs[$id] = $files[$path]
                return @{ path = $path; gitObjectType = 'blob'; objectId = $id }
            }
            return @{ path = $path; gitObjectType = 'blob'; content = $files[$path] }
        }
        if ($parsed.AbsolutePath -match '/blobs/([a-f0-9]{40})$') {
            $id = $Matches[1]
            & $assert ($query.'$format' -ceq 'octetStream' -and $query.download -ceq 'true') 'Blob request must preserve raw bytes.'
            if (-not $state.Blobs.ContainsKey($id)) { throw "Missing immutable blob: $id" }
            $bytes = $state.Blobs[$id]
            if ($state.CorruptBlobResponse) { $bytes = [byte[]](@(0x00) + $bytes) }
            return @{ Bytes = $bytes }
        }
        if ($parsed.AbsolutePath.EndsWith('/pushes')) {
            $oldHead = if ($state.BranchExists) { $state.InsertionHead } else { '0' * 40 }
            & $assert ($Body.refUpdates.Count -eq 1 -and $Body.refUpdates[0].oldObjectId -ceq $oldHead) 'Missing branch compare-and-swap guard.'
            & $assert ($Body.refUpdates[0].name -ceq 'refs/heads/dev/dotnet-bot/insertions/vstest15.9') 'Wrong insertion branch.'
            & $assert ($Body.commits.Count -eq 1 -and $Body.commits[0].parents[0] -ceq $state.Target) 'Wrong immutable push base.'
            if ($state.BranchExists -and $oldHead -cne $state.Target) {
                & $assert ($Body.commits[0].parents.Count -eq 2 -and $Body.commits[0].parents[1] -ceq $oldHead) 'Previous insertion ancestry was lost.'
            }
            else { & $assert ($Body.commits[0].parents.Count -eq 1) 'Unexpected insertion parent.' }
            $state.Pushes++
            $state.BranchExists = $true
            $state.InsertionHead = $state.Commit
            $state.InsertedFiles = $state.BaseFiles.Clone()
            $state.ChangedPaths = @()
            foreach ($change in $Body.commits[0].changes) {
                & $assert ($change.changeType -ceq 'edit') 'Unexpected change type.'
                $state.ChangedPaths += $change.item.path
                if ($change.item.path -ceq $appPath) {
                    & $assert ($change.newContent.contentType -ceq 'base64encoded') 'App.config push must preserve the BOM and exact bytes.'
                    $bytes = [Convert]::FromBase64String($change.newContent.content)
                    & $assert ((& $blobId $bytes) -ceq (& $blobId $state.SourceConfig)) 'App.config push changed the producer blob.'
                    $state.InsertedFiles[$change.item.path] = $bytes
                }
                else {
                    & $assert ($change.newContent.contentType -ceq 'rawtext') 'Unexpected text change encoding.'
                    $state.InsertedFiles[$change.item.path] = $change.newContent.content
                }
            }
            return @{ commits = @(@{ commitId = $state.Commit; parents = $Body.commits[0].parents }) }
        }
        if ($parsed.AbsolutePath.EndsWith('/pullrequests')) {
            if ($Method -ceq 'POST') {
                if ($state.FailPrCreation) { throw 'Synthetic create-PR failure after push' }
                $state.CreatedPrs++
                $state.Pr = @{
                    pullRequestId = 122 + $state.CreatedPrs; repository = @{ id = 'a290117c-5a8a-40f7-bc2c-f14dbe3acf6d' }
                    sourceRefName = $Body.sourceRefName; targetRefName = $Body.targetRefName
                    title = $Body.title; status = 'active'; autoCompleteSetBy = $null
                    lastMergeSourceCommit = @{ commitId = $state.Commit }; lastMergeTargetCommit = @{ commitId = $state.Target }
                }
                return $state.Pr
            }
            if ($query.'searchCriteria.status' -ceq 'all') {
                & $assert ($query.'searchCriteria.sourceRefName' -ceq 'refs/heads/dev/dotnet-bot/insertions/vstest15.9') 'Unscoped insertion history lookup.'
                return @{ value = @($(if ($null -ne $state.ClosedPr) { $state.ClosedPr })) }
            }
            $state.CandidateReads++
            $prs = @()
            if ($null -ne $state.Pr) { $prs += $state.Pr }
            if ($state.ExtraCandidate) { $prs += $state.Pr }
            if ($state.ExperimentCandidateAtRead -gt 0 -and $state.CandidateReads -ge $state.ExperimentCandidateAtRead) {
                $prs += $state.ExperimentPr
            }
            return @{ value = $prs }
        }
        if ($null -ne $state.Pr -and $parsed.AbsolutePath.EndsWith("/pullrequests/$($state.Pr.pullRequestId)")) { return $state.Pr }
        if ($parsed.AbsolutePath.EndsWith('/diffs/commits')) {
            $state.LastDiffSource = $query.targetVersion
            $state.LastDiffTarget = $query.baseVersion
            & $assert ($query.targetVersion -ceq $state.InsertionHead -and $query.baseVersion -ceq $state.Target) 'Diff used stale PR snapshots instead of live immutable refs.'
            if ($state.AdvanceSourceDuringDiff) { $state.InsertionHead = 'e' * 40 }
            if ($state.AdvanceTargetDuringDiff) { $state.Target = 'd' * 40 }
            if ($state.RemoveHoldDuringDiff) { $state.Pr.title = 'Hold removed concurrently' }
            if ($state.EnableAutoCompleteDuringDiff) { $state.Pr.autoCompleteSetBy = @{ id = 'unexpected' } }
            $changes = @($state.ChangedPaths | ForEach-Object { @{ changeType = 'edit'; item = @{ path = $_; gitObjectType = 'blob'; objectId = '1' * 40; originalObjectId = '2' * 40 } } })
            $ancestors = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach ($path in $state.ChangedPaths) {
                $parent = $path.Substring(0, $path.LastIndexOf('/'))
                while ($parent.Length -gt 0) {
                    if ($ancestors.Add($parent)) { $changes += @{ changeType = 'edit'; item = @{ path = $parent; gitObjectType = 'tree'; isFolder = $true; objectId = '1' * 40; originalObjectId = '2' * 40 } } }
                    $parent = $parent.Substring(0, $parent.LastIndexOf('/'))
                }
            }
            return @{ allChangesIncluded = $true; changes = $changes }
        }
        if ($Method -ceq 'GET' -and $parsed.AbsolutePath.EndsWith("/build/builds/$($state.Build.id)/leases")) {
            return @{ count = $state.RetentionLeases.Count; value = @($state.RetentionLeases) }
        }
        if ($Method -ceq 'POST' -and $parsed.AbsolutePath.EndsWith('/build/retention/leases')) {
            if ($state.FailRetention) { throw 'Synthetic retention lease failure after verified PR' }
            & $assert ($Body.Count -eq 1 -and $Body[0].runId -eq $state.Build.id -and
                $Body[0].definitionId -eq 1222 -and $Body[0].daysValid -eq 36501 -and
                $Body[0].protectPipeline -eq $false) 'Unexpected producer lease request.'
            $lease = $Body[0].Clone()
            $lease.leaseId = 100 + $state.RetentionLeases.Count
            $lease.createdOn = [DateTimeOffset]::UtcNow.ToString('o')
            $lease.validUntil = [DateTimeOffset]::UtcNow.AddDays($lease.daysValid).ToString('o')
            $state.RetentionLeases += $lease
            return @{ count = 1; value = @($lease) }
        }
        throw "Unexpected request: $Method $Uri"
    }.GetNewClosure()
    $download = {
        param($Uri, $Path)
        if ($state.FailDownload) { throw 'HTTP404: private drop missing' }
        & $assert ($Uri.StartsWith("$($state.Drop);", [StringComparison]::Ordinal)) 'Wrong drop destination.'
        $name = ($Uri -split ';')[1].TrimStart('/')
        Copy-Item -LiteralPath (Join-Path $artifact $name) -Destination $Path
    }.GetNewClosure()
    $arguments = @{ Request = $request; Download = $download; WorkDirectory = $work; ArtifactDirectory = $artifact.FullName }

    Invoke-ComponentInsertion @arguments -Phase Select
    Invoke-ComponentInsertion @arguments -Phase Validate
    Assert-Workflow ($state.Writes -eq 0) 'Read-only validation wrote to Azure DevOps.'
    Write-Host 'PASS Workflow: selection and live-drop validation are read-only.'

    $state.DropTaskNames = @($dropTaskName)
    Invoke-ComponentInsertion @arguments -Phase Select
    $state.DropTaskNames = @($protectedDropTaskName)
    foreach ($names in @(
        @($dropTaskName, $protectedDropTaskName),
        @($protectedDropTaskName, $protectedDropTaskName),
        @("unexpected $dropTaskName"),
        @("$protectedDropTaskName extra"),
        @([char]::ConvertFromUtf32(0x1F512) + $dropTaskName)
    )) {
        $state.DropTaskNames = $names
        Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Select } 'Drop signed artifacts'
    }
    $state.DropTaskNames = @($protectedDropTaskName)
    $state.DropState = 'inProgress'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Select } 'Drop signed artifacts'
    $state.DropState = 'completed'
    $state.DropType = 'Job'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Select } 'Drop signed artifacts'
    $state.DropType = 'Task'
    Assert-Workflow ($state.Writes -eq 0) 'Drop task identity validation produced writes.'
    Write-Host 'PASS Workflow: exact 1ES protected or original drop task name is required once; duplicates, lookalikes and incomplete/non-task records fail closed.'

    foreach ($result in @('skipped', 'failed', 'succeededWithIssues')) {
        $state.DropResult = $result
        Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Select } 'Drop signed artifacts'
    }
    $state.DropResult = 'succeeded'
    $state.OmitDropTask = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Select } 'Drop signed artifacts'
    $state.OmitDropTask = $false
    Assert-Workflow ($state.Writes -eq 0) 'A producer without a successful VS drop upload reached writes.'
    Write-Host 'PASS Workflow: retained artifacts without a successful VS drop upload cannot be selected.'

    $state.ChangeTargetAtRead = $state.TargetReads + 2
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'inputs changed'
    Assert-Workflow ($state.Writes -eq 0) 'Target race produced writes.'
    $state.ChangeTargetAtRead = 0
    $state.SourceHead = 'f' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'exact current component source'
    $state.SourceHead = $build.sourceVersion
    $state.FailTask = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'Producer prerequisite'
    $state.FailTask = $false
    $state.FailDownload = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'private drop missing'
    $state.FailDownload = $false
    Assert-Workflow ($state.Writes -eq 0) 'Source, prerequisite or drop failure produced writes.'
    Write-Host 'PASS Workflow: source/target race, failed gates and missing live drop fail before writes.'

    $originalRoot = $state.BaseFiles[$componentPath]
    $state.BaseFiles[$componentPath] = '{"Imports":["missing.json"],"Components":{}}'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'TF401174: missing'
    $state.BaseFiles[$componentPath] = '{"Imports":["components.json"],"Components":{}}'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'cyclic'
    $state.BaseFiles[$componentPath] = $originalRoot
    Assert-Workflow ($state.Writes -eq 0) 'Invalid imports produced writes.'
    Write-Host 'PASS Workflow: missing and cyclic imports fail closed.'

    $state.Pr = $state.ExperimentPr
    foreach ($phase in @('Validate', 'Insert')) {
        Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase $phase } 'active VSTest insertion PR uses another source branch'
    }
    Assert-Workflow ($state.Writes -eq 0) 'Existing experimental insertion was changed or duplicated.'
    $state.Pr.title = 'Renamed held trial'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'active VSTest insertion PR uses another source branch'
    Assert-Workflow ($state.Writes -eq 0) 'Renaming the experiment PR bypassed duplicate detection.'
    $state.Pr = $null
    Write-Host 'PASS Workflow: existing held experiment PR blocks servicing preflight and apply without writes.'

    $state.ExperimentCandidateAtRead = $state.CandidateReads + 2
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'inputs changed'
    Assert-Workflow ($state.Writes -eq 0) 'Experiment PR appearing at the fresh write boundary was changed or duplicated.'
    $state.ExperimentCandidateAtRead = 0
    Write-Host 'PASS Workflow: experiment PR appearing after planning blocks the fresh write boundary.'

    $state.FailPrCreation = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'create-PR failure after push'
    Assert-Workflow ($state.Pushes -eq 1 -and $state.CreatedPrs -eq 0) 'Expected exactly one orphan branch.'
    $state.FailPrCreation = $false
    $writes = $state.Writes
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'already exists without exactly one closed PR'
    Assert-Workflow ($state.Writes -eq $writes -and $state.Pushes -eq 1) 'Retry overwrote an orphan branch.'
    Write-Host 'PASS Workflow: create-PR failure cannot cause a duplicate push/PR loop.'

    # Begin a separate synthetic attempt; this only resets the fake server.
    $state.BranchExists = $false
    $state.FailRetention = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'retention lease failure after verified PR'
    Assert-Workflow ($state.CreatedPrs -eq 1) 'Retention failure must leave the verified held PR available for retry.'
    $pushes = $state.Pushes
    $state.FailRetention = $false
    $writes = $state.Writes
    Invoke-ComponentInsertion @arguments -Phase Validate
    Assert-Workflow ($state.Writes -eq $writes) 'Read-only validation attempted retention repair.'
    Invoke-ComponentInsertion @arguments -Phase Insert
    Assert-Workflow ($state.Pushes -eq $pushes -and $state.CreatedPrs -eq 1 -and
        $state.RetentionLeases.Count -eq 1) 'Retry did not repair only the missing lease on the existing PR.'
    Write-Host 'PASS Workflow: retention failure retries repair the producer lease without another VS push or PR; Validate remains read-only.'
    Assert-Workflow ($state.CreatedPrs -eq 1 -and $state.ChangedPaths.Count -eq 3) 'Insertion did not create one bounded held PR.'
    Assert-Workflow ((Get-InsertionBlobId $state.InsertedFiles[$appPath]) -ceq (Get-InsertionBlobId $state.SourceConfig) -and
        $state.InsertedFiles[$revisionPath] -ceq "181`n") 'Generated config/revision mismatch.'
    $writes = $state.Writes
    Invoke-ComponentInsertion @arguments -Phase Insert
    Assert-Workflow ($state.Writes -eq $writes -and $state.CreatedPrs -eq 1) 'Verified existing PR was not reused.'
    $state.ExtraCandidate = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'More than one'
    $state.ExtraCandidate = $false
    Assert-Workflow ($state.Writes -eq $writes) 'Duplicate PR detection produced writes.'
    Write-Host 'PASS Workflow: bounded atomic push, held structured PR and idempotent reuse.'

    $state.InsertionHead = 'e' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'PR identity'
    $state.Target = 'd' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'PR identity'
    $state.InsertionHead = $state.Commit
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'PR identity'
    $state.Target = 'b' * 40
    $state.Pr.lastMergeSourceCommit.commitId = 'e' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'PR identity'
    $state.Pr.lastMergeSourceCommit.commitId = $state.Commit
    $state.Pr.lastMergeTargetCommit.commitId = 'd' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'PR identity'
    $state.Pr.lastMergeTargetCommit.commitId = $state.Target
    Assert-Workflow ($state.Writes -eq $writes) 'Stale snapshot rejection produced writes.'
    Write-Host 'PASS Workflow: advanced live refs and stale PR source/target snapshots cannot verify reuse.'

    $state.InsertionHead = 'e' * 40
    $state.Pr.lastMergeSourceCommit.commitId = $state.InsertionHead
    $state.ChangedPaths += '/unrelated.txt'
    $state.InsertedFiles['/unrelated.txt'] = 'unrelated later commit'
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'unexpected diff'
    Assert-Workflow ($state.LastDiffSource -ceq $state.InsertionHead) 'Advanced source was not checked.'
    $state.ChangedPaths = @($state.ChangedPaths | Where-Object { $_ -cne '/unrelated.txt' })
    $state.InsertedFiles.Remove('/unrelated.txt')
    $state.InsertionHead = $state.Commit
    $state.Pr.lastMergeSourceCommit.commitId = $state.Commit
    $state.Target = 'd' * 40
    $state.Pr.lastMergeTargetCommit.commitId = $state.Target
    $state.BaseFiles[$revisionPath] = "190`n"
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'Insertion content mismatch'
    Assert-Workflow ($state.LastDiffTarget -ceq $state.Target) 'Advanced target was not used for planning/diff.'
    $state.BaseFiles[$revisionPath] = "180`n"
    $state.Target = 'b' * 40
    $state.Pr.lastMergeTargetCommit.commitId = $state.Target
    Assert-Workflow ($state.Writes -eq $writes) 'Unexpected current diff produced writes.'
    Write-Host 'PASS Workflow: current unrelated source changes and advanced target content fail bounded readback.'

    foreach ($flag in @('AdvanceSourceDuringDiff', 'AdvanceTargetDuringDiff', 'RemoveHoldDuringDiff', 'EnableAutoCompleteDuringDiff')) {
        $state[$flag] = $true
        $message = if ($flag -like 'Advance*') { 'ref changed during PR verification' } else { 'PR identity' }
        Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } $message
        $state[$flag] = $false
        $state.InsertionHead = $state.Commit
        $state.Target = 'b' * 40
        $state.Pr.title = "DO NOT MERGE VS Test Platform 'internal/rel/15.9/20260922.1' Insertion into rel/d15.9"
        $state.Pr.autoCompleteSetBy = $null
    }
    Assert-Workflow ($state.Writes -eq $writes) 'Concurrent ref/hold changes produced writes.'
    Write-Host 'PASS Workflow: final reuse recheck rejects concurrent source, target, hold and auto-complete changes.'

    $state.CorruptBlobResponse = $true
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'Raw git blob bytes do not match'
    $state.CorruptBlobResponse = $false
    $state.InsertedFiles[$appPath] = [byte[]]$state.SourceConfig[3..($state.SourceConfig.Length - 1)]
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'Insertion content mismatch'
    $state.InsertedFiles[$appPath] = $state.SourceConfig
    Invoke-ComponentInsertion @arguments -Phase Insert
    Assert-Workflow ($state.Writes -eq $writes) 'Blob/BOM validation produced writes.'
    Write-Host 'PASS Workflow: raw blob identity and BOM-only readback differences are enforced without writes.'

    $state.ClosedPr = ConvertFrom-InsertionJson ($state.Pr | ConvertTo-Json -Depth 20)
    $state.ClosedPr.status = 'completed'
    $state.Pr = $null
    $state.BaseFiles = $state.InsertedFiles.Clone()
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'target already contains'
    Assert-Workflow ($state.Writes -eq $writes) 'No-change target produced a dummy PR.'
    Write-Host 'PASS Workflow: no-change target produces no write or dummy PR.'

    $state.Target = 'd' * 40
    $state.BaseFiles['/unrelated-target.txt'] = 'VS changes since the previous insertion'
    $state.Build.id = 1235
    $state.Build.buildNumber = '20260924.1'
    $state.Build.sourceVersion = 'f' * 40
    $state.SourceHead = $state.Build.sourceVersion
    $state.Drop = $drop.Replace('20260922.1', $state.Build.buildNumber)
    $manifest.info.buildVersion = '15.9.3.2026092401'
    $manifest.packages[0].version = $manifest.info.buildVersion
    $manifest.packages[0].payloads[0].url = "$($state.Drop);/TestPlatform.vsix"
    $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $artifact "$component.vsman")
    $state.Commit = '8' * 40
    Invoke-ComponentInsertion @arguments -Phase Select
    Invoke-ComponentInsertion @arguments -Phase Validate
    Assert-Workflow ($state.Writes -eq $writes) 'Planning the next insertion wrote to VS.'

    foreach ($status in @('active', 'unknown')) {
        $state.ClosedPr.status = $status
        Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'branch history is active, unrelated or malformed'
    }
    $state.ClosedPr.status = 'completed'
    $priorHead = $state.ClosedPr.lastMergeSourceCommit.commitId
    $state.ClosedPr.lastMergeSourceCommit.commitId = '7' * 40
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'already exists without exactly one closed PR'
    $state.ClosedPr.lastMergeSourceCommit.commitId = $priorHead
    $state.ClosedPr.status = 'abandoned'
    Assert-Workflow ((Get-InsertionBranchBase $request 'refs/heads/dev/dotnet-bot/insertions/vstest15.9') -ceq $priorHead) 'Explicitly abandoned insertion cannot advance in a later cycle.'
    $state.ClosedPr.status = 'completed'
    $state.ChangeBranchAtRead = $state.BranchReads + 2
    Assert-WorkflowFailure { Invoke-ComponentInsertion @arguments -Phase Insert } 'already exists without exactly one closed PR'
    $state.ChangeBranchAtRead = 0
    Assert-Workflow ($state.Writes -eq $writes) 'Unsafe previous insertion history reached a write.'
    Write-Host 'PASS Workflow: active, malformed, stale or concurrently moved previous insertion history blocks the next cycle.'

    Invoke-ComponentInsertion @arguments -Phase Insert
    Assert-Workflow ($state.CreatedPrs -eq 2 -and $state.Pr.pullRequestId -eq 124) 'The second producer did not create exactly one new held PR.'
    Assert-Workflow ($state.ChangedPaths.Count -eq 1 -and $state.ChangedPaths[0] -ceq $componentPath) 'Unchanged App.config bumped the revision on a later insertion.'
    Assert-Workflow ($state.InsertedFiles[$revisionPath] -ceq "181`n" -and $state.InsertedFiles['/unrelated-target.txt'] -ceq 'VS changes since the previous insertion') 'Later insertion lost target content or changed the revision.'
    $writes = $state.Writes
    Invoke-ComponentInsertion @arguments -Phase Insert
    Assert-Workflow ($state.Writes -eq $writes -and $state.CreatedPrs -eq 2) 'Retry of the second producer created writes or a duplicate PR.'
    Write-Host 'PASS Workflow: successive servicing producers reuse the fixed branch append-only, preserve the current target, and retry without duplicates.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
Write-Host 'Insertion workflow self-tests passed.'
