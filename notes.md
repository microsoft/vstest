# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-07-06_

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
- **PR #16213**: Eliminate O(N) ConcurrentDictionary.AddOrUpdate + array allocs in DiscoveryDataAggregator hot path: OPEN — all CI green ✅ — created 2026-07-04. Branch: `efficiency/discovery-source-tracking-opt`
- **PR #16216**: Eliminate Duration.ToString() and Guid.ToString() allocations in IPC serializers: OPEN — all CI green ✅ — created 2026-07-05. Branch: `efficiency/eliminate-duration-tostring-in-serializers`
- **PR TBD** (created 2026-07-06): Eliminate GetString()-then-parse in V2 IPC deserializers (TryGetGuid, TryGetDateTimeOffset) — 3 string allocs per test eliminated. Branch: `efficiency/eliminate-getstring-parse-in-v2-converters`. CI pending.

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
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED 2026-06-30 — closed as draft; superseded by #16213)

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — Guid.ToString + Duration.ToString optimized ✅ (V1 converters also fixed in #16216)
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. IPC deserialize V2: GetRawText() in 9 Deserialize calls → OPEN in #16210; GetString()-then-parse for Guid+DateTimeOffset → OPEN in TBD PR
  6. Discovery: `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — O(N)→O(1) per source in #16213 ✅

- **GetRawText().Trim('"') pattern**: 5 remaining sites (no easy win; deferred)
- **TimeSpan.TryFormat**: use `format: default, formatProvider: CultureInfo.InvariantCulture` to satisfy CA1305
- **Utf8JsonWriter native overloads**: WriteStringValue(Guid), WriteString(string, Guid) avoid ToString allocations; WriteString(string, ReadOnlySpan<char>) + TryFormat avoids Duration.ToString alloc
- **JsonElement typed accessors**: TryGetGuid(), TryGetDateTimeOffset() avoid GetString()-then-parse allocations; both available since .NET Core 3.0 — safe under #if NETCOREAPP
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
- CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization (all V1+V2 converters including JsoniteConvert), and Filter.Source: fully scanned
- TrxLogger: scanned — main opportunities are one-per-run, not per-test; low impact
- ObjectModel: serialization-only patterns; low impact
- JsoniteConvert.cs (#if !NETCOREAPP): has remaining Duration.ToString() + Guid.ToString() patterns but only runs on net48x hosts
- MTP path (Microsoft.Testing.Platform adapters): scanned — no hot-path allocations found
- DataCollectors internals: not yet scanned; likely low impact (not per-test-result)
- CrossPlatEngine/Parallel (beyond DiscoveryDataAggregator): scanned ParallelRunDataAggregator — no significant per-test hot paths

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — CLOSED 2026-07-03
- Issue #16211: [efficiency-improver] Monthly Activity 2026-07 — created 2026-07-03, updated 2026-07-06
- Last run: 2026-07-06 (run ID 28810618351)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
