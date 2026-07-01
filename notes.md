# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-07-01_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Release`
- Run tests for a project: `.dotnet/dotnet test <test-project>.csproj -c Release --no-build -f net11.0`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it

## PR Status

### Open PRs
- **PR #aw_getRawText** (GetRawText elimination in 9 converters): OPEN — created 2026-07-01 (run 28535407444). Branch: `efficiency/avoid-getRawText-alloc-in-deserialize`
- **PR #aw_discAgg** (DiscoveryDataAggregator single-element array): OPEN — created 2026-07-01 (run 28535407444). Branch: `efficiency/discovery-aggregator-avoid-single-element-array`

### Merged PRs (all confirmed)
- PR #16193: v2 serialization Guid.ToString elimination (MERGED — confirmed in git log)
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
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED 2026-06-30 — CI was cancelled by Azure DevOps transient infra failure; re-attempted as #aw_discAgg)

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — optimized in #16193 ✅
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. IPC deserialize: GetRawText() in 9 converters — PR #aw_getRawText pending ⏳
  6. Discovery: `DiscoveryResultCache.AddTest` → `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — PR #aw_discAgg pending ⏳

- **GetRawText() pattern**: StjSafe.Deserialize<T>(element.GetRawText(), options) wastes 1 alloc + 1 reparse vs Deserialize<T>(element, options) which uses the pre-parsed token buffer directly
- **GetRawText().Trim('"') pattern**: Used in 6 files when `ValueKind != JsonValueKind.String`. The Trim is a no-op for non-string JSON values. However, GetRawText() itself is still needed to convert the JSON token to a string representation. No easy win here without deeper refactoring.
- **Utf8JsonWriter native overloads**: `WriteString(string, Guid)` etc. avoid ToString allocations (used in PR #16193)
- **TestProperty.Register/Find global lock**: potential contention under parallel discovery, but HashSet<Type> inside precludes simple ConcurrentDictionary swap; not worth changing
- **GitHubAPI note**: GitHub MCP tools fail with 403 (fine-grained token > 8 days). Use safeoutputs for writes; PRs/issues not directly verifiable via API.

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| PENDING | Code | PR #aw_getRawText: GetRawText→Deserialize(JsonElement) in 9 converters | Awaiting CI |
| PENDING | Code | PR #aw_discAgg: DiscoveryDataAggregator single-element arrays | Awaiting CI (prev #16177 closed by CI infra failure) |
| MEDIUM | Code | TestResultConverter (v1) Duration.ToString() | v1 protocol only (legacy) |
| MEDIUM | Code | TestObjectBaseConverter.WritePropertyValue TimeSpan case: ts.ToString() | Called for custom props only |
| LOW | Code | MSTestV1TelemetryHelper ContainsKey+[] double-hash | MSTestV1 only |

## Backlog Cursor
- CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization, and Filter.Source: fully scanned
- TrxLogger: scanned — main opportunities are one-per-run, not per-test; low impact
- ObjectModel: StoreKeyValuePairs.get ToList() is serialization-only; CacheLazyValuesOnSerializing ToArray() is serialization-only
- Remaining unexplored: DataCollectors internals, CrossPlatEngine/Parallel (beyond DiscoveryDataAggregator), CrossPlatEngine/Execution (thread/job queue areas beyond ManualResetEventSlim)
- GetRawText().Trim('"') pattern (6 files): no easy win; deferred

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — last updated 2026-06-30; needs closing
- Issue #aw_monthlyJul: [efficiency-improver] Monthly Activity 2026-07 — created 2026-07-01 (run 28535407444)
- Last run: 2026-07-01 (run ID 28535407444)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
