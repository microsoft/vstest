// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class ProxyExecutionManagerTests
    {
        private readonly ProxyExecutionManager testExecutionManager;

        private readonly Mock<ITestRuntimeProvider> mockTestHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private readonly Mock<TestRunCriteria> mockTestRunCriteria;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int clientConnectionTimeout = 400;

        public ProxyExecutionManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);
            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.clientConnectionTimeout);

            // Default to shared test host
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(true);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.testExecutionManager.Initialize();

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new[] { "c:\\e1.dll", "c:\\e2.dll" };
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                this.testExecutionManager.Initialize();

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
        public void InitializeShouldNotInitializeExtensionsIfTestHostIsNotShared()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(false);

            this.testExecutionManager.Initialize();

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldQueryTestHostManagerForExtensions()
        {
            TestPluginCache.Instance = null;
            try
            {
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(new[] { "c:\\e1.dll" });
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll" });

                this.testExecutionManager.Initialize();

                this.mockRequestSender.Verify(s => s.InitializeExecution(new[] { "he1.dll", "c:\\e1.dll" }, true), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void StartTestRunShouldNotIntializeIfDoneSoAlready()
        {
            this.testExecutionManager.Initialize();
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.AtMostOnce);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>()), Times.AtMostOnce);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeIfNotInitializedAlready()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsIfTestHostIsNotShared()
        {
            TestPluginCache.Instance = null;
            TestPluginCacheTests.SetupMockAdditionalPathExtensions(new[] { "x.dll" });
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(false);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfClientConnectionTimeout()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            Assert.ThrowsException<TestPlatformException>(() => this.testExecutionManager.SetupChannel(new List<string> { "source.dll" }));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleTestRunComplete()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);
            mockTestRunEventsHandler.Verify(s => s.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null));
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForSourcesThroughTheServer()
        {
            TestRunCriteriaWithSources testRunCriteriaPassed = null;
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), null))
                .Callback(
                    (TestRunCriteriaWithSources criteria, ITestRunEventsHandler sink) =>
                        {
                            testRunCriteriaPassed = criteria;
                        });

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(this.mockTestRunCriteria.Object.AdapterSourceMap.Keys, testRunCriteriaPassed.AdapterSourceMap.Keys);
            CollectionAssert.AreEqual(this.mockTestRunCriteria.Object.AdapterSourceMap.Values, testRunCriteriaPassed.AdapterSourceMap.Values);
            Assert.AreEqual(this.mockTestRunCriteria.Object.FrequencyOfRunStatsChangeEvent, testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(this.mockTestRunCriteria.Object.RunStatsChangeEventTimeout, testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(this.mockTestRunCriteria.Object.TestRunSettings, testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForTestsThroughTheServer()
        {
            TestRunCriteriaWithTests testRunCriteriaPassed = null;
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithTests>(), null))
                .Callback(
                    (TestRunCriteriaWithTests criteria, ITestRunEventsHandler sink) =>
                    {
                        testRunCriteriaPassed = criteria;
                    });
            var runCriteria = new Mock<TestRunCriteria>(
                                            new List<TestCase> { new TestCase("A.C.M", new System.Uri("executor://dummy"), "source.dll") },
                                            10);

            this.testExecutionManager.StartTestRun(runCriteria.Object, null);

            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(runCriteria.Object.Tests.ToList(), testRunCriteriaPassed.Tests.ToList());
            Assert.AreEqual(
                runCriteria.Object.FrequencyOfRunStatsChangeEvent,
                testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(
                runCriteria.Object.RunStatsChangeEventTimeout,
                testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(
                runCriteria.Object.TestRunSettings,
                testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void CloseShouldSignalServerSessionEnd()
        {
            this.testExecutionManager.Close();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldSignalServerSessionEndEachTime()
        {
            this.testExecutionManager.Close();
            this.testExecutionManager.Close();

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
