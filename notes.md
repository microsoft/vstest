# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-07-04_

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
- **PR #16210**: Eliminate GetRawText() string alloc across 9 STJ deserializer converters: OPEN — all CI green ✅ — created 2026-07-03. Branch: `efficiency/eliminate-getrawtext-in-serializers`
- **PR #aw_discAgg**: Eliminate O(N) ConcurrentDictionary.AddOrUpdate + array allocs in DiscoveryDataAggregator hot path: OPEN — created 2026-07-04. Branch: `efficiency/discovery-source-tracking-opt`

### Merged PRs (all confirmed)
- PR #16193: v2 serialization Guid.ToString elimination (MERGED 2026-07-01)
- PR #16144: DateTime.Now → UtcNow (MERGED)
- PR #16147: Task.FromResult(0) → Task.CompletedTask (MERGED)
- PR #16150: ManualResetEvent → ManualResetEventSlim (MERGED)
- PR #16160: FastFilter.Evaluate closure/double-lookup elimination (MERGED)
- PR #16165: List pre-allocation in DiscoveryResultCache + TestRunCache (MERGED)
- PR #16170: ContainsKey+TryGetValue → single TryGetValue in JsoniteConvert (MERGED)
- PR #16179: Condition.Evaluate string[1] fast-path (MERGED)
- PR #16182: FilterExpression.Evaluate leaf-node short-circuit (MERGED)

### Closed PRs
- PR #16139: ImmutableDictionary redundant lookups (CLOSED by maintainer — ToArray allocation concern)
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED 2026-06-30 — closed as draft; now superseded by #aw_discAgg)

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — Guid.ToString optimized in #16193 ✅
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. IPC deserialize: GetRawText() in 9 Deserialize calls → OPEN in #16210
  6. Discovery: `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — O(N)→O(1) per source in #aw_discAgg ✅

- **GetRawText().Trim('"') pattern**: 5 remaining sites (no easy win; deferred)
- **TimeSpan.TryFormat**: use `format: default, formatProvider: CultureInfo.InvariantCulture` to satisfy CA1305
- **Utf8JsonWriter native overloads**: WriteStringValue(Guid), WriteString(string, Guid) avoid ToString allocations
- **GitHubAPI note**: Use safeoutputs for writes; list_pull_requests may return large output — use search_pull_requests for filtered queries
- **test.sh -p pattern**: runs ALL test projects, not just matching ones; use `dotnet run --project test/<proj>.csproj -f net11.0` for targeted runs
- **8 pre-existing failures** in CrossPlatEngine tests on Linux: Windows-path tests (C:\...) — NOT caused by our changes

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| MEDIUM | Code | TestRunStatisticsConverter: TestOutcome.ToString() in WriteNumber (once per run) | Low impact |
| LOW | Code | GetRawText().Trim('"') pattern (5 remaining sites) | Deferred |
| LOW | Code | MSTestV1TelemetryHelper ContainsKey+[] double-hash | MSTestV1 only |

## Backlog Cursor
- CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization, and Filter.Source: fully scanned
- TrxLogger: scanned — main opportunities are one-per-run, not per-test; low impact
- ObjectModel: serialization-only patterns; low impact
- Remaining unexplored: DataCollectors internals, CrossPlatEngine/Parallel (beyond DiscoveryDataAggregator)

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — CLOSED 2026-07-03
- Issue #16211: [efficiency-improver] Monthly Activity 2026-07 — created 2026-07-03, updated 2026-07-04
- Last run: 2026-07-04 (run ID 28713232251)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
