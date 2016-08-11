// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DesignModeTestHostLauncherFactoryTests
    {
        [TestMethod]
        public void DesignModeTestHostFactoryShouldReturnNonDebugLauncherIfDebuggingDisabled()
        {
            var mockDesignModeClient = new Mock<IDesignModeClient>();
            var testRunRequestPayload = new TestRunRequestPayload { DebuggingEnabled = false };
            var launcher = DesignModeTestHostLauncherFactory.GetCustomHostLauncherForTestRun(mockDesignModeClient.Object, testRunRequestPayload);
            
            Assert.IsFalse(launcher.IsDebug, "Factory must not return debug launcher if debugging is disabled.");
        }

        [TestMethod]
        public void DesignModeTestHostFactoryShouldReturnDebugLauncherIfDebuggingEnabled()
        {
            var mockDesignModeClient = new Mock<IDesignModeClient>();
            var testRunRequestPayload = new TestRunRequestPayload { DebuggingEnabled = true };
            var launcher = DesignModeTestHostLauncherFactory.GetCustomHostLauncherForTestRun(mockDesignModeClient.Object, testRunRequestPayload);

            Assert.IsTrue(launcher.IsDebug, "Factory must not return debug launcher if debugging is disabled.");
        }
    }
}
