// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.TestPlatformHelpers
{
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    [TestClass]
    public class TestRequestManagerTests
    {
        private ITestRequestManager testRequestManager;

        private Mock<ITestPlatform> mockTestPlatform;

        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestPlatform = new Mock<ITestPlatform>();
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object);
        }

        [TestMethod]
        public void InitializeExtensionsShouldCallTestPlatformToInitialize()
        {
            var paths = new List<string>() { "a", "b" };
            this.testRequestManager.InitializeExtensions(paths);

            this.mockTestPlatform.Verify(mt => mt.Initialize(paths, false, true), Times.Once);
        }

        [TestMethod]
        public void ResetShouldResetCommandLineOptionsInstance()
        {
            var oldInstance = CommandLineOptions.Instance;
            this.testRequestManager.ResetOptions();

            var newInstance = CommandLineOptions.Instance;

            Assert.AreNotEqual(oldInstance, newInstance, "CommandLineOptions must be cleaned up");
        }

        [TestMethod]
        public void DiscoverTestsShouldCallTestPlatformAndSucceed()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createDiscoveryRequestCalled = 0;
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Callback<DiscoveryCriteria>(
                (discoveryCriteria) =>
                {
                    createDiscoveryRequestCalled++;
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();
            var success = this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object);

            Assert.IsTrue(success, "DiscoverTests call must succeed");

            Assert.AreEqual(createDiscoveryRequestCalled, 1, "CreateDiscoveryRequest must be invoked only once.");
            Assert.AreEqual(2, actualDiscoveryCriteria.Sources.Count(), "All Sources must be used for discovery request");
            Assert.AreEqual("a", actualDiscoveryCriteria.Sources.First(), "First Source in list is incorrect");
            Assert.AreEqual("b", actualDiscoveryCriteria.Sources.ElementAt(1), "Second Source in list is incorrect");

            mockDiscoveryRegistrar.Verify(md => md.RegisterDiscoveryEvents(It.IsAny<IDiscoveryRequest>()), Times.Once);
            mockDiscoveryRegistrar.Verify(md => md.UnregisterDiscoveryEvents(It.IsAny<IDiscoveryRequest>()), Times.Once);

            mockDiscoveryRequest.Verify(md => md.DiscoverAsync(), Times.Once);

            mockTestPlatformEventSource.Verify(mt => mt.DiscoveryRequestStart(), Times.Once);
            mockTestPlatformEventSource.Verify(mt => mt.DiscoveryRequestStop(), Times.Once);
        }

        [TestMethod]
        [Ignore]
        public void CancelTestRunShouldWaitForCreateTestRunRequest()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            TestRunCriteria observedCriteria = null;

            var sw = new Stopwatch();
            sw.Start();

            long createRunRequestTime = 0;
            long cancelRequestTime = 0;

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    Thread.Sleep(1);
                    createRunRequestTime = sw.ElapsedMilliseconds;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.CancelAsync()).Callback(() =>
            {
                Thread.Sleep(1);
                cancelRequestTime = sw.ElapsedMilliseconds;
            });

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var cancelTask = Task.Run(() => this.testRequestManager.CancelTestRun());
            var runTask = Task.Run(() => this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object));

            Task.WaitAll(cancelTask, runTask);

            Assert.IsTrue(cancelRequestTime > createRunRequestTime, "CancelRequest must execute after create run request");
        }

        [TestMethod]
        [Ignore]
        public void AbortTestRunShouldWaitForCreateTestRunRequest()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            TestRunCriteria observedCriteria = null;

            var sw = new Stopwatch();
            sw.Start();

            long createRunRequestTime = 0;
            long cancelRequestTime = 0;

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    Thread.Sleep(1);
                    createRunRequestTime = sw.ElapsedMilliseconds;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.Abort()).Callback(() =>
            {
                Thread.Sleep(1);
                cancelRequestTime = sw.ElapsedMilliseconds;
            });

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var cancelTask = Task.Run(() => this.testRequestManager.AbortTestRun());
            var runTask = Task.Run(() => this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object));

            Task.WaitAll(cancelTask, runTask);

            Assert.IsTrue(cancelRequestTime > createRunRequestTime, "CancelRequest must execute after create run request");
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallTestPlatformAndSucceed()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            string testCaseFilterValue = "TestFilter";
            CommandLineOptions.Instance.TestCaseFilterValue = testCaseFilterValue;
            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object);

            Assert.IsTrue(success, "RunTests call must succeed");

            Assert.AreEqual(testCaseFilterValue, observedCriteria.TestCaseFilter, "TestCaseFilter must be set");

            Assert.AreEqual(createRunRequestCalled, 1, "CreateRunRequest must be invoked only once.");
            Assert.AreEqual(2, observedCriteria.Sources.Count(), "All Sources must be used for discovery request");
            Assert.AreEqual("a", observedCriteria.Sources.First(), "First Source in list is incorrect");
            Assert.AreEqual("b", observedCriteria.Sources.ElementAt(1), "Second Source in list is incorrect");

            mockRunEventsRegistrar.Verify(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>()), Times.Once);
            mockRunEventsRegistrar.Verify(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>()), Times.Once);

            mockRunRequest.Verify(md => md.ExecuteAsync(), Times.Once);

            mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStart(), Times.Once);
            mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStop(), Times.Once);
        }

        [TestMethod]
        public void RunTestsMultipleCallsShouldNotRunInParallel()
        {
            var payload1 = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a" }
            };

            var payload2 = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "b" }
            };

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>()))
                .Returns(mockRunRequest.Object);

            var mockRunEventsRegistrar1 = new Mock<ITestRunEventsRegistrar>();
            var mockRunEventsRegistrar2 = new Mock<ITestRunEventsRegistrar>();

            // Setup the second one to wait
            var sw = new Stopwatch();
            sw.Start();

            long run1Start = 0;
            long run1Stop = 0;
            long run2Start = 0;
            long run2Stop = 0;
            mockRunEventsRegistrar1.Setup(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run1Start = sw.ElapsedMilliseconds;
                Thread.Sleep(1);
            });
            mockRunEventsRegistrar1.Setup(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run1Stop = sw.ElapsedMilliseconds;
                Thread.Sleep(10);
            });

            mockRunEventsRegistrar2.Setup(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run2Start = sw.ElapsedMilliseconds;
                Thread.Sleep(10);
            });
            mockRunEventsRegistrar2.Setup(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run2Stop = sw.ElapsedMilliseconds;
            });

            var mockCustomlauncher = new Mock<ITestHostLauncher>();
            var task1 = Task.Run(() =>
            {
                this.testRequestManager.RunTests(payload1, mockCustomlauncher.Object, mockRunEventsRegistrar1.Object);
            });
            var task2 = Task.Run(() =>
            {
                this.testRequestManager.RunTests(payload2, mockCustomlauncher.Object, mockRunEventsRegistrar2.Object);
            });

            Task.WaitAll(task1, task2);

            if (run1Start < run2Start)
            {
                Assert.IsTrue((run2Stop > run2Start)
                    && (run2Start > run1Stop)
                    && (run1Stop > run1Start));
            }
            else
            {
                Assert.IsTrue((run1Stop > run1Start)
                    && (run1Start > run2Stop)
                    && (run2Stop > run2Start));
            }
        }

        [TestMethod]
        public void RunTestsIfThrowsTestPlatformExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new TestPlatformException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        public void RunTestsIfThrowsSettingsExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new SettingsException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        public void RunTestsIfThrowsInvalidOperationExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new InvalidOperationException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void RunTestsIfThrowsExceptionShouldThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Callback<TestRunCriteria>(
                (runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new NotImplementedException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object);
        }
    }
}
