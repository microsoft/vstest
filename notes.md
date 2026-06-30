# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-06-30_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Release`
- Run tests for a project: `.dotnet/dotnet run --project test/<ProjectName>/<ProjectName>.csproj -c Release --no-build --framework net11.0`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it

## PR Status

### Open PRs
- **PR #16193** (v2 serialization ToString elimination): OPEN — created 2026-06-29 (run 28390618588). Branch: `efficiency/serialization-avoid-tostring-alloc-v2-converters`
- **PR #aw_pr_rawtext** (GetRawText elimination in 9 converters): OPEN — created 2026-06-30 (run 28462807343). Branch: `efficiency/avoid-getRawText-alloc-in-deserialize`

### Merged PRs (all confirmed)
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
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED 2026-06-30 — CI was cancelled by Azure DevOps transient infra failure on 2026-06-29; not merged; opportunity still valid)

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — PR #16193 pending ⏳
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. IPC deserialize: GetRawText() in 9 converters — PR #aw_pr_rawtext pending ⏳
  6. Discovery: `DiscoveryResultCache.AddTest` → `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — PR #16177 closed; opportunity still open

- **GetRawText() pattern**: StjSafe.Deserialize<T>(element.GetRawText(), options) wastes 1 alloc + 1 reparse vs Deserialize<T>(element, options) which uses the pre-parsed token buffer directly
- **Utf8JsonWriter native overloads**: `WriteString(string, Guid)` and `WriteString(string, ReadOnlySpan<char>)` avoid ToString allocations; both available since .NET Core 3.0
- **TestProperty.Register/Find global lock**: potential contention under parallel discovery, but HashSet<Type> inside precludes simple ConcurrentDictionary swap; not worth changing
- **MSTestV1TelemetryHelper**: ContainsKey + [] double-hash (lines 70-77); MSTestV1-only, LOW priority

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| HIGH | Code | DiscoveryDataAggregator `new[] { source }` in MarkSourcesBasedOnDiscoveredTestCases | PR #16177 closed (CI failure); re-attempt with fresh branch |
| PENDING | Code | PR #16193: v2 serializer ToString alloc | Awaiting CI |
| PENDING | Code | PR #aw_pr_rawtext: GetRawText→Deserialize(JsonElement) in 9 converters | Awaiting CI |
| MEDIUM | Code | TestResultConverter (v1) Duration.ToString() | v1 protocol only (legacy) |
| MEDIUM | Code | TestObjectBaseConverter.WritePropertyValue TimeSpan case: ts.ToString() | Called for custom props only |
| LOW | Code | MSTestV1TelemetryHelper ContainsKey+[] double-hash | MSTestV1 only |

## Backlog Cursor
- CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization, and Filter.Source: fully scanned
- TrxLogger: scanned (Converter.cs, TrxLogger.cs) — main opportunities are one-per-run, not per-test; low impact
- ObjectModel: StoreKeyValuePairs.get ToList() is serialization-only; CacheLazyValuesOnSerializing ToArray() is serialization-only
- Remaining unexplored: DataCollectors internals, CrossPlatEngine/Parallel, CrossPlatEngine/Execution (thread/job queue areas beyond ManualResetEventSlim)

## Monthly Activity Issue
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — updated 2026-06-30 (this run)
- Last run: 2026-06-30 (run ID 28462807343)
