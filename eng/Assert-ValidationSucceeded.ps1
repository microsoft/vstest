[CmdletBinding()]
param(
    [string] $JobStatus = $env:AGENT_JOBSTATUS,
    [switch] $SelfTest
)

$ErrorActionPreference = 'Stop'

function Assert-PassingJobStatus([string] $Status) {
    if ($Status -ne 'Succeeded') {
        throw "Validation must succeed before signing or delivery. Current job status: '$Status'."
    }
}

if ($SelfTest) {
    Assert-PassingJobStatus 'Succeeded'
    foreach ($status in @('SucceededWithIssues', 'Failed', 'Canceled', 'Skipped', '')) {
        $rejected = $false
        try {
            Assert-PassingJobStatus $status
        }
        catch {
            $rejected = $true
        }
        if (-not $rejected) { throw "Validation status self-test accepted '$status'." }
    }
    Write-Host 'Validation-status self-test passed.'
    return
}

Assert-PassingJobStatus $JobStatus
Write-Host 'All preceding validation steps succeeded without tolerated errors.'
