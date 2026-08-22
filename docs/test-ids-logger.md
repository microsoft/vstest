# Test id report logger

> [!WARNING]
> **This logger is temporary and will be removed.**
>
> It exists for one purpose: to let you build a mapping from the test case ids you have already
> stored to the ids that will replace them when vstest changes the algorithm it hashes test case ids
> with. It will be deleted at the same time as the SHA1 implementation it reports on, and there will
> be no replacement, because once SHA1 is gone there is nothing left to map from. Do not build a
> permanent pipeline on it.

## What this is for

vstest computes `TestCase.Id` by hashing a seed string. That hash has always been SHA1. A new
algorithm, xxHash128, is now available and will become the default in a later release, selected in
the meantime through the `VSTEST_TESTCASE_ID_ALGORITHM` environment variable - see
[Environment variables](environment-variables.md).

When the default moves, the id of every test whose id the platform computes changes. Anything that
stored those ids - Azure DevOps test case work item associations, a test history database, a flaky
test tracker, a triage spreadsheet - needs to know which old id becomes which new id.

Without this logger the only way to obtain that mapping is to run the whole suite twice, once with
each algorithm selected, and join the two result files on test name. That join is ambiguous in
practice: data driven tests routinely produce several rows that share a fully qualified name and
whose display names do not distinguish them, and there is no reliable key to join those rows on.

This logger removes the join. One run, and every test is reported once with **both** ids on the same
row.

## Enabling it

```shell
vstest.console.exe Tests.dll /logger:testids
```

```shell
dotnet test --logger:testids
```

By default the report is written to `TestIds.csv` in the test results directory. A different path
can be given with `LogFileName`, which may be absolute or relative to the test results directory:

```shell
vstest.console.exe Tests.dll /logger:"testids;LogFileName=ids\mapping.csv"
```

The logger also runs during discovery, so the mapping can be produced without executing anything:

```shell
vstest.console.exe Tests.dll /ListTests /logger:testids
```

Which algorithm the run itself uses does not matter. Both ids are reported either way; the run's
choice only determines which one the test actually carries.

## Output format

The report is CSV, quoted per RFC 4180 - a field containing a comma, a quote or a newline is
enclosed in quotes and embedded quotes are doubled. CSV is the format because the report is meant to
be joined against whatever you stored your ids in, and a flat table of scalar columns loads into a
database, a spreadsheet or a script without anything having to be written to parse it.

| Column | Meaning |
| ------ | ------- |
| `Source` | The test container the test was found in, as the adapter reported it. |
| `ExecutorUri` | The executor uri of the adapter that reported the test. |
| `FullyQualifiedName` | The fully qualified name of the test, as reported. |
| `DisplayName` | The display name of the test. For data driven tests this is often the only thing distinguishing one row from another. |
| `Id` | **The id the test actually carries today.** |
| `Sha1Id` | The id the SHA1 algorithm computes from this test's seed. |
| `XxHash128Id` | The id the xxHash128 algorithm computes from this test's seed. |
| `IdSource` | Where `Id` came from: `Sha1`, `XxHash128` or `SelfAssigned`. |

Rows are sorted by source, then fully qualified name, then id, so two reports of the same suite are
byte identical and can be diffed against each other.

### Deduplication

Each distinct test is reported once, keyed on source, executor uri, fully qualified name and the id
the test carries.

- A test that is **retried**, or that otherwise reports several results, carries the same id every
  time and is reported once.
- **Data driven rows that carry distinct ids** are reported as distinct rows, because each id needs
  its own mapping. This is precisely the case a two-run join cannot resolve.
- Data driven rows that share a single id collapse into one row - one id maps to one id, no matter
  how many ways it was rendered. Where that happens, `DisplayName` is the first one seen.

## The important part: not every id is computed by the platform

`Id` is deliberately reported separately from the two computed candidates, because an id is not
always computed by the platform at all. An adapter may assign one itself, and **MSTest v3 and v4 do
exactly that** through their own `TestIdGenerationStrategy`; those ids never go through the platform
hashing.

Such an id matches neither candidate. It is reported with `IdSource=SelfAssigned`, and it means:

> **This id will not change when the default algorithm moves. There is nothing to migrate for this
> test.**

A report that showed only `Sha1Id` and `XxHash128Id` would tell every MSTest user their ids are
about to change when in fact none of them are. Read `IdSource` before doing anything with a row:

| `IdSource` | What it means |
| ---------- | ------------- |
| `Sha1` | Platform computed with the legacy algorithm. **This id changes** to `XxHash128Id` when the default moves. |
| `XxHash128` | Platform computed with the new algorithm - this run already selected it. The id does not change. |
| `SelfAssigned` | The adapter assigned the id. **Nothing changes.** Ignore the two computed columns for this row. |

## Worked example

Running a suite that mixes an adapter using platform computed ids with an MSTest project:

```shell
vstest.console.exe Sample.Tests.dll /logger:testids
```

produces `TestResults\TestIds.csv`:

```csv
Source,ExecutorUri,FullyQualifiedName,DisplayName,Id,Sha1Id,XxHash128Id,IdSource
c:\src\Sample.Tests.dll,executor://sample/v1,Sample.Tests.Calculator.Adds,Adds,4ea1b0b6-0b17-3b06-a1c1-6a0a8ad0b6fd,4ea1b0b6-0b17-3b06-a1c1-6a0a8ad0b6fd,8f0a5b7c-1d22-8f31-9c44-2b7e6c0d1a55,Sha1
c:\src\Sample.Tests.dll,executor://sample/v1,Sample.Tests.Calculator.Divides,"Divides (1,0)",b21f7c30-9d51-3a0e-88b2-71c5a3e0f912,b21f7c30-9d51-3a0e-88b2-71c5a3e0f912,c7d3e881-4a60-8b19-a2f3-55e9d7c14b08,Sha1
c:\src\Sample.Tests.dll,executor://mstestadapter/v4,Sample.Tests.MSTestSuite.Works,Works,11111111-2222-3333-4444-555555555555,d4c2a919-77b3-3e2c-9d81-0f3a5b6c7d8e,3f9e1c22-8a70-8d45-b6c1-9e2d4a5f8b03,SelfAssigned
```

Three things to read off this:

- Row 1 and 2 are platform computed. Their ids move from `Id` to `XxHash128Id`.
- Row 2 shows the quoting: the display name `Divides (1,0)` contains a comma, so it is quoted.
- Row 3 is MSTest. `Id` matches neither computed column, `IdSource` says `SelfAssigned`, and this id
  is not going anywhere.

## Building an old to new mapping

Load the CSV and select the rows that will actually change:

```sql
-- Only Sha1 rows migrate. XxHash128 rows are already migrated, SelfAssigned rows never move.
SELECT Id AS OldId, XxHash128Id AS NewId, FullyQualifiedName, DisplayName, Source
FROM TestIds
WHERE IdSource = 'Sha1';
```

Then join that against your own records on `OldId` and rewrite them to `NewId`. Rows with
`IdSource = 'SelfAssigned'` should be left exactly as they are; rows with `IdSource = 'XxHash128'`
were produced by a run that had already opted in, and are already correct.

The same thing in PowerShell:

```powershell
Import-Csv TestResults\TestIds.csv |
    Where-Object IdSource -eq 'Sha1' |
    Select-Object @{ n = 'OldId'; e = { $_.Id } }, @{ n = 'NewId'; e = { $_.XxHash128Id } }, FullyQualifiedName |
    Export-Csv mapping.csv -NoTypeInformation
```

If a test in your records does not appear in the report at all, it was not discovered by this run -
the mapping cannot be produced for a test that no longer exists, and such records need deciding on
separately.

## Related

- [Environment variables](environment-variables.md) - `VSTEST_TESTCASE_ID_ALGORITHM`
- [Reporting test results](report.md) - test loggers in general
