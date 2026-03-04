---
name: vstest-build-test
description: Build, test, and validate changes in the vstest repository. Use when building vstest projects, running unit tests, smoke tests, or acceptance tests, or when deploying locally built vstest.console for manual testing.
---

# Building and Testing vstest

## Build

### Full Build (Recommended)

For projects with many cross-project dependencies (e.g., HtmlLogger, TrxLogger, vstest.console):

```powershell
./build.cmd -pack
```

This produces NuGet packages under `artifacts/packages/Debug/Shipping/`.

### Single Project Build

For isolated projects with few dependencies:

```powershell
./build.cmd -project <path-to-csproj>
```

> **Warning:** This does NOT work for projects like HtmlLogger that have many transitive dependencies. Use `-pack` instead.

### Release Configuration

```powershell
./build.cmd -c Release -pack
```

## Test

### Unit Tests (Default)

```powershell
./test.cmd
```

### Specific Test Assembly

Use `-p` to filter by assembly name pattern:

```powershell
./test.cmd -p htmllogger       # HTML logger tests
./test.cmd -p trxlogger        # TRX logger tests
./test.cmd -p datacollector    # Data collector tests
./test.cmd -p smoke            # Smoke tests
```

### Specific Test by Name

```powershell
./test.cmd -bl -c release /p:TestRunnerAdditionalArguments="'--filter TestName'" -Integration
```

## Manual Validation with vstest.console

After `./build.cmd -pack`, validate vstest.console changes by unzipping the built package:

1. Locate the package: `artifacts/packages/Debug/Shipping/Microsoft.TestPlatform.<version>-dev.nupkg`
2. Unzip it (`.nupkg` files are ZIP archives)
3. Run the local vstest.console against a test project

### Alternative: Direct Artifact Paths

- **xplat (netcoreapp):** `artifacts/<Configuration>/netcoreapp1.0/vstest.console.dll`
- **Windows desktop:** `artifacts/<Configuration>/net46/win7-x64/vstest.console.exe`

## Test Categories

| Category | Speed | What it tests | Filter |
|---|---|---|---|
| Unit tests | Fast | Individual units | `./test.cmd` (default) |
| Smoke tests | Slow | P0 end-to-end scenarios | `./test.cmd -p smoke` |
| Acceptance tests | Slowest | Extensive coverage | `-Integration` flag |

## Troubleshooting

- If build fails asking for .NET 4.6 targeting pack, install it from [Microsoft Downloads](https://www.microsoft.com/download/details.aspx?id=48136)
- Enable verbose diagnostics: see `docs/diagnose.md`
- For debugging, add `Debugger.Launch` at process entry points (testhost.exe, vstest.console.exe)
