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

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ParallelProxyExecutionManagerTests
    {
        private static readonly int taskTimeout = 15 * 1000; // In milliseconds

        private List<Mock<IProxyExecutionManager>> createdMockManagers;
        private Func<IProxyExecutionManager> proxyManagerFunc;
        private Mock<ITestRunEventsHandler> mockHandler;
        private Mock<ITestRuntimeProvider> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProxyDataCollectionManager> mockDataCollectionManager;
        private List<string> sources;
        private List<string> processedSources;
        private TestRunCriteria testRunCriteriaWithSources;
        private List<TestCase> testCases;
        private List<TestCase> processedTestCases;
        private TestRunCriteria testRunCriteriaWithTests;

        private bool proxyManagerFuncCalled;
        private ManualResetEventSlim executionCompleted;
        private Mock<IRequestData> mockRequestData;

        public ParallelProxyExecutionManagerTests()
        {
            this.executionCompleted = new ManualResetEventSlim(false);
            this.createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            this.proxyManagerFunc = () =>
                {
                    this.proxyManagerFuncCalled = true;
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };
            this.mockHandler = new Mock<ITestRunEventsHandler>();

            // Configure sources
            this.sources = new List<string>() { "1.dll", "2.dll" };
            this.processedSources = new List<string>();
            this.testRunCriteriaWithSources = new TestRunCriteria(sources, 100, false, string.Empty, TimeSpan.MaxValue, null, "Name~Test", new FilterOptions() { FilterRegEx = @"^[^\s\(]+" });

            // Configure testcases
            this.testCases = CreateTestCases();
            this.processedTestCases = new List<TestCase>();
            this.testRunCriteriaWithTests = new TestRunCriteria(this.testCases, 100);
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
        }

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersOnce()
        {
            InvokeAndVerifyInitialize(3);
        }

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersWithFalseFlagIfSkipDefaultAdaptersIsFalse()
        {
            InvokeAndVerifyInitialize(3, false);
        }

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersWithTrueFlagIfSkipDefaultAdaptersIsTrue()
        {
            InvokeAndVerifyInitialize(3, true);
        }

        [TestMethod]
        public void AbortShouldCallAllConcurrentManagersOnce()
        {
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, this.proxyManagerFunc, 4);

            parallelExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
            createdMockManagers.ForEach(em => em.Verify(m => m.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Once));
        }

        [TestMethod]
        public void CancelShouldCallAllConcurrentManagersOnce()
        {
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, this.proxyManagerFunc, 4);

            parallelExecutionManager.Cancel(It.IsAny<ITestRunEventsHandler>());

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
            createdMockManagers.ForEach(em => em.Verify(m => m.Cancel(It.IsAny<ITestRunEventsHandler>()), Times.Once));
        }

        [TestMethod]
        public void StartTestRunShouldProcessAllSources()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2);

            parallelExecutionManager.StartTestRun(testRunCriteriaWithSources, this.mockHandler.Object);

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(this.sources.Count, processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(processedSources);
        }



        [TestMethod]
        public void StartTestRunShouldProcessAllTestCases()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 3, setupTestCases: true);

            parallelExecutionManager.StartTestRun(this.testRunCriteriaWithTests, this.mockHandler.Object);

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(this.testCases.Count, processedTestCases.Count, "All Tests must be processed.");
            AssertMissingAndDuplicateTestCases(this.testCases, processedTestCases);
        }

        [TestMethod]
        public void StartTestRunWithSourcesShouldNotSendCompleteUntilAllSourcesAreProcessed()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2);

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(testRunCriteriaWithSources, this.mockHandler.Object);
            });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(this.sources.Count, this.processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(this.processedSources);
        }

        [TestMethod]
        public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfDataCollectionEnabled()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, TimeSpan.Zero);
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
            var proxyDataCollectionManager = new ProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2, setupTestCases: true);

            this.proxyManagerFuncCalled = false;
            parallelExecutionManager.HandlePartialRunComplete(proxyDataCollectionManager, completeArgs, null, null, null);
            Assert.IsTrue(this.proxyManagerFuncCalled);
        }

        [TestMethod]
        public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfDataCollectionEnabledAndCreatorWithDataCollection()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, TimeSpan.Zero);
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
            var proxyDataCollectionManager = new ProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
            var managers = new List<Mock<ProxyExecutionManagerWithDataCollection>>();            
            this.proxyManagerFunc = () =>
            {
                this.proxyManagerFuncCalled = true;
                var manager = new Mock<ProxyExecutionManagerWithDataCollection>(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
                managers.Add(manager);
                return manager.Object;
            };
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2, setupTestCases: true);

            this.proxyManagerFuncCalled = false;
            parallelExecutionManager.HandlePartialRunComplete(proxyDataCollectionManager, completeArgs, null, null, null);
            Assert.IsTrue(this.proxyManagerFuncCalled);

            var handler = parallelExecutionManager.GetHandlerForGivenManager(managers.Last().Object);
            Assert.IsTrue(handler is ParallelDataCollectionEventsHandler);            
        }

        [TestMethod]
        public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfIsAbortedIsTrue()
        {
            var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, TimeSpan.Zero);
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2, setupTestCases: true);

            this.proxyManagerFuncCalled = false;
            var proxyExecutionManagerManager = new ProxyExecutionManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object);
            parallelExecutionManager.HandlePartialRunComplete(proxyExecutionManagerManager, completeArgs, null, null, null);
            Assert.IsTrue(this.proxyManagerFuncCalled);
        }

        [TestMethod]
        public void StartTestRunWithTestsShouldNotSendCompleteUntilAllTestsAreProcessed()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 3, setupTestCases: true);

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(this.testRunCriteriaWithTests, this.mockHandler.Object);
            });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(this.testCases.Count, processedTestCases.Count, "All Tests must be processed.");
            AssertMissingAndDuplicateTestCases(this.testCases, processedTestCases);
        }

        [TestMethod]
        public void StartTestRunShouldNotProcessAllSourcesOnExecutionCancelsForAnySource()
        {
            var executionManagerMock = new Mock<IProxyExecutionManager>();
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, () => executionManagerMock.Object, 1);
            this.createdMockManagers.Add(executionManagerMock);
            this.SetupMockManagers(this.processedSources, isCanceled: true, isAborted: false);
            SetupHandleTestRunComplete(this.executionCompleted);

            Task.Run(() => { parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object); });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(1, this.processedSources.Count, "Abort should stop all sources execution.");
        }

        [TestMethod]
        public void StartTestRunShouldNotProcessAllSourcesOnExecutionAborted()
        {
            var executionManagerMock = new Mock<IProxyExecutionManager>();
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, () => executionManagerMock.Object, 1);
            this.createdMockManagers.Add(executionManagerMock);
            this.SetupMockManagers(this.processedSources, isCanceled: false, isAborted: false);
            SetupHandleTestRunComplete(this.executionCompleted);

            parallelExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());
            Task.Run(() => { parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object); });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(1, this.processedSources.Count, "Abort should stop all sources execution.");
        }

        [TestMethod]
        public void StartTestRunShouldProcessAllSourcesOnExecutionAbortsForAnySource()
        {
            var executionManagerMock = new Mock<IProxyExecutionManager>();
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, () => executionManagerMock.Object, 1);
            this.createdMockManagers.Add(executionManagerMock);
            this.SetupMockManagers(processedSources, isCanceled: false, isAborted: true);
            SetupHandleTestRunComplete(this.executionCompleted);

            Task.Run(() => { parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object); });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(2, this.processedSources.Count, "Abort should stop all sources execution.");
        }

        [TestMethod]
        public void StartTestRunShouldProcessAllSourceIfOneDiscoveryManagerIsStarved()
        {
            // Ensure that second discovery manager never starts. Expect 10 total tests.
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
                .Throws<NotImplementedException>();

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object);
            });

            // Processed sources should be 1 since the 2nd source is never discovered
            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            Assert.AreEqual(1, this.processedSources.Count, "All Sources must be processed.");
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndHandleLogMessageOfError()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
                .Throws<NotImplementedException>();

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object);
            });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            mockHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndHandleRawMessageOfTestMessage()
        {
            var parallelExecutionManager = this.SetupExecutionManager(this.proxyManagerFunc, 2);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
                .Throws<NotImplementedException>();

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(this.testRunCriteriaWithSources, this.mockHandler.Object);
            });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");
            mockHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
        }

        [TestMethod]
        public void StartTestRunShouldAggregateRunData()
        {
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, this.proxyManagerFunc, 2);
            var syncObject = new object();

            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).
                    Callback<TestRunCriteria, ITestRunEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                this.processedSources.AddRange(criteria.Sources);
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
                        this.executionCompleted.Set();
                    }
                });

            Task.Run(() =>
            {
                parallelExecutionManager.StartTestRun(testRunCriteriaWithSources, this.mockHandler.Object);
            });

            Assert.IsTrue(this.executionCompleted.Wait(taskTimeout), "Test run not completed.");

            Assert.IsNull(assertException, assertException?.ToString());
            Assert.AreEqual(sources.Count, this.processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(this.processedSources);
        }

        private ParallelProxyExecutionManager SetupExecutionManager(Func<IProxyExecutionManager> proxyManagerFunc, int parallelLevel)
        {
            return this.SetupExecutionManager(proxyManagerFunc, parallelLevel, false);
        }

        private ParallelProxyExecutionManager SetupExecutionManager(Func<IProxyExecutionManager> proxyManagerFunc, int parallelLevel, bool setupTestCases)
        {
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, proxyManagerFunc, parallelLevel);

            if (setupTestCases)
            {
                SetupMockManagersForTestCase(this.processedTestCases, this.testRunCriteriaWithTests);
            }
            else
            {
                this.SetupMockManagers(this.processedSources);
            }

            this.SetupHandleTestRunComplete(this.executionCompleted);
            return parallelExecutionManager;
        }

        private void SetupHandleTestRunComplete(ManualResetEventSlim completeEvent)
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
                            Assert.Fail("Concurrency issue detected: Test['{0}'] got processed twice", test.FullyQualifiedName);
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
                            Assert.AreEqual(testRunCriteria, criteria, "Mismatch in testRunCriteria");
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
                            Assert.AreEqual(testRunCriteriaWithSources, criteria, "Mismatch in testRunCriteria");
                            handler.HandleTestRunComplete(CreateTestRunCompleteArgs(isCanceled, isAborted), null, null, null);
                        });
            }
        }

        private void InvokeAndVerifyInitialize(int concurrentManagersCount, bool skipDefaultAdapters = false)
        {
            var parallelExecutionManager = new ParallelProxyExecutionManager(this.mockRequestData.Object, proxyManagerFunc, concurrentManagersCount);

            parallelExecutionManager.Initialize(skipDefaultAdapters);

            Assert.AreEqual(concurrentManagersCount, createdMockManagers.Count, $"Number of Concurrent Managers created should be {concurrentManagersCount}");
            createdMockManagers.ForEach(em => em.Verify(m => m.Initialize(skipDefaultAdapters), Times.Once));
        }
    }
}
