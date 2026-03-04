This is a .NET based repository that contains the VSTest test platform. Please follow these guidelines when contributing:

## Code Standards

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](../.editorconfig).

In addition to the rules enforced by `.editorconfig`, you SHOULD:

- Favor style and conventions that are consistent with the existing codebase.
- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- Respect StyleCop.Analyzers rules, in particular:
  - SA1028: Code must not contain trailing whitespace
  - SA1316: Tuple element names should use correct casing
  - SA1518: File is required to end with a single newline character

You MUST minimize adding public API surface area but any newly added public API MUST be declared in the related `PublicAPI.Unshipped.txt` file.

## Localization Guidelines

Anytime you add a new localization resource, you MUST:
- Add a corresponding entry in the localization resource file.
- Add an entry in all `*.xlf` files related to the modified `.resx` file.
- Do not modify existing entries in '*.xlf' files unless you are also modifying the corresponding `.resx` file.

## Build & Test Commands

### Full Build

```bash
# Windows
build.cmd
# Unix
./build.sh
```

### Building a Specific Project

```bash
dotnet build src/<ProjectName>/<ProjectName>.csproj --no-restore
```

### Running Unit Tests for a Specific Project

```bash
# Run all TFMs
dotnet test test/<TestProjectName>/<TestProjectName>.csproj --no-build

# Run a specific TFM
dotnet test test/<TestProjectName>/<TestProjectName>.csproj --no-build -f net9.0

# Run a specific test
dotnet test test/<TestProjectName>/<TestProjectName>.csproj --no-build -f net9.0 --filter "TestMethodName"
```

### Key Test Projects

| Component | Test Project |
|---|---|
| TRX Logger | `test/Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests` |
| HTML Logger | `test/Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests` |
| Object Model | `test/Microsoft.TestPlatform.ObjectModel.UnitTests` |
| Cross-Platform Engine | `test/Microsoft.TestPlatform.CrossPlatEngine.UnitTests` |
| Client | `test/Microsoft.TestPlatform.Client.UnitTests` |

## Repository Structure

- `src/` — Production source code
  - `Microsoft.TestPlatform.Extensions.TrxLogger/` — TRX file logger (generates `.trx` test result files)
  - `Microsoft.TestPlatform.Extensions.HtmlLogger/` — HTML logger (generates `.html` test reports via XML→XSLT transform)
  - `Microsoft.TestPlatform.ObjectModel/` — Shared object model (TestCase, TestResult, etc.)
  - `Microsoft.TestPlatform.Common/` — Common utilities
  - `Microsoft.TestPlatform.CrossPlatEngine/` — Test execution engine
  - `vstest.console/` — CLI console runner
- `test/` — Unit tests (mirrors `src/` structure with `.UnitTests` suffix)
- `eng/` — Build infrastructure
- `scripts/` — Helper scripts

## Logger Architecture Notes

### TRX Logger (`src/Microsoft.TestPlatform.Extensions.TrxLogger/`)

- **`TrxLogger.cs`** — Main logger class. Flow: `TestRunCompleteHandler` → compose XML DOM → `ReserveTrxFilePath` → `AdjustRunDeploymentRootForTrxSubdirectory` → `PopulateTrxFile`.
- **`Utility/Converter.cs`** — Converts VSTest object model to TRX object model. Handles file attachments.
- **`Utility/TrxFileHelper.cs`** — File path utilities. Use `MakePathRelative()` instead of `Path.GetRelativePath()` (netstandard2.0 compat).
- Tests use `TestableTrxLogger` which overrides `PopulateTrxFile` to capture TRX file path.

### HTML Logger (`src/Microsoft.TestPlatform.Extensions.HtmlLogger/`)

- **`HtmlLogger.cs`** — Main logger. Creates temp XML, transforms to HTML via XSLT, deletes XML.
- **`HtmlTransformer.cs`** — XSLT transformation from XML to HTML.
- Temp XML filenames include PID for cross-process uniqueness. File creation uses `FileMode.CreateNew` for atomicity.
- Tests mock `IFileHelper`, `IHtmlTransformer`, and `XmlObjectSerializer`.

## Analyzer Rules

The codebase enforces strict code analysis rules as errors:
- **CA1305**: Always provide `IFormatProvider` (e.g., `CultureInfo.InvariantCulture`) for `ToString()` calls.
- **CA1837**: Use `Environment.ProcessId` instead of `Process.GetCurrentProcess().Id` (net5.0+ only; suppress for net48 targets).
- Source projects target `netstandard2.0`/`net462` — many modern APIs are unavailable. Always check TFM compatibility.
