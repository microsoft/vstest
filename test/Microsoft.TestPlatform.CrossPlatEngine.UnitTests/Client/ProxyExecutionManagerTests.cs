// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class ProxyExecutionManagerTests
    {
        private ProxyExecutionManager testExecutionManager;

        private Mock<ITestHostManager> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.testableClientConnectionTimeout);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.testExecutionManager.Initialize(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new string[] { "e1.dll", "e2.dll" };

                // Setup Mocks.
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                this.testExecutionManager.Initialize(this.mockTestHostManager.Object);

                // Also verify that we have waited for client connection.
                this.mockRequestSender.Verify(s => s.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
                this.mockRequestSender.Verify(
                    s => s.InitializeExecution(extensions, true),
                    Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void StartTestRunShouldNotIntializeIfDoneSoAlready()
        {
            this.testExecutionManager.Initialize(this.mockTestHostManager.Object);

            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            var mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.AtMostOnce);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.AtMostOnce);
        }

        [TestMethod]
        public void StartTestRunShouldIntializeIfNotInitializedAlready()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            var mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldThrowExceptionIfClientConnectionTimeout()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            // Act.
            Assert.ThrowsException<TestPlatformException>(
                () => this.testExecutionManager.StartTestRun(null, null));
        }


        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForSourcesThroughTheServer()
        {
            TestRunCriteriaWithSources testRunCriteriaPassed = null;

            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), null))
                .Callback(
                    (TestRunCriteriaWithSources criteria, ITestRunEventsHandler sink) =>
                        {
                            testRunCriteriaPassed = criteria;
                        });
            var mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            // Assert.
            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(
                mockTestRunCriteria.Object.AdapterSourceMap.Keys,
                testRunCriteriaPassed.AdapterSourceMap.Keys);
            CollectionAssert.AreEqual(
                mockTestRunCriteria.Object.AdapterSourceMap.Values,
                testRunCriteriaPassed.AdapterSourceMap.Values);
            Assert.AreEqual(
                mockTestRunCriteria.Object.FrequencyOfRunStatsChangeEvent,
                testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(
                mockTestRunCriteria.Object.RunStatsChangeEventTimeout,
                testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(
                mockTestRunCriteria.Object.TestRunSettings,
                testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForTestsThroughTheServer()
        {
            TestRunCriteriaWithTests testRunCriteriaPassed = null;

            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithTests>(), null))
                .Callback(
                    (TestRunCriteriaWithTests criteria, ITestRunEventsHandler sink) =>
                    {
                        testRunCriteriaPassed = criteria;
                    });
            var mockTestRunCriteria =
                new Mock<TestRunCriteria>(
                    new List<TestCase> { new TestCase("A.C.M", new System.Uri("executor://dummy"), "source.dll") },
                    10);

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            // Assert.
            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(mockTestRunCriteria.Object.Tests.ToList(), testRunCriteriaPassed.Tests.ToList());
            Assert.AreEqual(
                mockTestRunCriteria.Object.FrequencyOfRunStatsChangeEvent,
                testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(
                mockTestRunCriteria.Object.RunStatsChangeEventTimeout,
                testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(
                mockTestRunCriteria.Object.TestRunSettings,
                testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void DisposeShouldSignalServerSessionEnd()
        {
            this.testExecutionManager.Dispose();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldSignalServerSessionEndEachTime()
        {
            this.testExecutionManager.Dispose();
            this.testExecutionManager.Dispose();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Exactly(2));
        }

        private void SignalEvent(ManualResetEvent manualResetEvent)
        {
            // Wait for the 100 ms.
            Task.Delay(200).Wait();

            manualResetEvent.Set();
        }
    }
}
