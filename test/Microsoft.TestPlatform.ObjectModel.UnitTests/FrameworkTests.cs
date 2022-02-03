﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using VisualStudio.TestTools.UnitTesting;

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
        var fx = Framework.FromString("framework35");
        Assert.AreEqual(".NETFramework,Version=v3.5", fx.Name);

        fx = Framework.FromString("FRAMEWORK40");
        Assert.AreEqual(".NETFramework,Version=v4.0", fx.Name);

        fx = Framework.FromString("Framework45");
        Assert.AreEqual(".NETFramework,Version=v4.5", fx.Name);

        fx = Framework.FromString("frameworKcore10");
        Assert.AreEqual(".NETCoreApp,Version=v1.0", fx.Name);

        fx = Framework.FromString("frameworkUAP10");
        Assert.AreEqual("UAP,Version=v10.0", fx.Name);
    }

    [TestMethod]
    public void FrameworkFromStringShouldTrimSpacesAroundFrameworkString()
    {
        var fx = Framework.FromString("  Framework35");

        Assert.AreEqual(".NETFramework,Version=v3.5", fx.Name);
        Assert.AreEqual("3.5.0.0", fx.Version);
    }

    [TestMethod]
    public void FrameworkFromStringShouldWorkForShortNames()
    {
        var fx = Framework.FromString("net451");
        Assert.AreEqual(".NETFramework,Version=v4.5.1", fx.Name);
        Assert.AreEqual("4.5.1.0", fx.Version);

        var corefx = Framework.FromString("netcoreapp2.0");
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
#if !NET451
        Assert.AreEqual(".NETCoreApp,Version=v1.0", Framework.DefaultFramework.Name);
#endif
    }
}