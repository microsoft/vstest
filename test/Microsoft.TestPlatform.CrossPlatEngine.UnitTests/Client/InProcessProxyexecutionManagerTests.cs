﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InProcessProxyExecutionManagerTests
    {
        private Mock<ITestHostManagerFactory> mockTestHostManagerFactory;
        private InProcessProxyExecutionManager inProcessProxyExecutionManager;
        private Mock<IExecutionManager> mockExecutionManager;
        private Mock<ITestRuntimeProvider> mockTestHostManager;

        public InProcessProxyExecutionManagerTests()
        {
            mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            mockExecutionManager = new Mock<IExecutionManager>();
            mockTestHostManager = new Mock<ITestRuntimeProvider>();
            mockTestHostManagerFactory.Setup(o => o.GetExecutionManager()).Returns(mockExecutionManager.Object);
            inProcessProxyExecutionManager = new InProcessProxyExecutionManager(mockTestHostManager.Object, mockTestHostManagerFactory.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockExecutionManager = null;
            mockTestHostManagerFactory = null;
            inProcessProxyExecutionManager = null;
            mockTestHostManager = null;
        }


        [TestMethod]
        public void StartTestRunShouldCallInitialize()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var mockTestMessageEventHandler = new Mock<ITestMessageEventHandler>();
            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>(), It.IsAny<ITestMessageEventHandler>()), Times.Once, "StartTestRun should call Initialize if not already initialized");
        }

        [TestMethod]
        public void StartTestRunShouldUpdateTestPlauginCacheWithExtensionsReturnByTestHost()
        {
            var path = Path.Combine(Path.GetTempPath(), "dummy.dll");
            mockTestHostManager.Setup(o => o.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string> { path });
            var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);
            expectedResult.Add(path);

            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            CollectionAssert.AreEquivalent(expectedResult, TestPluginCache.Instance.GetExtensionPaths(string.Empty));
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithAdapterSourceMap()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var manualResetEvent = new ManualResetEvent(true);

            mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
        }

        [TestMethod]
        public void StartTestRunShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);

            mockTestHostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources)).Returns(testRunCriteria.Sources);

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            mockTestHostManager.Verify(hm => hm.GetTestSources(testRunCriteria.Sources), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithTestCase()
        {
            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), "s.dll") },
                    frequencyOfRunStatsChangeEvent: 10);
            var manualResetEvent = new ManualResetEvent(true);

            mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
        }

        [TestMethod]
        public void StartTestRunShouldUpdateTestCaseSourceIfTestCaseSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "inputPackage.appxrecipe" };

            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), inputSource.FirstOrDefault()) },
                    frequencyOfRunStatsChangeEvent: 10);
            var manualResetEvent = new ManualResetEvent(false);

            mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);

            mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, inputSource.FirstOrDefault(), testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null))
                .Callback(() => manualResetEvent.Set());

            inProcessProxyExecutionManager = new InProcessProxyExecutionManager(mockTestHostManager.Object, mockTestHostManagerFactory.Object);

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
            mockExecutionManager.Verify(o => o.StartTestRun(testRunCriteria.Tests, inputSource.FirstOrDefault(), testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null));
            mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldNotUpdateTestCaseSourceIfTestCaseSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), actualSources.FirstOrDefault()) },
                    frequencyOfRunStatsChangeEvent: 10);
            var manualResetEvent = new ManualResetEvent(false);

            mockTestHostManager.Setup(hm => hm.GetTestSources(actualSources)).Returns(actualSources);

            mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null))
                .Callback(() => manualResetEvent.Set());

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
            mockExecutionManager.Verify(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null));
            mockTestHostManager.Verify(hm => hm.GetTestSources(actualSources));
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRunComplete()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var manualResetEvent = new ManualResetEvent(true);

            mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, mockTestRunEventsHandler.Object)).Callback(
                () => throw new Exception());

            mockTestRunEventsHandler.Setup(o => o.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null)).Callback(
                () => manualResetEvent.Set());

            inProcessProxyExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "ITestRunEventsHandler.HandleTestRunComplete should get called");
        }

        [TestMethod]
        public void AbortShouldCallExecutionManagerAbort()
        {
            var manualResetEvent = new ManualResetEvent(true);

            mockExecutionManager.Setup(o => o.Abort(It.IsAny<ITestRunEventsHandler>())).Callback(
                () => manualResetEvent.Set());

            inProcessProxyExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.Abort should get called");
        }

        [TestMethod]
        public void CancelShouldCallExecutionManagerCancel()
        {
            var manualResetEvent = new ManualResetEvent(true);

            mockExecutionManager.Setup(o => o.Cancel(It.IsAny<ITestRunEventsHandler>())).Callback(
                () => manualResetEvent.Set());

            inProcessProxyExecutionManager.Cancel(It.IsAny<ITestRunEventsHandler>());

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.Abort should get called");
        }
    }
}