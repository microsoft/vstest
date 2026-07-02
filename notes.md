# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-07-02_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Release`
- Run tests for a project: `.dotnet/dotnet test <test-project>.csproj -c Release -f net11.0`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it

## PR Status

### Open PRs
- **PR #aw_tsalloc** (TimeSpan.ToString()/Guid.ToString() elimination in serializers): OPEN — created 2026-07-02 (run 28608200346). Branch: `efficiency/avoid-timespan-tostring-in-serializers`

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
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED 2026-06-30 — CI was cancelled by Azure DevOps transient infra failure)

### Failed PR Creations (previous run 28535407444)
- PR #aw_getRawText (GetRawText elimination in 9 converters): NOT CREATED on GitHub — no open PR exists with this content as of 2026-07-02; all efficiency-improver PRs confirmed ≤ #16193
- PR #aw_discAgg (DiscoveryDataAggregator single-element array): NOT CREATED on GitHub — same finding

## Efficiency Notes (Key Insights)
- **Hot-path hierarchy** (frequency per test case, highest first):
  1. Filter eval: `FilterExpression.Evaluate` → `FastFilter.Evaluate` or `Condition.Evaluate` — all optimized ✅
  2. Test result: `TestRunCache.OnNewTestResult` → stats update — optimized ✅
  3. IPC write: `TestCaseConverterV2.Write`, `TestResultConverterV2.Write` — Guid.ToString optimized in #16193 ✅; TimeSpan.ToString optimized in #aw_tsalloc ✅
  4. IPC read: `JsoniteConvert.DeserializeTestCase/Result` — optimized ✅
  5. IPC deserialize: GetRawText() in 15 places across 10 serialization converters — UNIMPLEMENTED (previous run PR failed)
  6. Discovery: `DiscoveryResultCache.AddTest` → `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases` — UNIMPLEMENTED (previous run PR failed)

- **GetRawText() pattern**: StjSafe.Deserialize<T>(element.GetRawText(), options) wastes 1 alloc + 1 reparse vs Deserialize<T>(element, options). StjSafe has both overloads.
- **GetRawText().Trim('"') pattern**: no easy win; deferred
- **TimeSpan.TryFormat**: use `format: default, formatProvider: CultureInfo.InvariantCulture` to satisfy CA1305. Produces identical "c" format as ToString().
- **Utf8JsonWriter native overloads**: WriteStringValue(Guid), WriteString(string, Guid), WriteStringValue(ReadOnlySpan<char>) all avoid ToString allocations
- **GitHubAPI note**: GitHub MCP tools fail with 403 (fine-grained token > 8 days). Use safeoutputs for writes; PRs/issues not directly verifiable via API. list_pull_requests returns 403; search_pull_requests works for label: queries.

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| HIGH | Code | GetRawText→Deserialize(JsonElement): 15 occurrences in TestCaseConverter, TestObjectBaseConverter, AttachmentConverters, ExceptionConverter, TestRunChangedEventArgsConverter, TestRunCompleteEventArgsConverter, TestExecutionContextConverter, AfterTestRunEndResultConverter, DiscoveryCriteriaConverter, TestResultConverter/V2/CaseConverterV2/ObjectConverter | Next PR |
| HIGH | Code | DiscoveryDataAggregator: eliminate string[1] alloc per test case in source tracking | Next PR after GetRawText |
| MEDIUM | Code | TestRunStatisticsConverter: TestOutcome.ToString() in WriteNumber (once per run) | Low impact |
| LOW | Code | MSTestV1TelemetryHelper ContainsKey+[] double-hash | MSTestV1 only |

## Backlog Cursor
- CrossPlatEngine/Execution, CrossPlatEngine/Discovery, CommunicationUtilities/Serialization, and Filter.Source: fully scanned
- TrxLogger: scanned — main opportunities are one-per-run, not per-test; low impact
- ObjectModel: serialization-only patterns; low impact
- Remaining unexplored: DataCollectors internals, CrossPlatEngine/Parallel (beyond DiscoveryDataAggregator)
- GetRawText().Trim('"') pattern (6 files): no easy win; deferred

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — needs closing
- Issue #aw_july26: [efficiency-improver] Monthly Activity 2026-07 — created 2026-07-02 (run 28608200346)
- Last run: 2026-07-02 (run ID 28608200346)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
