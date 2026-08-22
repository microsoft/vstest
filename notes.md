# Efficiency Improver — vstest Repo Memory
_Last updated: 2026-08-22_

## Build / Test Commands
- Bootstrap + full build: `./build.sh` (downloads pinned .NET 11 SDK to `.dotnet/`)
- Build specific project: `.dotnet/dotnet build <csproj> -c Release`
- Run specific test project: `.dotnet/dotnet run --project test/<proj>/<proj>.csproj -c Release --no-build -f net11.0 -- --filter "TestName"`
- CI-equivalent build: `./build.sh -c Release`
- Test TFMs: `net11.0` and `net481` (per project)
- SDK note: `global.json` pins .NET 11.0.100-preview.5; SDK not pre-installed in agent — `./build.sh` bootstraps it
- Note: `test.sh -p <pattern>` runs ALL tests, not just matching ones; use `dotnet run` on specific project instead

## PR Status

### Open PRs
(none — all prior efficiency PRs closed/merged)

### Merged PRs (all confirmed)
- PR #16210: Eliminate GetRawText() string alloc across 9 STJ deserializer converters (MERGED 2026-07-09)
- PR #16193: v2 serialization Guid.ToString elimination (MERGED 2026-07-01)
- PR #16144: DateTime.Now → UtcNow (MERGED)
- PR #16147: Task.FromResult(0) → Task.CompletedTask (MERGED)
- PR #16150: ManualResetEvent → ManualResetEventSlim (MERGED)
- PR #16160: FastFilter.Evaluate closure/double-lookup elimination (MERGED)
- PR #16165: List pre-allocation in DiscoveryResultCache + TestRunCache (MERGED)
- PR #16170: ContainsKey+TryGetValue → single TryGetValue in JsoniteConvert (MERGED)
- PR #16179: Condition.Evaluate string[1] fast-path (MERGED)
- PR #16182: FilterExpression.Evaluate leaf-node short-circuit (MERGED)

### Closed/Rejected PRs
- PR #16139: ImmutableDictionary redundant lookups (CLOSED — ToArray allocation concern)
- PR #16177: DiscoveryDataAggregator string[1] (CLOSED — superseded)
- PR #16213: DiscoveryDataAggregator O(N) patterns (CLOSED — too small a win)
- PR #16216: Duration.ToString / Guid.ToString allocs (CLOSED — too small a win)
- PR #16222: TryGetGuid/TryGetDateTimeOffset (CLOSED — too small a win)

## Maintainer Instructions (Issue #16229)
- Weekly runs only; ≥15-20% measurable improvement required
- Only HIGH-impact items actionable; MEDIUM goes to backlog only
- Focus on fixed per-invocation overhead, not per-test micro-opts
- Common case is 1 test; ~90% runs <1000 tests
- O(n²) always in scope regardless of N

## Efficiency Notes (Key Insights)
- **Workload profile**: single test is most common; total run time ~400ms-1.5s
- **Run settings XML parsing**: parsed 5-6× per test run — not meeting the bar alone
- **XmlRunSettingsUtilities.ReaderSettings**: property creates new XmlReaderSettings on every call — MEDIUM fix
- **GetRunConfigurationNode**: called 15+ times per run, each parsing XML — MEDIUM impact
- **All hot-path allocations**: filter eval, IPC serialization/deserialization — fully optimized in prior runs
- **NuGet.Frameworks code**: vendored, out of scope for efficiency changes
- **MTP bridge** (new code): scanned multiple times — clean, no hot-path issues
- **CA1310/ordinal comparisons**: enabled in #16388 — already handled by maintainers

## Optimization Backlog (sorted by priority)
| Priority | Area | Opportunity | Notes |
|---|---|---|---|
| MEDIUM | Code | Run settings XML parsed 5-6× per test run — could be reduced to 1 pass | Requires API changes; saves maybe 2-5ms; may not meet bar |
| MEDIUM | Code | XmlRunSettingsUtilities.ReaderSettings property allocates new XmlReaderSettings per call (~15 startup call sites) | Easy fix; minor GC reduction |

## Backlog Cursor
- All key hot paths fully scanned (IPC serialization V1+V2, filter eval, discovery aggregator, test run cache, parallel runners)
- TestRequestManager startup path: fully scanned — XML parsing redundancy is the main finding (MEDIUM)
- MTP bridge code: scanned multiple times — no issues
- NuGet.Frameworks: vendored, skip
- **Status after 2026-08-22 scan**: No new HIGH-impact items found. Recent commits (Aug 15–22) are CA1310/ordinal fix, reflection deserialization removal, backslash normalization fix, infra.
- Next area to investigate: TRX logger performance (only on test completion, likely cold path but worth scanning)

## Monthly Activity Issues
- Issue #16140: [efficiency-improver] Monthly Activity 2026-06 — CLOSED 2026-07-03
- Issue #16211: [efficiency-improver] Monthly Activity 2026-07 — CLOSED 2026-08-01
- Issue #16332: [efficiency-improver] Monthly Activity 2026-08 — active, updated 2026-08-22
- Last run: 2026-08-22 (run ID 32585558358)

## Maintainer-Checked Items (do not include in Suggested Actions)
- (none yet)
