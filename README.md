### VSTest
The Visual Studio Test Platform is an open and extensible test platform that enables running tests, collect diagnostics data and report results. The Test Platform supports running tests written in various test frameworks, and using a pluggable adapter model. Based on user-choice, the desired test framework and its corresponding adapter can be acquired as a vsix or as NuGet package as the case may be. Adapters can be written in terms of a public API exposed by the Test Platform.

The Test Platform currently ships as part Visual Studio 2017, and in the .NET Core Tools Preview 3.

### Build status
|            |Debug |Release |
|:----------:|:----------------:|:------------------:|
|**master**  |[![Build Status](https://ci.dot.net/buildStatus/icon?job=Microsoft_vstest/master/Windows_NT_Debug)](https://ci.dot.net/job/Microsoft_vstest/job/master/job/Windows_NT_Debug/)|[![Build Status](https://ci.dot.net/buildStatus/icon?job=Microsoft_vstest/master/Windows_NT_Release)](https://ci.dot.net/job/Microsoft_vstest/job/master/job/Windows_NT_Release/)|

### Contributing
There are many ways to contribute to VSTest
- [Submit issues](https://github.com/Microsoft/vstest/issues) and help verify fixes as they are checked in.
- Review the [source code changes](https://github.com/Microsoft/vstest/pulls).
- [Contribute features and fixes](https://github.com/Microsoft/vstest-docs/blob/master/docs/contribute.md).
- Contribute to the [documentation](https://github.com/Microsoft/vstest-docs).

### Documentation
- [Test Platform Architecture](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0001-Test-Platform-Architecture.md)
- [Test Discovery Protocol](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0002-Test-Discovery-Protocol.md)
- [Test Execution Protocol](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0003-Test-Execution-Protocol.md)
- [Adapter Extensibility](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0004-Adapter-Extensibility.md)
- [Test Platform SDK](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0005-Test-Platform-SDK.md)
- [Editors API Specification](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0007-Editors-API-Specification.md)
- [Data collection Protocol](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0006-DataCollection-Protocol.md)
- [Translation Layer](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0008-TranslationLayer.md)
- [Editors API Revision Update](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0009-Editors-API-RevisionUpdate.md)

### Building
VSTest can be built from within Visual Studio or from the CLI.
- [Building with Visual Studio](https://github.com/Microsoft/vstest-docs/blob/master/docs/contribute.md#building-with-visual-studio)
- [Building with CLI, CI, Editors](https://github.com/Microsoft/vstest-docs/blob/master/docs/contribute.md#building-with-cli-ci-editors)

### Microsoft Open Source Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### License
VSTest platform is licensed under the [MIT license](https://github.com/Microsoft/vstest/blob/master/LICENSE)

### Roadmap
For details on our planned features and future direction please refer to our [roadmap](https://github.com/Microsoft/vstest-docs/blob/master/roadmap.md).
