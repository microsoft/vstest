# Microsoft.TestPlatform.Portable

A portable subset of binaries for the Visual Studio Test Platform (vstest). This package contains the test platform toolset for cross-platform test execution scenarios, including `vstest.console`, test hosts, and data collectors.

## Usage

```xml
<PackageReference Include="Microsoft.TestPlatform.Portable" Version="x.y.z" />
```

> **Note:** Most test projects should reference `Microsoft.NET.Test.Sdk` instead. This package is intended for scenarios that require the portable test platform toolset directly.

## Links

- [Visual Studio Test Platform Documentation](https://github.com/microsoft/vstest)
- [Test Platform Architecture](https://github.com/microsoft/vstest/blob/main/docs/RFCs/0001-Test-Platform-Architecture.md)
- [Packaging](https://github.com/microsoft/vstest/blob/main/docs/RFCs/0014-Packaging.md)
- [License](https://github.com/microsoft/vstest/blob/main/LICENSE)
