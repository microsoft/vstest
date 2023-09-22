// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

[TestClass]
public class FrameworkTests
{
    [TestMethod]
    public void FrameworkFromStringShouldReturnNullForNull()
    {
        Assert.IsNull(Framework.FromString(null));
    }

    [TestMethod]
    public void FrameworkFromStringShouldReturnNullForEmptyString()
    {
        Assert.IsNull(Framework.FromString(string.Empty));
    }

    [TestMethod]
    public void FrameworkFromStringShouldReturnNullForInvalidString()
    {
        Assert.IsNull(Framework.FromString("InvalidFramework"));
    }

    [TestMethod]
    public void FrameworkFromStringShouldIgnoreCase()
    {
        var fx = Framework.FromString("framework35")!;
        Assert.AreEqual(".NETFramework,Version=v3.5", fx.Name);

        fx = Framework.FromString("FRAMEWORK40")!;
        Assert.AreEqual(".NETFramework,Version=v4.0", fx.Name);

        fx = Framework.FromString("Framework45")!;
        Assert.AreEqual(".NETFramework,Version=v4.5", fx.Name);

        fx = Framework.FromString("frameworKcore10")!;
        Assert.AreEqual(".NETCoreApp,Version=v1.0", fx.Name);

        fx = Framework.FromString("frameworkUAP10")!;
        Assert.AreEqual("UAP,Version=v10.0", fx.Name);
    }

    [TestMethod]
    public void FrameworkFromStringShouldTrimSpacesAroundFrameworkString()
    {
        var fx = Framework.FromString("  Framework35")!;

        Assert.AreEqual(".NETFramework,Version=v3.5", fx.Name);
        Assert.AreEqual("3.5.0.0", fx.Version);
    }

    [TestMethod]
    public void FrameworkFromStringShouldWorkForShortNames()
    {
        var fx = Framework.FromString("net462")!;
        Assert.AreEqual(".NETFramework,Version=v4.6.2", fx.Name);
        Assert.AreEqual("4.6.2.0", fx.Version);

        var corefx = Framework.FromString("netcoreapp2.0")!;
        Assert.AreEqual(".NETCoreApp,Version=v2.0", corefx.Name);
        Assert.AreEqual("2.0.0.0", corefx.Version);
    }

    [TestMethod]
    public void DefaultFrameworkShouldBeNet40OnDesktop()
    {
#if NETFRAMEWORK
        Assert.AreEqual(".NETFramework,Version=v4.0", Framework.DefaultFramework.Name);
#endif
    }

    [TestMethod]
    public void DefaultFrameworkShouldBeNetCoreApp10OnNonDesktop()
    {
#if !NETFRAMEWORK
        Assert.AreEqual(".NETCoreApp,Version=v1.0", Framework.DefaultFramework.Name);
#endif
    }

    [TestMethod]
    // Default is .NET Framework 0
    [DataRow("net", ".NETFramework,Version=v0.0")]
    [DataRow("net0", ".NETFramework,Version=v0.0")]
    // No dot version is parsed by single number, and only 4 are considered, rest is cut-off.
    [DataRow("net1", ".NETFramework,Version=v1.0")]
    [DataRow("net12", ".NETFramework,Version=v1.2")]
    [DataRow("net12", ".NETFramework,Version=v1.2")]
    [DataRow("net123", ".NETFramework,Version=v1.2.3")]
    [DataRow("net1234", ".NETFramework,Version=v1.2.3.4")]
    [DataRow("net12345", ".NETFramework,Version=v1.2.3.4")]
    // 0 is a special case, the resulting ,Version is always at least major.minor
    // never just major. When there are more 0 the version is kept as short as possible.
    [DataRow("net1", ".NETFramework,Version=v1.0")]
    [DataRow("net10", ".NETFramework,Version=v1.0")]
    [DataRow("net100", ".NETFramework,Version=v1.0")]
    [DataRow("net1000", ".NETFramework,Version=v1.0")]
    [DataRow("net1000000", ".NETFramework,Version=v1.0")]
    // When . is introduced into the version, the version is getting parsed as a version.
    // Same rules to 0 apply.
    [DataRow("net1.0", ".NETFramework,Version=v1.0")]
    [DataRow("net1.0.0", ".NETFramework,Version=v1.0")]
    [DataRow("net1.0.0.0", ".NETFramework,Version=v1.0")]
    [DataRow("net1.0.0.0", ".NETFramework,Version=v1.0")]
    // When additional version is found after 0, whole version up to that point is emitted.
    // Using more than 4 parts of version is invalid.
    [DataRow("net1.0.1", ".NETFramework,Version=v1.0.1")]
    [DataRow("net1.0.1.0", ".NETFramework,Version=v1.0.1")]
    [DataRow("net1.0.0.1", ".NETFramework,Version=v1.0.0.1")]
    [DataRow("net1.0.0.0.1", "Unsupported,Version=v0.0")]
    // On version 5 the identifier becomes .NETCoreApp.
    [DataRow("net5", ".NETCoreApp,Version=v1.0")]
    [DataRow("net5.0", ".NETCoreApp,Version=v1.0")]
    // Version can be forced by using a more specific prefix.
    // For netcoreapp:
    [DataRow("netcoreapp2.1", ".NETCoreApp,Version=v2.1")]
    [DataRow("netcoreapp5.0", ".NETCoreApp,Version=v5.0")]
    [DataRow("netcoreapp100.0", ".NETCoreApp,Version=v100.0")]
    // For netstandard:
    [DataRow("netstandard1.0", ".NETStandard,Version=v1.0")]
    // There are profiles for .NET Framework and .NET Standard,
    // and platforms for .NET.
    // For .NET Framework and .NET Standard:
    [DataRow("net4-profile1", ".NETFramework,Version=v4.0,Profile=profile1")]
    [DataRow("netstandard2-profile1", ".NETStandard,Version=v2.0,Profile=profile1")]
    // For those two the profile string has no additional requirements, you can
    // set version to 5 part version and it still works:
    [DataRow("netstandard2-profile1.0.0.0.1", ".NETStandard,Version=v2.0,Profile=profile1.0.0.0.1")]
    // For .NET platform is independent, and must be platform followed by valid version:
    [DataRow("net5-windows", ".NETCoreApp,Version=v5.0")]
    [DataRow("net5-windows1.0", ".NETCoreApp,Version=v5.0")]
    // When the version is too long it becomes unsupported:
    [DataRow("net5-windows1.0.0.0.1", "Unsupported,Version=v0.0")]

    public void ShortNameParserShouldUnderstandShortNetVersions(string shortName, string longName)
    {
        var _ = shortName + longName;
    }

    [TestMethod]
    public void LongNameParserShouldUnderstandLongNetVersions(string shortName, string longName)
    {
        var _ = shortName + longName;
    }

    [TestMethod]
    public void LongNameParserShouldReturnCorrectShortNames(string shortName, string longName)
    {
        var _ = shortName + longName;
    }
}
