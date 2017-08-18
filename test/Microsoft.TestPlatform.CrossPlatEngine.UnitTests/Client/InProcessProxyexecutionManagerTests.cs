// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InProcessProxyExecutionManagerTests
    {
        private Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private InProcessProxyExecutionManager inProcessProxyExecutionManager;
        private Mock<IExecutionManager> mockExecutionManager;

        [TestInitialize]
        public void TestInitialize()
        {
            this.mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            this.mockExecutionManager = new Mock<IExecutionManager>();
            this.inProcessProxyExecutionManager = new InProcessProxyExecutionManager(this.mockTestHostManagerFactory.Object);

            this.mockTestHostManagerFactory.Setup(o => o.GetExecutionManager()).Returns(this.mockExecutionManager.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.mockExecutionManager = null;
            this.mockTestHostManagerFactory = null;
            this.inProcessProxyExecutionManager = null;
        }

        [TestMethod]
        public void InitializeShouldCallExecutionManagerInitializeWithEmptyIEnumerable()
        {
            this.inProcessProxyExecutionManager.Initialize();
            this.mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSetIsInitializedTotrue()
        {
            this.inProcessProxyExecutionManager.Initialize();
            Assert.IsTrue(this.inProcessProxyExecutionManager.IsInitialized);
        }

        [TestMethod]
        public void InitializeShouldCallExecutionManagerInitializeWithEmptyIEnumerableOnlyOnce()
        {
            this.inProcessProxyExecutionManager.Initialize();
            this.inProcessProxyExecutionManager.Initialize();
            this.mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallInitializeIfNotAlreadyInitialized()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotCallInitializeIfAlreadyInitialized()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            this.inProcessProxyExecutionManager.Initialize();
            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithAdapterSourceMap()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithTestCase()
        {
            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), "s.dll") },
                    frequencyOfRunStatsChangeEvent: 10);
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRunComplete()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, mockTestRunEventsHandler.Object)).Callback(
                () => throw new Exception());

            mockTestRunEventsHandler.Setup(o => o.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "ITestRunEventsHandler.HandleTestRunComplete should get called");
        }

        [TestMethod]
        public void AbortShouldCallExecutionManagerAbort()
        {
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.Abort()).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.Abort();

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.Abort should get called");
        }

        [TestMethod]
        public void CancelShouldCallExecutionManagerCancel()
        {
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.Cancel()).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.Cancel();

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.Abort should get called");
        }
    }
}
