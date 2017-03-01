// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using Moq;

    [TestClass]
    public class TestPlatformTests
    {
        private readonly Mock<ITestEngine> testEngine;
        private readonly Mock<IProxyDiscoveryManager> discoveryManager;
        private readonly Mock<ITestExtensionManager> extensionManager;
        private readonly Mock<ITestHostManager> hostManager;
        private readonly Mock<IProxyExecutionManager> executionManager;

        public TestPlatformTests()
        {
            this.testEngine = new Mock<ITestEngine>();
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.extensionManager = new Mock<ITestExtensionManager>();
            this.executionManager = new Mock<IProxyExecutionManager>();
            this.hostManager = new Mock<ITestHostManager>();
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldCreateDiscoveryRequestWithGivenCriteriaAndReturnIt()
        {
            this.testEngine.Setup(te => te.GetDefaultTestHostManager(It.IsAny<RunConfiguration>())).Returns(this.hostManager.Object);
            this.discoveryManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetDiscoveryManager(this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object);
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);

            var discoveryRequest = tp.CreateDiscoveryRequest(discoveryCriteria);

            Assert.AreEqual(discoveryCriteria, discoveryRequest.DiscoveryCriteria);
        }

        [TestMethod]
        public void CreateDiscoveryRequestThrowsIfDiscoveryCriteriaIsNull()
        {
            TestPlatform tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateDiscoveryRequest(null));
        }

        [TestMethod]
        public void UpdateExtensionsShouldUpdateTheEngineWithAdditionalExtensions()
        {
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object);
            var additionalExtensions = new List<string> { "e1.dll", "e2.dll" };

            tp.UpdateExtensions(additionalExtensions, loadOnlyWellKnownExtensions: true);

            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesingModeIsFalse()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            Mock<IFileHelper> mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly)).Returns(additionalExtensions);


            this.testEngine.Setup(te => te.GetDefaultTestHostManager(It.IsAny<RunConfiguration>())).Returns(this.hostManager.Object);
            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, mockFileHelper.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml, TimeSpan.Zero, mockCustomLauncher.Object);
            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria);
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldCreateTestRunRequestWithSpecifiedCriteria()
        {
            this.testEngine.Setup(te => te.GetDefaultTestHostManager(It.IsAny<RunConfiguration>())).Returns(this.hostManager.Object);
            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria);

            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldSetCustomHostLauncherOnEngineDefaultLauncherIfSpecified()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.testEngine.Setup(te => te.GetDefaultTestHostManager(It.IsAny<RunConfiguration>())).Returns(this.hostManager.Object);
            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, null, TimeSpan.Zero, mockCustomLauncher.Object);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria);

            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
            this.hostManager.Verify(hl => hl.SetCustomLauncher(mockCustomLauncher.Object), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestThrowsIfTestRunCriteriaIsNull()
        {
            var tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateTestRunRequest(null));
        }

        private class TestableTestPlatform : TestPlatform
        {
            public TestableTestPlatform(ITestEngine testEngine) : base(testEngine)
            {
            }

            public TestableTestPlatform(ITestEngine testEngine, IFileHelper fileHelper) : base(testEngine)
            {
                base.fileHelper = fileHelper;
            }
        }
    }
}
