$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$entrypoint = Join-Path $PSScriptRoot 'Invoke-Insertion.ps1'
$workDirectory = Join-Path ([IO.Path]::GetTempPath()) ('vstest-insertion-guard-' + [Guid]::NewGuid().ToString('N'))
$guardError = 'Writes require private pipeline 1595 running the merged internal/rel/15.9 insertion YAML outside a PR.'
$routeError = 'This driver only supports internal/rel/15.9 -> rel/d15.9.'
$environment = @{
    TF_BUILD = 'True'
    SYSTEM_DEFINITIONID = '1595'
    SYSTEM_TEAMPROJECTID = '7ea9116e-9fac-403d-b258-b31fcf1bb293'
    BUILD_REPOSITORY_ID = 'ef257391-4d76-4240-8649-3f21ac49c392'
    BUILD_REASON = 'Manual'
    BUILD_SOURCEBRANCH = 'refs/heads/internal/rel/15.9'
    BUILD_SOURCEVERSION = 'a' * 40
}
$cases = @(
    @{ Name = 'Canonical SYSTEM_DEFINITIONID with absent BUILD_DEFINITIONID passes identity guard'; Changes = @{}; Error = 'INSERTION_TOKEN is required.' }
    @{ Name = 'Missing SYSTEM_DEFINITIONID rejects insertion'; Changes = @{ SYSTEM_DEFINITIONID = $null }; Error = $guardError }
    @{ Name = 'Wrong SYSTEM_DEFINITIONID rejects insertion'; Changes = @{ SYSTEM_DEFINITIONID = '1222' }; Error = $guardError }
    @{ Name = 'Misleading BUILD_DEFINITIONID cannot replace missing SYSTEM_DEFINITIONID'; Changes = @{ SYSTEM_DEFINITIONID = $null; BUILD_DEFINITIONID = '1595' }; Error = $guardError }
    @{ Name = 'Misleading BUILD_DEFINITIONID cannot override wrong SYSTEM_DEFINITIONID'; Changes = @{ SYSTEM_DEFINITIONID = '1222'; BUILD_DEFINITIONID = '1595' }; Error = $guardError }
    @{ Name = 'Missing TF_BUILD rejects insertion'; Changes = @{ TF_BUILD = $null }; Error = $guardError }
    @{ Name = 'False TF_BUILD rejects insertion'; Changes = @{ TF_BUILD = 'False' }; Error = $guardError }
    @{ Name = 'Missing BUILD_SOURCEBRANCH rejects insertion'; Changes = @{ BUILD_SOURCEBRANCH = $null }; Error = $guardError }
    @{ Name = 'Experiment branch rejects servicing insertion'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/internal/experiments/15.9-vs-insertion' }; Error = $guardError }
    @{ Name = 'Public branch rejects servicing insertion'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/rel/15.9' }; Error = $guardError }
    @{ Name = 'Other servicing branch rejects insertion'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/heads/internal/rel/17.11' }; Error = $guardError }
    @{ Name = 'PR ref rejects insertion'; Changes = @{ BUILD_SOURCEBRANCH = 'refs/pull/123/merge' }; Error = $guardError }
    @{ Name = 'PR reason rejects insertion even on servicing ref'; Changes = @{ BUILD_REASON = 'PullRequest' }; Error = $guardError }
    @{ Name = 'Missing build reason rejects insertion'; Changes = @{ BUILD_REASON = $null }; Error = $guardError }
    @{ Name = 'Missing private repository identity rejects insertion'; Changes = @{ BUILD_REPOSITORY_ID = $null }; Error = $guardError }
    @{ Name = 'Wrong repository identity rejects insertion'; Changes = @{ BUILD_REPOSITORY_ID = 'a290117c-5a8a-40f7-bc2c-f14dbe3acf6d' }; Error = $guardError }
    @{ Name = 'Missing private project identity rejects insertion'; Changes = @{ SYSTEM_TEAMPROJECTID = $null }; Error = $guardError }
    @{ Name = 'Wrong project identity rejects insertion'; Changes = @{ SYSTEM_TEAMPROJECTID = 'public' }; Error = $guardError }
    @{ Name = 'Missing immutable source rejects insertion'; Changes = @{ BUILD_SOURCEVERSION = $null }; Error = $guardError }
    @{ Name = 'Abbreviated source rejects insertion'; Changes = @{ BUILD_SOURCEVERSION = '4878a2ea' }; Error = $guardError }
    @{ Name = 'Experiment route parameter rejects insertion'; Changes = @{}; Arguments = @('-ComponentBranchName', 'internal/experiments/15.9-vs-insertion'); Error = $routeError }
    @{ Name = 'Public route parameter rejects insertion'; Changes = @{}; Arguments = @('-ComponentBranchName', 'rel/15.9'); Error = $routeError }
    @{ Name = 'Other VS target parameter rejects insertion'; Changes = @{}; Arguments = @('-VisualStudioBranchName', 'main'); Error = $routeError }
)
foreach ($case in $cases) {
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $PSHOME 'pwsh.exe'))
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $entrypoint, '-Phase', 'Insert', '-WorkDirectory', $workDirectory)) {
        $start.ArgumentList.Add($argument)
    }
    if ($case.ContainsKey('Arguments')) {
        foreach ($argument in $case.Arguments) { $start.ArgumentList.Add($argument) }
    }
    # Run the production entrypoint, but stop at its missing-token check before
    # creating any API delegate. Inherited CI credentials must never reach a test.
    foreach ($name in @('INSERTION_TOKEN', 'BUILD_DEFINITIONID') + @($environment.Keys)) {
        $null = $start.Environment.Remove($name)
    }
    foreach ($name in $environment.Keys) { $start.Environment[$name] = $environment[$name] }
    foreach ($name in $case.Changes.Keys) {
        if ($null -eq $case.Changes[$name]) { $null = $start.Environment.Remove($name) }
        else { $start.Environment[$name] = $case.Changes[$name] }
    }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw "Could not start entrypoint regression: $($case.Name)" }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) {
            $process.Kill($true)
            throw "Entrypoint regression timed out: $($case.Name)"
        }
        $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -eq 0 -or -not $text.Contains($case.Error, [StringComparison]::Ordinal)) {
            throw "Unexpected entrypoint result for $($case.Name): $text"
        }
        if ($case.Error -cne $guardError -and $text.Contains($guardError, [StringComparison]::Ordinal)) {
            throw 'Canonical Azure Pipelines environment did not pass the production identity guard.'
        }
        if (Test-Path -LiteralPath $workDirectory) { throw 'Entrypoint guard regression reached the insertion workflow.' }
        Write-Host "PASS Entrypoint: $($case.Name) (no token, no API writes)."
    }
    finally { $process.Dispose() }
}
Write-Host "Insertion entrypoint self-tests passed: $($cases.Count)."
