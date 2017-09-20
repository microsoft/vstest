// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Client.Execution;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Engine;

    [TestClass]
    public class TestRunRequestTests
    {
        TestRunRequest testRunRequest;
        Mock<IProxyExecutionManager> executionManager;
        TestRunCriteria testRunCriteria;
        private Mock<IRequestData> mockRequestData;

        public TestRunRequestTests()
        {
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1);
            executionManager = new Mock<IProxyExecutionManager>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object);
        }

        [TestMethod]
        public void ConstructorSetsTestRunCriteriaExecutionManagerAndState()
        {
            Assert.AreEqual(TestRunState.Pending, testRunRequest.State);
            Assert.AreEqual(testRunCriteria, testRunRequest.TestRunConfiguration);
            Assert.AreEqual(executionManager.Object, testRunRequest.ExecutionManager);
        }

        [TestMethod]
        public void ExecuteAsycIfTestRunRequestIsDisposedThrowsObjectDisposedException()
        {
            testRunRequest.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => testRunRequest.ExecuteAsync());
        }

        [TestMethod]
        public void ExecuteAsycIfStateIsNotPendingThrowsInvalidOperationException()
        {
            testRunRequest.ExecuteAsync();
            Assert.ThrowsException<InvalidOperationException>(() => testRunRequest.ExecuteAsync());
        }

        [TestMethod]
        public void ExecuteAsyncSetsStateToInProgressAndCallManagerToStartTestRun()
        {
            testRunRequest.ExecuteAsync();

            Assert.AreEqual(TestRunState.InProgress, testRunRequest.State);
            executionManager.Verify(em => em.StartTestRun(testRunCriteria, testRunRequest), Times.Once);
        }

        [TestMethod]
        public void ExecuteAsyncIfStartTestRunThrowsExceptionSetsStateToPendingAndThrowsThatException()
        {
            executionManager.Setup(em => em.StartTestRun(testRunCriteria, testRunRequest)).Throws(new Exception("DummyException"));
            try
            {
                testRunRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is Exception);
                Assert.AreEqual("DummyException", ex.Message);
                Assert.AreEqual(TestRunState.Pending, testRunRequest.State);
            }
        }

        [TestMethod]
        public void AbortIfTestRunRequestDisposedShouldThrowObjectDisposedException()
        {
            testRunRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => testRunRequest.Abort());
        }

        [TestMethod]
        public void AbortIfTestRunStateIsNotInProgressShouldNotCallExecutionManagerAbort()
        {
            //ExecuteAsync has not been called, so State is not InProgress
            testRunRequest.Abort();
            executionManager.Verify(dm => dm.Abort(), Times.Never);
        }

        [TestMethod]
        public void AbortIfDiscoveryIsinProgressShouldCallDiscoveryManagerAbort()
        {
            // Set the State to InProgress
            testRunRequest.ExecuteAsync();

            testRunRequest.Abort();
            executionManager.Verify(dm => dm.Abort(), Times.Once);
        }

        [TestMethod]
        public void WaitForCompletionIfTestRunRequestDisposedShouldThrowObjectDisposedException()
        {
            testRunRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => testRunRequest.WaitForCompletion());
        }

        [TestMethod]
        public void WaitForCompletionIfTestRunStatePendingShouldThrowInvalidOperationException()
        {
            Assert.ThrowsException<InvalidOperationException>(() => testRunRequest.WaitForCompletion());
        }

        [TestMethod]
        public void CancelAsyncIfTestRunRequestDisposedThrowsObjectDisposedException()
        {
            testRunRequest.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => testRunRequest.CancelAsync());
        }

        [TestMethod]
        public void CancelAsyncIfTestRunStateNotInProgressWillNotCallExecutionManagerCancel()
        {
            testRunRequest.CancelAsync();
            executionManager.Verify(dm => dm.Cancel(), Times.Never);
        }

        [TestMethod]
        public void CancelAsyncIfTestRunStateInProgressCallsExecutionManagerCancel()
        {
            testRunRequest.ExecuteAsync();
            testRunRequest.CancelAsync();
            executionManager.Verify(dm => dm.Cancel(), Times.Once);
        }


        [TestMethod]
        public void OnTestSessionTimeoutShouldCallCancel()
        {
            this.testRunRequest.ExecuteAsync();
            this.testRunRequest.OnTestSessionTimeout(null);
            this.executionManager.Verify(o => o.Cancel(), Times.Once);
        }

        [TestMethod]
        public void OnTestSessionTimeoutShouldLogMessage()
        {
            bool handleLogMessageCalled = false;
            bool handleRawMessageCalled = false;

            this.testRunRequest.TestRunMessage += (object sender, TestRunMessageEventArgs e) =>
                {
                    handleLogMessageCalled = true;
                };

            this.testRunRequest.OnRawMessageReceived += (object sender, string message) =>
                {
                    handleRawMessageCalled = true;
                };

            this.testRunRequest.OnTestSessionTimeout(null);

            Assert.IsTrue(handleLogMessageCalled, "OnTestSessionTimeout should call HandleLogMessage");
            Assert.IsTrue(handleRawMessageCalled, "OnTestSessionTimeout should call HandleRawMessage");
        }

        [TestMethod]
        public void OnTestSessionTimeoutShouldGetCalledWhenExecutionCrossedTestSessionTimeout()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TestSessionTimeout>1000</TestSessionTimeout>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, true, settingsXml);
            var executionManager = new Mock<IProxyExecutionManager>();
            var testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object);

            ManualResetEvent onTestSessionTimeoutCalled = new ManualResetEvent(true);
            onTestSessionTimeoutCalled.Reset();
            executionManager.Setup(o => o.Cancel()).Callback(() => onTestSessionTimeoutCalled.Set());

            testRunRequest.ExecuteAsync();
            onTestSessionTimeoutCalled.WaitOne(20 * 1000);

            executionManager.Verify(o => o.Cancel(), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunStatsChangeShouldInvokeListenersWithTestRunChangedEventArgs()
        {
            var mockStats = new Mock<ITestRunStatistics>();

            var testResults = new List<ObjectModel.TestResult>
                                  {
                                      new ObjectModel.TestResult(
                                          new ObjectModel.TestCase(
                                          "A.C.M",
                                          new Uri("executor://dummy"),
                                          "A"))
                                  };
            var activeTestCases = new List<ObjectModel.TestCase>
                                      {
                                          new ObjectModel.TestCase(
                                              "A.C.M2",
                                              new Uri("executor://dummy"),
                                              "A")
                                      };
            var testRunChangedEventArgs = new TestRunChangedEventArgs(mockStats.Object, testResults, activeTestCases);
            TestRunChangedEventArgs receivedArgs = null;

            testRunRequest.OnRunStatsChange += (object sender, TestRunChangedEventArgs e) =>
                {
                    receivedArgs = e;
                };

            // Act.
            testRunRequest.HandleTestRunStatsChange(testRunChangedEventArgs);

            // Assert.
            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(testRunChangedEventArgs.TestRunStatistics, receivedArgs.TestRunStatistics);
            CollectionAssert.AreEqual(
                testRunChangedEventArgs.NewTestResults.ToList(),
                receivedArgs.NewTestResults.ToList());
            CollectionAssert.AreEqual(testRunChangedEventArgs.ActiveTests.ToList(), receivedArgs.ActiveTests.ToList());
        }

        [TestMethod]
        public void HandleRawMessageShouldCallOnRawMessageReceived()
        {
            string rawMessage = "HelloWorld";
            string messageReceived = null;

            // Call should NOT fail even if onrawmessagereceived is not registered.
            testRunRequest.HandleRawMessage(rawMessage);

            EventHandler<string> handler = (sender, e) => { messageReceived = e; };
            testRunRequest.OnRawMessageReceived += handler;

            testRunRequest.HandleRawMessage(rawMessage);

            Assert.AreEqual(rawMessage, messageReceived, "RunRequest should just pass the message as is.");
            testRunRequest.OnRawMessageReceived -= handler;
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, string>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            this.testRunRequest.ExecuteAsync();

            // Act
            this.testRunRequest.HandleTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null, null, TimeSpan.FromSeconds(0), dict), null, null, null);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecForRun, It.IsAny<string>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add("DummyMessage", "DummyValue"), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldPublishMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var mockMetricsPublisher = new Mock<IMetricsPublisher>();
            var mockDataSerializer = new Mock<IDataSerializer>();

            var dict = new Dictionary<string, string>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            var testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, mockDataSerializer.Object, mockMetricsPublisher.Object);
            testRunRequest.ExecuteAsync();

            // Act
            testRunRequest.HandleTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null, null, TimeSpan.FromSeconds(0), dict), null, null, null);

            // Verify.
            mockMetricsPublisher.Verify(rd => rd.PublishMetrics(TelemetryDataConstants.TestExecutionCompleteEvent, dict), Times.Once);
        }

        [TestMethod]
        public void DisposeShouldDisposeMetricsPublisher()
        {
            var mockMetricsPublisher = new Mock<IMetricsPublisher>();
            var mockDataSerializer = new Mock<IDataSerializer>();

            var testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, mockDataSerializer.Object, mockMetricsPublisher.Object);
            testRunRequest.ExecuteAsync();

            // Act
            testRunRequest.Dispose();

            // Verify.
            mockMetricsPublisher.Verify(rd => rd.Dispose(), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldCloseExecutionManager()
        {
            var events = new List<string>();
            this.executionManager.Setup(em => em.Close()).Callback(() => events.Add("close"));
            this.testRunRequest.OnRunCompletion += (s, e) => events.Add("complete");
            this.testRunRequest.ExecuteAsync();

            this.testRunRequest.HandleTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null, null, TimeSpan.FromSeconds(0), null), null, null, null);

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("close", events[0]);
            Assert.AreEqual("complete", events[1]);
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldNotCallCustomLauncherIfTestRunIsNotInProgress()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            executionManager = new Mock<IProxyExecutionManager>();
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object);

            var testProcessStartInfo = new TestProcessStartInfo();
            testRunRequest.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            mockCustomLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Never);
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldNotCallCustomLauncherIfLauncherIsNotDebug()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            executionManager = new Mock<IProxyExecutionManager>();
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object);

            testRunRequest.ExecuteAsync();

            var testProcessStartInfo = new TestProcessStartInfo();
            testRunRequest.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            mockCustomLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Never);
        }

        [TestMethod]
        public void LaunchProcessWithDebuggerAttachedShouldCallCustomLauncherIfLauncherIsDebugAndRunInProgress()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            executionManager = new Mock<IProxyExecutionManager>();
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object);

            testRunRequest.ExecuteAsync();

            var testProcessStartInfo = new TestProcessStartInfo();
            mockCustomLauncher.Setup(ml => ml.IsDebug).Returns(true);
            testRunRequest.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            mockCustomLauncher.Verify(ml => ml.LaunchTestHost(testProcessStartInfo), Times.Once);
        }

        /// <summary>
        /// ExecuteAsync should invoke OnRunStart event.
        /// </summary>
        [TestMethod]
        public void ExecuteAsyncShouldInvokeOnRunStart()
        {
            bool onRunStartHandlerCalled = false;
            this.testRunRequest.OnRunStart += (s, e) => onRunStartHandlerCalled = true;

            // Action
            this.testRunRequest.ExecuteAsync();

            // Assert
            Assert.IsTrue(onRunStartHandlerCalled, "ExecuteAsync should invoke OnRunstart event");
        }
    }
}
