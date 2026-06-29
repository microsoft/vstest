# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-06-29_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Debug`
- Run unit tests by pattern: `./test.sh -p <pattern>`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it

## PR Status

### Open PRs
- **PR #16177** (DiscoveryDataAggregator string[1] elimination): OPEN — CI `Build Windows Release` was CANCELLED (transient AzDO infra). No code issues. Added comment 2026-06-29 requesting re-trigger. Branch: `efficiency/discovery-single-source-no-array-alloc-427a6270783bc05c`
- **PR #aw_pr_serial** (v2 serialization ToString elimination): CREATED 2026-06-29 — eliminates `Guid.ToString()` in `TestCaseConverterV2.Write` and `TimeSpan.ToString()` in `TestResultConverterV2.Write`. Branch: `efficiency/serialization-avoid-tostring-alloc-v2-converters`

### Merged PRs (all confirmed)
- PR #16144: DateTime.Now → UtcNow (MERGED)
- PR #16147: Task.FromResult(0) → Task.CompletedTask (MERGED)
- PR #16150: ManualResetEvent → ManualResetEventSlim (MERGED)
- PR #16160: FastFilter.Evaluate closure/double-lookup elimination (MERGED)
- PR #16165: List pre-allocation in DiscoveryResultCache + TestRunCache (MERGED)
- PR #16170: ContainsKey+TryGetValue → single TryGetValue in JsoniteConvert (MERGED)
- PR #16179: Condition.Evaluate string[1] fast-path (MERGED 2026-06-29)
- PR #16182: FilterExpression.Evaluate leaf-node short-circuit (MERGED 2026-06-29)

### Closed PRs
- PR #16139: ImmutableDictionary redundant lookups (CLOSED by maintainer — ToArray allocation concern)

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — v2 serialization; just optimized ✅
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. Discovery: `DiscoveryResultCache.AddTest` → `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — PR #16177 pending

- **TestProperty.Register/Find global lock**: potential contention under parallel discovery, but HashSet<Type> inside precludes simple ConcurrentDictionary swap; not worth changing
- **MSTestV1TelemetryHelper**: ContainsKey + [] double-hash (lines 70-77); MSTestV1-only, LOW priority
- **Utf8JsonWriter native overloads**: `WriteString(string, Guid)` and `WriteString(string, ReadOnlySpan<char>)` avoid ToString allocations; both available since .NET Core 3.0

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| HIGH | Build | PR #16043: MsCoverageReferencedPathMaps — maintainers' own PR | Monitor for merge |
| PENDING | Code | PR #16177: DiscoveryDataAggregator string[1] | Awaiting CI re-trigger |
| PENDING | Code | PR #aw_pr_serial: v2 serializer ToString alloc | Awaiting CI |
| LOW | Code | MSTestV1TelemetryHelper ContainsKey+[] double-hash | MSTestV1 only |

## Backlog Cursor
- All main hot paths in CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization, and Filter.Source have been scanned
- Remaining unexplored: ObjectModel internals, TestLoggers, DataCollectors
- Low-value remaining: TestObjectBaseConverter.WritePropertyValue (char/TimeSpan paths), TestObject.CacheLazyValuesOnSerializing (serialization-only, not per-test)

## Monthly Activity Issue
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — updated 2026-06-29
- Last run: 2026-06-29 (this run)
