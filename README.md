### VSTest
The Visual Studio Test Platform is an open and extensible test platform that enables running tests, collect diagnostics data and report results. The Test Platform supports running tests written in various test frameworks, and using a pluggable adapter model. Based on user-choice, the desired test framework and its corresponding adapter can be acquired as a vsix or as NuGet package as the case may be. Adapters can be written in terms of a public API exposed by the Test Platform.

The Test Platform currently ships as part Visual Studio 2019, and in the .NET Core Tools Preview 3.

### Build status
[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/Microsoft/vstest/microsoft.vstest.ci?branchName=main)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=935&branchName=main)

### Contributing
There are many ways to contribute to VSTest
- [Submit issues](https://github.com/Microsoft/vstest/issues) and help verify fixes as they are checked in.
- Review the [source code changes](https://github.com/Microsoft/vstest/pulls).
- [Contribute features and fixes](https://github.com/Microsoft/vstest-docs/blob/main/docs/contribute.md).
- Contribute to the [documentation](https://github.com/Microsoft/vstest-docs).

NOTE: When adding a new public API, always add it directly to the `PublicAPI.Shipped.txt` file. This helps us ensure we are always considering potential breaking changes (even between successive commits of un-released version) and avoids the burden of the unshipped to shipped commit.

### Documentation
- [Test Platform Architecture](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0001-Test-Platform-Architecture.md)
- [Test Discovery Protocol](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0002-Test-Discovery-Protocol.md)
- [Test Execution Protocol](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0003-Test-Execution-Protocol.md)
- [Adapter Extensibility](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md)
- [Test Platform SDK](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0005-Test-Platform-SDK.md)
- [Editors API Specification](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0007-Editors-API-Specification.md)
- [Data collection Protocol](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0006-DataCollection-Protocol.md)
- [Translation Layer](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0008-TranslationLayer.md)
- [Editors API Revision Update](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0009-Editors-API-RevisionUpdate.md)
- [TranslationLayer](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0008-TranslationLayer.md)
- [Source Information For Discovered Tests](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0010-Source-Information-For-Discovered-Tests.md)
- [Test Session Timeout](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0011-Test-Session-Timeout.md)
- [Test Adapter Lookup](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0013-Test-Adapter-Lookup.md)
- [Packaging](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0014-Packaging.md)
- [Telemetry](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0015-Telemetry.md)
- [Loggers Information From RunSettings](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0016-Loggers-Information-From-RunSettings.md)
- [Properties for TestCases in Managed Code](https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md)
- [Skip Default Adapters](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0018-Skip-Default-Adapters.md)
- [Disable Appdomain While Running Tests](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0019-Disable-Appdomain-While-Running-Tests.md)
- [Improving Logic To Pass Sources To Adapters](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0020-Improving-Logic-To-Pass-Sources-To-Adapters.md)
- [Code Coverage for .Net Core](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0021-CodeCoverageForNetCore.md)
- [User Specified TestAdapter Lookup](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0022-User-Specified-TestAdapter-Lookup.md)
- [TestSettings Deprecation](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0023-TestSettings-Deprecation.md)
- [Blame Collector Options](https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0024-Blame-Collector-Options.md)

### Building
VSTest can be built from within Visual Studio or from the CLI.
- [Building with Visual Studio](https://github.com/Microsoft/vstest-docs/blob/main/docs/contribute.md#building-with-visual-studio)
- [Building with CLI, CI, Editors](https://github.com/Microsoft/vstest-docs/blob/main/docs/contribute.md#building-with-cli-ci-editors)

#### Servicing validation on Windows

Use the SDK pinned in `global.json`. `eng\build.ps1 -restore -build` bootstraps the SDK when needed.
Use a short worktree path to avoid .NET Framework adapter-loading failures at `MAX_PATH`.
Do not use `-prepareMachine` on a shared development machine.
Before localized packaging, run `eng\build.ps1 -restore -build -ci -warnAsError $false -nodeReuse $false`
with the intended configuration. An ordinary inner-loop build is English-only and can replace the
localized build outputs consumed by packaging.

`eng\build.ps1 -test` runs unit tests, including the communication platform tests.
`eng\build.ps1 -integrationTest` runs normal acceptance tests and excludes historical compatibility
combinations. Before acceptance tests, run `scripts\build.ps1` with the same configuration and
version suffix used for packaging, selecting `-Steps PrepareAcceptanceTests`. This builds normal
assets against the local packages without changing NuGet sources or building the compatibility matrix.

Both test commands require `VSTEST_CONSOLE_PATH` to point to
`artifacts\<configuration>\netcoreapp2.1\vstest.console.dll` after packaging, not the console bundled
in the SDK. The validation pipeline checks the staged console's version and starts the console before
running tests. A successful validation requires nonempty unit and
acceptance TRX files; known missing assets and empty AdapterUtilities suites are not allowed.

For local Release validation, use the same prepared package cache and private runtimes as CI:

```powershell
$env:TP_ROOT_DIR = $PWD.Path
$env:NUGET_PACKAGES = "$PWD\packages"
$env:DOTNET_ROOT = "$PWD\.dotnet"
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_ROOT_X86 = "$PWD\.dotnet\x86"
${env:DOTNET_ROOT(x86)} = $env:DOTNET_ROOT_X86
$env:VSTEST_CONSOLE_PATH = "$PWD\artifacts\Release\netcoreapp2.1\vstest.console.dll"
.\eng\build.ps1 -configuration Release -test -warnAsError $false -nodeReuse $false
.\eng\build.ps1 -configuration Release -integrationTest -warnAsError $false -nodeReuse $false
```

Historical combinations remain available separately through `-BuildCompatibilityTests` when preparing
assets and `eng\build.ps1 -compatibilityTest` when running them. Performance tests are selected
explicitly with `eng\build.ps1 -performanceTest` and may require administrator access for ETW.

### Microsoft Open Source Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### License
VSTest platform is licensed under the [MIT license](https://github.com/Microsoft/vstest/blob/main/LICENSE)

### Issue Tracking
Please see [issue tracking](https://github.com/Microsoft/vstest-docs/blob/main/issuetracking.md) for a description of the workflow we use to process issues.

### Roadmap
For more information on shipped and upcoming features/enhancements please refer to our [Releases](https://github.com/Microsoft/vstest-docs/blob/main/docs/releases.md) and [Quarterly Checkin reports](https://github.com/Microsoft/vstest-docs/tree/main/Quarterly%20Checkins)
