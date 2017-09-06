// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
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
            this.mockTestHostManagerFactory = new Mock<ITestHostManagerFactory>();
            this.mockExecutionManager = new Mock<IExecutionManager>();
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockTestHostManagerFactory.Setup(o => o.GetExecutionManager()).Returns(this.mockExecutionManager.Object);
            this.inProcessProxyExecutionManager = new InProcessProxyExecutionManager(this.mockTestHostManager.Object, this.mockTestHostManagerFactory.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.mockExecutionManager = null;
            this.mockTestHostManagerFactory = null;
            this.inProcessProxyExecutionManager = null;
            this.mockTestHostManager = null;
        }


        [TestMethod]
        public void StartTestRunShouldCallInitialize()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockExecutionManager.Verify(o => o.Initialize(Enumerable.Empty<string>()), Times.Once, "StartTestRun should call Initialize if not already initialized");
        }

        [TestMethod]
        public void StartTestRunShouldUpdateTestPlauginCacheWithExtensionsReturnByTestHost()
        {
            this.mockTestHostManager.Setup(o => o.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string> { "C:\\dummy.dll" });
            List<string> expectedResult = new List<string>();
            if (TestPluginCache.Instance.PathToExtensions != null)
            {
                expectedResult.AddRange(TestPluginCache.Instance.PathToExtensions);
            }
            expectedResult.Add("C:\\dummy.dll");

            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsFalse(expectedResult.Except(TestPluginCache.Instance.PathToExtensions).Any());
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithAdapterSourceMap()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            Assert.IsTrue(manualResetEvent.WaitOne(5000), "IExecutionManager.StartTestRun should get called");
        }

        [TestMethod]
        public void StartTestRunShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(testRunCriteria.Sources)).Returns(testRunCriteria.Sources);

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(testRunCriteria.Sources), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCallExecutionManagerStartTestRunWithTestCase()
        {
            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), "s.dll") },
                    frequencyOfRunStatsChangeEvent: 10);
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null)).Callback(
                () => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

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

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, inputSource.FirstOrDefault(), testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null))
                .Callback(() => manualResetEvent.Set());

            this.inProcessProxyExecutionManager = new InProcessProxyExecutionManager(this.mockTestHostManager.Object, this.mockTestHostManagerFactory.Object);

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockExecutionManager.Verify(o => o.StartTestRun(testRunCriteria.Tests, inputSource.FirstOrDefault(), testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null));

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
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

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(actualSources)).Returns(actualSources);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null))
                .Callback(() => manualResetEvent.Set());

            this.inProcessProxyExecutionManager.StartTestRun(testRunCriteria, null);

            this.mockExecutionManager.Verify(o => o.StartTestRun(testRunCriteria.Tests, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, null));

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(actualSources));
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRunComplete()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "source.dll" }, 10);
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var manualResetEvent = new ManualResetEvent(true);

            this.mockExecutionManager.Setup(o => o.StartTestRun(testRunCriteria.AdapterSourceMap, null, testRunCriteria.TestRunSettings, It.IsAny<TestExecutionContext>(), null, mockTestRunEventsHandler.Object)).Callback(
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
