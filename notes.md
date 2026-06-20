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
- `LengthPrefixCommunicationChannel.Send` and `NotifyDataAvailable` both return `Task.FromResult(0)` — should be `Task.CompletedTask` to avoid per-message `Task<int>` allocation.
- Filter source files (`src/Microsoft.TestPlatform.Filter.Source/`) compiled into `Microsoft.TestPlatform.Common` via explicit `<Compile Include=...>` references.
- Tests live in `test/Microsoft.TestPlatform.Common.UnitTests/` and `test/Microsoft.TestPlatform.Filter.Source.UnitTests/`.
- CrossPlatEngine tests: `test/Microsoft.TestPlatform.CrossPlatEngine.UnitTests/` — TFMs: `net11.0;net481`.

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| HIGH | Build | Issue #15295: `MsCoverageReferencedPathMaps` MSBuild target runs sequentially on every incremental build | HIGH |
| MEDIUM | Code-Level | `Task.FromResult(0)` → `Task.CompletedTask` in `LengthPrefixCommunicationChannel.Send/NotifyDataAvailable` (IPC hot path, avoids per-message `Task<int>` heap allocation) | MEDIUM |
| LOW | Code-Level | `Condition.GetPropertyValue` allocates `string[1]` per non-array property in slow-filter path | LOW |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | #16139 | FastFilter: avoid redundant dictionary lookups in Evaluate + single-pass ValidForProperties scan |
| 2026-06-20 | TBD (aw_pr_utcnow) | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/`
- Next to scan: `src/Microsoft.TestPlatform.CoreUtilities/` (shared utilities, potentially hot)

## Last Run

- 2026-06-20: Task 3 (DateTime.UtcNow PR), Task 4 (checked PR #16139 — all CI green), Task 7 (monthly summary)
