$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Insertion.psm1') -Force -DisableNameChecking

function Assert-Retention([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-RetentionFailure([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Expected retention failure containing '$Message'."
}
$build = @{
    id = 1234; buildNumber = '20260925.5'
    status = 'completed'; result = 'succeeded'; reason = 'manual'
    sourceVersion = 'a' * 40; sourceBranch = 'refs/heads/internal/rel/15.9'
    definition = @{ id = 1222 }; repository = @{ id = 'ef257391-4d76-4240-8649-3f21ac49c392' }
}
$owner = 'vstest-insertion:a290117c-5a8a-40f7-bc2c-f14dbe3acf6d/123'
$state = @{
    Leases = @(); Gets = 0; Posts = 0; Posted = $null
    CountDelta = 0; HideCreated = $false; FailGet = $false; FailPost = $false
}
$assert = Get-Command Assert-Retention
$request = {
    param($Method, $Uri, $Body)
    if ($Method -ceq 'GET' -and $Uri -ceq 'https://dev.azure.com/dnceng/internal/_apis/build/builds/1234/leases?api-version=7.1') {
        $state.Gets++
        if ($state.FailGet) { throw 'Synthetic GET authorization failure' }
        & $assert ($null -eq $Body) 'Lease GET must not carry a body.'
        return @{ count = $state.Leases.Count + $state.CountDelta; value = @($state.Leases) }
    }
    if ($Method -ceq 'POST' -and $Uri -ceq 'https://dev.azure.com/dnceng/internal/_apis/build/retention/leases?api-version=7.1') {
        $state.Posts++
        if ($state.FailPost) { throw 'Synthetic POST authorization failure' }
        & $assert ($Body -is [Collections.IList] -and $Body.Count -eq 1 -and $Body[0].Count -eq 5) 'Lease creation must send exactly one documented request object.'
        $state.Posted = $Body[0].Clone()
        & $assert ($Body[0].runId -eq 1234 -and $Body[0].definitionId -eq 1222 -and
            $Body[0].daysValid -eq 36501 -and $Body[0].ownerId -ceq $owner -and
            $Body[0].protectPipeline -eq $false) 'Lease creation changed identity or protected the pipeline itself.'
        $lease = @{
            leaseId = 456; runId = 1234; definitionId = 1222; ownerId = $owner
            protectPipeline = $false; createdOn = [DateTimeOffset]::UtcNow.ToString('o')
            validUntil = [DateTimeOffset]::UtcNow.AddDays(36501).ToString('o')
        }
        if (-not $state.HideCreated) { $state.Leases += $lease }
        return @{ count = 1; value = @($lease) }
    }
    throw "Unexpected retention request: $Method $Uri"
}.GetNewClosure()

$lease = Set-InsertionProducerRetention $request $build 123
Assert-Retention ($lease.leaseId -eq 456 -and $state.Gets -eq 2 -and $state.Posts -eq 1) 'Lease was not created and independently read back.'
$null = Set-InsertionProducerRetention $request $build 123
Assert-Retention ($state.Posts -eq 1) 'Retry created another retention lease.'
$original = $state.Leases[0].Clone()
$state.Leases[0].createdOn = [DateTimeOffset]::UtcNow.AddYears(-2).ToString('o')
$state.Leases[0].validUntil = [DateTimeOffset]::UtcNow.AddYears(-2).AddDays(36501).ToString('o')
$null = Set-InsertionProducerRetention $request $build 123
Assert-Retention ($state.Posts -eq 1) 'An older valid forever lease was unnecessarily recreated.'
$state.Leases = @($original.Clone())
Write-Host 'PASS Retention: documented PR-specific forever lease is read back and reused without extending pipeline protection.'

foreach ($pair in @(
    @('runId', 999), @('definitionId', 1595), @('leaseId', 0),
    @('protectPipeline', $true), @('protectPipeline', 'false'),
    @('createdOn', 'invalid'), @('validUntil', 'invalid'),
    @('validUntil', [DateTimeOffset]::UtcNow.AddDays(30).ToString('o'))
)) {
    $state.Leases = @($original.Clone())
    $state.Leases[0][$pair[0]] = $pair[1]
    Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'lease was not verified'
}
$state.Leases = @($original.Clone(), $original.Clone())
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'duplicate insertion retention leases'
$state.Leases = @($original.Clone())
$state.CountDelta = 1
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'incomplete or malformed'
$state.CountDelta = 0
Assert-Retention ($state.Posts -eq 1) 'Malformed existing retention triggered a replacement write.'
Write-Host 'PASS Retention: wrong identities, ambiguous leases, insufficient duration and malformed readback fail closed.'

$state.Leases = @()
$state.HideCreated = $true
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'lease was not verified'
$state.HideCreated = $false
$posts = $state.Posts
$state.FailGet = $true
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'GET authorization failure'
$state.FailGet = $false
Assert-Retention ($state.Posts -eq $posts) 'Failed lease inspection reached a write.'
$state.FailPost = $true
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 123 } 'POST authorization failure'
$state.FailPost = $false
Assert-Retention ($state.Posts -eq ($posts + 1)) 'Failed lease creation was retried automatically.'
Write-Host 'PASS Retention: missing readback and transport/authorization failures are surfaced without fallback or repeated writes.'

$gets = $state.Gets
Assert-RetentionFailure { Set-InsertionProducerRetention $request $build 0 } 'verified VS PR ID'
$bad = $build.Clone()
$bad.sourceBranch = 'refs/heads/internal/experiments/15.9-vs-insertion'
Assert-RetentionFailure { Set-InsertionProducerRetention $request $bad 123 } 'exact current component source'
$bad = $build.Clone()
$bad.definition = @{ id = 1595 }
Assert-RetentionFailure { Set-InsertionProducerRetention $request $bad 123 } 'exact current component source'
Assert-Retention ($state.Gets -eq $gets) 'Invalid producer or PR reached retention transport.'
Write-Host 'PASS Retention: unverified producer, experimental source and invalid PR IDs reject before transport.'

$module = Get-Module Insertion
foreach ($body in @(@{ runId = 1234 }, ,@(@{ runId = 1234 }))) {
    $serialized = & $module {
        param($Value)
        function script:Invoke-WebRequest {
            param($Method, $Uri, $Headers, $Body, $ContentType, $ErrorAction)
            return @{ Content = '{"request":' + [Text.Encoding]::UTF8.GetString($Body) + '}' }
        }
        try {
            return Invoke-InsertionRequest 'POST' 'https://dev.azure.com/dnceng/internal/_apis/build/retention/leases?api-version=7.1' @{} $Value
        }
        finally { Remove-Item Function:\Invoke-WebRequest }
    } $body
    if ($body -is [Collections.IList]) {
        Assert-Retention ($serialized.request -is [Collections.IList] -and
            $serialized.request.Count -eq 1 -and $serialized.request[0].runId -eq 1234) 'HTTP serialization unwrapped the one-element lease array.'
    }
    else {
        Assert-Retention ($serialized.request -is [Collections.IDictionary] -and
            $serialized.request.runId -eq 1234) 'HTTP serialization changed an existing object request.'
    }
}
Write-Host 'PASS Retention: actual HTTP request serialization preserves single-lease arrays and existing object bodies.'
Write-Host 'Insertion retention self-tests passed: 5.'
