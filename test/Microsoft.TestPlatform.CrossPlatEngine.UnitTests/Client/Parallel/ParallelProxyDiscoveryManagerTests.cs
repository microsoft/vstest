// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace TestPlatform.CrossPlatEngine.UnitTests.Client;

[TestClass]
public class ParallelProxyDiscoveryManagerTests
{
    private const int TaskTimeout = 15 * 1000; // In milliseconds.
    private readonly List<Mock<IProxyDiscoveryManager>> _createdMockManagers;
    private readonly Func<IProxyDiscoveryManager> _proxyManagerFunc;
    private readonly Mock<ITestDiscoveryEventsHandler2> _mockHandler;
    private readonly List<string> _sources = new() { "1.dll", "2.dll" };
    private readonly DiscoveryCriteria _testDiscoveryCriteria;
    private bool _proxyManagerFuncCalled;
    private readonly List<string> _processedSources;
    private readonly ManualResetEventSlim _discoveryCompleted;
    private readonly Mock<IRequestData> _mockRequestData;

    public ParallelProxyDiscoveryManagerTests()
    {
        _processedSources = new List<string>();
        _createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
        _proxyManagerFunc = () =>
        {
            _proxyManagerFuncCalled = true;
            var manager = new Mock<IProxyDiscoveryManager>();
            _createdMockManagers.Add(manager);
            return manager.Object;
        };
        _mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
        _testDiscoveryCriteria = new DiscoveryCriteria(_sources, 100, null);
        _discoveryCompleted = new ManualResetEventSlim(false);
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
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, _proxyManagerFunc, 4, false);

        parallelDiscoveryManager.Abort();

        Assert.AreEqual(4, _createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
        _createdMockManagers.ForEach(dm => dm.Verify(m => m.Abort(), Times.Once));
    }

    [TestMethod]
    public void DiscoverTestsShouldProcessAllSources()
    {
        // Testcase filter should be passed to all parallel discovery criteria.
        _testDiscoveryCriteria.TestCaseFilter = "Name~Test";
        var parallelDiscoveryManager = SetupDiscoveryManager(_proxyManagerFunc, 2, false);

        Task.Run(() => parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object));

        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        Assert.AreEqual(_sources.Count, _processedSources.Count, "All Sources must be processed.");
        AssertMissingAndDuplicateSources(_processedSources);
    }

    [TestMethod]
    public void HandlePartialDiscoveryCompleteShouldReturnTrueIfDiscoveryWasAbortedWithEventHandler()
    {
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, _proxyManagerFunc, 1, false);
        var proxyDiscovermanager = new ProxyDiscoveryManager(_mockRequestData.Object, new Mock<ITestRequestSender>().Object, new Mock<ITestRuntimeProvider>().Object);

        parallelDiscoveryManager.Abort(_mockHandler.Object);
        bool isPartialDiscoveryComplete = parallelDiscoveryManager.HandlePartialDiscoveryComplete(proxyDiscovermanager, 20, new List<TestCase>(), isAborted: false);

        Assert.IsTrue(isPartialDiscoveryComplete);
    }

    [TestMethod]
    public void HandlePartialDiscoveryCompleteShouldReturnTrueIfDiscoveryWasAborted()
    {
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, _proxyManagerFunc, 1, false);
        var proxyDiscovermanager = new ProxyDiscoveryManager(_mockRequestData.Object, new Mock<ITestRequestSender>().Object, new Mock<ITestRuntimeProvider>().Object);

        parallelDiscoveryManager.Abort();
        bool isPartialDiscoveryComplete = parallelDiscoveryManager.HandlePartialDiscoveryComplete(proxyDiscovermanager, 20, new List<TestCase>(), isAborted: false);

        Assert.IsTrue(isPartialDiscoveryComplete);
    }

    [TestMethod]
    public void DiscoveryTestsShouldStopDiscoveryIfAbortionWasRequested()
    {
        // Since the hosts are aborted, total aggregated tests sent across will be -1
        var discoveryManagerMock = new Mock<IProxyDiscoveryManager>();
        _createdMockManagers.Add(discoveryManagerMock);
        var parallelDiscoveryManager = SetupDiscoveryManager(() => discoveryManagerMock.Object, 1, true);

        Task.Run(() =>
        {
            parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object);
            parallelDiscoveryManager.Abort();
        });

        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        Assert.AreEqual(1, _processedSources.Count, "One source should be processed.");
    }

    [TestMethod]
    public void DiscoveryTestsShouldStopDiscoveryIfAbortionWithEventHandlerWasRequested()
    {
        // Since the hosts are aborted, total aggregated tests sent across will be -1
        var discoveryManagerMock = new Mock<IProxyDiscoveryManager>();
        _createdMockManagers.Add(discoveryManagerMock);
        var parallelDiscoveryManager = SetupDiscoveryManager(() => discoveryManagerMock.Object, 1, true);

        Task.Run(() =>
        {
            parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object);
            parallelDiscoveryManager.Abort(_mockHandler.Object);
        });

        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        Assert.AreEqual(1, _processedSources.Count, "One source should be processed.");
    }

    [TestMethod]
    public void DiscoveryTestsShouldProcessAllSourceIfOneDiscoveryManagerIsStarved()
    {
        // Ensure that second discovery manager never starts. Expect 10 total tests.
        // Override DiscoveryComplete since overall aborted should be true
        var parallelDiscoveryManager = SetupDiscoveryManager(_proxyManagerFunc, 2, false);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
            .Throws<NotImplementedException>();
        _mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => _discoveryCompleted.Set());

        Task.Run(() => parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object));

        // Processed sources should be 1 since the 2nd source is never discovered
        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        Assert.AreEqual(1, _processedSources.Count, "All Sources must be processed.");
    }

    [TestMethod]
    public void DiscoveryTestsShouldCatchExceptionAndHandleLogMessageOfError()
    {
        // Ensure that second discovery manager never starts. Expect 10 total tests.
        // Override DiscoveryComplete since overall aborted should be true
        var parallelDiscoveryManager = SetupDiscoveryManager(_proxyManagerFunc, 2, false);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
            .Throws<NotImplementedException>();
        _mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => _discoveryCompleted.Set());

        Task.Run(() => parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object));

        // Processed sources should be 1 since the 2nd source is never discovered
        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        _mockHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void DiscoveryTestsShouldCatchExceptionAndHandleRawMessageOfTestMessage()
    {
        // Ensure that second discovery manager never starts. Expect 10 total tests.
        // Override DiscoveryComplete since overall aborted should be true
        var parallelDiscoveryManager = SetupDiscoveryManager(_proxyManagerFunc, 2, false);
        _createdMockManagers[1].Reset();
        _createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
            .Throws<NotImplementedException>();
        _mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => _discoveryCompleted.Set());

        Task.Run(() => parallelDiscoveryManager.DiscoverTests(_testDiscoveryCriteria, _mockHandler.Object));

        // Processed sources should be 1 since the 2nd source is never discovered
        Assert.IsTrue(_discoveryCompleted.Wait(TaskTimeout), "Test discovery not completed.");
        _mockHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
    }

    [TestMethod]
    public void HandlePartialDiscoveryCompleteShouldCreateANewProxyDiscoveryManagerIfIsAbortedIsTrue()
    {
        _proxyManagerFuncCalled = false;
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, _proxyManagerFunc, 1, false);
        var proxyDiscovermanager = new ProxyDiscoveryManager(_mockRequestData.Object, new Mock<ITestRequestSender>().Object, new Mock<ITestRuntimeProvider>().Object);

        parallelDiscoveryManager.HandlePartialDiscoveryComplete(proxyDiscovermanager, 20, new List<TestCase>(), isAborted: true);

        Assert.IsTrue(_proxyManagerFuncCalled);
    }

    private IParallelProxyDiscoveryManager SetupDiscoveryManager(Func<IProxyDiscoveryManager> getProxyManager, int parallelLevel, bool abortDiscovery)
    {
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, getProxyManager, parallelLevel, false);
        SetupDiscoveryTests(_processedSources, abortDiscovery);

        // Setup a complete handler for parallel discovery manager
        _mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
            .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>(
                (discoveryCompleteEventArgs, lastChunk) => _discoveryCompleted.Set());

        return parallelDiscoveryManager;
    }

    private void SetupDiscoveryTests(List<string> processedSources, bool isAbort)
    {
        var syncObject = new object();
        foreach (var manager in _createdMockManagers)
        {
            manager.Setup(m => m.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>())).
                Callback<DiscoveryCriteria, ITestDiscoveryEventsHandler2>(
                    (criteria, handler) =>
                    {
                        lock (syncObject)
                        {
                            processedSources.AddRange(criteria.Sources);
                        }

                        Task.Delay(100).Wait();

                        Assert.AreEqual(_testDiscoveryCriteria.TestCaseFilter, criteria.TestCaseFilter);
                        handler.HandleDiscoveryComplete(isAbort ? new DiscoveryCompleteEventArgs(-1, isAbort) : new DiscoveryCompleteEventArgs(10, isAbort), null);
                    });
        }
    }

    private void AssertMissingAndDuplicateSources(List<string> processedSources)
    {
        foreach (var source in _sources)
        {
            bool matchFound = false;

            foreach (var processedSrc in processedSources)
            {
                if (processedSrc.Equals(source))
                {
                    if (matchFound)
                    {
                        Assert.Fail("Concurrency issue detected: Source['{0}'] got processed twice", processedSrc);
                    }

                    matchFound = true;
                }
            }

            Assert.IsTrue(matchFound, "Concurrency issue detected: Source['{0}'] did NOT get processed at all", source);
        }
    }

    private void InvokeAndVerifyInitialize(int concurrentManagersCount, bool skipDefaultAdapters = false)
    {
        var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(_mockRequestData.Object, _proxyManagerFunc, concurrentManagersCount, false);

        // Action
        parallelDiscoveryManager.Initialize(skipDefaultAdapters);

        // Verify
        Assert.AreEqual(concurrentManagersCount, _createdMockManagers.Count, $"Number of Concurrent Managers created should be {concurrentManagersCount}");
        _createdMockManagers.ForEach(dm => dm.Verify(m => m.Initialize(skipDefaultAdapters), Times.Once));
    }
}
