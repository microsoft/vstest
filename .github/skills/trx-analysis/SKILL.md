---
name: trx-analysis
description: Parse and analyze Visual Studio TRX test result files. Use when asked about slow tests, test durations, test frequency, flaky tests, failure analysis, or test execution patterns from TRX files.
---

# TRX Test Results Analysis

Parse `.trx` files (Visual Studio Test Results XML) to answer questions about test performance, frequency, failures, and patterns.

## TRX File Format

TRX files use XML namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`. Key elements:

- `TestRun.Results.UnitTestResult` — individual test executions with `testName`, `duration` (HH:mm:ss.fffffff), `outcome` (Passed/Failed/NotExecuted)
- `TestRun.TestDefinitions.UnitTest` — test metadata including class and method info
- `TestRun.ResultSummary` — aggregate pass/fail/skip counts

## Loading a TRX File

```powershell
[xml]$trx = Get-Content "path/to/file.trx"
$results = $trx.TestRun.Results.UnitTestResult
```

## Common Queries

### Top N slowest tests

```powershell
$results | ForEach-Object {
    [PSCustomObject]@{
        Test     = $_.testName
        Seconds  = [TimeSpan]::Parse($_.duration).TotalSeconds
        Outcome  = $_.outcome
    }
} | Sort-Object Seconds -Descending | Select-Object -First 25 |
  Format-Table @{L='Sec';E={'{0,6:N1}' -f $_.Seconds}}, Outcome, Test -AutoSize
```

### Slowest test from each distinct class (top N)

```powershell
$results | ForEach-Object {
    $parts = $_.testName -split '\.'
    [PSCustomObject]@{
        Test      = $_.testName
        ClassName = ($parts[0..($parts.Length-2)] -join '.')
        Seconds   = [TimeSpan]::Parse($_.duration).TotalSeconds
    }
} | Sort-Object Seconds -Descending |
  Group-Object ClassName | ForEach-Object { $_.Group | Select-Object -First 1 } |
  Sort-Object Seconds -Descending | Select-Object -First 10 |
  Format-Table @{L='Sec';E={'{0,6:N1}' -f $_.Seconds}}, ClassName, Test -AutoSize
```

### Most-executed tests (parameterization frequency)

Extract the base method name before parameterization and count runs:

```powershell
$results | ForEach-Object {
    $name = $_.testName
    if ($name -match '^(\S+?)[\s(]') { $base = $Matches[1] } else { $base = $name }
    [PSCustomObject]@{ Base = $base; Seconds = [TimeSpan]::Parse($_.duration).TotalSeconds }
} | Group-Object Base | ForEach-Object {
    [PSCustomObject]@{
        Runs     = $_.Count
        TotalSec = ($_.Group | Measure-Object Seconds -Sum).Sum
        Test     = $_.Name
    }
} | Sort-Object TotalSec -Descending | Select-Object -First 20 |
  Format-Table @{L='Runs';E={$_.Runs}}, @{L='TotalSec';E={'{0,7:N1}' -f $_.TotalSec}}, Test -AutoSize
```

### Failed tests

```powershell
$results | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object {
    [PSCustomObject]@{
        Test    = $_.testName
        Seconds = [TimeSpan]::Parse($_.duration).TotalSeconds
        Error   = $_.Output.ErrorInfo.Message
    }
} | Format-Table -Wrap
```

### Summary statistics

```powershell
$summary = $trx.TestRun.ResultSummary.Counters
[PSCustomObject]@{
    Total    = $summary.total
    Passed   = $summary.passed
    Failed   = $summary.failed
    Skipped  = $summary.notExecuted
    Duration = $trx.TestRun.Times.finish
} | Format-List
```

## Tips

- Parameterized tests appear as separate `UnitTestResult` entries. Use regex `'^(\S+?)[\s(]'` to extract the base method name.
- Sort by **TotalSec** (runs × avg duration) to find tests that consume the most CI time overall, even if each individual run is fast.
- TRX files from CI are typically found in `TestResults/` or as pipeline artifacts.
