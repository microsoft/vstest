// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

#nullable disable

namespace TestPlatform.CrossPlatEngine.UnitTests.Client;

[TestClass]
public class ParallelProxyExecutionManagerTests
{
    private static readonly int TaskTimeout = 15 * 1000; // In milliseconds

    private readonly List<Mock<IProxyExecutionManager>> _createdMockManagers;
    private Func<IProxyExecutionManager> _proxyManagerFunc;
    private readonly Mock<ITestRunEventsHandler> _mockHandler;
    private Mock<ITestRuntimeProvider> _mockTestHostManager;

    private Mock<ITestRequestSender> _mockRequestSender;

    private Mock<IProxyDataCollectionManager> _mockDataCollectionManager;
    private readonly List<string> _sources;
    private readonly List<string> _processedSources;
    private readonly TestRunCriteria _testRunCriteriaWithSources;
    private readonly List<TestCase> _testCases;
    private readonly List<TestCase> _processedTestCases;
    private readonly TestRunCriteria _testRunCriteriaWithTests;

    private bool _proxyManagerFuncCalled;
    private readonly ManualResetEventSlim _executionCompleted;
    private readonly Mock<IRequestData> _mockRequestData;

    public ParallelProxyExecutionManagerTests()
    {
        _executionCompleted = new ManualResetEventSlim(false);
        _createdMockManagers = new List<Mock<IProxyExecutionManager>>();
        _proxyManagerFunc = () =>
        {
            _proxyManagerFuncCalled = true;
            var manager = new Mock<IProxyExecutionManager>();
            _createdMockManagers.Add(manager);
            return manager.Object;
        };
        _mockHandler = new Mock<ITestRunEventsHandler>();

        // Configure sources
        _sources = new List<string>() { "1.dll", "2.dll" };
        _processedSources = new List<string>();
        _testRunCriteriaWithSources = new TestRunCriteria(_sources, 100, false, string.Empty, TimeSpan.MaxValue, null, "Name~Test", new FilterOptions() { FilterRegEx = @"^[^\s\(]+" });

        // Configure testcases
        _testCases = CreateTestCases();
        _processedTestCases = new List<TestCase>();
        _testRunCriteriaWithTests = new TestRunCriteria(_testCases, 100);
        _mockRequestData = new Mock<IRequestData>();
        _mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new NoOpMetricsCollection());
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
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, _proxyManagerFunc, 4);

        parallelExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

        Assert.AreEqual(4, _createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
        _createdMockManagers.ForEach(em => em.Verify(m => m.Abort(It.IsAny<ITestRunEventsHandler>()), Times.Once));
    }

    [TestMethod]
    public void CancelShouldCallAllConcurrentManagersOnce()
    {
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, _proxyManagerFunc, 4);

        parallelExecutionManager.Cancel(It.IsAny<ITestRunEventsHandler>());

        Assert.AreEqual(4, _createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
        _createdMockManagers.ForEach(em => em.Verify(m => m.Cancel(It.IsAny<ITestRunEventsHandler>()), Times.Once));
    }

    [TestMethod]
    public void StartTestRunShouldProcessAllSources()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2);

        parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object);

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(_sources.Count, _processedSources.Count, "All Sources must be processed.");
        AssertMissingAndDuplicateSources(_processedSources);
    }



    [TestMethod]
    public void StartTestRunShouldProcessAllTestCases()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 3, setupTestCases: true);

        parallelExecutionManager.StartTestRun(_testRunCriteriaWithTests, _mockHandler.Object);

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(_testCases.Count, _processedTestCases.Count, "All Tests must be processed.");
        AssertMissingAndDuplicateTestCases(_testCases, _processedTestCases);
    }

    [TestMethod]
    public void StartTestRunWithSourcesShouldNotSendCompleteUntilAllSourcesAreProcessed()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2);

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(_sources.Count, _processedSources.Count, "All Sources must be processed.");
        AssertMissingAndDuplicateSources(_processedSources);
    }

    [TestMethod]
    public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfDataCollectionEnabled()
    {
        var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, null, TimeSpan.Zero);
        _mockTestHostManager = new Mock<ITestRuntimeProvider>();
        _mockRequestSender = new Mock<ITestRequestSender>();
        _mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
        var proxyDataCollectionManager = new ProxyExecutionManagerWithDataCollection(_mockRequestData.Object, _mockRequestSender.Object, _mockTestHostManager.Object, _mockDataCollectionManager.Object);
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2, setupTestCases: true);

        _proxyManagerFuncCalled = false;
        parallelExecutionManager.HandlePartialRunComplete(proxyDataCollectionManager, completeArgs, null, null, null);
        Assert.IsTrue(_proxyManagerFuncCalled);
    }

    [TestMethod]
    public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfDataCollectionEnabledAndCreatorWithDataCollection()
    {
        var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, null, TimeSpan.Zero);
        _mockTestHostManager = new Mock<ITestRuntimeProvider>();
        _mockRequestSender = new Mock<ITestRequestSender>();
        _mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
        var proxyDataCollectionManager = new ProxyExecutionManagerWithDataCollection(_mockRequestData.Object, _mockRequestSender.Object, _mockTestHostManager.Object, _mockDataCollectionManager.Object);
        var managers = new List<Mock<ProxyExecutionManagerWithDataCollection>>();
        _proxyManagerFunc = () =>
        {
            _proxyManagerFuncCalled = true;
            var manager = new Mock<ProxyExecutionManagerWithDataCollection>(_mockRequestData.Object, _mockRequestSender.Object, _mockTestHostManager.Object, _mockDataCollectionManager.Object);
            managers.Add(manager);
            return manager.Object;
        };
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2, setupTestCases: true);

        _proxyManagerFuncCalled = false;
        parallelExecutionManager.HandlePartialRunComplete(proxyDataCollectionManager, completeArgs, null, null, null);
        Assert.IsTrue(_proxyManagerFuncCalled);

        var handler = parallelExecutionManager.GetHandlerForGivenManager(managers.Last().Object);
        Assert.IsTrue(handler is ParallelDataCollectionEventsHandler);
    }

    [TestMethod]
    public void HandlePartialRunCompleteShouldCreateNewProxyExecutionManagerIfIsAbortedIsTrue()
    {
        var completeArgs = new TestRunCompleteEventArgs(null, true, true, null, null, null, TimeSpan.Zero);
        _mockTestHostManager = new Mock<ITestRuntimeProvider>();
        _mockRequestSender = new Mock<ITestRequestSender>();
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2, setupTestCases: true);

        _proxyManagerFuncCalled = false;
        var proxyExecutionManagerManager = new ProxyExecutionManager(_mockRequestData.Object, _mockRequestSender.Object, _mockTestHostManager.Object);
        parallelExecutionManager.HandlePartialRunComplete(proxyExecutionManagerManager, completeArgs, null, null, null);
        Assert.IsTrue(_proxyManagerFuncCalled);
    }

    [TestMethod]
    public void StartTestRunWithTestsShouldNotSendCompleteUntilAllTestsAreProcessed()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 3, setupTestCases: true);

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithTests, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(_testCases.Count, _processedTestCases.Count, "All Tests must be processed.");
        AssertMissingAndDuplicateTestCases(_testCases, _processedTestCases);
    }

    [TestMethod]
    public void StartTestRunShouldNotProcessAllSourcesOnExecutionCancelsForAnySource()
    {
        var executionManagerMock = new Mock<IProxyExecutionManager>();
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, () => executionManagerMock.Object, 1);
        _createdMockManagers.Add(executionManagerMock);
        SetupMockManagers(_processedSources, isCanceled: true, isAborted: false);
        SetupHandleTestRunComplete(_executionCompleted);

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(1, _processedSources.Count, "Abort should stop all sources execution.");
    }

    [TestMethod]
    public void StartTestRunShouldNotProcessAllSourcesOnExecutionAborted()
    {
        var executionManagerMock = new Mock<IProxyExecutionManager>();
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, () => executionManagerMock.Object, 1);
        _createdMockManagers.Add(executionManagerMock);
        SetupMockManagers(_processedSources, isCanceled: false, isAborted: false);
        SetupHandleTestRunComplete(_executionCompleted);

        parallelExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());
        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(1, _processedSources.Count, "Abort should stop all sources execution.");
    }

    [TestMethod]
    public void StartTestRunShouldProcessAllSourcesOnExecutionAbortsForAnySource()
    {
        var executionManagerMock = new Mock<IProxyExecutionManager>();
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, () => executionManagerMock.Object, 1);
        _createdMockManagers.Add(executionManagerMock);
        SetupMockManagers(_processedSources, isCanceled: false, isAborted: true);
        SetupHandleTestRunComplete(_executionCompleted);

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(2, _processedSources.Count, "Abort should stop all sources execution.");
    }

    [TestMethod]
    public void StartTestRunShouldProcessAllSourceIfOneDiscoveryManagerIsStarved()
    {
        // Ensure that second discovery manager never starts. Expect 10 total tests.
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
            .Throws<NotImplementedException>();

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        // Processed sources should be 1 since the 2nd source is never discovered
        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        Assert.AreEqual(1, _processedSources.Count, "All Sources must be processed.");
    }

    [TestMethod]
    public void StartTestRunShouldCatchExceptionAndHandleLogMessageOfError()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
            .Throws<NotImplementedException>();

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        _mockHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void StartTestRunShouldCatchExceptionAndHandleRawMessageOfTestMessage()
    {
        var parallelExecutionManager = SetupExecutionManager(_proxyManagerFunc, 2);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(em => em.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>()))
            .Throws<NotImplementedException>();

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");
        _mockHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
    }

    [TestMethod]
    public void StartTestRunShouldAggregateRunData()
    {
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, _proxyManagerFunc, 2);
        var syncObject = new object();

        foreach (var manager in _createdMockManagers)
        {
            manager.Setup(m => m.StartTestRun(It.IsAny<TestRunCriteria>(), It.IsAny<ITestRunEventsHandler>())).
                Callback<TestRunCriteria, ITestRunEventsHandler>(
                    (criteria, handler) =>
                    {
                        lock (syncObject)
                        {
                            _processedSources.AddRange(criteria.Sources);
                        }

                        Task.Delay(100).Wait();
                        var stats = new Dictionary<TestOutcome, long>
                        {
                            { TestOutcome.Passed, 3 },
                            { TestOutcome.Failed, 2 }
                        };
                        var runAttachments = new Collection<AttachmentSet>
                        {
                            new AttachmentSet(new Uri("hello://x/"), "Hello")
                        };
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
                            TestRunStatistics(5, stats), isCanceled, isAborted, null, runAttachments, new Collection<InvokedDataCollector>(), timespan);
                        handler.HandleTestRunComplete(completeArgs, null, runAttachments, executorUris);
                    });
        }

        Exception assertException = null;
        _mockHandler.Setup(m => m.HandleTestRunComplete(
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
                        _executionCompleted.Set();
                    }
                });

        Task.Run(() => parallelExecutionManager.StartTestRun(_testRunCriteriaWithSources, _mockHandler.Object));

        Assert.IsTrue(_executionCompleted.Wait(TaskTimeout), "Test run not completed.");

        Assert.IsNull(assertException, assertException?.ToString());
        Assert.AreEqual(_sources.Count, _processedSources.Count, "All Sources must be processed.");
        AssertMissingAndDuplicateSources(_processedSources);
    }

    private ParallelProxyExecutionManager SetupExecutionManager(Func<IProxyExecutionManager> proxyManagerFunc, int parallelLevel)
    {
        return SetupExecutionManager(proxyManagerFunc, parallelLevel, false);
    }

    private ParallelProxyExecutionManager SetupExecutionManager(Func<IProxyExecutionManager> proxyManagerFunc, int parallelLevel, bool setupTestCases)
    {
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, proxyManagerFunc, parallelLevel);

        if (setupTestCases)
        {
            SetupMockManagersForTestCase(_processedTestCases, _testRunCriteriaWithTests);
        }
        else
        {
            SetupMockManagers(_processedSources);
        }

        SetupHandleTestRunComplete(_executionCompleted);
        return parallelExecutionManager;
    }

    private void SetupHandleTestRunComplete(ManualResetEventSlim completeEvent)
    {
        _mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()))
            .Callback<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (testRunCompleteArgs, testRunChangedEventArgs, attachmentSets, executorUris) => completeEvent.Set());
    }

    private void AssertMissingAndDuplicateSources(List<string> processedSources)
    {
        foreach (var source in _sources)
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
        foreach (var manager in _createdMockManagers)
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
        TestCase tc1 = new("dll1.class1.test1", new Uri("hello://x/"), "1.dll");
        TestCase tc21 = new("dll2.class21.test21", new Uri("hello://x/"), "2.dll");
        TestCase tc22 = new("dll2.class21.test22", new Uri("hello://x/"), "2.dll");
        TestCase tc31 = new("dll3.class31.test31", new Uri("hello://x/"), "3.dll");
        TestCase tc32 = new("dll3.class31.test32", new Uri("hello://x/"), "3.dll");

        var tests = new List<TestCase>() { tc1, tc21, tc22, tc31, tc32 };
        return tests;
    }

    private void SetupMockManagers(List<string> processedSources, bool isCanceled = false, bool isAborted = false)
    {
        var syncObject = new object();
        foreach (var manager in _createdMockManagers)
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
                        Assert.AreEqual(_testRunCriteriaWithSources, criteria, "Mismatch in testRunCriteria");
                        handler.HandleTestRunComplete(CreateTestRunCompleteArgs(isCanceled, isAborted), null, null, null);
                    });
        }
    }

    private void InvokeAndVerifyInitialize(int concurrentManagersCount, bool skipDefaultAdapters = false)
    {
        var parallelExecutionManager = new ParallelProxyExecutionManager(_mockRequestData.Object, _proxyManagerFunc, concurrentManagersCount);

        parallelExecutionManager.Initialize(skipDefaultAdapters);

        Assert.AreEqual(concurrentManagersCount, _createdMockManagers.Count, $"Number of Concurrent Managers created should be {concurrentManagersCount}");
        _createdMockManagers.ForEach(em => em.Verify(m => m.Initialize(skipDefaultAdapters), Times.Once));
    }
}
