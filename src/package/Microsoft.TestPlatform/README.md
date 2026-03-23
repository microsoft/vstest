# Microsoft.TestPlatform

The full set of binaries for the Visual Studio Test Platform (vstest). This package contains the complete test platform including `vstest.console`, test hosts, data collectors, and extensions for both .NET and .NET Framework.

## Usage

```xml
<PackageReference Include="Microsoft.TestPlatform" Version="x.y.z" />
```

> **Note:** Most test projects should reference `Microsoft.NET.Test.Sdk` instead, which provides a lighter-weight integration. This package is intended for scenarios that require the full test platform toolset.

## Links

- [Visual Studio Test Platform Documentation](https://github.com/microsoft/vstest)
- [Test Platform Architecture](https://github.com/microsoft/vstest/blob/main/docs/RFCs/0001-Test-Platform-Architecture.md)
- [Packaging](https://github.com/microsoft/vstest/blob/main/docs/RFCs/0014-Packaging.md)
- [License](https://github.com/microsoft/vstest/blob/main/LICENSE)
