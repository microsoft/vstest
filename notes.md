# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-10-10_

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
(none — all prior efficiency PRs closed/merged)

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
- **Run settings XML parsing**: parsed 5-6× per test run — not meeting the bar alone
- **XmlRunSettingsUtilities.ReaderSettings**: property creates new XmlReaderSettings on every call — MEDIUM fix
- **GetRunConfigurationNode**: called 15+ times per run, each parsing XML — MEDIUM impact
- **All hot-path allocations**: filter eval, IPC serialization/deserialization — fully optimized in prior runs
- **NuGet.Frameworks code**: vendored, out of scope for efficiency changes
- **MTP bridge** (new code): scanned multiple times — clean, no hot-path issues
- **CA1310/ordinal comparisons**: enabled in #16388 — already handled by maintainers
- **TRX logger**: cold path (post-test-completion), EqtBaseCollection uses legacy Hashtable but not hot path — not actionable

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| MEDIUM | Code | Run settings XML parsed 5-6× per test run — could be reduced to 1 pass | Requires API changes; saves maybe 2-5ms; may not meet bar |
| MEDIUM | Code | XmlRunSettingsUtilities.ReaderSettings property allocates new XmlReaderSettings per call (~15 startup call sites) | Easy fix; minor GC reduction |
| MEDIUM | Code | `RunSpecificTestsArgumentProcessor.DiscoveryRequest_OnDiscoveredTests` (src/vstest.console/Processors/RunSpecificTestsArgumentProcessor.cs:291-300) does nested loop: per discovered test, scans full `_selectedTestNames` list with culture-aware IndexOf. Only active with `--Tests` filter. O(discoveredTests × filterNames), uncached. | Only edge case (large filter lists + large suites) would show effect; typical filter lists are 1-10 names so unlikely to clear 15-20% bar |

## Backlog Cursor
- All key hot paths fully scanned (IPC serialization V1+V2, filter eval, discovery aggregator, test run cache, parallel runners)
- TestRequestManager startup path: fully scanned — XML parsing redundancy is the main finding (MEDIUM)
- MTP bridge code: scanned multiple times — no issues
- NuGet.Frameworks: vendored, skip
- TRX logger: scanned 2026-08-29 — cold path, no actionable findings
- **2026-09-12 scan**: Scanned CrossPlatEngine (ParallelRunDataAggregator, DiscovererEnumerator), Common, Client, vstest.console arg processors, and new TestIdsLogger (#16443) for O(n²) patterns. All nested-loop/Contains patterns found operate on small fixed-size collections (adapters, data collectors, search dirs), not test-count-scaled — LOW priority, no new backlog items. TestIdsLogger confirmed clean (StringBuilder-per-row, single linear pass) and opt-in (`--logger` flag), so not a default-path concern.
- **2026-09-19 scan**: No new commits since last run (repo HEAD unchanged at "Preserve OutputType for Android test projects (#16496)"). Delegated a fresh sub-agent scan of CommunicationUtilities, Common, vstest.console, testhost/testhost.x86, Client, ObjectModel, datacollector explicitly excluding already-confirmed-clean areas. Found one new MEDIUM item (see backlog table) and confirmed no HIGH-impact findings — no per-call Regex re-creation, no repeated GetTypes()/reflection scans, no blocking sync I/O on handshake path found beyond what's already known.
- Only 2 open perf/efficiency-tagged issues found: #15295 (MSBuild target optimization — still "State: In-PR" label, no new maintainer activity, not an application code path we'd touch) and #16433 (test parallelism/shared state — explicitly says "no speed to gain here", not an efficiency target). No new comments on either since last check.
- **2026-10-10 scan**: Only 1 commit since last run (05cd781, code-coverage dependency bump — no perf impact). No open efficiency-improver PRs to maintain (previous PR list all merged/closed). Checked #15295 (now has a merged-pending-fix PR #16043 addressing BuildInParallel + DesignTimeBuild skip — tracked, not our action) and #16433 (unchanged, still "no speed to gain" per author) — neither actionable. **Task 6 finding**: discovered the entire Acceptance.IntegrationTests `Performance/` test suite (12 files, ~1345 lines — PerformanceTests, ProtocolV1Tests, ProtocolV2Tests, SocketTests, PerfAnalyzer, TelemetryPerfTestBase, ExecutionPerfTests, DiscoveryPerfTests) is wrapped in `#if NETFRAMEWORK` but the csproj only targets `net11.0` (no net481/net472) — so NETFRAMEWORK is never defined and this code is dead/never compiled, not just `[Ignore]`d. CI's `--performanceTest` flag gives false confidence; no working perf regression signal exists for IPC protocol serialization, socket throughput, or e2e discovery/execution timing at the acceptance-test level. Filed as issue (see below) rather than fixing directly — requires maintainer decision between (a) multi-targeting + un-ignoring with tuned thresholds, (b) porting to modern TFM, or (c) deleting and relying on the working `CommunicationUtilities.UnitTests` Performance-category tests instead (which do multi-target `net11.0;net481` and are correctly wired through `eng/build.ps1`'s `TestCategory=Performance` filter). No Task 3 PR this run — Task 2 backlog remains MEDIUM-only; spent the run on Task 6 infrastructure gap instead, per prior run's recommendation.
- **2026-09-26 scan**: Only 1 commit since last run (8c8561a, "Mark the xxHash128 test id API as experimental (#16465)" — attribute-only change, no perf impact). No new open issues labeled `efficiency` or `Area: Performance` beyond the Monthly Activity issue itself. No open efficiency-improver PRs to maintain. Delegated a fresh sub-agent scan of previously-unscanned areas: HtmlLogger (XSLT/DataContractSerializer only run once at TestRunComplete, cold path — clean), TrxLogger (re-confirmed cold/post-completion path), BlameDataCollector (per-test handlers are O(1) ConcurrentQueue/Dictionary ops, dump-file loops bounded by small dump count on crash path only — clean), CoreUtilities (StringBuilderExtensions/StringExtensions/Helpers — simple O(1)/O(len) utilities, no issues), and remaining CrossPlatEngine subdirs (EventHandlers, DataCollection, Client, Execution — TestCaseEventsHandler, InProcDataCollectionExtensionManager/Sink, TestLoggerManager, ParallelOperationManager, ProxyDataCollectionManager, BaseRunTests — all O(1) per-test-result ops bounded by adapter/logger/collector count, not test count). No new HIGH or MEDIUM findings. Backlog unchanged (still MEDIUM-only).
- Next area to investigate: any new code areas added in upcoming commits; otherwise backlog is nearly exhausted for this codebase snapshot — consider Task 6 (measurement infrastructure) on a future run if Task 2 keeps returning empty.
- **2026-10-03 scan**: Only 1 commit since last run (920d069, "Branding as 18.13.0 (#16538)" — version bump, no perf impact). No open efficiency-improver PRs. Delegated a fresh sub-agent scan of the last remaining unscanned src dirs: AdapterUtilities, Build, Execution.Shared, Filter.Source, Hashing.Source, PlatformAbstractions, TestHostProvider, Utilities, VsTestConsole.TranslationLayer, SettingsMigrator, DataCollectors, AttachVS. No new HIGH findings — all loops bounded by small/fixed N (XML attrs, event-log entries, MSBuild task settings). Confirmed existing MEDIUM items (XmlRunSettingsUtilities.ReaderSettings per-call allocation; runsettings XML re-parsed across RunSettingsArgumentProcessor/TestRequestManager/InferRunSettingsHelper/EnableCodeCoverageArgumentProcessor) still accurate, no change in classification. Checked open issues for efficiency signal: #16379 (TestCaseFilter ignored under MTP bridge — functional bug, not efficiency), #15295 and #16433 unchanged (previously assessed, not actionable). Virtually the entire codebase has now been scanned across ~8 runs with only MEDIUM-or-below findings remaining — backlog essentially exhausted. Recommend next run prioritize Task 6 (measurement infrastructure) since Task 2 has returned empty for 3 consecutive runs.

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — CLOSED 2026-07-03
- Issue #16211: [efficiency-improver] Monthly Activity 2026-07 — CLOSED 2026-08-01
- Issue #16332: [efficiency-improver] Monthly Activity 2026-08 — CLOSED 2026-09-12
- Issue #16479: [efficiency-improver] Monthly Activity 2026-09 — CLOSED 2026-10-03 (month rollover)
- Issue #16549: [efficiency-improver] Monthly Activity 2026-10 — active
- Last run: 2026-10-10 (run ID 38068848933)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
