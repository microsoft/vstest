// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformTests
    {
        private readonly Mock<ITestEngine> testEngine;
        private readonly Mock<IProxyDiscoveryManager> discoveryManager;
        private readonly Mock<ITestExtensionManager> extensionManager;
        private readonly Mock<ITestRuntimeProvider> hostManager;
        private readonly Mock<IProxyExecutionManager> executionManager;
        private readonly Mock<IFileHelper> mockFileHelper;

        public TestPlatformTests()
        {
            this.testEngine = new Mock<ITestEngine>();
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.extensionManager = new Mock<ITestExtensionManager>();
            this.executionManager = new Mock<IProxyExecutionManager>();
            this.hostManager = new Mock<ITestRuntimeProvider>();
            this.mockFileHelper = new Mock<IFileHelper>();
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersAndCreateDiscoveryRequestWithGivenCriteriaAndReturnIt()
        {
            this.discoveryManager.Setup(dm => dm.Initialize()).Verifiable();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            this.testEngine.Setup(te => te.GetDiscoveryManager(this.hostManager.Object, It.IsAny<DiscoveryCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);

            var discoveryRequest = tp.CreateDiscoveryRequest(discoveryCriteria, It.IsAny<ProtocolConfig>());

            this.hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            this.discoveryManager.Verify(dm => dm.Initialize(), Times.Once);
            Assert.AreEqual(discoveryCriteria, discoveryRequest.DiscoveryCriteria);
        }

        [TestMethod]
        public void CreateDiscoveryRequestThrowsIfDiscoveryCriteriaIsNull()
        {
            TestPlatform tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateDiscoveryRequest(null, It.IsAny<ProtocolConfig>()));
        }

        [TestMethod]
        public void UpdateExtensionsShouldUpdateTheEngineWithAdditionalExtensions()
        {
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var additionalExtensions = new List<string> { "e1.dll", "e2.dll" };

            tp.UpdateExtensions(additionalExtensions, loadOnlyWellKnownExtensions: true);

            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void ClearExtensionsShouldClearTheExtensionsCachedInEngine()
        {
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);

            tp.ClearExtensions();

            this.extensionManager.Verify(em => em.ClearExtensions());
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesingModeIsFalseForRunAll()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml, TimeSpan.Zero);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria, It.IsAny<ProtocolConfig>());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesingModeIsFalseForRunSelected()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<TestCase> { new TestCase("dll1.class1.test1", new Uri("hello://x/"), "xyz\\1.dll") }, 10, false, settingsXml);

            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria, It.IsAny<ProtocolConfig>());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldNotUpdateTestSourcesIfSelectedTestAreRun()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<TestCase> { new TestCase("dll1.class1.test1", new Uri("hello://x/"), "xyz\\1.dll") }, 10, false, settingsXml);
            this.hostManager.Setup(hm => hm.GetTestSources(It.IsAny<IEnumerable<string>>()))
                .Returns(new List<string> { "xyz\\1.dll" });

            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            tp.CreateTestRunRequest(testRunCriteria, It.IsAny<ProtocolConfig>());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
            this.hostManager.Verify(hm => hm.GetTestSources(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersAndCreateTestRunRequestWithSpecifiedCriteria()
        {
            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria, It.IsAny<ProtocolConfig>());

            var actualTestRunRequest = testRunRequest as TestRunRequest;

            this.hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            this.executionManager.Verify(em => em.Initialize(), Times.Once);
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldSetCustomHostLauncherOnEngineDefaultLauncherIfSpecified()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.executionManager.Setup(dm => dm.Initialize()).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.hostManager.Object, It.IsAny<TestRunCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(testRunCriteria, It.IsAny<ProtocolConfig>());

            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
            this.hostManager.Verify(hl => hl.SetCustomLauncher(mockCustomLauncher.Object), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestThrowsIfTestRunCriteriaIsNull()
        {
            var tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateTestRunRequest(null, It.IsAny<ProtocolConfig>()));
        }

        /// <summary>
        /// Logger extensions should be updated when design mode is false.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldUpdateLoggerExtensionWhenDesingModeIsFalse()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.discoveryManager.Setup(dm => dm.Initialize()).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var discoveryCriteria = new DiscoveryCriteria(new List<string> { @"x:dummy\foo.dll" }, 1, settingsXml);
            this.hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            this.testEngine.Setup(te => te.GetDiscoveryManager(this.hostManager.Object, It.IsAny<DiscoveryCriteria>(), It.IsAny<ProtocolConfig>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            // Action
            var discoveryRequest = tp.CreateDiscoveryRequest(discoveryCriteria, It.IsAny<ProtocolConfig>());

            // Verify
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        private class TestableTestPlatform : TestPlatform
        {
            public TestableTestPlatform(ITestEngine testEngine, ITestRuntimeProvider hostProvider) : base(testEngine, new FileHelper(), new TestableTestRuntimeProviderManager(hostProvider))
            {
            }

            public TestableTestPlatform(ITestEngine testEngine, IFileHelper fileHelper, ITestRuntimeProvider hostProvider) : base(testEngine, fileHelper, new TestableTestRuntimeProviderManager(hostProvider))
            {
            }
        }

        private class TestableTestRuntimeProviderManager : TestRuntimeProviderManager
        {
            private readonly ITestRuntimeProvider hostProvider;

            public TestableTestRuntimeProviderManager(ITestRuntimeProvider hostProvider)
                : base(TestSessionMessageLogger.Instance)
            {
                this.hostProvider = hostProvider;
            }

            public override ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
            {
                return this.hostProvider;
            }
        }
    }
}
