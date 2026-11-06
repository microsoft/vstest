<#
.SYNOPSIS
    Fails if a file under src/ changed without being declared here, or if "under src/" has stopped
    being a valid stand-in for "compiled into a product assembly".

    THIS SCRIPT WRITES AND DELETES A FILE IN THE WORKING TREE, on every run, not only under
    -SelfTest. It writes a probe under src/ to test the linked-source map, and removes a leftover
    probe from an interrupted earlier run, because an orphan poisons the real verdict. Removal is
    conditional on the file's CONTENT matching this script's marker, never on its path alone.

    That belongs here rather than only at the call site: 16.11's point is that the synopsis is what
    a reader reads before deciding whether a thing is safe to run, and "Assert-" reads as read-only.
    The behaviour was already documented beside the code and in the removal's own comment - which
    protects nobody who has not already decided to read that far.

    This synopsis was corrected ONCE BEFORE, for naming only one of five failure modes, and the
    destructive behaviour still was not added. Fixing the instance is not fixing the category.

.DESCRIPTION
    WHAT CAN FAIL THE BUILD FROM THIS SCRIPT, listed because the synopsis used to name only the
    first of them while five more had been added underneath it. 16.11 found the same drift in their
    own .PARAMETER block - a header describing a self-test that no longer existed - and the header
    is the block somebody reads before deciding whether to trust anything below it.

      1  an UNDECLARED changed file under src/                      exit 1
      2  the proxy is no longer valid: a src/ project, or a shared
         import, compiles a file from outside src/                  exit 1
      3  the supplied baseline ref disagrees with the base derived
         from the branch's own commit timestamps                    throws
      4  the diff produced nothing at all, so the command failed     throws
      5  -SelfTest cannot detect a planted probe, or does not
         restore the tree afterwards                                exit 1

    And what it reports WITHOUT failing, which is deliberate and is not a weaker version of the
    above:

      compiled sources changed, separated from build inputs, because only the former change an
      assembly's MVID directly
      compiled files changed OUTSIDE src/ - these feed test assemblies, which ARE in the null
      build's compared population, so a clean src/ number is not a clean population number

    This branch's mandate is OLD CODE, NEW INFRASTRUCTURE, and its acceptance criterion is the null
    build: build the shipped source on modern infrastructure and RETAIN the artifacts, so a future
    servicing patch is diffed against them and every byte-identical assembly is proven untouched.

    That criterion makes a product source edit expensive in a way that is easy to miss:

        Arcade passes /p:ContinuousIntegrationBuild=true  (eng/common/tools.ps1)
        deterministic compilation derives the MVID from its inputs, INCLUDING source text
        => an edit with NO behaviour change still changes the assembly bytes

    So a comment added to a file under src/ - the most obviously harmless edit there is - makes its
    assembly read as MODIFIED on the first servicing diff, and the explanation is "somebody added a
    comment". The value of retaining the null-build artifacts is that a differing assembly MEANS
    something; every undeclared edit spends a little of that value.

    This script does not forbid product source changes. Four of the five below are the reason this
    branch builds at all. It forbids UNDECLARED ones: to add a file you must write down why, which is
    exactly the review step that was missing. It therefore does not need deleting when real servicing
    code lands - that change gets an entry, like these.

.NOTES
    WHY THERE IS NO COMMITTED PROJECTION TO REGENERATE. Every input this gate uses is derived when it
    runs: the declared set is a literal in this file, the base comes from the branch's own commit
    timestamps, the changed set from git, and the proxy check reads the projects and shared imports
    off disk. Nothing is precomputed and stored.

    That is a trade, not a virtue, and 16.11's line takes the other side deliberately. Their gate
    reads a committed projection of the compared set, which is REVIEWABLE IN A DIFF - somebody can
    see the population change in a pull request. Mine cannot be reviewed that way; it can only be
    re-run.

    What the trade buys here is that a whole failure class cannot occur: their gate's remedy text
    told operators to "regenerate the projection" in four places while no script existed to do it,
    because it had been produced by commands typed into a shell that died with the session. An
    instruction that reads as complete and cannot be followed is the same defect as a measurement
    that reads as complete and answers a different question - and theirs sat in the remedy text of a
    gate wired into CI.

    So: if a precomputed artifact is ever added here, it needs a generator committed beside it in the
    same change, and the generator must be able to reproduce the artifact already committed. "There
    is a generator" is a claim about a file; "the generator reproduces what everyone relies on" is a
    claim about the artifact.


    HOW TO CHECK THIS SCRIPT'S RESULT, because the obvious way loses it.

    This script reports through Write-Host, which writes to the INFORMATION stream. A caller that
    captures with 2>&1 gets NOTHING and every match against that capture returns false:

        pwsh -NoProfile -File <this script>   2>&1     verdict visible   (external process)
        & "$PWD\<this script>"                2>&1     verdict LOST      (in-process)
        & "$PWD\<this script>"                6>&1     verdict visible   (in-process)

    Measured on both lines. I hit it while running a swallow test on this very gate: both assertions
    reported FALSE while the gate had behaved correctly and its output was on screen.

      A check that reads the wrong stream reports a confident FAILURE about a run that succeeded.
      That is worse than the falsely-clean answers elsewhere in this file - a false pass is found
      when something downstream breaks, but a false failure sends somebody to fix what is not
      wrong, and it is believed faster because nobody suspects a check of being pessimistic.

    PREFER THE EXIT CODE. It is set correctly for an in-process call - verified in both directions
    AT 334090d8e, including exit 0 after a prior non-zero, so a stale value cannot be mistaken for
    this run's. The pipeline wires both gates that way and matches no text at all. Capture the output
    only for diagnostics, and then use 6>&1.

    THAT HASH IS NOT DECORATION. 16.11's rule, which corrects a claim of mine: "a verification result
    is worth nothing without the commit it was produced at - it was tested" is a claim about a VERSION,
    and the version is not usually stated. This file changed roughly fifteen times; every undated
    result in it described a script that no longer existed, and read as a property of the gate.

    Audited the rest of this header for the same defect and CLASSIFIED rather than counted - the raw
    count was 17 lines matching 'verified|mutation', which is not a measurement, since most are the
    failure-mode list or narrative. Of six real claims, FIVE cannot age: three are facts outside this
    file (a package's contents, that the VSIX does not build locally, that the base blob starts FF FE
    - an immutable property of a committed object), one is prose about evidence, one is history
    explaining why a line exists. Exactly one aged, and it is the paragraph above.

    MUTATION RESULTS, DATED AT 84b4b8768 - re-run them and append your own hash rather than trust these:

        clean tree                                      exit 0
        -SelfTest                                       exit 0
        UNDECLARED src/*.cs edited, uncommitted         exit 1   SCOPE: 1 UNDECLARED
        OUT-OF-SCOPE file edited, uncommitted           exit 0   <- discriminates
        REFUSAL (-BaselineRef at an all-zero SHA)       exit 1   refusal text asserted, not just exit 1

    This table was previously dated 334090d8e / 76dc86f5f, and THIS FILE CHANGED THREE TIMES after
    that run (540e0534b, 479da5eae, 84b4b8768) - so the table had stopped describing the script,
    which is the aging the dating exists to make visible. Dating is not protection against aging.
    An UNDATED table would have aged identically and still been quoted.

    The last two rows are the pair that matters. A clean-tree pass alone proves only that the gate ran.

    A HEADER DATING ITSELF CAN NEVER NAME ITS OWN COMMIT - the hash does not exist until after the
    commit is made, so the stated hash is always the PARENT. That is not a flaw to work around, but
    it does mean the recorded hash cannot answer "did the commit that wrote this table change the
    behaviour it describes?". Answered by measurement instead of assumption: the four results above
    were re-run at 76dc86f5f, the commit that added this block, and are unchanged. Do the same rather
    than reasoning that an edit was "only a comment" - that reasoning is what the rule above forbids.

    AND DO NOT MATCH ON THE HEADLINE, because a clean run and a finding share every obvious token:

        clean     GUARD-RESULT PRODUCT SOURCE SCOPE: 0 UNDECLARED  (5 declared, 0 stale)
        finding   GUARD-RESULT PRODUCT SOURCE SCOPE: 1 UNDECLARED

    -match 'UNDECLARED', 'GUARD-RESULT' and 'SCOPE' are all TRUE on both. A caller checking any of
    them learns nothing and believes it learned something.

    16.11 hit the same shape from a different mechanism: their control's pattern matched their gate's
    own "nothing to report" summary line, because PowerShell -match is CASE-INSENSITIVE and their
    clean line was the lower-case version of the phrase they searched for. The check reported the
    control as flagged, which would have meant the gate flags everything and the isolation proved
    nothing.

      A check whose pattern matches its own clean-run output cannot distinguish a finding from its
      absence. Same family as a grep that matches the note documenting the fix - and it appeared
      inside the test written to establish a clause.

    Nothing here is currently exposed: all three self-tests on this line decide structurally, not by
    text - $detected uses -contains on a collection, $flagged uses .Problems.Count, and the
    measurement self-test compares counts. That is worth stating because it is the property that
    makes them sound, and it would be easy to "simplify" one into a string match later.

    AND IF YOU MUST MATCH TEXT, MATCH THE CLEAN HEADLINE AND TREAT ITS ABSENCE AS FAILURE.
    Measured across a clean run, an undeclared-file finding, a proxy-invalid finding, A REFUSAL, and a
    PARAMETER-BINDING ERROR - five outcomes. 16.11 measured three and found both tokens they
    recommended were silent on the fourth; they then found a fifth, which is the binding error, and it
    is reachable here for the same reason it is there:

        'SCOPE: 0 UNDECLARED'   clean TRUE   undeclared FALSE  proxy FALSE  refusal FALSE  binding FALSE
        'UNDECLARED COMPILED'   clean FALSE  undeclared TRUE   proxy FALSE  refusal FALSE  binding FALSE
        'NO LONGER VALID'       clean FALSE  undeclared FALSE  proxy TRUE   refusal FALSE  binding FALSE

    THE BINDING ERROR IS NOT DISTINGUISHABLE FROM A REFUSAL BY EXIT CODE. Both exit 1. -Base and
    -ComparedSet and -Declarations and -Path are parameters of INNER FUNCTIONS in this file; passing
    any of them to the SCRIPT is a binding error that produces exit 1 and NO gate output at all. A
    caller that treats "exit 1" as "the gate ran and found something" is wrong on that row.

    Discriminate by OUTPUT, not by code:
        binding error   no 'GUARD-RESULT' line at all       the gate never ran
        real refusal    refusal text present, no verdict    the gate ran and declined to decide
        finding         'GUARD-RESULT' with a non-zero count

    'GUARD-RESULT' IS A VERDICT MARKER, NOT A LIVENESS MARKER. Measured on all four outcomes: it
    prints on clean and on a finding, and NOT on a refusal or a binding error. So it answers "did
    this reach a verdict", which is a third question distinct from both "did it pass" and "did it
    run". Two tokens are required to separate the four outcomes and no single token can do it - which
    is why the table above has a text column as well.

    This was recommended to 16.11 as a marker printed on every run. It is not, and the recommendation
    was made without measuring the property being recommended. Their gate's banner has the mirror
    defect - it prints only on a CLEAN run, so it answers "did it pass" - and neither of us had
    measured our own before proposing it to the other.

    The clean headline is FALSE on all four non-clean rows, so matching it still fails closed - but
    that was true for a reason never established until the fifth row was measured, and being right for
    an unestablished reason is not the same as being right.

    The refusal row was produced with -BaselineRef pointing at an all-zero SHA, verified to be the
    real refusal (the merge-base throw at line ~525) and not something else: the first attempt used
    -Base, which is an INNER function's parameter, so it raised a parameter-binding exception. That
    also exits 1 with the token FALSE, and would have looked like a passing test of a path never
    reached. Distinguished by asserting the refusal TEXT is present, not merely that the exit was 1.

    The two finding-side tokens each identify ONE failure mode and are silent on the other, so a
    caller matching either learns nothing about the mode it does not cover. The clean headline is the
    only string that separates "nothing wrong" from every other outcome at once - and matching for it
    is FAIL-CLOSED, because a garbled, truncated or empty capture also fails to match and therefore
    reads as not-clean.

    This paragraph exists because the warning above it was only half an answer. Naming what not to
    match, without naming what does work, leaves a reader knowing their tool is broken and holding
    nothing - the same obligation this branch's csproj comment discharges by pointing at
    SelectNodes('//TargetFramework') after saying the grep lies.

    WHY THE LIVENESS CHECK BELOW EXISTS. Writing this script, my first measurement printed
    "src/ files changed: 0" from a git diff USAGE ERROR - a malformed revision argument produced
    empty output, and .Count on nothing is 0. A clean tree and a broken command are the same
    observation. So: assert a known-non-zero value (the total changed file count) before believing
    any zero derived from the same command.

    IT IS CALLED A LIVENESS CHECK AND NOT A POSITIVE CONTROL, and this header said the wrong one
    until 16.11's audit of their own labels prompted me to re-read mine. Both sides of it come from
    the same git command, so it can only establish that the instrument ran - not that the answer is
    right. Run against the wrong base it passed happily with 364 changed files while the whole
    measurement was meaningless.

    The three are different and the vocabulary is load-bearing:

      liveness      did the instrument run at all?          one side is enough
      state         did the tree return to a known state?   before and after, one instrument
      cross-check   is the answer RIGHT?                    needs a side from OUTSIDE the input

    The base check further down is a real cross-check by that test - it derives the base from the
    branch's own commit timestamps and never touches the supplied ref.

    A stale label is worse than none, because "positive control passed" is exactly the phrase a
    future reader would quote as evidence the result was verified.
#>
[CmdletBinding()]
param(
    # The servicing baseline this branch departs from.
    [string] $BaselineRef = 'origin/rel/15.9',

    [string] $HeadRef = 'HEAD',

    # Print the declared set and exit 0 without gating.
    [switch] $List,

    # Verify the script detects an undeclared change AND leaves the tree as it found it.
    # Plants a probe under src/, confirms THIS SCRIPT'S OWN detection function sees it as UNDECLARED,
    # removes it, then re-runs that same function to prove the removal was complete. Exits non-zero
    # if the probe is missed or if anything is left behind.
    #
    # The second half is not decoration: the first version checked cleanup with git status by eye,
    # which is a person verifying a machine. And a leftover probe from an interrupted earlier run is
    # removed at startup regardless of this switch - but only when its content matches this script's
    # marker, never on path alone, so the heal cannot swallow a real file that happens to sit there.
    [switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# SELF-HEAL A LEFTOVER SELF-TEST PROBE BEFORE MEASURING ANYTHING.
#
# -SelfTest plants a file inside src/ so the gate's own detection can see it, and removes it in a
# finally. A finally does not run when the process is KILLED - and a build being cancelled mid-step is
# not hypothetical here, it happened on 3064582. On an agent that reuses its workspace the leftover
# probe would then be reported by the NEXT run as an UNDECLARED product source change.
#
# That failure would be the gate reporting a defect IT CREATED ITSELF, with a message telling somebody
# they edited src/. Worse than a false negative: a confident false positive pointing at an innocent
# person, produced by the instrument that is supposed to be the trustworthy one.
#
# 16.11 hit the general form of this and drew the line I am borrowing. Their Test-SigningSetAssertion
# mutates the real Sign.proj to prove each rule fires alone, and they deliberately did NOT wire it -
# the step that makes it trustworthy is the step that makes it destructive, and a process killed
# mid-test would leave a modified signing input. Their instrument mutates something the build depends
# on, so it stays manual. Mine only creates one file at a path it owns, so it can stay wired - but
# only if it cleans up after a KILL as well as after a return.
$probeRelative = 'src/__scope_selftest_probe.cs'
# ONE marker, used by BOTH the planter and the healer. Two literals would be two places to change,
# and the failure mode of a drifted pair is silent: the healer stops recognising the planter's file
# and starts leaving orphans, while every test still passes.
$probeMarker = '// scope-gate self-test probe - safe to delete'
$repoRootForProbe = (& git rev-parse --show-toplevel 2>$null | Out-String).Trim()
if ($repoRootForProbe) {
    $leftoverProbe = Join-Path $repoRootForProbe $probeRelative
    if (Test-Path -LiteralPath $leftoverProbe) {
        # REMOVE ON CONTENT, NEVER ON PATH ALONE.
        #
        # The first version of this self-heal deleted whatever sat at the probe path. That is the
        # defect the self-heal was written to avoid, one level down: a gate that quietly destroys a
        # real file is worse than a gate that falsely accuses somebody of creating one. 16.11 caught
        # it by testing the discriminating pair - the same non-probe content at two paths must
        # produce opposite verdicts - which path-based removal cannot do at all.
        $existing = (Get-Content -LiteralPath $leftoverProbe -Raw -ErrorAction SilentlyContinue)
        if ($existing -and $existing.Trim() -eq $probeMarker) {
            Write-Host "Removing a leftover self-test probe at ${probeRelative} - a previous run was interrupted before its cleanup."
            Remove-Item -LiteralPath $leftoverProbe -Force
        }
        else {
            Write-Host "A file exists at ${probeRelative} but does NOT carry this script's probe marker, so it is NOT being removed. It is classified below like any other file."
        }
    }
}

# DETECT THE DAY THIS GATE'S OWN SCOPING ASSUMPTION STOPS HOLDING.
#
# The gate is directory-scoped: it treats "under src/" as a proxy for "compiled into a product
# assembly". That proxy is only valid while no product project compiles a file from outside src/.
#
# Measured today: 26 csproj under src/, one <Compile Include> containing '..', and ZERO resolving
# outside src/. The five shared imports every project inherits - Directory.Build.props/.targets and
# the three scripts\build files - add no <Compile Include> at all.
#
# 16.11 found the general form: their gate resolved a changed file to its NEAREST ANCESTOR csproj,
# which is a proxy for "which projects compile this file", and they had 9 files linked into projects
# outside their own directory. Nothing was missed, because every linked file happened to sit beside a
# project already in the compared set - and as they put it, that is safety by luck, and the luck is
# not a property of the gate.
#
# So this checks the proxy rather than trusting it. A single <Compile Include> reaching outside src/
# would mean a product assembly depends on a file this gate never looks at, and the gate would
# otherwise go on printing a clean number.
function Get-ProductSourcesOutsideScope {
    $violations = @()

    # SCAN THE SHARED IMPORTS TOO, not only the projects.
    #
    # I measured these by hand when writing this check - Directory.Build.props/.targets and the three
    # scripts\build files, 0 <Compile Include> between them - and then wrote a function that scanned
    # src\**\*.csproj only. So the control encoded the conclusion and not the check, and a Compile
    # item added to a shared import later would be inherited by every product project while this
    # function reported clean. 16.11 closed the same gap on their line by re-deriving their link map
    # at gate time instead of trusting a committed one.
    $sharedImports = @(
        'Directory.Build.props'
        'Directory.Build.targets'
        'scripts\build\TestPlatform.Settings.targets'
        'scripts\build\TestPlatform.targets'
        'scripts\build\TestPlatform.Dependencies.props'
    ) | Where-Object { Test-Path $_ } | ForEach-Object { Get-Item $_ }

    # -ErrorVariable, NOT -ErrorAction SilentlyContinue. 16.11 measured that the first version of this
    # guard was fail-closed against TOTAL enumeration failure and OPEN against PARTIAL: with one
    # unreadable subdirectory the walk returns 1 of 2 projects, $Error.Count is 1, and a >0 count check
    # PASSES on a short map. A short linked-source map answers linked-source questions CLEAN by having
    # fewer things to compare against - the same false-clean the guard was added to close, one level in.
    # The error record EXISTS in this recursive form, so capturing it is sufficient; that is not true of
    # the non-recursive form, where the provider swallows the failure before a record is created.
    $enumErrors = $null
    $projFiles = @(Get-ChildItem 'src' -Recurse -Filter '*.csproj' -File -ErrorAction SilentlyContinue -ErrorVariable enumErrors)
    if ($enumErrors -and $enumErrors.Count -gt 0) {
        throw "Enumerating 'src' for *.csproj reported $($enumErrors.Count) error(s) - first: $($enumErrors[0].Exception.Message). The walk completed PARTIALLY, so the linked-source map would be short, and a short map reports every projection valid by having fewer things to check. Refusing on an incomplete enumeration."
    }
    # A ZERO HERE IS NOT A MEASUREMENT. -ErrorAction SilentlyContinue makes an enumeration FAILURE -
    # missing directory, permission fault, path-length limit - indistinguishable from a repository
    # that genuinely contains no project files. This branch has dozens, so zero means the command
    # failed, and an empty map would make every linked-source question answer CLEAN by having nothing
    # to compare against. Same defect 16.11 fixed in their Get-ChangedSourcePaths, found by their
    # suppression audit: the third outcome - I COULD NOT ENUMERATE - collapsing into the first.
    if ($projFiles.Count -eq 0) {
        throw "Enumerating 'src' for *.csproj returned NOTHING. This branch has dozens, so the enumeration failed rather than finding none. Refusing to build a linked-source map from an empty set - it would report every projection as valid by having nothing to check."
    }
    $toScan = $projFiles + $sharedImports

    foreach ($proj in $toScan) {
        $doc = New-Object System.Xml.XmlDocument
        try { $doc.Load($proj.FullName) } catch { continue }

        # XPath with local-name(), for two reasons learned the hard way tonight:
        #   property access ($group.Compile) THROWS under StrictMode when an ItemGroup has no Compile
        #   child, which is most of them - the same defect as reading .UnitTestResult on an empty
        #   <Results/>. A first version of this function did exactly that and died on the clean tree.
        #   And local-name() ignores the MSBuild namespace, which old-style projects declare and
        #   SDK-style ones do not, so one expression covers both.
        foreach ($item in $doc.SelectNodes("//*[local-name()='Compile']")) {
            # BOTH attributes. Include adds a file; Update retargets one the SDK already globbed, and
            # it can point outside the project directory just as easily. Checking only Include reads
            # as thorough and covers one of the two ways to do the thing being guarded against.
            foreach ($attr in @('Include', 'Update')) {
                $value = $item.GetAttribute($attr)
                if ([string]::IsNullOrWhiteSpace($value)) { continue }
                # A semicolon-separated value is several files, not one. 16.11's probe counted such an
                # entry as one file and reported a wrong number because of it.
                foreach ($single in ($value -split ';')) {
                    if ([string]::IsNullOrWhiteSpace($single) -or $single -notmatch '\.\.') { continue }
                    $resolved = [IO.Path]::GetFullPath((Join-Path $proj.DirectoryName $single))
                    $relative = $resolved.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, '').Replace('\', '/')
                    if ($relative -notlike 'src/*' -and $relative -notlike 'shared/*') {
                        $violations += "$($proj.Name) compiles $relative  [$attr]"
                    }
                }
            }
        }
    }
    return , @($violations)
}

$proxyViolations = Get-ProductSourcesOutsideScope
if ($proxyViolations.Count -gt 0) {
    Write-Host ''
    Write-Host "GUARD-RESULT PRODUCT SOURCE SCOPE: THIS GATE'S SCOPE IS NO LONGER VALID"
    foreach ($v in $proxyViolations) { Write-Host "  $v" }
    Write-Host 'A project under src/ now compiles a file from outside src/, so "under src/" has stopped'
    Write-Host 'being a usable proxy for "compiled into a product assembly". Widen the scope or declare'
    Write-Host 'the linked path - do not let the gate keep reporting a clean number over a set that no'
    Write-Host 'longer contains its own subject.'
    exit 1
}

# Every path here is a DELIBERATE product source change, with the reason it was worth its MVID.
# Adding a path without a reason defeats the point of the script.
$declared = [ordered]@{
    'src/Microsoft.TestPlatform.CoreUtilities/Microsoft.TestPlatform.CoreUtilities.csproj' =
        'Drops the Microsoft.Internal.Dia.Interop package reference. Interop types are embedded, so it is a compile-time dependency only, and it ships in none of the nine 15.9.2 packages nor the five shipped artifacts. Modern NuGet copies lib\net451 to the output anyway, which is how it appeared in the staged payload.'

    'src/package/nuspec/Microsoft.TestPlatform.Build.csproj' =
        'Shares a file name with src\Microsoft.TestPlatform.Build. Arcade derives obj/out directories from the project name, so both wrote to the same folder and raced, producing intermittent CS2012 and MSB3554 that read as machine contention.'

    'src/package/VSIXProject/TestPlatform.csproj' =
        @'
Corrects Condition="Exists('($TestPlatformRoot)...')" to '$(TestPlatformRoot)'.

WHAT THIS ENTRY USED TO SAY, AND WHY IT WAS WRONG: "a malformed variable reference that parsed,
evaluated to a harmless literal, and never matched - so the import silently never happened." True,
and it describes the typo rather than the CHANGE. Correcting the condition does something material,
and 16.11 challenged this entry on exactly that basis.

WHAT IT ACTUALLY DOES. The imported props sets VSToolsPath, VsSDKInstall, VsSDKIncludes and
VsSDKToolsPath to the NuGet package tools. Line 6 of this project already defaults VSToolsPath to
$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion) when empty, and it is
evaluated FIRST, so before the correction:

    VSToolsPath = the AGENT MACHINE's installed Visual Studio
    line 255 then imports Microsoft.VsSDK.targets from that installed VS

and after it, the props import overwrites VSToolsPath unconditionally:

    VSToolsPath = the PINNED NuGet package microsoft.vssdk.buildtools 15.8.3247

So this switches which VSSDK builds the VSIX, from whatever is installed on the agent to a version
pinned in the repository. Defensible as "new infrastructure" - it makes the VSIX build reproducible
and agent-image-independent, which is this branch's whole purpose - but it is a BEHAVIOUR CHANGE,
not a tidy-up.

MEASURED: the typo is present at the servicing base c9769b68f (2019-04-30), so the SHIPPED 15.9.2
VSIX was built with the agent's installed VS and never imported these props. The correction arrived
in d57cff57f, the first commit of the servicing effort, bundled with everything else - so no build on
this line has ever run without it and history cannot say whether it is required.

16.11 REACHED THE OPPOSITE CONCLUSION ON THE SAME LINE OF THE SAME FILE (3965b7a14, "Restore the 2021
props-import typo, and make VSToolsPath absolute so 3a stands alone"). Their argument: on a branch
whose purpose is to reproduce what shipped, a defect that was present when it shipped is part of the
specification, so correcting it departs from 2019 rather than restoring it. They kept their build
working by defining VSToolsPath absolutely instead.

Both positions are coherent and they are the cleanest cross-line disagreement of this effort:

    a silent no-op in a pipeline is a bug
    a silent no-op that was in the shipped build is a requirement

AND THERE IS A THIRD OPTION, WHICH THIS ENTRY ORIGINALLY MISSED. 16.11 found it by checking where
their VSToolsPath actually resolves. The choice is not correct-the-typo versus leave-it:

    A  correct the typo    the props import fires and sets VSToolsPath to the pinned package  THIS LINE
    B  leave the typo      VSToolsPath falls back to the agent's installed Visual Studio      NOBODY
    C  leave the typo AND set VSToolsPath explicitly to the pinned package                    16.11

B is the outcome worth avoiding and it is the one neither line chose. A and C reach THE SAME TOOLS by
different routes: A by restoring an import that never ran in 2019, C by preserving the 2019 import
behaviour and pinning the path directly. C is strictly better on fidelity at equal reproducibility.

C IS AVAILABLE HERE AND COSTS ONE PROPERTY. Measured: line 6 of this project defaults VSToolsPath to
$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion) - the AGENT's VS - so
adopting C means changing that default to the pinned package path and restoring the typo. 16.11's
line 35 already does exactly that.

ONE NAMED GAP IN C, measured in the package rather than assumed. The props sets FIVE properties -
ThisPackageDirectory, VSToolsPath, VsSDKInstall, VsSDKIncludes, VsSDKToolsPath - and I first said
four, having read the four that mattered and counted them as all of them.
Microsoft.VsSDK.targets defines NONE
of them itself, so under C the other four are unset. Their usage in that targets file:

    VsSDKIncludes    0 references          dead
    VsSDKToolsPath   0 references          dead
    VsSDKInstall     3 references, all inside Target "RunRegRiched"
    OutputPath       2 references          positive control, so the zeros above are measurements

TWO CLAIMS I MADE ABOUT THAT TARGET WERE WRONG. 16.11 - the line actually running C - corrected them,
and both were then verified here against this repo's own copy of the package:

  I said   "RunRegRiched is not on the build dependency chain"
  truth    it IS: <PrepareForBuildDependsOn>$(PrepareForBuildDependsOn); RunRegRiched
           It is unreachable because the target carries Condition="'$(RunRegRiched)' == 'true'" and
           that property is set NOWHERE - not in the targets, not in the props, not in this repo.

  I said   "its !Exists guard becomes true, so it would Exec an empty path"
  truth    the negated condition is on the <Warning>. The <Exec> carries the POSITIVE
           Condition="Exists(...)", so an empty VsSDKInstall SKIPS the Exec and fires a warning.
           An empty path is never executed.

The conclusion - latent, not active - survives both corrections. The REASONS did not, and the reasons
are what survives a future edit: somebody setting RunRegRiched=true for a VS integration scenario
would revive the target, and "not on the chain" would have told them that was impossible.

DO NOT "FIX" THIS BY SETTING THE DEAD PROPERTIES. Two are referenced nowhere and the third is
reachable only through a property nothing sets, so setting them changes no behaviour while adding
divergence from the shipped project. The reader who tidies them would be diligent, and wrong, and
green.

AND THE FACT THAT OUTRANKS THE ANALYSIS: 16.11 landed C in 3965b7a14 on 2026-09-01 and has produced a
VSIX in CI three times since (3064282, 3064344, 3064417). The reasoning above explains a result that
was already observed; it is not a prediction that C will work.

Both lines already fail loudly if the package is missing at all: <Error> guards on
the props and the targets, at lines 260 and 261 of this file.

NOT DECIDED HERE. Kept as-is because reverting changes which VSSDK builds the VSIX and cannot be
verified without a CI run - the VSIX does not build locally under dotnet msbuild at all, since the
VSSDK tasks are .NET Framework tasks (MSB4061, "Type must be a type provided by the runtime"), which
constrains BOTH lines equally. 16.11's "mine builds without it" is a CI observation, not a local one.
This is a fidelity-versus-reproducibility question for a human, and with C on the table the gap
between the two lines is much smaller than it first appeared.
'@

    'src/Microsoft.TestPlatform.PlatformAbstractions/Microsoft.TestPlatform.PlatformAbstractions.csproj' =
        @'
Adds ExcludeAssets="runtime" to the Microsoft.Internal.Dia.Interop PackageReference - the second half
of the same fix as CoreUtilities.csproj above, with the mechanism written into the file.

THIS ENTRY PREVIOUSLY READ "Build-infrastructure alignment for the modern Arcade toolset." That was
not merely thin, it was uninformative to the point of being wrong: it described a category rather than
a change, and a reader could not have connected it to the Dia.Interop finding it belongs to.

AND IT IS THE ONE DECLARED CHANGE NOBODY CAN REVIEW IN A DIFF. This file is UTF-16 LE - 3,831 NUL
bytes at the servicing base, 4,635 now - so git treats it as binary:

    git diff --stat   ...PlatformAbstractions.csproj | Bin 7664 -> 9272 bytes
                      1 file changed, 0 insertions(+), 0 deletions(-)

Every pull request showing this change shows nothing. The encoding is PRE-EXISTING and was not
introduced by this effort - verified by reading the base blob's first bytes, FF FE - so it is not a
defect to fix here, but it does mean this declaration is the ONLY description a reviewer gets.

That is why the change is spelled out above rather than categorised. 16.11 named the property this
file lacks: a committed artifact is reviewable in a pull request, and the day it changes a human sees
it in a diff and decides whether it should have. For this one file that property is structurally
absent, and it is absent for four of the five other declared paths in exactly the opposite way -
they are plain text and reviewable, so nobody thinks to check which is which.

To see what actually changed, decode both blobs as Unicode and compare:

    git cat-file blob <rev>:<path>   ->   [IO.File]::ReadAllText(<file>, [Text.Encoding]::Unicode)

BEFORE WIDENING THE COMPARED SET, READ THIS. 16.11 found the interaction on their line and it is
sharper here. Measured across every tracked .cs/.csproj/.props/.targets/.config in this repo:

    UTF-16 tracked files, whole repo   1     <- this file, and nothing else
    of those inside src/               1     <- INSIDE this gate's directory scope
    of those in the compared set       0     <- kept out ONLY by the extension filter

On 16.11's line the equivalent files sit under test/ AND outside the compared set - two independent
reasons, either one sufficient. Here there is ONE: '.csproj' is absent from the compared-extension
array. Add build inputs to that array and this gate will correctly demand a declaration for a change
no reviewer can see, and the declaration becomes the entire review.

Which is not a forecast. It is what the entry above already IS. That is the argument for widening as
much as against it - the prose is only trustworthy because someone decoded the blob, and nothing in
the gate makes the next author do that. If the set widens, this note is the instruction: the
declaration must state what changed, not that something did, because it is all a reviewer will get.
'@

    # ---- Retained connection lifecycle and diagnostic changes. ----

    'src/Microsoft.TestPlatform.CommunicationUtilities/SocketServer.cs' =
        'Stops the listener during shutdown and does not publish an accepted client after stopping.'

    'src/Microsoft.TestPlatform.CommunicationUtilities/SocketClient.cs' =
        'Closes a client if cancellation wins the connect race, before creating its channel.'

    'src/Microsoft.TestPlatform.CommunicationUtilities/SocketCommunicationManager.cs' =
        'Cancels pending accepts and connection attempts on stop, and closes the accepted socket.'

    'src/Microsoft.TestPlatform.CommunicationUtilities/TestRequestSender.cs' =
        'Reports endpoint faults and protocol errors to the waiting caller, and redacts logged message payloads.'

    'src/Microsoft.TestPlatform.VsTestConsole.TranslationLayer/VsTestConsoleWrapper.cs' =
        'Shares connection timeout and error handling with the asynchronous wrapper.'

    'src/Microsoft.TestPlatform.Common/Utilities/SecretRedaction.cs' =
        'Redacts values with sensitive property names before diagnostic JSON is logged.'

    'src/Microsoft.TestPlatform.CommunicationUtilities/Messages/Message.cs' =
        'Redacts sensitive message payload properties when formatting a message for logging.'

    'src/Microsoft.TestPlatform.Common/DataCollection/DataCollectionManager.cs' =
        'Redacts sensitive environment variable values before logging collector configuration.'

    'src/Microsoft.TestPlatform.TestHostProvider/Properties/AssemblyInfo.cs' =
        'Uses an escaped copyright symbol so its metadata survives different source encodings.'

    'src/Microsoft.TestPlatform.VsTestConsole.TranslationLayer/Interfaces/ITranslationLayerRequestSenderAsync.cs' =
        'Separates listening from waiting so the asynchronous wrapper can launch the console between them.'

    'src/Microsoft.TestPlatform.VsTestConsole.TranslationLayer/VsTestConsoleWrapperAsync.cs' =
        'Listens before launching the console, then waits for its connection.'

    'src/Microsoft.TestPlatform.VsTestConsole.TranslationLayer/VsTestConsoleRequestSender.cs' =
        'Does not run the existing protocol handshake if the connection was not accepted.'

    'src/Microsoft.TestPlatform.PlatformAbstractions/netcore/System/ProcessStartInfoExtensions.cs' =
        'Removes inherited environment variables when supplied a null value rather than writing an empty value.'

    'src/vstest.console/CommandLine/Executor.cs' =
        'Prints a short product version normally and the full version in diagnostic mode.'

    'src/Microsoft.TestPlatform.CommunicationUtilities/Friends.cs' =
        'Grants internal access to the translation layer and client that use shared connection handling.'

    'src/Microsoft.TestPlatform.CoreUtilities/Friends.cs' =
        'Grants internal access to the utilities, engine, translation layer, and test assemblies.'
}

function Get-ChangedSourceFiles {
    param([string] $Base, [string] $Head)

    # TEST THE CAPABILITY, NOT THE PROXY.
    #
    # An earlier version refused whenever `git rev-parse --is-shallow-repository` said true. That is
    # a PROXY for "a merge base cannot be computed", and build 3065098 proved the two come apart:
    # `git fetch --unshallow origin` pulled all ~140 branches in 9 seconds, and the repository still
    # reported shallow afterwards - because the checked-out commit's own graft boundary survives even
    # when every ref has been completed. Refusing there rejects a tree that may be perfectly able to
    # answer the question.
    #
    # So: ask for the merge base first. If it computes, the depth is sufficient BY DEMONSTRATION and
    # shallowness is irrelevant. Only when it fails does shallowness become the explanation, and then
    # it is reported as the cause rather than as "fetch the baseline ref", which was the misleading
    # advice on 3065092 - the ref had just been fetched and asserted present.
    $mergeBase = (& git merge-base $Head $Base 2>$null | Out-String).Trim()
    if (-not $mergeBase) {
        $isShallow = ((& git rev-parse --is-shallow-repository 2>$null | Out-String).Trim() -eq 'true')
        if ($isShallow) {
            throw ("REFUSING: no merge base between '$Head' and '$Base', and this repository is " +
                "SHALLOW - the two share no locally present ancestor. Deepen the checkout before this " +
                "gate runs. Do NOT read this as 'the baseline ref is missing': that is a different " +
                "failure, and on build 3065092 the ref had already been fetched and asserted present " +
                "one step earlier, so that advice sent the reader to re-check the one proven thing.")
        }
        throw ("REFUSING: no merge base between '$Head' and '$Base', and this repository is NOT " +
            "shallow, so depth is not the explanation and the baseline ref itself is. Fetch it; do " +
            "not treat this as 'nothing changed'.")
    }

    # CROSS-CHECK THE BASE BY A METHOD THAT DOES NOT DEPEND ON THE SUPPLIED REF.
    #
    # The base is the whole answer, and the natural thing to reach for is the wrong thing. 16.11 ran
    # this audit against merge-base with origin/MAIN and got 245 changed src/ files including
    # TestPluginCache.cs and AssemblyResolver.cs - they were about to report a catastrophe. That diff
    # was BRANCH DIVERGENCE, not work: a servicing branch parted from main years ago, so almost
    # everything differs and none of it was done here.
    #
    # THE FIRST VERSION OF THIS CHECK WAS VACUOUS AND A TEST CAUGHT IT. It compared the merge base
    # against the parent of the first commit "not in $Base". Both of those compute where the branch
    # left the ref you handed in, so they agree for ANY ref, including a wrong one. Run against
    # origin/main it happily reported 149 src/ files and never mentioned the base. A cross-check
    # whose two methods share the input they are supposed to be checking is not a cross-check.
    #
    # This derives the resumption point from the branch's own shape instead. A servicing branch sits
    # untouched for years and then restarts, so the largest gap between consecutive commits IS the
    # start of the effort. That needs no ref at all and cannot be fooled by passing a wrong one.
    # THE WINDOW MUST BE PROVEN TO HAVE ARRIVED BEFORE ANYTHING IS DERIVED FROM IT.
    #
    # This walk is bounded twice: by -n below, and by the CLONE, because a CI checkout is shallow
    # unless something deepened it. Both truncations look identical from here - a short list - and
    # the original code drew a conclusion from whatever it got. If the walk came back with one entry
    # the loop never ran, $gapBase stayed $null, and the `if ($gapBase -and ...)` guard was false, so
    # the cross-check SILENTLY DID NOTHING while still reading as protection.
    #
    # 16.11 measured the same shape on their line and it is worse than a hang: at depth 20 and depth
    # 100 their base derivation reported a 0-day gap and NO ERROR, where the true gap is 1,855 days.
    # A bounded window that undershoots swaps a visible failure for a silently wrong scope audit.
    #
    # So: a truncated window REFUSES. It does not fall through to the merge base, because "I could
    # not check" and "I checked and it agreed" must never produce the same outcome.
    $walkLimit = 400
    $walk = @(& git log --first-parent --format='%H %at' $Head -n $walkLimit 2>$null)
    $isShallow = ((& git rev-parse --is-shallow-repository 2>$null | Out-String).Trim() -eq 'true')

    if ($walk.Count -lt 2) {
        throw ("Cannot cross-check the base: the history walk returned $($walk.Count) commit(s)" +
            "$(if ($isShallow) { ' and this is a SHALLOW repository' }). The gap derivation needs at " +
            "least two commits. Deepen the checkout (git fetch --unshallow) before this gate runs. " +
            "Refusing rather than trusting '$Base' unchecked - a cross-check that cannot run must not " +
            "look like one that ran and agreed.")
    }
    if ($isShallow) {
        # DEFERRED, NOT IMMEDIATE - the same proxy error as the merge-base check above.
        # Shallowness truncates the walk, but a truncated walk that ALREADY CONTAINS a multi-year
        # pause has found the resumption point, and a longer pause hidden beyond the boundary cannot
        # move it earlier. Refusing here unconditionally rejects a tree that has demonstrably
        # answered the question. The refusal is therefore folded into the truncation test below,
        # where it fires only when the walk found nothing adequate.
        Write-Host "NOTE: repository is shallow; the history walk may be truncated by the clone. This is only a problem if no adequate pause is found - see the truncation check below."
    }

    $gapBase = $null
    $largestGap = 0
    for ($i = 0; $i -lt $walk.Count - 1; $i++) {
        $newer = [long](($walk[$i] -split ' ')[1])
        $older = [long](($walk[$i + 1] -split ' ')[1])
        $gap = $newer - $older
        if ($gap -gt $largestGap) {
            $largestGap = $gap
            $gapBase = ($walk[$i + 1] -split ' ')[0]
        }
    }

    # A TRUNCATED WALK CANNOT REPORT AN ABSENCE. It can report a PRESENCE.
    #
    # The walk stops early for two reasons that look identical from here: our own -n limit, and the
    # clone's shallow boundary. Either way, if an adequate pause WAS found the resumption point is
    # established and a longer pause beyond the boundary cannot move it earlier. If none was found,
    # the absence is a property of the window rather than of the branch, and saying otherwise is the
    # universal-negative defect this file has now hit in three places.
    $hitLimit = ($walk.Count -eq $walkLimit)
    $truncated = $hitLimit -or $isShallow
    $gapDays = [math]::Round($largestGap / 86400)
    if ($truncated -and $gapDays -lt 180) {
        $why = @()
        if ($hitLimit) { $why += "hit its own limit of $walkLimit commits" }
        if ($isShallow) { $why += 'was truncated by a SHALLOW clone' }
        throw ("Cannot cross-check the base: the history walk " + ($why -join ' and ') +
            ", and found no pause longer than $gapDays day(s) inside that window. A longer pause may " +
            "lie beyond the boundary, so this is a TRUNCATED SEARCH and NOT a report that the branch " +
            "has no resumption point. Deepen the checkout or raise the limit. Refusing rather than " +
            "trusting '$Base' unchecked - a cross-check that could not run must not look like one " +
            "that ran and agreed.")
    }

    if ($gapBase -and $gapDays -ge 180 -and $gapBase -ne $mergeBase) {
        throw ("Base disagreement. merge-base with '$Base' is $mergeBase, but this branch's history " +
               "shows a $gapDays-day pause ending at $gapBase, which is where the servicing effort " +
               "restarted. A diff against the wrong base reports branch divergence as work - that is " +
               "how a sibling line got 245 changed files and nearly reported a catastrophe.")
    }

    # Compare the merge base to the WORKING TREE, not to $Head.
    #
    # This originally used `git diff --name-only <base> <head>`, which compares two COMMITS and is
    # blind to uncommitted edits. The mutation test caught it: a comment appended to a src/ file was
    # not detected, and the gate exited 0 while the tree carried exactly the change it exists to
    # stop. An edit is dangerous when it is BUILT, which happens before it is committed.
    $tracked = @(& git diff --name-only $mergeBase --)
    $untracked = @(& git ls-files --others --exclude-standard --)
    $all = @($tracked + $untracked | Sort-Object -Unique)

    # LIVENESS CHECK, and deliberately named that rather than "positive control".
    #
    # It answers exactly one question: did the diff command produce anything at all? A zero here
    # means the command failed, not that the branch is empty, and every zero derived from it would
    # be meaningless.
    #
    # IT DOES NOT VALIDATE THE BASE, and the earlier name implied otherwise. Run against origin/main
    # this passed cheerfully with 364 changed files while the whole measurement was wrong - the
    # answer was live and unsound at the same time. 16.11's rule is what makes the distinction
    # sayable: a real cross-check needs one side from OUTSIDE the input, and both sides of this one
    # come from the same command. The base is validated by the shape derivation above, which uses no
    # ref at all; this only proves the instrument ran.
    #
    # A liveness check labelled as a control is its own instance of tonight's family: a plausible
    # reassurance about a question it cannot answer.
    if ($all.Count -eq 0) {
        throw "The diff against '$mergeBase' reported NO changed files at all. This branch has changes, so the command failed. A zero from a broken command is not a measurement."
    }

    return [pscustomobject]@{
        MergeBase = $mergeBase
        TotalChanged = $all.Count
        AllChanged = $all
        SourceFiles = @($all | Where-Object { $_ -like 'src/*' -or $_ -like 'shared/*' })
    }
}

# Only these extensions are compiled INTO an assembly, so only these can change its MVID directly.
# A .csproj or .config change can change what is compiled, which is why they are still declared -
# but the distinction matters when judging exposure, and 16.11's line has 6 changed src/ files and
# ZERO compiled ones while this line has one deliberate compiled change.
#
# .xlf IS ON THIS LIST AND IT WAS MISSING, which 16.11 found by applying "a directory is a proxy for
# a population, and proxies drift" to their own extension list. The same drift was here:
#
#   *.resources.dll in the compared population   364 of 462   79%
#   non-satellite                                 98          positive control
#   .xlf tracked by git                          238, ALL under src/
#   CULTURE-specific .resx tracked by git          0   <- generated during the build, not checked in
#
# The list tracked .resx. Right idea, wrong file: the culture .resx do not exist in this repository -
# the build writes them out of the .xlf. So the CHECKED-IN SOURCE for 79% of the compared assemblies
# was an extension this gate classified as a mere "build input", while it printed a complete-looking
# "compiled sources changed: 1 of 5".
#
# Zero .xlf have changed on this branch, so nothing was actually missed. Correct today, for no reason
# the gate could rely on - the same safety-by-luck as the linked-source case.
$CompiledExtensions = @('.cs', '.vb', '.fs', '.resx', '.xlf')

function Test-IsCompiled {
    param([string] $Path)
    return $CompiledExtensions -contains [IO.Path]::GetExtension($Path)
}

if ($List) {
    Write-Host "Declared product source changes: $($declared.Count)"
    foreach ($k in $declared.Keys) { Write-Host "  $k`n      $($declared[$k])" }
    exit 0
}

$result = Get-ChangedSourceFiles -Base $BaselineRef -Head $HeadRef

Write-Host "Baseline      : $BaselineRef  ($($result.MergeBase.Substring(0,9)))"
Write-Host "Total changed : $($result.TotalChanged) files   [liveness check - the diff ran; this does NOT validate the base]"
Write-Host "Under src/    : $($result.SourceFiles.Count)"

$undeclared = @($result.SourceFiles | Where-Object { -not $declared.Contains($_) })
$missing = @($declared.Keys | Where-Object { $_ -notin $result.SourceFiles })

foreach ($f in $result.SourceFiles) {
    $mark = if ($declared.Contains($f)) { 'declared  ' } else { 'UNDECLARED' }
    $kind = if (Test-IsCompiled $f) { 'COMPILED   ' } else { 'build input' }
    Write-Host "  $mark $kind $f"
}

$compiled = @($result.SourceFiles | Where-Object { Test-IsCompiled $_ })
Write-Host "Compiled sources changed: $($compiled.Count) of $($result.SourceFiles.Count)  [only these change an assembly's MVID directly]"

# REPORT WHAT THIS GATE DOES NOT COVER, because its scope and the acceptance criterion's scope are
# different sets and the difference was invisible until 16.11 measured it on their line.
#
# This gate is DIRECTORY-scoped: it asks "what changed under src/". The acceptance criterion is
# POPULATION-scoped: the null build hashes every assembly the compiler emits under artifacts\obj,
# and TestPlatform.sln contains test projects, so TEST ASSEMBLIES ARE IN THE COMPARED SET.
#
# Measured here: 36 of the compared assemblies are test assemblies, and this effort has changed
# files that feed them. So "0 UNDECLARED" is a true statement about src/ and NOT a statement that
# the retained reference set still describes this branch.
#
# It is not asserted, for a reason that is itself measured rather than assumed: the null build
# compares TWO BUILDS OF THE SAME TREE, so a test-code change appears identically on both sides and
# cannot make it fail. What a test-code change does affect is the RETAINED reference set, which a
# future servicing patch is diffed against - those assemblies will differ, and the explanation will
# be "we edited tests" rather than anything about the patch.
#
# Printed rather than gated because deciding that test changes must be declared is a policy question
# for the owners, and a gate that blocks on it would block the work this branch exists to do. What is
# not acceptable is the previous state: a clean number computed over a population that excludes its
# own subject.
$outsideScope = @($result.AllChanged | Where-Object { (Test-IsCompiled $_) -and $_ -notlike 'src/*' -and $_ -notlike 'shared/*' })
Write-Host ""
Write-Host "OUTSIDE THIS GATE'S SCOPE, reported not asserted: $($outsideScope.Count) compiled file(s) changed outside src/"
if ($outsideScope.Count -gt 0) {
    foreach ($f in ($outsideScope | Select-Object -First 12)) { Write-Host "  not covered  $f" }
    if ($outsideScope.Count -gt 12) { Write-Host "  ... and $($outsideScope.Count - 12) more" }
    Write-Host "  These feed test assemblies, which ARE in the null build's compared population."
    Write-Host "  They cannot make the null build fail - it compares two builds of the same tree - but they"
    Write-Host "  do mean the RETAINED reference set no longer matches this branch for those assemblies."
}

# A declared path that is no longer changed is reported, but does not fail: reverting a product
# change is the safe direction, and the entry is then stale rather than dangerous.
foreach ($f in $missing) {
    Write-Host "  stale     $f  (declared here but no longer differs from the baseline)"
}

if ($SelfTest) {
    # Call the gate's OWN detection function, not a parallel mechanism.
    #
    # The first version of this self-test planted a file and then looked for it with
    # `git status --porcelain`. It passed while the gate itself was blind, because the two used
    # different commands. A self-test that exercises a different code path than the thing it
    # certifies proves nothing about that thing - it only proves the probe was written.
    $probe = 'src/__scope_selftest_probe.cs'
    $full = Join-Path (& git rev-parse --show-toplevel) $probe
    # Uses the SAME $probeMarker the healer above matches on, so the two cannot drift apart.
    $probeMarker | Set-Content -LiteralPath $full -Encoding utf8
    try {
        $after = Get-ChangedSourceFiles -Base $BaselineRef -Head $HeadRef
        $detected = @($after.SourceFiles | Where-Object { -not $declared.Contains($_) }) -contains $probe
        Write-Host "SELF-TEST planted $probe -> seen by the gate as UNDECLARED: $detected"
        if (-not $detected) {
            Write-Host 'GUARD-RESULT PRODUCT SOURCE SCOPE: SELF-TEST FAILED - a planted file was not seen.'
            exit 1
        }
    }
    finally {
        Remove-Item -LiteralPath $full -Force -ErrorAction SilentlyContinue
    }

    # PROVE THE RESTORATION, do not eyeball it. 16.11's Test-SigningSetAssertion mutates the real
    # Sign.proj, confirms each rule fires alone, then restores the tree AND RE-ASSERTS. I had been
    # checking cleanup with git status by eye, which is a person verifying a machine - the same
    # defect as running a gate by hand. An experiment that leaves its subject dirty has contaminated
    # every measurement taken after it, and the failure would surface somewhere unrelated.
    $restored = Get-ChangedSourceFiles -Base $BaselineRef -Head $HeadRef
    $leftover = @($restored.SourceFiles | Where-Object { -not $declared.Contains($_) })
    Write-Host "SELF-TEST after cleanup -> undeclared src/ changes remaining: $($leftover.Count) (must be 0)"
    if ($leftover.Count -gt 0) {
        foreach ($l in $leftover) { Write-Host "      LEFTOVER $l" }
        Write-Host 'GUARD-RESULT PRODUCT SOURCE SCOPE: SELF-TEST FAILED - the probe was not fully removed.'
        exit 1
    }
}

if ($undeclared.Count -gt 0) {
    Write-Host ''
    Write-Host "GUARD-RESULT PRODUCT SOURCE SCOPE: $($undeclared.Count) UNDECLARED"
    Write-Host 'Each file above marked UNDECLARED changes a shipped assembly, including a comment-only'
    Write-Host 'edit, because deterministic compilation derives the MVID from the source text. Either'
    Write-Host 'revert it, or add it to $declared in this script with the reason it is worth that.'
    exit 1
}

Write-Host ''
Write-Host "GUARD-RESULT PRODUCT SOURCE SCOPE: 0 UNDECLARED  ($($result.SourceFiles.Count) declared, $($missing.Count) stale)"
exit 0
