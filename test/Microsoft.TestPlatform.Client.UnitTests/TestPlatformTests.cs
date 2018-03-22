// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests
{
    using System;
    using System.Collections.Generic;

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
        private readonly Mock<ITestLoggerManager> loggerManager;
        private readonly Mock<IFileHelper> mockFileHelper;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;

        public TestPlatformTests()
        {
            this.testEngine = new Mock<ITestEngine>();
            this.discoveryManager = new Mock<IProxyDiscoveryManager>();
            this.extensionManager = new Mock<ITestExtensionManager>();
            this.executionManager = new Mock<IProxyExecutionManager>();
            this.loggerManager = new Mock<ITestLoggerManager>();
            this.hostManager = new Mock<ITestRuntimeProvider>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersAndCreateDiscoveryRequestWithGivenCriteriaAndReturnIt()
        {
            this.discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            this.testEngine.Setup(te => te.GetDiscoveryManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);

            var discoveryRequest = tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            this.hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            this.discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
            Assert.AreEqual(discoveryCriteria, discoveryRequest.DiscoveryCriteria);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersWithFalseFlagWhenSkipDefaultAdaptersIsFalse()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = false
            };

            InvokeCreateDiscoveryRequest(options);

            this.discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersWithTrueFlagWhenSkipDefaultAdaptersIsTrue()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = true
            };

            InvokeCreateDiscoveryRequest(options);

            this.discoveryManager.Verify(dm => dm.Initialize(true), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersWithFalseFlagWhenTestPlatformOptionsIsNull()
        {
            InvokeCreateDiscoveryRequest();

            this.discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestThrowsIfDiscoveryCriteriaIsNull()
        {
            TestPlatform tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateDiscoveryRequest(this.mockRequestData.Object, null, new TestPlatformOptions()));
        }

        [TestMethod]
        public void UpdateExtensionsShouldUpdateTheEngineWithAdditionalExtensions()
        {
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var additionalExtensions = new List<string> { "e1.dll", "e2.dll" };

            tp.UpdateExtensions(additionalExtensions, skipExtensionFilters: true);

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
        public void CreateTestRunRequestShouldThrowExceptionIfNoTestHostproviderFound()
        {
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetFrameworkVersion>.NETPortable,Version=v4.5</TargetFrameworkVersion>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml, TimeSpan.Zero);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, null);
            bool exceptionThrown = false;

            try
            {
                tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            }
            catch(TestPlatformException ex)
            {
                exceptionThrown = true;
                Assert.AreEqual("No suitable test runtime provider found for this run.", ex.Message);
            }

            Assert.IsTrue(exceptionThrown, "TestPlatformException should get thrown");
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesingModeIsFalseForRunAll()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);
            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

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

            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesignModeIsFalseForRunSelected()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<TestCase> { new TestCase("dll1.class1.test1", new Uri("hello://x/"), "xyz\\1.dll") }, 10, false, settingsXml);

            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldNotUpdateTestSourcesIfSelectedTestAreRun()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

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

            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
            this.hostManager.Verify(hm => hm.GetTestSources(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersAndCreateTestRunRequestWithSpecifiedCriteria()
        {
            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            var actualTestRunRequest = testRunRequest as TestRunRequest;

            this.hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            this.executionManager.Verify(em => em.Initialize(false), Times.Once);
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersWithFalseFlagWhenSkipDefaultAdaptersIsFalse()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = false
            };

            InvokeCreateTestRunRequest(options);

            this.executionManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersWithTrueFlagWhenSkipDefaultAdaptersIsTrue()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = true
            };

            InvokeCreateTestRunRequest(options);

            this.executionManager.Verify(dm => dm.Initialize(true), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersWithFalseFlagWhenTestPlatformOptionsIsNull()
        {
            InvokeCreateTestRunRequest();

            this.executionManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldSetCustomHostLauncherOnEngineDefaultLauncherIfSpecified()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
            this.hostManager.Verify(hl => hl.SetCustomLauncher(mockCustomLauncher.Object), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestThrowsIfTestRunCriteriaIsNull()
        {
            var tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateTestRunRequest(this.mockRequestData.Object, null, new TestPlatformOptions()));
        }


        [TestMethod]
        public void CreateDiscoveryRequestShouldThrowExceptionIfNoTestHostproviderFound()
        {
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object))
                .Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetFrameworkVersion>.NETPortable,Version=v4.5</TargetFrameworkVersion>
                     </RunConfiguration>
                </RunSettings>";

            var discoveryCriteria = new DiscoveryCriteria(new List<string> { @"x:dummy\foo.dll" }, 1, settingsXml);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, null);
            bool exceptionThrown = false;

            try
            {
                tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());
            }
            catch (TestPlatformException ex)
            {
                exceptionThrown = true;
                Assert.AreEqual("No suitable test runtime provider found for this run.", ex.Message);
            }

            Assert.IsTrue(exceptionThrown, "TestPlatformException should get thrown");
        }

        /// <summary>
        /// Logger extensions should be updated when design mode is false.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldUpdateLoggerExtensionWhenDesignModeIsFalse()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), System.IO.SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            this.discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();

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

            this.testEngine.Setup(te => te.GetDiscoveryManager(It.IsAny<IRequestData>(), this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.mockFileHelper.Object, this.hostManager.Object);

            // Action
            var discoveryRequest = tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            // Verify
            this.extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        /// <summary>
        /// Create test run request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateTestRunRequestShouldInitializeLoggerManagerForDesignMode()
        {
            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>True</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            this.loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create discovery request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeLoggerManagerForDesignMode()
        {
            this.testEngine.Setup(te => te.GetDiscoveryManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>True</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, settingsXml);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            this.loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create test run request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateTestRunRequestShouldInitializeLoggerManagerForNonDesignMode()
        {
            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>False</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, settingsXml);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            this.loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create discovery request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeLoggerManagerForNonDesignMode()
        {
            this.testEngine.Setup(te => te.GetDiscoveryManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>False</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 10, settingsXml);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            this.loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        private void InvokeCreateDiscoveryRequest(TestPlatformOptions options = null)
        {
            this.discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            this.hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            this.testEngine.Setup(te => te.GetDiscoveryManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(this.discoveryManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);
            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);

            tp.CreateDiscoveryRequest(this.mockRequestData.Object, discoveryCriteria, options);
        }

        private void InvokeCreateTestRunRequest(TestPlatformOptions options = null)
        {
            this.executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            this.testEngine.Setup(te => te.GetExecutionManager(this.mockRequestData.Object, this.hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(this.executionManager.Object);
            this.testEngine.Setup(te => te.GetExtensionManager()).Returns(this.extensionManager.Object);
            this.testEngine.Setup(te => te.GetLoggerManager(this.mockRequestData.Object)).Returns(this.loggerManager.Object);

            var tp = new TestableTestPlatform(this.testEngine.Object, this.hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            this.hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            tp.CreateTestRunRequest(this.mockRequestData.Object, testRunCriteria, options);
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
