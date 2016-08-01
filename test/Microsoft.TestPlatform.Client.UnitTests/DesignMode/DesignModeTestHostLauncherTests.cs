// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    [TestClass]
    public class DesignModeTestHostLauncherTests
    {
        [TestMethod]
        public void DesignModeTestHostLauncherLaunchTestHostShouldCallDesignModeClientToLaunchCustomHost()
        {
            var mockDesignModeClient = new Mock<IDesignModeClient>();
            var launcher = new DesignModeTestHostLauncher(mockDesignModeClient.Object);
            Assert.IsFalse(launcher.IsDebug, "Default launcher must not implement debug launcher interface.");

            var testProcessStartInfo = new TestProcessStartInfo();

            launcher.LaunchTestHost(testProcessStartInfo);

            mockDesignModeClient.Verify(md => md.LaunchCustomHost(testProcessStartInfo), Times.Once);
        }

        [TestMethod]
        public void DesignModeDebugTestHostLauncherLaunchTestHostShouldCallDesignModeClientToLaunchCustomHost()
        {
            var mockDesignModeClient = new Mock<IDesignModeClient>();
            var launcher = new DesignModeDebugTestHostLauncher(mockDesignModeClient.Object);
            Assert.IsTrue(launcher.IsDebug, "Debug launcher must implement debug launcher interface.");

            var testProcessStartInfo = new TestProcessStartInfo();

            launcher.LaunchTestHost(testProcessStartInfo);

            mockDesignModeClient.Verify(md => md.LaunchCustomHost(testProcessStartInfo), Times.Once);
        }
    }
}

