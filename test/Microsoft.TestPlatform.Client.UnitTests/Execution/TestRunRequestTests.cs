// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.Execution
{
    using Client.Execution;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Host;
    using ObjectModel.Engine;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    [TestClass]
    public class TestRunRequestTests
    {
        TestRunRequest testRunRequest;
        Mock<IProxyExecutionManager> executionManager;
        TestRunCriteria testRunCriteria;

        [TestInitialize]
        public void TestInit()
        {
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1);
            executionManager = new Mock<IProxyExecutionManager>();
            testRunRequest = new TestRunRequest(testRunCriteria, executionManager.Object);
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
        public void HandleTestRunStatsChangeShouldInokeListenersWithTestRunChangedEventArgs()
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
        public void LaunchProcessWithDebuggerAttachedShouldNotCallCustomLauncherIfTestRunIsNotInProgress()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testRunCriteria = new TestRunCriteria(new List<string> { "foo" }, 1, false, null, TimeSpan.Zero, mockCustomLauncher.Object);
            executionManager = new Mock<IProxyExecutionManager>();
            testRunRequest = new TestRunRequest(testRunCriteria, executionManager.Object);

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
            testRunRequest = new TestRunRequest(testRunCriteria, executionManager.Object);

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
            testRunRequest = new TestRunRequest(testRunCriteria, executionManager.Object);

            testRunRequest.ExecuteAsync();

            var testProcessStartInfo = new TestProcessStartInfo();
            mockCustomLauncher.Setup(ml => ml.IsDebug).Returns(true);
            testRunRequest.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

            mockCustomLauncher.Verify(ml => ml.LaunchTestHost(testProcessStartInfo), Times.Once);
        }
    }
}
