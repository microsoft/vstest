# Efficiency Improver — vstest Repo Memory

## Build/Test Commands

- **Build (Debug):** `./.dotnet/dotnet build <project.csproj> -c Debug`
- **Full build:** `./build.sh` (downloads SDK if needed; slow first time)
- **Run unit tests:** `./.dotnet/dotnet test <test.csproj> -c Debug -f net11.0`
- **Filter tests:** `./.dotnet/dotnet test <test.csproj> --filter "Filtering" -c Debug -f net11.0`
- **Release build (CI match):** `./build.sh -c Release` (but note: `--no-restore` flag not supported via build.sh)
- Net test project TFMs: typically `net11.0` and `net481`/`net48`

## Efficiency Notes

- `FastFilter.Evaluate` is the hot path for test filtering — called once per test case when a filter is active.
- `ImmutableDictionary` (used for `FilterProperties`) does NOT have `Deconstruct` on net462/netstandard2.0; use `kvp.Key` / `kvp.Value` pattern.
- `TestRunCache` and `DiscoveryResultCache` both use `DateTime.Now` for time deltas — potential for small improvement using `DateTime.UtcNow` (avoids timezone conversion).
- The filter source files (`src/Microsoft.TestPlatform.Filter.Source/`) are compiled into `Microsoft.TestPlatform.Common` via explicit `<Compile Include=...>` references.
- Tests live in `test/Microsoft.TestPlatform.Common.UnitTests/` and `test/Microsoft.TestPlatform.Filter.Source.UnitTests/`.

## Optimisation Backlog

| Priority | Focus Area | Opportunity | Estimated Impact |
|----------|------------|-------------|------------------|
| MEDIUM | Code-Level | `DateTime.Now` → `DateTime.UtcNow` in TestRunCache and DiscoveryResultCache time-delta calculations (avoids timezone conversion per test result) | LOW-MEDIUM |
| LOW | Code-Level | `Condition.GetPropertyValue` allocates `string[1]` per non-array property in slow-filter path | LOW |
| MEDIUM | Code-Level | Issue #15295: `MsCoverageReferencedPathMaps` MSBuild target runs sequentially on every incremental build — high impact but complex MSBuild change | HIGH (build time) |

## Completed Work

| Date | PR | Description |
|------|-----|-------------|
| 2026-06-19 | TBD | FastFilter: avoid redundant dictionary lookups in Evaluate + single-pass ValidForProperties scan. Branch: `efficiency/fast-filter-avoid-redundant-dict-lookups` |

## Backlog Cursor

- Last scanned: `src/Microsoft.TestPlatform.Filter.Source/`, `src/Microsoft.TestPlatform.CrossPlatEngine/`
- Next to scan: `src/Microsoft.TestPlatform.CommunicationUtilities/` (serialization hot path)

## Last Run

- 2026-06-19: Task 1 (commands), Task 2 (opportunities), Task 3 (FastFilter PR), Task 7 (monthly summary)
