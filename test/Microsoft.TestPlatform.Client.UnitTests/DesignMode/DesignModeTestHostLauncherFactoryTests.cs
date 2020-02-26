// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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

            // Accepted behavior.
            // No more debug launchers are returned, not even in debugging context.
            // Workflow changed and debugging is no longer a matter of correctly getting the
            // launcher. The test platform must explicitly make a request to attach to the debugger
            // based on info it gets from the adapters.
            Assert.IsFalse(launcher.IsDebug, "Factory must return non-debug launcher if debugging is enabled.");
        }
    }
}
