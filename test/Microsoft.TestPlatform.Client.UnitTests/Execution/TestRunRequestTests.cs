// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Client.Execution;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Engine;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

    [TestClass]
    public class TestRunRequestTests
    {
        TestRunRequest testRunRequest;
        Mock<IProxyExecutionManager> executionManager;
        private readonly Mock<ITestLoggerManager> loggerManager;
        TestRunCriteria testRunCriteria;
        private Mock<IRequestData> mockRequestData;

        private Mock<IDataSerializer> mockDataSerializer;

        public TestRunRequestTests()
        {
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1);
            executionManager = new Mock<IProxyExecutionManager>();
            this.loggerManager = new Mock<ITestLoggerManager>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
            this.mockDataSerializer = new Mock<IDataSerializer>();
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object, this.mockDataSerializer.Object);
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
        public void AbortIfTestRunRequestDisposedShouldNotThrowException()
        {
            testRunRequest.Dispose();
            testRunRequest.Abort();
        }

        [TestMethod]
        public void AbortIfTestRunStateIsNotInProgressShouldNotCallExecutionManagerAbort()
        {
            //ExecuteAsync has not been called, so State is not InProgress
            testRunRequest.Abort();
            executionManager.Verify(dm => dm.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Never);
        }

        [TestMethod]
        public void AbortIfDiscoveryIsinProgressShouldCallDiscoveryManagerAbort()
        {
            // Set the State to InProgress
            testRunRequest.ExecuteAsync();

            testRunRequest.Abort();
            executionManager.Verify(dm => dm.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Once);
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
        public void CancelAsyncIfTestRunRequestDisposedShouldNotThrowException()
        {
            testRunRequest.Dispose();
            testRunRequest.CancelAsync();
        }

        [TestMethod]
        public void CancelAsyncIfTestRunStateNotInProgressWillNotCallExecutionManagerCancel()
        {
            testRunRequest.CancelAsync();
            executionManager.Verify(dm => dm.Cancel(It.IsAny<ITestRunEventsHandler>()), Times.Never);
        }

        [TestMethod]
        public void CancelAsyncIfTestRunStateInProgressCallsExecutionManagerCancel()
        {
            testRunRequest.ExecuteAsync();
            testRunRequest.CancelAsync();
            executionManager.Verify(dm => dm.Cancel(It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void OnTestSessionTimeoutShouldCallAbort()
        {
            this.testRunRequest.ExecuteAsync();
            this.testRunRequest.OnTestSessionTimeout(null);
            this.executionManager.Verify(o => o.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Once);
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
            var testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object);

            ManualResetEvent onTestSessionTimeoutCalled = new ManualResetEvent(true);
            onTestSessionTimeoutCalled.Reset();
            executionManager.Setup(o => o.Abort(It.IsAny<ITestRunEventsHandler>())).Callback(() => onTestSessionTimeoutCalled.Set());

            testRunRequest.ExecuteAsync();
            onTestSessionTimeoutCalled.WaitOne(20 * 1000);

            executionManager.Verify(o => o.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        /// <summary>
        /// Test session timeout should be infinity if TestSessionTimeout is 0.
        /// </summary>
        [TestMethod]
        public void OnTestSessionTimeoutShouldNotGetCalledWhenTestSessionTimeoutIsZero()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TestSessionTimeout>0</TestSessionTimeout>
                     </RunConfiguration>
                </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, true, settingsXml);
            var executionManager = new Mock<IProxyExecutionManager>();
            var testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object);

            executionManager.Setup(o => o.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).Callback(() => System.Threading.Thread.Sleep(5 * 1000));

            testRunRequest.ExecuteAsync();

            executionManager.Verify(o => o.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Never);
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

            // Call should NOT fail even if on raw message received is not registered.
            testRunRequest.HandleRawMessage(rawMessage);

            EventHandler<string> handler = (sender, e) => { messageReceived = e; };
            testRunRequest.OnRawMessageReceived += handler;

            testRunRequest.HandleRawMessage(rawMessage);

            Assert.AreEqual(rawMessage, messageReceived, "RunRequest should just pass the message as is.");
            testRunRequest.OnRawMessageReceived -= handler;
        }

        [TestMethod]
        public void HandleRawMessageShouldAddVSTestDataPointsIfTelemetryOptedIn()
        {
            bool onDiscoveryCompleteInvoked = true;
            this.mockRequestData.Setup(x => x.IsTelemetryOptedIn).Returns(true);
            this.testRunRequest.OnRawMessageReceived += (object sender, string e) =>
                {
                    onDiscoveryCompleteInvoked = true;
                };

            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete });

            this.mockDataSerializer.Setup(x => x.DeserializePayload<TestRunCompletePayload>(It.IsAny<Message>()))
                .Returns(new TestRunCompletePayload()
                {
                    TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.MinValue)
                });

            this.testRunRequest.HandleRawMessage(string.Empty);

            this.mockDataSerializer.Verify(x => x.SerializePayload(It.IsAny<string>(), It.IsAny<TestRunCompletePayload>()), Times.Once);
            this.mockRequestData.Verify(x => x.MetricsCollection, Times.AtLeastOnce);
            Assert.IsTrue(onDiscoveryCompleteInvoked);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeHandleTestRunCompleteOfLoggerManager()
        {
            this.loggerManager.Setup(x => x.LoggersInitialized).Returns(true);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete });

            var testRunCompleteEvent = new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null,
                null, TimeSpan.FromSeconds(0));
            this.mockDataSerializer.Setup(x => x.DeserializePayload<TestRunCompletePayload>(It.IsAny<Message>()))
                .Returns(new TestRunCompletePayload()
                {
                    TestRunCompleteArgs = testRunCompleteEvent
                });

            this.testRunRequest.ExecuteAsync();
            this.testRunRequest.HandleRawMessage(string.Empty);

            this.loggerManager.Verify(lm => lm.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldNotInvokeHandleTestRunCompleteOfLoggerManagerWhenNoLoggersInitiailized()
        {
            this.loggerManager.Setup(x => x.LoggersInitialized).Returns(false);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete });

            var testRunCompleteEvent = new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null,
                null, TimeSpan.FromSeconds(0));
            this.mockDataSerializer.Setup(x => x.DeserializePayload<TestRunCompletePayload>(It.IsAny<Message>()))
                .Returns(new TestRunCompletePayload()
                {
                    TestRunCompleteArgs = testRunCompleteEvent
                });

            this.testRunRequest.ExecuteAsync();
            this.testRunRequest.HandleRawMessage(string.Empty);

            this.loggerManager.Verify(lm => lm.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void HandleRawMessageShouldInvokeShouldInvokeHandleTestRunStatsChangeOfLoggerManagerWhenLastChunkAvailable()
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

            this.loggerManager.Setup(x => x.LoggersInitialized).Returns(true);
            this.mockDataSerializer.Setup(x => x.DeserializeMessage(It.IsAny<string>()))
                .Returns(new Message() { MessageType = MessageType.ExecutionComplete });

            var testRunChangedEventArgs = new TestRunChangedEventArgs(mockStats.Object, testResults, activeTestCases);
            var testRunCompleteEvent = new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null,
                null, TimeSpan.FromSeconds(0));

            this.mockDataSerializer.Setup(x => x.DeserializePayload<TestRunCompletePayload>(It.IsAny<Message>()))
                .Returns(new TestRunCompletePayload()
                {
                    TestRunCompleteArgs = testRunCompleteEvent,
                    LastRunTests = testRunChangedEventArgs
                });

            this.testRunRequest.ExecuteAsync();
            this.testRunRequest.HandleRawMessage(string.Empty);

            loggerManager.Verify(lm => lm.HandleTestRunStatsChange(testRunChangedEventArgs), Times.Once);
            loggerManager.Verify(lm => lm.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunStatsChangeShouldInvokeHandleTestRunStatsChangeOfLoggerManager()
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
            testRunRequest.HandleTestRunStatsChange(testRunChangedEventArgs);

            loggerManager.Verify(lm => lm.HandleTestRunStatsChange(testRunChangedEventArgs), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldInvokeHandleTestRunStatsChangeOfLoggerManagerWhenLastChunkAvailable()
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
            var testRunCompleteEvent = new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null,
                null, TimeSpan.FromSeconds(0));

            testRunRequest.ExecuteAsync();
            testRunRequest.HandleTestRunComplete(testRunCompleteEvent, testRunChangedEventArgs, null, null);

            loggerManager.Verify(lm => lm.HandleTestRunStatsChange(testRunChangedEventArgs), Times.Once);
            loggerManager.Verify(lm => lm.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldInvokeHandleTestRunCompleteOfLoggerManager()
        {
            var testRunCompleteEvent = new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null,
                null, TimeSpan.FromSeconds(0));

            testRunRequest.ExecuteAsync();
            testRunRequest.HandleTestRunComplete(testRunCompleteEvent, null, null, null);

            loggerManager.Verify(lm => lm.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleLogMessageShouldInvokeHandleLogMessageOfLoggerManager()
        {
            testRunRequest.HandleLogMessage(TestMessageLevel.Error, "hello");
            loggerManager.Verify(lm => lm.HandleTestRunMessage(It.IsAny<TestRunMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>();
            dict.Add("DummyMessage", "DummyValue");

            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            this.testRunRequest.ExecuteAsync();
            var testRunCompeleteEventsArgs = new TestRunCompleteEventArgs(
                new TestRunStatistics(1, null),
                false,
                false,
                null,
                null,
                TimeSpan.FromSeconds(0));
            testRunCompeleteEventsArgs.Metrics = dict;

            // Act
            this.testRunRequest.HandleTestRunComplete(testRunCompeleteEventsArgs, null, null, null);

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecForRun, It.IsAny<double>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add("DummyMessage", "DummyValue"), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldHandleListAttachments()
        {
            bool attachmentsFound = false;
            this.testRunRequest.OnRunCompletion += (s, e) =>
             {
                 attachmentsFound = e.AttachmentSets != null && e.AttachmentSets.Count == 1;
             };

            List<AttachmentSet> attachmentSets = new List<AttachmentSet> { new AttachmentSet(new Uri("datacollector://attachment"), "datacollectorAttachment") };

            this.testRunRequest.ExecuteAsync();
            var testRunCompeleteEventsArgs = new TestRunCompleteEventArgs(
                new TestRunStatistics(1, null),
                false,
                false,
                null,
                null,
                TimeSpan.FromSeconds(0));

            // Act
            this.testRunRequest.HandleTestRunComplete(testRunCompeleteEventsArgs, null, attachmentSets, null);

            // Verify.
            Assert.IsTrue(attachmentsFound);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldHandleCollectionAttachments()
        {
            bool attachmentsFound = false;
            this.testRunRequest.OnRunCompletion += (s, e) =>
            {
                attachmentsFound = e.AttachmentSets != null && e.AttachmentSets.Count == 1;
            };

            Collection<AttachmentSet> attachmentSets = new Collection<AttachmentSet>(new List<AttachmentSet> { new AttachmentSet(new Uri("datacollector://attachment"), "datacollectorAttachment") });

            this.testRunRequest.ExecuteAsync();
            var testRunCompeleteEventsArgs = new TestRunCompleteEventArgs(
                new TestRunStatistics(1, null),
                false,
                false,
                null,
                null,
                TimeSpan.FromSeconds(0));

            // Act
            this.testRunRequest.HandleTestRunComplete(testRunCompeleteEventsArgs, null, attachmentSets, null);

            // Verify.
            Assert.IsTrue(attachmentsFound);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldHandleEmptyAttachments()
        {
            bool attachmentsFound = false;
            this.testRunRequest.OnRunCompletion += (s, e) =>
            {
                attachmentsFound = (e.AttachmentSets.Count == 0);
            };

            this.testRunRequest.ExecuteAsync();
            var testRunCompeleteEventsArgs = new TestRunCompleteEventArgs(
                new TestRunStatistics(1, null),
                false,
                false,
                null,
                null,
                TimeSpan.FromSeconds(0));

            // Act
            this.testRunRequest.HandleTestRunComplete(testRunCompeleteEventsArgs, null, null, null);

            // Verify.
            Assert.IsTrue(attachmentsFound);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldCloseExecutionManager()
        {
            var events = new List<string>();
            this.executionManager.Setup(em => em.Close()).Callback(() => events.Add("close"));
            this.testRunRequest.OnRunCompletion += (s, e) => events.Add("complete");
            this.testRunRequest.ExecuteAsync();

            this.testRunRequest.HandleTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(1, null), false, false, null, null, TimeSpan.FromSeconds(0)), null, null, null);

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
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object);

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
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object);

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
            testRunRequest = new TestRunRequest(this.mockRequestData.Object, testRunCriteria, executionManager.Object, loggerManager.Object);

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

        [TestMethod]
        public void ExecuteAsyncShouldInvokeHandleTestRunStartOfLoggerManager()
        {
            this.testRunRequest.ExecuteAsync();

            loggerManager.Verify(lm => lm.HandleTestRunStart(It.IsAny<TestRunStartEventArgs>()), Times.Once);
        }
    }
}
