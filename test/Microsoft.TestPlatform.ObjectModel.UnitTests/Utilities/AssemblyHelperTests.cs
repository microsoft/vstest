﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

#nullable disable

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Utilities;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

[TestClass]
public class AssemblyHelperTests
{
    private readonly Mock<IRunContext> _runContext;
    private readonly Mock<IRunSettings> _runSettings;

    public AssemblyHelperTests()
    {
        _runContext = new Mock<IRunContext>();
        _runSettings = new Mock<IRunSettings>();
    }

    [TestMethod]
    public void SetNetFrameworkCompatiblityModeShouldSetAppDomainTargetFrameWorkWhenFramework40()
    {
        _runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>Framework40</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
        _runContext.Setup(rc => rc.RunSettings).Returns(_runSettings.Object);
        AppDomainSetup appDomainSetup = new();

        AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, _runContext.Object);

        Assert.AreEqual(".NETFramework,Version=v4.0", appDomainSetup.TargetFrameworkName);
    }

    [TestMethod]
    public void SetNetFrameworkCompatiblityModeShouldSetAppDomainTargetFrameWorkWhenNetFrameworkVersionv40()
    {
        _runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
        _runContext.Setup(rc => rc.RunSettings).Returns(_runSettings.Object);
        AppDomainSetup appDomainSetup = new();

        AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, _runContext.Object);

        Assert.AreEqual(".NETFramework,Version=v4.0", appDomainSetup.TargetFrameworkName);
    }

    [TestMethod]
    public void SetNetFrameworkCompatiblityModeShouldNotSetAppDomainTargetFrameWorkWhenFramework45()
    {
        _runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>Framework45</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
        _runContext.Setup(rc => rc.RunSettings).Returns(_runSettings.Object);
        AppDomainSetup appDomainSetup = new();

        AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, _runContext.Object);

        Assert.IsNull(appDomainSetup.TargetFrameworkName);
    }

    [TestMethod]
    public void SetNetFrameworkCompatiblityModeShouldNotSetAppDomainTargetFrameWorkWhenNetFrameworkVersionv45()
    {
        Mock<IRunContext> runContext = new();
        Mock<IRunSettings> runSettings = new();
        runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
        runContext.Setup(rc => rc.RunSettings).Returns(runSettings.Object);
        AppDomainSetup appDomainSetup = new();

        AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, runContext.Object);

        Assert.IsNull(appDomainSetup.TargetFrameworkName);
    }
}
#endif
