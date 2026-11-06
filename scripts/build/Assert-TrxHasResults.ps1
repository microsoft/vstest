<#
.SYNOPSIS
    Fails when a test run produced a result file with no results, or when this gate cannot establish
    that it did.

    IN ITS WIRED MODE THIS SCRIPT WRITES NOTHING. All five of its filesystem operations are inside
    the -SelfTest path and are confined to [IO.Path]::GetTempPath() plus a fresh GUID, which is
    derived and can never be caller-supplied. Stated here anyway, because "Assert-" reads read-only
    and this gate runs in CI immediately before signing - so a reader deciding whether it is safe to
    run needs the distinction between "writes nothing here" and "writes nothing ever", and only the
    first is true.

.DESCRIPTION
    WHAT CAN FAIL THE BUILD FROM THIS SCRIPT. The synopsis used to name only the first of these, which
    left somebody debugging a red build starting from a description that could not explain four of the
    five reasons. 16.11 found the same ratio in their own gate - one of eight - by counting throw and
    exit sites against what the header claimed.

    FINDINGS - the gate decided, and the answer is bad:

      1  a .trx contains zero result nodes                                        exit 1
      2  a .trx has results but Counters/@total is 0                              exit 1
      3  a .trx carries a RunInfo whose outcome is not Passed                     exit 1

    REFUSALS - the gate could not decide, and says so instead of passing:

      4  the results directory does not exist                                     throws
      5  no .trx was found under it                                               throws
      6  neither -ResultsDirectory nor -SelfTest was supplied                     throws
      7  -SelfTest could not detect a defect it is supposed to catch              exit 1

    The distinction is not decoration. Being unable to establish that tests produced results is not
    evidence that they did, and a gate that treats "I could not look" as "nothing wrong" is the exact
    defect this one exists to catch - a .trx with no results is already a green step reporting a run
    that never happened.

    A .trx with zero results is not a failure to anything that reads it. The file exists, it is
    well-formed, it is published, the step is green, and the only signal is a <RunInfo> warning
    inside the file that nothing reads.

    That is how EventLogCollector.UnitTests went from 2017-09-07 to 2026-09-02 without a single one
    of its 35 tests ever executing, on this line and on 16.11. The build was green the entire time.

    THIS GATE IS FAIL-CLOSED ON PURPOSE. An empty result file blocks. The alternative - warn and
    continue - is what the platform already does, and it is the reason nobody noticed for nine years:

        "Publish TRX Test Results: No test result files matching '[ '*.trx' ]' were found."

    is a WARNING. A build where the entire test suite produced nothing stays green.

.NOTES
    WHY THE COUNTS ARE TAKEN THE WAY THEY ARE. Each of these was measured, and the obvious
    alternative is wrong:

    1. RESULTS ARE COUNTED WITH XPath //t:UnitTestResult, NOT with property access.
       $trx.TestRun.Results.UnitTestResult on a <Results/> element with no children yields a single
       phantom object, so @(...).Count is 1 and an EMPTY file reports one result. XPath returns 0
       correctly. Two APIs, same file, opposite answers, and only one of them can be used here.

    2. Counters/@notExecuted IS NEVER READ. It reports 0 while results carry outcome="NotExecuted".
       Observed on this line, on 16.11, and against shipped 17.14.1 - and observed DYNAMICALLY:
       enabling the 35 tests moved the true count from 12 to 13 while the counter stayed 0. A field
       wrong by a constant might be an offset; a field that does not move when the quantity moves is
       not tracking the quantity. Outcomes are counted from the results themselves.

    3. THE RECONCILIATION IS PRINTED, NEVER ASSERTED. 16.11 derived that Counters/@total equals the
       number of result nodes minus the parent row of each data-driven test. It closes exactly on
       their data. It is not asserted here because it describes MSTest's undocumented counting
       convention - a property of a TOOL VERSION, not of this repository, which can move without
       anything here moving.

       Measured, and this is why it matters on this line specifically:

         adapter 14.0.2424.03 (here)   data-driven results are FLAT top-level rows, 0 parents
         adapter 14.0.4503.01 (16.11)  the same tests NEST under a parent row

       This line has 88 data-driven attributes and zero nested results. An MSTest upgrade alone -
       no test change, no repo change - would move every node count. That is a servicing-
       infrastructure change of exactly the kind this branch exists to make, so a gate asserting the
       rule would report a successful upgrade as this line's data being wrong.

    4. A ZERO FILE COUNT IS NOT A PASS. If no .trx is found at all, this throws rather than
       reporting "0 empty files". A gate that runs over nothing must not return the same answer as a
       gate that ran over everything and found nothing wrong.
#>
[CmdletBinding()]
param(
    # NOT marked Mandatory, but the reason originally given here was WRONG and is corrected in place
    # rather than quietly deleted.
    #
    # WHAT I CLAIMED, in 312c54252's message and in the record: that a missing mandatory parameter
    # makes PowerShell PROMPT, that a prompt on a CI agent blocks forever with no output, and that
    # this is "exactly the signature of the 67-minute stall on 3064582".
    #
    # WHAT IS ACTUALLY TRUE, measured after 16.11 challenged it, on pwsh 7.6.5, both invocation forms,
    # each with a 10-second timeout:
    #
    #     pwsh -File     missing mandatory -> ERROR, exit 1, 0.9s, no prompt
    #     pwsh -Command  missing mandatory -> ERROR, exit 1, 0.7s, no prompt
    #
    # and 16.11 measured the same on Windows PowerShell 5.1. It does not prompt and it does not hang.
    #
    # I REASONED THE MECHANISM AND REPORTED IT AS A MEASUREMENT. Worse, I attached it to 3064582,
    # which this script post-dates by weeks and could not have caused - a real stall and an invented
    # explanation, joined because both involved silence. That is the same error this file's own gates
    # exist to catch: a plausible answer to a question nobody actually asked the system.
    #
    # The parameter stays non-mandatory anyway, for the reason that survives: an explicit throw names
    # both valid invocations, where the built-in error names only the missing parameter. That is a
    # smaller benefit than the one I claimed, and it is the real one.
    [string] $ResultsDirectory,

    # Verify the gate detects each defect it claims to. Exits non-zero if any mutant is missed.
    [switch] $SelfTest,

    # Floors for suite SIZE. An emptiness check cannot see a suite that shrank: every remaining file
    # is non-empty, so "0 EMPTY OF 20" passes after 13 projects vanish. These are deliberately NOT
    # defaulted - a floor invented here would be a number nobody decided, and the value is part of an
    # open decision (what a test-asset build failure should do to signing). When neither is supplied
    # the pass line says so explicitly rather than reading as completeness.
    [int] $MinimumReportCount,
    [int] $MinimumTotalResults,

    # Result files that are EXPECTED to contain no results, read from the same baseline the
    # known-test-failures gate uses so there is one list and not two. An entry names a file; a file
    # that is empty AND named is reported and does not block, a file that is empty and NOT named
    # still blocks exactly as before.
    #
    # This exists because on this line the netcore suites cannot run at all - the modern dotnet test
    # CLI drives a console the testhost refuses, and this line's own console rejects an argument the
    # CLI always appends. That is declared in the baseline with its cause and its removal condition.
    # Without this parameter the declaration is invisible here and the same fact has to be asserted
    # in two places, which is how the two places drift apart.
    #
    # Deliberately NOT defaulted to a path. A gate that finds its own exceptions is a gate that can
    # be given exceptions by putting a file somewhere; the caller names the baseline or gets the
    # strict behaviour.
    [string] $ExpectedEmptyBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $SelfTest -and -not $ResultsDirectory) {
    throw 'Specify -ResultsDirectory, or -SelfTest to verify the gate itself.'
}

$TrxNamespace = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'

function Test-SuiteFloor {
    <#
        Evaluates suite-size floors. Extracted from the script body ON PURPOSE: logic that lives
        only in the body runs after -SelfTest has already exited, so it cannot be controlled without
        spawning a child process, and an outcome with no fixture is an uncontrolled outcome. Returns
        the violations; an empty result means every supplied floor was met. Floors that were not
        supplied are not evaluated and are not reported as met.
    #>
    param(
        [int] $ReportCount,
        [int] $TotalResults,
        [hashtable] $Supplied
    )

    $violations = @()
    if ($Supplied.ContainsKey('MinimumReportCount') -and $ReportCount -lt $Supplied['MinimumReportCount']) {
        $violations += "result files $ReportCount is below the required minimum $($Supplied['MinimumReportCount'])"
    }
    if ($Supplied.ContainsKey('MinimumTotalResults') -and $TotalResults -lt $Supplied['MinimumTotalResults']) {
        $violations += "result nodes $TotalResults is below the required minimum $($Supplied['MinimumTotalResults'])"
    }

    return @($violations)
}

function Test-TrxFile {
    param([string] $Path)

    $doc = New-Object System.Xml.XmlDocument
    $doc.Load($Path)
    $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
    $ns.AddNamespace('t', $TrxNamespace)

    # See note 1: XPath, never property access.
    $results = $doc.SelectNodes('//t:UnitTestResult', $ns)
    $counters = $doc.SelectSingleNode('/t:TestRun/t:ResultSummary/t:Counters', $ns)
    $runInfos = $doc.SelectNodes('/t:TestRun/t:ResultSummary/t:RunInfos/t:RunInfo', $ns)

    $total = if ($counters -and $counters.total) { [int]$counters.total } else { 0 }

    # See note 2: outcomes come from the results, never from the counters.
    $outcomes = @{}
    foreach ($r in $results) {
        $o = if ($r.outcome) { $r.outcome } else { 'Unknown' }
        if (-not $outcomes.ContainsKey($o)) { $outcomes[$o] = 0 }
        $outcomes[$o]++
    }

    $badRunInfos = @($runInfos | Where-Object { $_.outcome -and $_.outcome -ne 'Passed' })

    $problems = @()
    if ($results.Count -eq 0) { $problems += 'contains ZERO result nodes' }
    if ($total -eq 0) { $problems += 'Counters/@total is 0' }
    foreach ($ri in $badRunInfos) {
        $text = ($ri.Text -replace '\s+', ' ').Trim()
        if ($text.Length -gt 160) { $text = $text.Substring(0, 160) + '...' }
        $problems += "RunInfo outcome=$($ri.outcome): $text"
    }

    # See note 3: printed, never asserted. Parents are top-level rows that carry InnerResults.
    $parents = $doc.SelectNodes('/t:TestRun/t:Results/t:UnitTestResult[t:InnerResults]', $ns)

    return [pscustomobject]@{
        Name       = Split-Path $Path -Leaf
        Results    = $results.Count
        Parents    = $parents.Count
        Total      = $total
        Outcomes   = $outcomes
        Problems   = $problems
    }
}

function Invoke-TrxGate {
    param([string] $Directory)

    if (-not (Test-Path $Directory)) {
        throw "Results directory not found: $Directory. This is not 'no empty files', it is no measurement."
    }

    $files = @(Get-ChildItem $Directory -Recurse -Filter '*.trx' -File)

    # See note 4.
    if ($files.Count -eq 0) {
        throw "No .trx found under $Directory. A gate that ran over nothing must not report a pass."
    }

    $reports = foreach ($f in $files) { Test-TrxFile -Path $f.FullName }
    return , @($reports)
}

if ($SelfTest) {
    # Mutants are built from a synthetic file and run through the SAME function the gate uses.
    # A self-test that re-implements the detection certifies only that the probe was written.
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("trxgate-selftest-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null
    try {
        $healthy = @"
<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="$TrxNamespace">
  <Results>
    <UnitTestResult testName="A" outcome="Passed" />
    <UnitTestResult testName="B" outcome="NotExecuted" />
  </Results>
  <ResultSummary outcome="Completed"><Counters total="2" executed="1" passed="1" notExecuted="0" /></ResultSummary>
</TestRun>
"@
        $emptyResults = @"
<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="$TrxNamespace">
  <Results />
  <ResultSummary outcome="Completed">
    <Counters total="0" executed="0" passed="0" />
    <RunInfos><RunInfo outcome="Warning"><Text>No test is available in the assembly.</Text></RunInfo></RunInfos>
  </ResultSummary>
</TestRun>
"@
        # Results present but total forced to 0 - proves the two clauses do independent work.
        $lyingTotal = @"
<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="$TrxNamespace">
  <Results><UnitTestResult testName="A" outcome="Passed" /></Results>
  <ResultSummary outcome="Completed"><Counters total="0" executed="0" passed="0" /></ResultSummary>
</TestRun>
"@
        # Results present, total > 0, and a FAILING RunInfo. This is a real shape, not a contrivance:
        # 16.11 found a partial run with 77 results and a deployment warning on their line.
        #
        # IT IS HERE BECAUSE THE RunInfo CLAUSE HAD NEVER FIRED ALONE. The empty-results case above
        # trips all three clauses at once, so it proves nothing about any one of them - a branch that
        # passes as part of a bundle is untested, and this gate's whole subject is checks that look
        # verified without being so. Under this case the other two clauses are silent by construction.
        $partialRun = @"
<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="$TrxNamespace">
  <Results>
    <UnitTestResult testName="A" outcome="Passed" />
    <UnitTestResult testName="B" outcome="Passed" />
  </Results>
  <ResultSummary outcome="Completed">
    <Counters total="2" executed="2" passed="2" />
    <RunInfos><RunInfo outcome="Error"><Text>Failed to deploy. The run completed partially.</Text></RunInfo></RunInfos>
  </ResultSummary>
</TestRun>
"@
        $cases = @(
            @{ Name = 'healthy';       Xml = $healthy;      MustFlag = $false }
            @{ Name = 'empty-results'; Xml = $emptyResults; MustFlag = $true }
            @{ Name = 'lying-total';   Xml = $lyingTotal;   MustFlag = $true }
            @{ Name = 'partial-run';   Xml = $partialRun;   MustFlag = $true }
        )

        $failed = 0
        foreach ($c in $cases) {
            $dir = Join-Path $tmp $c.Name
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            $c.Xml | Set-Content -LiteralPath (Join-Path $dir 'case.trx') -Encoding utf8

            $r = (Invoke-TrxGate -Directory $dir)[0]
            $flagged = $r.Problems.Count -gt 0
            $ok = ($flagged -eq $c.MustFlag)
            if (-not $ok) { $failed++ }
            Write-Host ("  SELF-TEST {0,-14} flagged={1,-5} expected={2,-5} {3}" -f $c.Name, $flagged, $c.MustFlag, $(if ($ok) { 'OK' } else { 'FAILED' }))
            if ($flagged) { foreach ($p in $r.Problems) { Write-Host "      -> $p" } }
        }

        # The phantom-node defect, asserted directly. Property access has TWO failure modes here and
        # neither yields a usable count:
        #   without StrictMode it returns a single phantom object, so an EMPTY file counts as 1
        #   with    StrictMode it THROWS, because the property does not exist on <Results />
        # This script runs under StrictMode, so it sees the second. Both are recorded because a
        # reader copying this idiom elsewhere may not have StrictMode on, and would get the silent
        # overcount rather than an error.
        $doc = New-Object System.Xml.XmlDocument
        $doc.Load((Join-Path (Join-Path $tmp 'empty-results') 'case.trx'))
        $propertyOutcome = try {
            "returned $(@($doc.TestRun.Results.UnitTestResult).Count) - a PHANTOM, the file has none"
        }
        catch {
            'THREW under StrictMode - the property does not exist on an empty <Results />'
        }
        $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
        $ns.AddNamespace('t', $TrxNamespace)
        $viaXPath = $doc.SelectNodes('//t:UnitTestResult', $ns).Count
        Write-Host "  SELF-TEST phantom-node  property access: $propertyOutcome"
        Write-Host "  SELF-TEST phantom-node  XPath returned $viaXPath, which is the true count"
        if ($viaXPath -ne 0) { $failed++ }

        # SUITE-SIZE FLOORS, each outcome with a fixture. An emptiness check cannot see a shrunken
        # suite - every remaining file is non-empty, so "0 EMPTY OF 20" passes after 13 projects
        # vanish. The floor is the only thing that can see it, and a floor with no control is a
        # floor nobody has watched fail.
        $floorCases = @(
            @{ n = 'no floor supplied';  s = @{};                                                      files = 33; nodes = 4469; expect = 0 }
            @{ n = 'both floors met';    s = @{ MinimumReportCount = 33; MinimumTotalResults = 4469 };  files = 33; nodes = 4469; expect = 0 }
            @{ n = 'files below floor';  s = @{ MinimumReportCount = 40 };                              files = 33; nodes = 4469; expect = 1 }
            @{ n = 'nodes below floor';  s = @{ MinimumTotalResults = 5000 };                           files = 33; nodes = 4469; expect = 1 }
            @{ n = 'both below floor';   s = @{ MinimumReportCount = 40; MinimumTotalResults = 5000 };  files = 33; nodes = 4469; expect = 2 }
            @{ n = 'unsupplied floor is not met-by-default'; s = @{ MinimumTotalResults = 5000 };       files = 0;  nodes = 4469; expect = 1 }
        )
        $floorGot = foreach ($c in $floorCases) {
            $got = @(Test-SuiteFloor -ReportCount $c.files -TotalResults $c.nodes -Supplied $c.s).Count
            if ($got -ne $c.expect) { $failed++; Write-Host "  SELF-TEST FAILURE: floor case '$($c.n)': expected $($c.expect) violation(s), got $got" }
            $got
        }
        Write-Host "  SELF-TEST suite floors  $($floorCases.Count) cases, violations $(($floorGot) -join ',')"

        # A directory with no .trx must throw, not report a pass.
        $emptyDir = Join-Path $tmp 'no-files'
        New-Item -ItemType Directory -Path $emptyDir -Force | Out-Null
        $threw = $false
        try { Invoke-TrxGate -Directory $emptyDir | Out-Null } catch { $threw = $true }
        Write-Host "  SELF-TEST no-files      threw=$threw  expected=True"
        if (-not $threw) { $failed++ }

        if ($failed -gt 0) {
            Write-Host "GUARD-RESULT TRX RESULTS: SELF-TEST FAILED - $failed case(s)"
            exit 1
        }
        Write-Host 'GUARD-RESULT TRX RESULTS: SELF-TEST PASSED'
        exit 0
    }
    finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$reports = Invoke-TrxGate -Directory $ResultsDirectory

# Read the declared-empty list before anything is judged, and fail loudly if it was named and cannot
# be read. A baseline that silently resolves to "nothing is expected" would make this gate stricter
# than intended in a way that looks like a real finding.
$expectedEmpty = @()
if ($ExpectedEmptyBaseline) {
    if (-not (Test-Path -LiteralPath $ExpectedEmptyBaseline)) {
        throw "ExpectedEmptyBaseline '$ExpectedEmptyBaseline' does not exist. Refusing to run, because treating it as an empty list would silently turn declared files back into findings."
    }
    $baselineJson = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $ExpectedEmptyBaseline).Path)
    $baselineEntries = @($baselineJson | ConvertFrom-Json)
    # Windows PowerShell 5.1 emits a JSON array as ONE object, so @(...) wraps it again and the
    # single element is itself an Object[]. pwsh 7 does not, which is how this passes locally and
    # silently selects nothing on the agent. Same unwrap as Assert-KnownTestFailures.ps1, and the
    # two must keep agreeing because they now read the same file.
    if ($baselineEntries.Count -eq 1 -and $baselineEntries[0] -is [System.Array]) {
        $baselineEntries = @($baselineEntries[0])
    }
    $expectedEmpty = @($baselineEntries |
        Where-Object { $_.PSObject.Properties.Name -contains 'emptyResultFile' } |
        ForEach-Object { $_.emptyResultFile })
    Write-Host "Expected-empty declarations: $($expectedEmpty.Count) (from $ExpectedEmptyBaseline)"
}

Write-Host "Result files inspected: $($reports.Count)"

$totalResults = 0
$allOutcomes = @{}
foreach ($r in $reports) {
    $totalResults += $r.Results
    foreach ($k in $r.Outcomes.Keys) {
        if (-not $allOutcomes.ContainsKey($k)) { $allOutcomes[$k] = 0 }
        $allOutcomes[$k] += $r.Outcomes[$k]
    }
    if ($r.Problems.Count -gt 0) {
        if ($expectedEmpty -contains $r.Name) {
            Write-Host "  EXPECTED EMPTY     $($r.Name)"
        }
        else {
            Write-Host "  EMPTY OR DEGRADED  $($r.Name)"
        }
        foreach ($p in $r.Problems) { Write-Host "      $p" }
    }
}

# A declaration that never fires is worth printing. It does not block - a suite that started working
# again is good news - but an unreported stale entry is how a list stops describing the build.
$firedNames = @($reports | Where-Object { $_.Problems.Count -gt 0 } | ForEach-Object { $_.Name })
$staleExpected = @($expectedEmpty | Where-Object { $firedNames -notcontains $_ })
if ($staleExpected.Count -gt 0) {
    Write-Host "Stale expected-empty declarations, did not fire this run (not a failure): $($staleExpected.Count)"
    foreach ($s in $staleExpected) { Write-Host "      $s" }
}

Write-Host "Result nodes: $totalResults"
foreach ($k in ($allOutcomes.Keys | Sort-Object)) { Write-Host "  outcome $k : $($allOutcomes[$k])" }

# Printed, never asserted. See note 3.
$parents = ($reports | Measure-Object -Property Parents -Sum).Sum
$declaredTotal = ($reports | Measure-Object -Property Total -Sum).Sum
$reconciles = ($totalResults - $parents) -eq $declaredTotal
Write-Host "Reconciliation (PRINTED, NOT ASSERTED): nodes $totalResults - parents $parents = $($totalResults - $parents) vs Counters/@total $declaredTotal -> $(if ($reconciles) { 'closes' } else { 'DOES NOT CLOSE - a human should look; this is not a gate' })"
if ($parents -eq 0) {
    Write-Host "  parents is 0, so this line's data does not exercise the parent term at all. That is a property of MSTest 14.0.2424.03 writing data-driven results flat, not evidence about the rule."
}

$bad = @($reports | Where-Object { $_.Problems.Count -gt 0 -and $expectedEmpty -notcontains $_.Name })
if ($bad.Count -gt 0) {
    Write-Host ''
    Write-Host "GUARD-RESULT TRX RESULTS: $($bad.Count) FILE(S) WITH NO RESULTS"
    Write-Host 'A result file with no results is not a passing test run. Something was built and'
    Write-Host 'executed and discovered nothing - most often a missing test adapter. See the'
    Write-Host 'EventLogCollector.UnitTests.csproj comment for how that happens and stays invisible.'
    exit 1
}

Write-Host ''
# SCOPE ON THE PASS LINE. "0 EMPTY OF 33" reads as a statement about suite health, and it is not
# one: it establishes that no file is empty, and nothing about whether the suite SHRANK. A run that
# lost 13 projects prints "0 EMPTY OF 20" and passes here. Refusal messages on this line have twice
# been found asserting more than the check established; a pass line is the harder case, because it
# is the line people actually read.
#
# The minimum is NOT invented here. What the floor should be, and what a test-asset build failure
# should do to signing, are one decision and remain open. Until then this reports the
# numbers a shrink would move and states plainly that nothing enforces them.
if ($PSBoundParameters.ContainsKey('MinimumReportCount') -or $PSBoundParameters.ContainsKey('MinimumTotalResults')) {
    $floors = @{}
    foreach ($k in @('MinimumReportCount', 'MinimumTotalResults')) {
        if ($PSBoundParameters.ContainsKey($k)) { $floors[$k] = $PSBoundParameters[$k] }
    }
    $shrunk = @(Test-SuiteFloor -ReportCount $reports.Count -TotalResults $totalResults -Supplied $floors)
    if ($shrunk.Count -gt 0) {
        Write-Host "GUARD-RESULT TRX RESULTS: SUITE SHRANK - $($shrunk -join '; ')"
        Write-Host 'Every file that ran was non-empty, so the emptiness check passed. Fewer tests ran'
        Write-Host 'than required, which an emptiness check cannot see and this floor exists to catch.'
        exit 1
    }
    Write-Host "GUARD-RESULT TRX RESULTS: 0 EMPTY OF $($reports.Count); floor ENFORCED (files >= $(if ($floors.ContainsKey('MinimumReportCount')) { $floors['MinimumReportCount'] } else { 'n/a' }), nodes >= $(if ($floors.ContainsKey('MinimumTotalResults')) { $floors['MinimumTotalResults'] } else { 'n/a' }))"
    exit 0
}

Write-Host "GUARD-RESULT TRX RESULTS: 0 EMPTY OF $($reports.Count) files, $totalResults result nodes; SHRINK NOT ENFORCED - no minimum supplied, so this is NOT evidence the suite is complete"
exit 0
