# Efficiency Improver — vstest Repo Memory

## Build/Test Commands

- **Build (Debug):** `.dotnet/dotnet build <project.csproj> -c Debug`
- **Full build:** `./build.sh` (downloads SDK if needed; slow first time)
- **Run unit tests:** `.dotnet/dotnet run --project <test.csproj> -f net11.0 -- --filter "<pattern>"`
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

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| HIGH | Build | Issue #15295 / PR #16043: `MsCoverageReferencedPathMaps` — PR already open by maintainers; monitor for merge | HIGH |
| LOW | Code-Level | `Condition.GetPropertyValue` allocates `string[1]` per non-array property in slow-filter path | LOW |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | #16139 (closed, not merged) | FastFilter: redundant dict lookups + ValidForProperties — CLOSED by maintainer (ToArray allocation concern) |
| 2026-06-20 | #16144 (MERGED) | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |
| 2026-06-21 | #16147 (MERGED) | Replace Task.FromResult(0) with Task.CompletedTask in IPC channel |
| 2026-06-22 | #16150 (MERGED) | Replace ManualResetEvent with ManualResetEventSlim in JobQueue |
| 2026-06-23 | efficiency/fastfilter-no-closure-no-double-lookup (PR pending) | FastFilter.Evaluate: foreach kvp + foreach instead of Any(lambda); NO ValidForProperties change |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/`, `src/Microsoft.TestPlatform.CoreUtilities/`, `src/Microsoft.TestPlatform.Client/` (partial — ManualResetEvent usage checked), `src/Microsoft.TestPlatform.Common/` (partial: InternalTestLoggerEvents scanned — queue-based, no new hot paths)
- Next to scan: `src/vstest.console/` orchestration paths, deeper scan of `src/Microsoft.TestPlatform.Common/ExtensionFramework/` plugin discovery paths

## Last Run

- 2026-06-23: Task 2 (scanned Common/CrossPlatEngine/vstest.console partial), Task 3 (new FastFilter PR: efficiency/fastfilter-no-closure-no-double-lookup — kvp iteration + foreach eliminates closure allocations; tests 108+45 pass), Task 4 (confirmed PRs #16144, #16147, #16150 all MERGED; PR #16139 closed by maintainer), Task 7 (monthly summary updated)
