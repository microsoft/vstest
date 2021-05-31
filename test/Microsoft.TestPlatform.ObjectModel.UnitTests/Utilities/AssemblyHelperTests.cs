// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Utilities
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;

    [TestClass]
    public class AssemblyHelperTests
    {
        private Mock<IRunContext> runContext;
        private Mock<IRunSettings> runSettings;

        public AssemblyHelperTests()
        {
            this.runContext = new Mock<IRunContext>();
            this.runSettings = new Mock<IRunSettings>();
        }

        [TestMethod]
        public void SetNETFrameworkCompatiblityModeShouldSetAppDomainTargetFrameWorkWhenFramework40()
        {
            this.runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>Framework40</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
            this.runContext.Setup(rc => rc.RunSettings).Returns(runSettings.Object);
            AppDomainSetup appDomainSetup = new AppDomainSetup();

            AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, runContext.Object);

            Assert.AreEqual(".NETFramework,Version=v4.0", appDomainSetup.TargetFrameworkName);
        }

        [TestMethod]
        public void SetNETFrameworkCompatiblityModeShouldSetAppDomainTargetFrameWorkWhenNETFrameworkVersionv40()
        {
            this.runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
            this.runContext.Setup(rc => rc.RunSettings).Returns(runSettings.Object);
            AppDomainSetup appDomainSetup = new AppDomainSetup();

            AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, runContext.Object);

            Assert.AreEqual(".NETFramework,Version=v4.0", appDomainSetup.TargetFrameworkName);
        }

        [TestMethod]
        public void SetNETFrameworkCompatiblityModeShouldNotSetAppDomainTargetFrameWorkWhenFramework45()
        {
            this.runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>Framework45</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
            this.runContext.Setup(rc => rc.RunSettings).Returns(runSettings.Object);
            AppDomainSetup appDomainSetup = new AppDomainSetup();

            AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, runContext.Object);

            Assert.IsNull(appDomainSetup.TargetFrameworkName);
        }

        [TestMethod]
        public void SetNETFrameworkCompatiblityModeShouldNotSetAppDomainTargetFrameWorkWhenNETFrameworkVersionv45()
        {
            Mock<IRunContext> runContext = new Mock<IRunContext>();
            Mock<IRunSettings> runSettings = new Mock<IRunSettings>();
            runSettings.Setup(rs => rs.SettingsXml).Returns(@"<RunSettings> <RunConfiguration> <TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion> </RunConfiguration> </RunSettings>");
            runContext.Setup(rc => rc.RunSettings).Returns(runSettings.Object);
            AppDomainSetup appDomainSetup = new AppDomainSetup();

            AssemblyHelper.SetNETFrameworkCompatiblityMode(appDomainSetup, runContext.Object);

            Assert.IsNull(appDomainSetup.TargetFrameworkName);
        }
    }
}
#endif
