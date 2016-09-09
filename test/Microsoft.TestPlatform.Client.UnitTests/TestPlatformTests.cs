// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformTests
    {
        private Mock<ITestEngine> testEngine;
        private Mock<IProxyDiscoveryManager> discoveryManager;
        private Mock<ITestExtensionManager> extensionManager;
        private Mock<ITestHostManager> hostManager;
        private Mock<IProxyExecutionManager> executionManager;

        [TestInitialize]
        public void Initialize()
        {
            testEngine = new Mock<ITestEngine>();
            discoveryManager = new Mock<IProxyDiscoveryManager>();
            extensionManager = new Mock<ITestExtensionManager>();
            executionManager = new Mock<IProxyExecutionManager>();
            hostManager = new Mock<ITestHostManager>();
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldCreateDiscoveryRequestWithGivenCriteriaAndReturnIt()
        {
            testEngine.Setup(te => te.GetDefaultTestHostManager(ObjectModel.Architecture.X86, ObjectModel.Framework.DefaultFramework)).Returns(hostManager.Object);
            discoveryManager.Setup(dm => dm.Initialize(It.IsAny<ITestHostManager>())).Verifiable();
            testEngine.Setup(te => te.GetDiscoveryManager()).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object);

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
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object);

            var additionalExtensions = new List<string> { "e1.dll", "e2.dll" };

            tp.UpdateExtensions(additionalExtensions, loadOnlyWellKnownExtensions: true);

            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldCreateTestRunRequestWithSpecifiedCriteria()
        {
            testEngine.Setup(te => te.GetDefaultTestHostManager(ObjectModel.Architecture.X86, ObjectModel.Framework.DefaultFramework)).Returns(hostManager.Object);
            executionManager.Setup(dm => dm.Initialize(It.IsAny<ITestHostManager>())).Verifiable();
            testEngine.Setup(te => te.GetExecutionManager(It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object);

            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria);
            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldSetCustomHostLauncherOnEngineDefaultLauncherIfSpecified()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testEngine.Setup(te => te.GetDefaultTestHostManager(ObjectModel.Architecture.X86, ObjectModel.Framework.DefaultFramework)).Returns(hostManager.Object);
            executionManager.Setup(dm => dm.Initialize(It.IsAny<ITestHostManager>())).Verifiable();

            testEngine.Setup(te => te.GetExecutionManager(It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object);

            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria);
            var actualTestRunRequest = testRunRequest as TestRunRequest;

            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
            hostManager.Verify(hl => hl.SetCustomLauncher(mockCustomLauncher.Object), Times.Once);
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
        }
    }
}
