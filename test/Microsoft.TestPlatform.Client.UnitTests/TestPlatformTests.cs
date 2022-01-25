// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests
{
    using System;
    using System.IO;
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
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;

        public TestPlatformTests()
        {
            testEngine = new Mock<ITestEngine>();
            discoveryManager = new Mock<IProxyDiscoveryManager>();
            extensionManager = new Mock<ITestExtensionManager>();
            executionManager = new Mock<IProxyExecutionManager>();
            loggerManager = new Mock<ITestLoggerManager>();
            hostManager = new Mock<ITestRuntimeProvider>();
            mockFileHelper = new Mock<IFileHelper>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersAndCreateDiscoveryRequestWithGivenCriteriaAndReturnIt()
        {
            discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            testEngine.Setup(te => te.GetDiscoveryManager(mockRequestData.Object, hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            var discoveryRequest = tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
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

            discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersWithTrueFlagWhenSkipDefaultAdaptersIsTrue()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = true
            };

            InvokeCreateDiscoveryRequest(options);

            discoveryManager.Verify(dm => dm.Initialize(true), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeManagersWithFalseFlagWhenTestPlatformOptionsIsNull()
        {
            InvokeCreateDiscoveryRequest();

            discoveryManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateDiscoveryRequestThrowsIfDiscoveryCriteriaIsNull()
        {
            TestPlatform tp = new();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateDiscoveryRequest(mockRequestData.Object, null, new TestPlatformOptions()));
        }

        [TestMethod]
        public void UpdateExtensionsShouldUpdateTheEngineWithAdditionalExtensions()
        {
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            var additionalExtensions = new List<string> { "e1.dll", "e2.dll" };

            tp.UpdateExtensions(additionalExtensions, skipExtensionFilters: true);

            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, true));
        }

        [TestMethod]
        public void ClearExtensionsShouldClearTheExtensionsCachedInEngine()
        {
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            tp.ClearExtensions();

            extensionManager.Verify(em => em.ClearExtensions());
        }

        [TestMethod]
        public void CreateTestRunRequestShouldThrowExceptionIfNoTestHostproviderFound()
        {
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetFrameworkVersion>.NETPortable,Version=v4.5</TargetFrameworkVersion>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml, TimeSpan.Zero);
            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, null);
            bool exceptionThrown = false;

            try
            {
                tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
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
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);
            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var temp = Path.GetTempPath();
            var testRunCriteria = new TestRunCriteria(new List<string> { $@"{temp}foo.dll" }, 10, false, settingsXml, TimeSpan.Zero);
            hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldUpdateLoggerExtensionWhenDesignModeIsFalseForRunSelected()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<TestCase> { new TestCase("dll1.class1.test1", new Uri("hello://x/"), $"xyz{Path.DirectorySeparatorChar}1.dll") }, 10, false, settingsXml);

            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, hostManager.Object);

            var testRunRequest = tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        [TestMethod]
        public void CreateTestRunRequestShouldNotUpdateTestSourcesIfSelectedTestAreRun()
        {
            var additionalExtensions = new List<string> { "foo.TestLogger.dll", "Joo.TestLogger.dll" };
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<TestCase> { new TestCase("dll1.class1.test1", new Uri("hello://x/"), $"xyz{Path.DirectorySeparatorChar}1.dll") }, 10, false, settingsXml);
            hostManager.Setup(hm => hm.GetTestSources(It.IsAny<IEnumerable<string>>()))
                .Returns(new List<string> { $"xyz{Path.DirectorySeparatorChar}1.dll" });

            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, hostManager.Object);

            tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());
            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
            hostManager.Verify(hm => hm.GetTestSources(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersAndCreateTestRunRequestWithSpecifiedCriteria()
        {
            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            var actualTestRunRequest = testRunRequest as TestRunRequest;

            hostManager.Verify(hm => hm.Initialize(It.IsAny<TestSessionMessageLogger>(), It.IsAny<string>()), Times.Once);
            executionManager.Verify(em => em.Initialize(false), Times.Once);
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

            executionManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersWithTrueFlagWhenSkipDefaultAdaptersIsTrue()
        {
            var options = new TestPlatformOptions()
            {
                SkipDefaultAdapters = true
            };

            InvokeCreateTestRunRequest(options);

            executionManager.Verify(dm => dm.Initialize(true), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldInitializeManagersWithFalseFlagWhenTestPlatformOptionsIsNull()
        {
            InvokeCreateTestRunRequest();

            executionManager.Verify(dm => dm.Initialize(false), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestShouldSetCustomHostLauncherOnEngineDefaultLauncherIfSpecified()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            var testRunRequest = tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            var actualTestRunRequest = testRunRequest as TestRunRequest;
            Assert.AreEqual(testRunCriteria, actualTestRunRequest.TestRunCriteria);
            hostManager.Verify(hl => hl.SetCustomLauncher(mockCustomLauncher.Object), Times.Once);
        }

        [TestMethod]
        public void CreateTestRunRequestThrowsIfTestRunCriteriaIsNull()
        {
            var tp = new TestPlatform();

            Assert.ThrowsException<ArgumentNullException>(() => tp.CreateTestRunRequest(mockRequestData.Object, null, new TestPlatformOptions()));
        }


        [TestMethod]
        public void CreateDiscoveryRequestShouldThrowExceptionIfNoTestHostproviderFound()
        {
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object))
                .Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetFrameworkVersion>.NETPortable,Version=v4.5</TargetFrameworkVersion>
                     </RunConfiguration>
                </RunSettings>";

            var discoveryCriteria = new DiscoveryCriteria(new List<string> { @"x:dummy\foo.dll" }, 1, settingsXml);
            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, null);
            bool exceptionThrown = false;

            try
            {
                tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());
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
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(additionalExtensions);

            discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>false</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var temp = Path.GetTempPath();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { $@"{temp}foo.dll" }, 1, settingsXml);
            hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            testEngine.Setup(te => te.GetDiscoveryManager(It.IsAny<IRequestData>(), hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object, mockFileHelper.Object, hostManager.Object);

            // Action
            var discoveryRequest = tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            // Verify
            extensionManager.Verify(em => em.UseAdditionalExtensions(additionalExtensions, false));
        }

        /// <summary>
        /// Create test run request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateTestRunRequestShouldInitializeLoggerManagerForDesignMode()
        {
            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>True</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, false, settingsXml);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create discovery request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeLoggerManagerForDesignMode()
        {
            testEngine.Setup(te => te.GetDiscoveryManager(mockRequestData.Object, hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>True</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { @"x:dummy\foo.dll" }, 10, settingsXml);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create test run request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateTestRunRequestShouldInitializeLoggerManagerForNonDesignMode()
        {
            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>False</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10, false, settingsXml);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, new TestPlatformOptions());

            loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        /// <summary>
        /// Create discovery request should initialize logger manager for design mode.
        /// </summary>
        [TestMethod]
        public void CreateDiscoveryRequestShouldInitializeLoggerManagerForNonDesignMode()
        {
            testEngine.Setup(te => te.GetDiscoveryManager(mockRequestData.Object, hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>False</DesignMode>
                     </RunConfiguration>
                </RunSettings>";
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 10, settingsXml);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, new TestPlatformOptions());

            loggerManager.Verify(lm => lm.Initialize(settingsXml));
        }

        [TestMethod]
        public void StartTestSessionShouldThrowExceptionIfTestSessionCriteriaIsNull()
        {
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            Assert.ThrowsException<ArgumentNullException>(() =>
                tp.StartTestSession(
                    new Mock<IRequestData>().Object,
                    null,
                    new Mock<ITestSessionEventsHandler>().Object));
        }

        [TestMethod]
        public void StartTestSessionShouldReturnFalseIfDesignModeIsDisabled()
        {
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            var testSessionCriteria = new StartTestSessionCriteria()
            {
                RunSettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <RunSettings>
                         <RunConfiguration>
                           <DesignMode>false</DesignMode>
                         </RunConfiguration>
                    </RunSettings>"
            };

            Assert.IsFalse(
                tp.StartTestSession(
                    new Mock<IRequestData>().Object,
                    testSessionCriteria,
                    new Mock<ITestSessionEventsHandler>().Object));
        }

        [TestMethod]
        public void StartTestSessionShouldReturnFalseIfTestSessionManagerIsNull()
        {
            testEngine.Setup(
                te => te.GetTestSessionManager(
                    It.IsAny<IRequestData>(),
                    It.IsAny<StartTestSessionCriteria>()))
                .Returns((IProxyTestSessionManager)null);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            var testSessionCriteria = new StartTestSessionCriteria()
            {
                RunSettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <RunSettings>
                         <RunConfiguration>
                           <DesignMode>true</DesignMode>
                         </RunConfiguration>
                    </RunSettings>"
            };

            Assert.IsFalse(
                tp.StartTestSession(
                    new Mock<IRequestData>().Object,
                    testSessionCriteria,
                    mockEventsHandler.Object));

            mockEventsHandler.Verify(
                eh => eh.HandleStartTestSessionComplete(null),
                Times.Once);
        }

        [TestMethod]
        public void StartTestSessionShouldReturnTrueIfTestSessionManagerStartSessionReturnsTrue()
        {
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            var testSessionCriteria = new StartTestSessionCriteria()
            {
                RunSettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <RunSettings>
                         <RunConfiguration>
                           <DesignMode>true</DesignMode>
                         </RunConfiguration>
                    </RunSettings>"
            };

            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();
            var mockTestSessionManager = new Mock<IProxyTestSessionManager>();
            mockTestSessionManager.Setup(
                tsm => tsm.StartSession(It.IsAny<ITestSessionEventsHandler>()))
                .Returns(true);
            testEngine.Setup(
                te => te.GetTestSessionManager(
                    It.IsAny<IRequestData>(),
                    It.IsAny<StartTestSessionCriteria>()))
                .Returns(mockTestSessionManager.Object);

            Assert.IsTrue(
               tp.StartTestSession(
                   new Mock<IRequestData>().Object,
                   testSessionCriteria,
                   mockEventsHandler.Object));

            mockTestSessionManager.Verify(
                tsm => tsm.StartSession(mockEventsHandler.Object));
        }

        [TestMethod]
        public void StartTestSessionShouldReturnFalseIfTestSessionManagerStartSessionReturnsFalse()
        {
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            var testSessionCriteria = new StartTestSessionCriteria()
            {
                RunSettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <RunSettings>
                         <RunConfiguration>
                           <DesignMode>true</DesignMode>
                         </RunConfiguration>
                    </RunSettings>"
            };

            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();
            var mockTestSessionManager = new Mock<IProxyTestSessionManager>();
            mockTestSessionManager.Setup(
                tsm => tsm.StartSession(It.IsAny<ITestSessionEventsHandler>()))
                .Returns(false);
            testEngine.Setup(
                te => te.GetTestSessionManager(
                    It.IsAny<IRequestData>(),
                    It.IsAny<StartTestSessionCriteria>()))
                .Returns(mockTestSessionManager.Object);

            Assert.IsFalse(
               tp.StartTestSession(
                   new Mock<IRequestData>().Object,
                   testSessionCriteria,
                   mockEventsHandler.Object));

            mockTestSessionManager.Verify(
                tsm => tsm.StartSession(mockEventsHandler.Object));
        }

        private void InvokeCreateDiscoveryRequest(TestPlatformOptions options = null)
        {
            discoveryManager.Setup(dm => dm.Initialize(false)).Verifiable();
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "foo" }, 1, null);
            hostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources))
                .Returns(discoveryCriteria.Sources);

            testEngine.Setup(te => te.GetDiscoveryManager(mockRequestData.Object, hostManager.Object, It.IsAny<DiscoveryCriteria>())).Returns(discoveryManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);
            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);

            tp.CreateDiscoveryRequest(mockRequestData.Object, discoveryCriteria, options);
        }

        private void InvokeCreateTestRunRequest(TestPlatformOptions options = null)
        {
            executionManager.Setup(dm => dm.Initialize(false)).Verifiable();
            testEngine.Setup(te => te.GetExecutionManager(mockRequestData.Object, hostManager.Object, It.IsAny<TestRunCriteria>())).Returns(executionManager.Object);
            testEngine.Setup(te => te.GetExtensionManager()).Returns(extensionManager.Object);
            testEngine.Setup(te => te.GetLoggerManager(mockRequestData.Object)).Returns(loggerManager.Object);

            var tp = new TestableTestPlatform(testEngine.Object, hostManager.Object);
            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 10);
            hostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources))
                .Returns(testRunCriteria.Sources);

            tp.CreateTestRunRequest(mockRequestData.Object, testRunCriteria, options);
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
                return hostProvider;
            }
        }
    }
}
