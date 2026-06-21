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
- `LengthPrefixCommunicationChannel.Send` and `NotifyDataAvailable` — fixed in PR (efficiency/task-completed-ipc): `Task.CompletedTask` instead of `Task.FromResult(0)`.
- `JobQueue` in `CoreUtilities` uses kernel-backed `ManualResetEvent` for `_jobAdded` and `_queueProcessing`; `ManualResetEventSlim` would reduce kernel transitions when waits are short. The `waitEvent` in `Flush()` is per-call heap allocation — could reuse a semaphore.
- Filter source files (`src/Microsoft.TestPlatform.Filter.Source/`) compiled into `Microsoft.TestPlatform.Common` via explicit `<Compile Include=...>` references.
- Tests live in `test/Microsoft.TestPlatform.Common.UnitTests/` and `test/Microsoft.TestPlatform.Filter.Source.UnitTests/`.
- CrossPlatEngine tests: `test/Microsoft.TestPlatform.CrossPlatEngine.UnitTests/` — TFMs: `net11.0;net481`.

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| HIGH | Build | Issue #15295 / PR #16043: `MsCoverageReferencedPathMaps` — PR already open by maintainers; monitor for merge | HIGH |
| MEDIUM | Code-Level | `JobQueue._jobAdded`/`_queueProcessing` use kernel-backed `ManualResetEvent`; switch to `ManualResetEventSlim` would reduce kernel transitions for short waits. Per-call `waitEvent` in `Flush()` is allocating a kernel object each flush. | MEDIUM |
| LOW | Code-Level | `Condition.GetPropertyValue` allocates `string[1]` per non-array property in slow-filter path | LOW |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | #16139 | FastFilter: avoid redundant dictionary lookups in Evaluate + single-pass ValidForProperties scan |
| 2026-06-20 | #16144 | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |
| 2026-06-21 | TBD (efficiency/task-completed-ipc) | Replace Task.FromResult(0) with Task.CompletedTask in IPC channel (LengthPrefixCommunicationChannel + TcpClientExtensions) |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/`, `src/Microsoft.TestPlatform.CoreUtilities/`
- Next to scan: `src/Microsoft.TestPlatform.Client/` and `src/Microsoft.TestPlatform.Common/` (orchestration paths)

## Last Run

- 2026-06-21: Task 3 (Task.CompletedTask IPC PR), Task 4 (checked PRs #16139 + #16144 — all CI green), Task 2 (scanned CoreUtilities — found ManualResetEvent opportunity), Task 7 (monthly summary)
