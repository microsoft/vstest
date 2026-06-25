# Efficiency Improver — vstest Repo Memory

## Build/Test Commands

- **Build (Debug):** `.dotnet/dotnet build <project.csproj> -c Debug`
- **Full build:** `./build.sh` (downloads SDK if needed; slow first time)
- **Run unit tests:** `./test.sh -p <pattern>`
- **Full build (CI-like):** `./build.sh -c Release`
- Net test project TFMs: typically `net11.0` and `net481`; check per project csproj

## Efficiency Notes

- `FastFilter.Evaluate` is the hot path for test filtering — called once per test case when a filter is active.
- `ImmutableDictionary` (used for `FilterProperties`) does NOT have `Deconstruct` on net462/netstandard2.0; use `kvp.Key` / `kvp.Value` pattern.
- `TestRunCache.CheckForCacheHit` called on every `OnTestStarted` and `OnNewTestResult` — the execution hot path.
- `DiscoveryResultCache.AddTest` called on every discovered test case — the discovery hot path.
- `LengthPrefixCommunicationChannel.Send` and `NotifyDataAvailable` — fixed in PR #16147 (MERGED 2026-06-23): `Task.CompletedTask` instead of `Task.FromResult(0)`.
- `JobQueue` in `CoreUtilities` — fixed in PR #16150 (MERGED 2026-06-23): `ManualResetEventSlim` instead of `ManualResetEvent` for `_jobAdded`, `_queueProcessing`, and `Flush()` wait event.
- `FastFilter.Evaluate`: PR #16139 closed by maintainer (nohwnd comment: "not worth it with the other allocations it introduces") — the `ValidForProperties` ToArray change introduced an allocation in the success path. New PR (efficiency/fastfilter-no-closure-no-double-lookup) targets ONLY Evaluate: kvp iteration + foreach instead of Any(lambda), avoids ToArray change entirely.
- Filter source files (`src/Microsoft.TestPlatform.Filter.Source/`) compiled into `Microsoft.TestPlatform.Common` via explicit `<Compile Include=...>` references.
- Tests live in `test/Microsoft.TestPlatform.Common.UnitTests/` and `test/Microsoft.TestPlatform.Filter.Source.UnitTests/`.
- CrossPlatEngine tests: `test/Microsoft.TestPlatform.CrossPlatEngine.UnitTests/` — TFMs: `net11.0;net481`.
- Client `ManualResetEvent` in `TestRunRequest._runCompletionEvent` and `DiscoveryRequest._discoveryCompleted` are long-duration per-run waits — NOT hot paths; not worth changing.
- `JsoniteConvert.DeserializeTestCase` and `DeserializeTestResult`: fixed in PR #aw_pr_ipc_deser (2026-06-25) — replaced `ContainsKey`+`TryGetValue` double-hash with single `TryGetValue`; eliminates ~20K redundant hash lookups per 10K-test run.

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| HIGH | Build | Issue #15295 / PR #16043: `MsCoverageReferencedPathMaps` — PR already open by maintainers; monitor for merge | HIGH |
| LOW | Code-Level | `Condition.GetPropertyValue` allocates `string[1]` per non-array property in slow-filter path | LOW |
| LOW | Code-Level | `MsTestV1TelemetryHelper.AddTelemetry`: `ContainsKey` + `[]` double-hash (MSTest v1 users only; not a priority) | LOW |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | #16139 (closed, not merged) | FastFilter: redundant dict lookups + ValidForProperties — CLOSED by maintainer (ToArray allocation concern) |
| 2026-06-20 | #16144 (MERGED) | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |
| 2026-06-21 | #16147 (MERGED) | Replace Task.FromResult(0) with Task.CompletedTask in IPC channel |
| 2026-06-22 | #16150 (MERGED) | Replace ManualResetEvent with ManualResetEventSlim in JobQueue |
| 2026-06-23 | #16160 (MERGED) | FastFilter.Evaluate: foreach kvp + foreach instead of Any(lambda); NO ValidForProperties change |
| 2026-06-24 | #16165 (open, CI green) | Pre-allocate List<T>(InitialCapacity) in DiscoveryResultCache+TestRunCache; remove Collection<T> virtual-method layer |
| 2026-06-25 | #aw_pr_ipc_deser (open, CI pending) | JsoniteConvert: replace ContainsKey+TryGetValue with single TryGetValue in DeserializeTestCase+DeserializeTestResult |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/` (JsoniteConvert hot path fixed), `src/Microsoft.TestPlatform.CoreUtilities/`, `src/Microsoft.TestPlatform.Client/` (partial — ManualResetEvent usage checked), `src/Microsoft.TestPlatform.Common/` (partial: InternalTestLoggerEvents scanned, ExtensionFramework init paths — not hot), `src/vstest.console/` (orchestration — init-time only, not hot paths)
- Next to scan: deeper scan of `src/Microsoft.TestPlatform.CommunicationUtilities/` serialization path for additional hot-path opportunities; `src/Microsoft.TestPlatform.CrossPlatEngine/Execution/` for other per-test-result patterns

## Last Run

- 2026-06-25: Task 4 (PR #16165 all CI green, mergeable), Task 2 (scanned JsoniteConvert — found ContainsKey+TryGetValue double-hash in IPC deserialization), Task 3 (created PR ipc-deserialize-single-lookup: eliminates ~20K redundant hash lookups per 10K-test run; 630/630 tests pass), Task 7 (monthly summary updated: PR #16165 number fixed, new PR #aw_pr_ipc_deser added)
