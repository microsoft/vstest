# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-07-18_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Release`
- Run specific test project: `.dotnet/dotnet run --project test/<proj>/<proj>.csproj -c Release --no-build -f net11.0 -- --filter "TestName"`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it
- Note: `test.sh -p <pattern>` runs ALL tests, not just matching ones; use `dotnet run` on specific project instead

## PR Status

### Open PRs
(none — all prior efficiency PRs closed)

### Merged PRs (all confirmed)
- PR #16210: Eliminate GetRawText() string alloc across 9 STJ deserializer converters (MERGED 2026-07-09)
- PR #16193: v2 serialization Guid.ToString elimination (MERGED 2026-07-01)
- PR #16144: DateTime.Now → UtcNow (MERGED)
- PR #16147: Task.FromResult(0) → Task.CompletedTask (MERGED)
- PR #16150: ManualResetEvent → ManualResetEventSlim (MERGED)
- PR #16160: FastFilter.Evaluate closure/double-lookup elimination (MERGED)
- PR #16165: List pre-allocation in DiscoveryResultCache + TestRunCache (MERGED)
- PR #16170: ContainsKey+TryGetValue → single TryGetValue in JsoniteConvert (MERGED)
- PR #16179: Condition.Evaluate string[1] fast-path (MERGED)
- PR #16182: FilterExpression.Evaluate leaf-node short-circuit (MERGED)

### Closed/Rejected PRs
- PR #16139: ImmutableDictionary redundant lookups (CLOSED — ToArray allocation concern)
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED — superseded)
- PR #16213: DiscoveryDataAggregator O(N) patterns (CLOSED — too small a win)
- PR #16216: Duration.ToString / Guid.ToString allocs (CLOSED — too small a win)
- PR #16222: TryGetGuid/TryGetDateTimeOffset (CLOSED — too small a win)

## Maintainer Instructions (Issue #16229)
- Weekly runs only; ≥15-20% measurable improvement required
- Only HIGH-impact items actionable; MEDIUM goes to backlog only
- Focus on fixed per-invocation overhead, not per-test micro-opts
- Common case is 1 test; ~90% runs <1000 tests
- O(n²) always in scope regardless of N

## Efficiency Notes (Key Insights)
- **Workload profile**: single test is most common; total run time ~400ms-1.5s
- **Run settings XML parsing**: parsed 5-6× per test run (TestRequestManager.EnsureSettingsAreInitialized does 3 parses, AddFakesConfigurationToRunsettings does 2 more). Each parse is <1ms for typical settings — not meeting the bar alone.
- **XmlRunSettingsUtilities.ReaderSettings**: property creates new XmlReaderSettings on every call — ~15 call sites in startup path. Easy MEDIUM fix (make static readonly), not HIGH.
- **GetRunConfigurationNode**: called 15+ times per run, each parsing XML. Caching would be MEDIUM impact.
- **DiscovererEnumerator.GetDiscovererToSourcesMap**: has .Except().ToList() in outer loop, but N is tiny (< 10).
- **TestRunCache.OnTestCompletion**: fallback linear scan for test ID matching — edge case only.
- **All hot-path allocations**: filter eval, IPC serialization/deserialization — fully optimized in prior runs.
- **Big wins likely only in**: reducing XML parse count (bundle into single pass), testhost R2R compilation.

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| MEDIUM | Code | Run settings XML parsed 5-6× per test run — could be reduced to 1 pass | Requires API changes; saves maybe 2-5ms; may not meet bar |
| MEDIUM | Code | XmlRunSettingsUtilities.ReaderSettings property allocates new XmlReaderSettings per call (~15 startup call sites) | Easy fix; minor GC reduction |
| LOW | Code | TestRunStatisticsConverter: TestOutcome.ToString() in WriteNumber (once per run) | Very low impact |

## Backlog Cursor
- All key hot paths fully scanned (IPC serialization V1+V2, filter eval, discovery aggregator, test run cache, parallel runners)
- TestRequestManager startup path: fully scanned — XML parsing redundancy is the main finding (MEDIUM)
- InferHelper, TestPluginCache, FakesUtilities, TestLoggerManager, DiscovererEnumerator: scanned — no high-impact issues found
- TestPlatform.cs CreateDiscoveryRequest/CreateTestRunRequest: scanned — multiple GetRunConfigurationNode calls (MEDIUM)
- Unexplored: testhost process launch optimization (R2R compilation)
- **Status after 2026-07-18 scan**: No new HIGH-impact items found. Codebase effectively fully optimised for current hot paths.

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — CLOSED 2026-07-03
- Issue #16211: [efficiency-improver] Monthly Activity 2026-07 — created 2026-07-03, active
- Last run: 2026-07-18 (run ID 29652868130)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
