# vstest.console.exe command line options

This document lists the command line options understood by `vstest.console.exe`. It is
generated from the argument processors in
[`src/vstest.console/Processors`](../src/vstest.console/Processors) and mirrors the
built-in `vstest.console.exe --Help` output.

Options are case-insensitive and accept either a `/Option` or a `--Option` prefix (for
example `/Parallel` and `--Parallel` are equivalent). Some options also have a short
form (for example `/lt` for `/ListTests`). Options that take a value use a colon, for
example `/Settings:test.runsettings`.

> **Using `dotnet test`?** `dotnet test` exposes most of these capabilities through its
> own switches (for example `--filter`, `--logger`, `--collect`, `--diag`,
> `--results-directory`, `--framework`). See
> [dotnet test options](https://learn.microsoft.com/dotnet/core/tools/dotnet-test) and
> [vstest.console.exe options](https://learn.microsoft.com/visualstudio/test/vstest-console-options)
> for the full, actively maintained reference. To pass runsettings values inline from
> `dotnet test`, use the `-- [name]=[value]` syntax described in
> [RunSettingsArguments.md](./RunSettingsArguments.md).

## Usage

```shell
vstest.console.exe [TestFileNames] [Options]
```

`TestFileNames` are one or more test containers (assemblies or other sources) separated by
spaces. Wildcards are supported, for example `**\*.Tests.dll`.

## Test selection and filtering

### `/Tests:<Test Names>`

Run tests with names that match the provided values. To provide multiple values, separate
them by commas.

```shell
vstest.console.exe MyTests.dll /Tests:TestMethod1
vstest.console.exe MyTests.dll /Tests:TestMethod1,TestMethod2
```

### `/TestCaseFilter:<Expression>`

Run tests that match the given expression. `<Expression>` is of the format
`<property>Operator<value>[|&<Expression>]` where `Operator` is one of `=`, `!=` or `~`
(`~` has *contains* semantics and applies to string properties such as `DisplayName`).
Parentheses `()` group sub-expressions.

```shell
vstest.console.exe MyTests.dll /TestCaseFilter:"Priority=1"
vstest.console.exe MyTests.dll /TestCaseFilter:"(FullyQualifiedName~Nightly|Name=MyTestMethod)"
```

See [filter.md](./filter.md) for the full filtering reference, supported properties per test
framework, and escaping rules.

### `/ListTests:<File Name>` (short form `/lt`)

Lists all discovered tests from the given test container instead of running them.

## Discovery and execution behavior

### `/Parallel`

Run the tests in parallel. By default up to all available cores on the machine may be
used. The number of cores to use may be configured with the `MaxCpuCount` element in a
settings file.

### `/InIsolation`

Runs the tests in an isolated process. This makes `vstest.console.exe` less likely to be
stopped by an error in the tests, but tests may run slower.

### `/Platform:<Platform type>`

Target platform architecture to be used for test execution. Valid values are `x86`, `x64`
and `ARM`.

### `/Framework:<Framework Version>`

Target .NET framework version to be used for test execution. Valid values are for example
`".NETFramework,Version=v4.5.1"` or `".NETCoreApp,Version=v1.0"`. Other supported values
are `Framework40`, `Framework45`, `FrameworkCore10` and `FrameworkUap10`.

### `/Environment:<NAME>=<VALUE>` (short form `/e`)

Sets the value of an environment variable for the test host. Creates the variable if it
does not exist, overrides it if it does. This implies `/InIsolation` and forces the tests
to run in an isolated process. Specify the option multiple times to set multiple
variables.

```shell
vstest.console.exe MyTests.dll -e:VARIABLE1=VALUE1
vstest.console.exe MyTests.dll -e:ANOTHER_VARIABLE="VALUE WITH SPACES"
```

## Adapters

### `/TestAdapterPath:<path>`

Makes `vstest.console.exe` use custom test adapters from the given path in the test run.

### `/TestAdapterLoadingStrategy:<strategy>`

Affects adapter loading behavior. Supported values (which can be combined):

| Strategy | Behavior |
| --- | --- |
| `Explicit` | Only load adapters specified by `/TestAdapterPath` (or the `RunConfiguration.TestAdaptersPaths` node). If no adapter path is specified, the run fails. Implies `/InIsolation`. |
| `Default` | Load adapters as if the argument was not specified (next to source, provided paths, and the default directory). |
| `DefaultRuntimeProviders` | Load the default runtime providers shipped with the Test Platform. |
| `ExtensionsDirectory` | Load adapters inside the `Extensions` folder. |
| `NextToSource` | Load adapters next to the source. |
| `Recursive` | Recursively search folders when loading adapters. Requires `Explicit` or `NextToSource`. |

## Settings

### `/Settings:<Settings File>`

Settings to use when running tests. See
[configure.md](./configure.md) and the
[.runsettings reference](https://learn.microsoft.com/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file).

### RunSettings arguments (`-- [name]=[value]`)

Pass runsettings configuration inline through the command line. Arguments are specified as
`[name]=[value]` pairs after `-- ` (note the space after `--`). Use a space to separate
multiple pairs; all arguments after `--` are treated as runsettings arguments, so they must
appear at the end of the command line.

```shell
vstest.console.exe MyTests.dll -- MSTest.MapInconclusiveToFailed=True
```

See [RunSettingsArguments.md](./RunSettingsArguments.md) for the full syntax, precedence
rules, and shell-escaping guidance.

## Loggers, data collection, and results

### `/logger:<Logger Uri/FriendlyName>`

Specify a logger for test results. For example, to log results into a Visual Studio Test
Results File (TRX) use `/logger:trx[;LogFileName=<name>]`. The console logger verbosity can
be set with `/logger:console;verbosity=<quiet|minimal|normal|detailed>`. More info:
[console logger](https://aka.ms/console-logger).

### `/Collect:<DataCollector FriendlyName>`

Enables a data collector for the test run (for example `/Collect:"Code Coverage"` or
`/Collect:"XPlat Code Coverage"`). More info: [vstest collect](https://aka.ms/vstest-collect).
See [analyze.md](./analyze.md) for code coverage.

### `/Blame:[CollectDump];[CollectAlways]=[Value];[DumpType]=[Value]`

Runs the test in blame mode to isolate a problematic test that crashes the test host. It
creates a `Sequence.xml` file capturing the order of execution before the crash, and can
optionally collect a process dump.

- `CollectAlways` — collect a dump on exit even when there is no crash (`true`/`false`).
- `DumpType` — dump type (`mini`/`full`).

Collecting a crash dump on Windows requires `procdump.exe`/`procdump64.exe` on `PATH` or in
the directory pointed to by the `PROCDUMP_PATH` environment variable
([download procdump](https://learn.microsoft.com/sysinternals/downloads/procdump)).

```shell
vstest.console.exe MyTests.dll /Blame
vstest.console.exe MyTests.dll /Blame:CollectDump;CollectAlways=true;DumpType=full
```

See [extensions/blame-datacollector.md](./extensions/blame-datacollector.md) for the full
blame collector reference.

### `/ResultsDirectory:<path>`

The test results directory is created at the specified path if it does not exist.

## Diagnostics

### `/Diag:<Path to log file>`

Enable diagnostic logs for the test platform, written to the provided file. Set the trace
level with `/Diag:<path>;tracelevel=<off|error|warning|info|verbose>` (default `verbose`).

```shell
vstest.console.exe MyTests.dll /Diag:log.txt
vstest.console.exe MyTests.dll /Diag:log.txt;tracelevel=info
```

See [diagnose.md](./diagnose.md) and [troubleshooting.md](./troubleshooting.md) for more.

## General

### `/Help` (short form `/?`)

Display the usage message.

### `@<file>`

Read a response file for more options. Each option is placed on its own line in the file.

```shell
vstest.console.exe @options.rsp
```

## Editor/IDE integration options

These options are used by IDEs and hosting tools (such as Visual Studio and
`dotnet test`) that host `vstest.console.exe`. They are not typically used directly from a
shell.

### `/Port:<Port>`

The port for the socket connection used to receive event messages from the host.

### `/ParentProcessId:<ParentProcessId>`

Process id of the parent process responsible for launching the current process. Used so the
runner can exit when its parent exits.

## See also

- [filter.md](./filter.md) — test case filtering reference
- [RunSettingsArguments.md](./RunSettingsArguments.md) — passing runsettings from the command line
- [configure.md](./configure.md) — `.runsettings` configuration
- [analyze.md](./analyze.md) — code coverage
- [diagnose.md](./diagnose.md) / [troubleshooting.md](./troubleshooting.md) — diagnostics
- [environment-variables.md](./environment-variables.md) — environment variables
- [vstest.console.exe options (Microsoft Learn)](https://learn.microsoft.com/visualstudio/test/vstest-console-options)
- [dotnet test options (Microsoft Learn)](https://learn.microsoft.com/dotnet/core/tools/dotnet-test)
