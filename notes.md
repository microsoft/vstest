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
- `LengthPrefixCommunicationChannel.Send` and `NotifyDataAvailable` — fixed in PR #16147: `Task.CompletedTask` instead of `Task.FromResult(0)`.
- `JobQueue` in `CoreUtilities` — fixed in PR (efficiency/job-queue-mre-slim): `ManualResetEventSlim` instead of `ManualResetEvent` for `_jobAdded`, `_queueProcessing`, and `Flush()` wait event.
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
| 2026-06-19 | #16139 | FastFilter: avoid redundant dictionary lookups in Evaluate + single-pass ValidForProperties scan |
| 2026-06-20 | #16144 | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |
| 2026-06-21 | #16147 | Replace Task.FromResult(0) with Task.CompletedTask in IPC channel (LengthPrefixCommunicationChannel + TcpClientExtensions) |
| 2026-06-22 | TBD (efficiency/job-queue-mre-slim) | Replace ManualResetEvent with ManualResetEventSlim in JobQueue (BackgroundJobProcessor + Flush) |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/`, `src/Microsoft.TestPlatform.CoreUtilities/`, `src/Microsoft.TestPlatform.Client/` (partial — ManualResetEvent usage checked)
- Next to scan: `src/Microsoft.TestPlatform.Common/` (deeper scan beyond filter), `src/vstest.console/` orchestration paths

## Last Run

- 2026-06-22: Task 3 (ManualResetEventSlim JobQueue PR), Task 4 (checked PRs #16139, #16144, #16147 — all CI green), Task 2 (scanned Client — no new hot-path opportunities), Task 7 (monthly summary updated)
