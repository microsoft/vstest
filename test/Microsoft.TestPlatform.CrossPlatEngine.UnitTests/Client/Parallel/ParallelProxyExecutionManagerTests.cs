// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ParallelProxyExecutionManagerTests
    {
        private static readonly int taskTimeout = 15 * 1000; // In milli seconds

        private IParallelProxyExecutionManager proxyParallelExecutionManager;
        private List<Mock<IProxyExecutionManager>> createdMockManagers;
        private Func<IProxyExecutionManager> proxyManagerFunc;
        private Mock<ITestRunEventsHandler> mockHandler;
        private Mock<ITestRuntimeProvider> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProxyDataCollectionManager> mockDataCollectionManager;
        private List<string> sources;
        private TestRunCriteria testRunCriteria;

        private bool proxyManagerFuncCalled;

        public ParallelProxyExecutionManagerTests()
        {
            this.createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            this.proxyManagerFunc = () =>
                {
                    this.proxyManagerFuncCalled = true;
                var manager = new Mock<IProxyExecutionManager>();
                createdMockManagers.Add(manager);
                return manager.Object;
            };
            this.mockHandler = new Mock<ITestRunEventsHandler>();
            this.sources = new List<string>() { "1.dll", "2.dll" };
            this.testRunCriteria = new TestRunCriteria(sources, 100);
        }

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersOnce()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 3);
            this.proxyParallelExecutionManager.Initialize();

            Assert.AreEqual(3, createdMockManagers.Count, "Number of Concurrent Managers created should be 3");

            foreach (var manager in createdMockManagers)
            {
                manager.Verify(m => m.Initialize(), Times.Once);
            }
        }

        [TestMethod]
        public void AbortShouldCallAllConcurrentManagersOnce()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 4);
            this.proxyParallelExecutionManager.Abort();

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");

            foreach (var manager in createdMockManagers)
            {
                manager.Verify(m => m.Abort(), Times.Once);
            }
        }

        [TestMethod]
        public void CancelShouldCallAllConcurrentManagersOnce()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 4);
            this.proxyParallelExecutionManager.Cancel();

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");

            foreach (var manager in createdMockManagers)
            {
                manager.Verify(m => m.Cancel(), Times.Once);
            }
        }

        [TestMethod]
        public void StartTestRunShouldProcessAllSources()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 2);

            // Testcase filter should be passed to all parallel test run criteria.
            this.testRunCriteria.TestCaseFilter = "Name~Test";
            var processedSources = new List<string>();
            this.SetupMockManagers(processedSources);
            AutoResetEvent completeEvent = new AutoResetEvent(false);
            this.SetupHandleTestRunComplete(completeEvent);

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(this.sources.Count, processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(processedSources);
        }

        [TestMethod]
        public void StartTestRunShouldProcessAllTestCases()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 3);
            var tests = CreateTestCases();
            var testRunCriteria = new TestRunCriteria(tests, 100);
            var processedTestCases = new List<TestCase>();
            SetupMockManagersForTestCase(processedTestCases, testRunCriteria);
            AutoResetEvent completeEvent = new AutoResetEvent(false);
            this.SetupHandleTestRunComplete(completeEvent);

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(tests.Count, processedTestCases.Count, "All Tests must be processed.");
            AssertMissingAndDuplicateTestCases(tests, processedTestCases);
        }

        [TestMethod]
        public void StartTestRunWithSourcesShouldNotSendCompleteUntilAllSourcesAreProcessed()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 2);
            var processedSources = new List<string>();
            this.SetupMockManagers(processedSources);
            AutoResetEvent completeEvent = new AutoResetEvent(false);

            SetupHandleTestRunComplete(completeEvent);

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);
            });

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(processedSources);
        }

        [TestMethod]
        public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfDataCollectionEnabled()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, TimeSpan.Zero);
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();

            var proxyDataCollectionManager = new ProxyExecutionManagerWithDataCollection(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 2);

            var tests = CreateTestCases();
            var testRunCriteria = new TestRunCriteria(tests, 100);
            var processedTestCases = new List<TestCase>();
            SetupMockManagersForTestCase(processedTestCases, testRunCriteria);

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            SetupHandleTestRunComplete(completeEvent);

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);
            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            this.proxyManagerFuncCalled = false;

            this.proxyParallelExecutionManager.HandlePartialRunComplete(proxyDataCollectionManager, completeArgs, null, null, null);

            Assert.IsTrue(this.proxyManagerFuncCalled);
        }

        [TestMethod]
        public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfIsAbortedIsTrue()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, TimeSpan.Zero);
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 2);

            var tests = CreateTestCases();
            var testRunCriteria = new TestRunCriteria(tests, 100);
            var processedTestCases = new List<TestCase>();
            SetupMockManagersForTestCase(processedTestCases, testRunCriteria);

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            SetupHandleTestRunComplete(completeEvent);

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);
            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");

            this.proxyManagerFuncCalled = false;
            var proxyExecutionManagerManager = new ProxyExecutionManager(this.mockRequestSender.Object, this.mockTestHostManager.Object);
            this.proxyParallelExecutionManager.HandlePartialRunComplete(proxyExecutionManagerManager, completeArgs, null, null, null);

            Assert.IsTrue(this.proxyManagerFuncCalled);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldNotSendCompleteUntilAllTestsAreProcessed()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 3);

            var tests = CreateTestCases();
            var testRunCriteria = new TestRunCriteria(tests, 100);
            var processedTestCases = new List<TestCase>();
            SetupMockManagersForTestCase(processedTestCases, testRunCriteria);

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            SetupHandleTestRunComplete(completeEvent);

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);
            });

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(tests.Count, processedTestCases.Count, "All Tests must be processed.");
            AssertMissingAndDuplicateTestCases(tests, processedTestCases);
        }

        [TestMethod]
        public void ExecutionTestsShouldNotProcessAllSourcesOnExecutionCancelsForAnySource()
        {
            var executionManagerMock = new Mock<IProxyExecutionManager>();
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(() => executionManagerMock.Object, 1);
            this.createdMockManagers.Add(executionManagerMock);
            var processedSources = new List<string>();
            this.SetupMockManagers(processedSources, isCanceled: true, isAborted: false);
            AutoResetEvent completeEvent = new AutoResetEvent(false);
            SetupHandleTestRunComplete(completeEvent);

            Task.Run(() => { this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object); });

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(1, processedSources.Count, "Abort should stop all sources execution.");
        }


        [TestMethod]
        public void ExecutionTestsShouldProcessAllSourcesOnExecutionAbortsForAnySource()
        {
            var executionManagerMock = new Mock<IProxyExecutionManager>();
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(() => executionManagerMock.Object, 1);
            this.createdMockManagers.Add(executionManagerMock);
            var processedSources = new List<string>();
            this.SetupMockManagers(processedSources, isCanceled: false, isAborted: true);
            AutoResetEvent completeEvent = new AutoResetEvent(false);
            SetupHandleTestRunComplete(completeEvent);

            Task.Run(() => { this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object); });

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");
            Assert.AreEqual(1, processedSources.Count, "Abort should stop all sources execution.");
        }

        [TestMethod]
        public void StartTestRunShouldAggregateRunData()
        {
            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(this.proxyManagerFunc, 2);
            var processedSources = new List<string>();
            var syncObject = new object();

            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).
                    Callback<TestRunCriteria, ITestRunEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                processedSources.AddRange(criteria.Sources);
                            }

                            Task.Delay(100).Wait();
                            var stats = new Dictionary<TestOutcome, long>();
                            stats.Add(TestOutcome.Passed, 3);
                            stats.Add(TestOutcome.Failed, 2);
                            var runAttachments = new Collection<AttachmentSet>();
                            runAttachments.Add(new AttachmentSet(new Uri("hello://x/"), "Hello"));
                            var executorUris = new List<string>() { "hello1" };
                            bool isCanceled = false;
                            bool isAborted = false;
                            TimeSpan timespan = TimeSpan.FromMilliseconds(100);

                            if (string.Equals(criteria.Sources?.FirstOrDefault(), "2.dll"))
                            {
                                isCanceled = true;
                                isAborted = true;
                                timespan = TimeSpan.FromMilliseconds(200);
                            }

                            var completeArgs = new TestRunCompleteEventArgs(new
                                TestRunStatistics(5, stats), isCanceled, isAborted, null, runAttachments, timespan);
                            handler.HandleTestRunComplete(completeArgs, null, runAttachments, executorUris);
                        });
            }

            AutoResetEvent completeEvent = new AutoResetEvent(false);
            Exception assertException = null;
            this.mockHandler.Setup(m => m.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback
                <TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (completeArgs, runChangedArgs, runAttachments, executorUris) =>
                {
                    try
                    {
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), completeArgs.ElapsedTimeInRunningTests,
                            "Time should be max of all");
                        Assert.AreEqual(2, completeArgs.AttachmentSets.Count,
                            "All Complete Arg attachments should return");
                        Assert.AreEqual(2, runAttachments.Count, "All RunContextAttachments should return");

                        Assert.IsTrue(completeArgs.IsAborted, "Aborted value must be OR of all values");
                        Assert.IsTrue(completeArgs.IsCanceled, "Canceled value must be OR of all values");

                        Assert.AreEqual(10, completeArgs.TestRunStatistics.ExecutedTests,
                            "Stats must be aggregated properly");

                        Assert.AreEqual(6, completeArgs.TestRunStatistics.Stats[TestOutcome.Passed],
                            "Stats must be aggregated properly");
                        Assert.AreEqual(4, completeArgs.TestRunStatistics.Stats[TestOutcome.Failed],
                            "Stats must be aggregated properly");
                    }
                    catch (Exception ex)
                    {
                        assertException = ex;
                    }
                    finally
                    {
                        completeEvent.Set();
                    }
                });

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, this.mockHandler.Object);
            });

            Assert.IsTrue(completeEvent.WaitOne(taskTimeout), "Test run not completed.");

            Assert.IsNull(assertException, assertException?.ToString());
            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(processedSources);
        }

        private void SetupHandleTestRunComplete(AutoResetEvent completeEvent)
        {
            this.mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                    (testRunCompleteArgs, testRunChangedEventArgs, attachmentSets, executorUris) => { completeEvent.Set(); });
        }

        private void AssertMissingAndDuplicateSources(List<string> processedSources)
        {
            foreach (var source in this.sources)
            {
                var matchFound = false;

                foreach (var processedSrc in processedSources)
                {
                    if (processedSrc.Equals(source))
                    {
                        if (matchFound)
                        {
                            Assert.Fail("Concurrreny issue detected: Source['{0}'] got processed twice", processedSrc);
                        }

                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Source['{0}'] did NOT get processed at all", source);
            }
        }

        private static TestRunCompleteEventArgs CreateTestRunCompleteArgs(bool isCanceled = false, bool isAborted = false)
        {
            return new TestRunCompleteEventArgs(
                new TestRunStatistics(new Dictionary<TestOutcome, long>()),
                isCanceled,
                isAborted,
                null,
                null,
                TimeSpan.FromMilliseconds(1));
        }

        private static void AssertMissingAndDuplicateTestCases(List<TestCase> tests, List<TestCase> processedTestCases)
        {
            foreach (var test in tests)
            {
                bool matchFound = false;

                foreach (var processedTest in processedTestCases)
                {
                    if (processedTest.FullyQualifiedName.Equals(test.FullyQualifiedName))
                    {
                        if (matchFound)
                            Assert.Fail("Concurrreny issue detected: Test['{0}'] got processed twice", test.FullyQualifiedName);
                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Test['{0}'] did NOT get processed at all",
                    test.FullyQualifiedName);
            }
        }

        private void SetupMockManagersForTestCase(List<TestCase> processedTestCases, TestRunCriteria testRunCriteria)
        {
            var syncObject = new object();
            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).
                    Callback<TestRunCriteria, ITestRunEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                processedTestCases.AddRange(criteria.Tests);
                            }

                            Task.Delay(100).Wait();

                            // Duplicated testRunCriteria should match the actual one.
                            Assert.AreEqual(testRunCriteria, criteria, "Mismastch in testRunCriteria");
                            handler.HandleTestRunComplete(CreateTestRunCompleteArgs(), null, null, null);
                        });
            }
        }

        private static List<TestCase> CreateTestCases()
        {
            TestCase tc1 = new TestCase("dll1.class1.test1", new Uri("hello://x/"), "1.dll");
            TestCase tc21 = new TestCase("dll2.class21.test21", new Uri("hello://x/"), "2.dll");
            TestCase tc22 = new TestCase("dll2.class21.test22", new Uri("hello://x/"), "2.dll");
            TestCase tc31 = new TestCase("dll3.class31.test31", new Uri("hello://x/"), "3.dll");
            TestCase tc32 = new TestCase("dll3.class31.test32", new Uri("hello://x/"), "3.dll");

            var tests = new List<TestCase>() { tc1, tc21, tc22, tc31, tc32 };
            return tests;
        }

        private void SetupMockManagers(List<string> processedSources, bool isCanceled = false, bool isAborted = false)
        {
            var syncObject = new object();
            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).
                    Callback<TestRunCriteria, ITestRunEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                processedSources.AddRange(criteria.Sources);
                            }
                            Task.Delay(100).Wait();

                            // Duplicated testRunCriteria should match the actual one.
                            Assert.AreEqual(testRunCriteria, criteria, "Mismastch in testRunCriteria");
                            handler.HandleTestRunComplete(CreateTestRunCompleteArgs(isCanceled, isAborted), null, null, null);
                        });
            }
        }
    }
}