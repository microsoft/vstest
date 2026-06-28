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
- `JsoniteConvert.DeserializeTestCase` and `DeserializeTestResult`: fixed in PR #16170 (2026-06-25) — replaced `ContainsKey`+`TryGetValue` double-hash with single `TryGetValue`; eliminates ~20K redundant hash lookups per 10K-test run.
- `DiscoveryDataAggregator.MarkSourcesBasedOnDiscoveredTestCases`: fixed in PR #16177 (2026-06-26, CI green; 2026-06-28 fix pushed: per-test-case `_isMessageSent` guard restored inside foreach loop per code review; new test added) — extracted `MarkSourceWithStatus` private helper; hot loop now calls it directly instead of `MarkSourcesWithStatus(new[] { source }, ...)`; eliminates ~10K `string[1]` allocations per 10K-test discovery run.
- `Condition.Evaluate` slow-filter path: fixed in PR #16179 (2026-06-27) — fast path for `string` property values avoids `new string[1]` allocation per evaluated test case; affects all `~`/`!~` filters (e.g., `FullyQualifiedName~Test`); ~240KB GC pressure eliminated per 10K-test filter pass.
- `FilterExpression.Evaluate` slow-filter path: fixed in PR #aw_pr_fexpr (2026-06-28) — leaf-node fast path avoids `IterateFilterExpression` allocation (2× `Stack<T>` ~48B each + lambda ~32B = ~128B per test case) for single-condition nodes; most common `~` filter (`FullyQualifiedName~Test`) is always a leaf. ~1.25MB GC pressure eliminated per 10K-test `~` filter pass.

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| HIGH | Build | Issue #15295 / PR #16043: `MsCoverageReferencedPathMaps` — PR already open by maintainers; monitor for merge | HIGH |
| LOW | Code-Level | `MsTestV1TelemetryHelper.AddTelemetry`: `ContainsKey` + `[]` double-hash (MSTest v1 users only; not a priority) | LOW |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | #16139 (closed, not merged) | FastFilter: redundant dict lookups + ValidForProperties — CLOSED by maintainer (ToArray allocation concern) |
| 2026-06-20 | #16144 (MERGED) | Replace DateTime.Now with DateTime.UtcNow in TestRunCache + DiscoveryResultCache hot paths |
| 2026-06-21 | #16147 (MERGED) | Replace Task.FromResult(0) with Task.CompletedTask in IPC channel |
| 2026-06-22 | #16150 (MERGED) | Replace ManualResetEvent with ManualResetEventSlim in JobQueue |
| 2026-06-23 | #16160 (MERGED) | FastFilter.Evaluate: foreach kvp + foreach instead of Any(lambda); NO ValidForProperties change |
| 2026-06-24 | #16165 (MERGED 2026-06-26) | Pre-allocate List<T>(InitialCapacity) in DiscoveryResultCache+TestRunCache; remove Collection<T> virtual-method layer |
| 2026-06-25 | #16170 (MERGED 2026-06-27) | JsoniteConvert: replace ContainsKey+TryGetValue with single TryGetValue in DeserializeTestCase+DeserializeTestResult |
| 2026-06-26 | #16177 (open, CI green) | DiscoveryDataAggregator: eliminate string[1] array per test case in discovery source tracking hot loop |
| 2026-06-27 | #16179 (open, CI green) | Condition.Evaluate: fast path for string property values; eliminates string[1] per test case with ~/!~ filters |
| 2026-06-28 | #aw_pr_fexpr (open) | FilterExpression.Evaluate: leaf-node fast path; avoids 2×Stack + lambda per test case in all ~ filters; ~1.25MB GC pressure eliminated per 10K-test run |
| 2026-06-28 | #16177 (fix pushed) | DiscoveryDataAggregator: restored per-test-case _isMessageSent guard inside foreach loop (reviewer feedback fix); added pinning test |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`, `src/Microsoft.TestPlatform.CommunicationUtilities/` (JsoniteConvert hot path fixed), `src/Microsoft.TestPlatform.CoreUtilities/`, `src/Microsoft.TestPlatform.Client/` (partial — ManualResetEvent usage checked), `src/Microsoft.TestPlatform.Common/` (partial: InternalTestLoggerEvents scanned, ExtensionFramework init paths — not hot), `src/vstest.console/` (orchestration — init-time only, not hot paths)
- Next to scan: deeper scan of `src/Microsoft.TestPlatform.CommunicationUtilities/` serialization path for additional hot-path opportunities; `src/Microsoft.TestPlatform.CrossPlatEngine/Execution/` for other per-test-result patterns

## Last Run

- 2026-06-28: Task 4 (PR #16177: pushed fix for per-test-case `_isMessageSent` guard inside loop + new pinning test; PR #16179: CI green, clean expert review — no action needed), Task 3 (scanned FilterExpression.Evaluate: leaf-node fast path created in PR #aw_pr_fexpr; eliminates 2×Stack+lambda ~128B per test case with `FullyQualifiedName~Test`-style filters; ~1.25MB GC per 10K-test run), Task 7 (monthly summary updated, issue #16140)
