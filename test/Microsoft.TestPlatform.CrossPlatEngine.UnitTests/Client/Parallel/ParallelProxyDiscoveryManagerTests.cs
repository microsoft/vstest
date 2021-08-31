﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
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

    [TestClass]
    public class ParallelProxyDiscoveryManagerTests
    {
        private const int taskTimeout = 15 * 1000; // In milliseconds.
        private List<Mock<IProxyDiscoveryManager>> createdMockManagers;
        private Func<IProxyDiscoveryManager> proxyManagerFunc;
        private Mock<ITestDiscoveryEventsHandler2> mockHandler;
        private List<string> sources = new List<string>() { "1.dll", "2.dll" };
        private DiscoveryCriteria testDiscoveryCriteria;
        private bool proxyManagerFuncCalled;
        private List<string> processedSources;
        private ManualResetEventSlim discoveryCompleted;
        private Mock<IRequestData> mockRequestData;

        public ParallelProxyDiscoveryManagerTests()
        {
            this.processedSources = new List<string>();
            this.createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            this.proxyManagerFunc = () =>
            {
                this.proxyManagerFuncCalled = true;
                var manager = new Mock<IProxyDiscoveryManager>();
                this.createdMockManagers.Add(manager);
                return manager.Object;
            };
            this.mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.testDiscoveryCriteria = new DiscoveryCriteria(sources, 100, null);
            this.discoveryCompleted = new ManualResetEventSlim(false);
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
            var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.mockRequestData.Object, this.proxyManagerFunc, 4, false);

            parallelDiscoveryManager.Abort();

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");
            createdMockManagers.ForEach(dm => dm.Verify(m => m.Abort(), Times.Once));
        }

        [TestMethod]
        public void DiscoverTestsShouldProcessAllSources()
        {
            // Testcase filter should be passed to all parallel discovery criteria.
            this.testDiscoveryCriteria.TestCaseFilter = "Name~Test";
            var parallelDiscoveryManager = this.SetupDiscoveryManager(this.proxyManagerFunc, 2, false);

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
            });

            Assert.IsTrue(this.discoveryCompleted.Wait(ParallelProxyDiscoveryManagerTests.taskTimeout), "Test discovery not completed.");
            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");
            AssertMissingAndDuplicateSources(processedSources);
        }

        /// <summary>
        ///  Create ParallelProxyDiscoveryManager with parallel level 1 and two source,
        ///  Abort in any source should not stop discovery for other sources.
        /// </summary>
        [TestMethod]
        public void DiscoveryTestsShouldProcessAllSourcesOnDiscoveryAbortsForAnySource()
        {
            // Since the hosts are aborted, total aggregated tests sent across will be -1
            var discoveryManagerMock = new Mock<IProxyDiscoveryManager>();
            this.createdMockManagers.Add(discoveryManagerMock);
            var parallelDiscoveryManager = this.SetupDiscoveryManager(() => discoveryManagerMock.Object, 1, true, totalTests: -1);

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
            });

            Assert.IsTrue(this.discoveryCompleted.Wait(ParallelProxyDiscoveryManagerTests.taskTimeout), "Test discovery not completed.");
            Assert.AreEqual(2, processedSources.Count, "All Sources must be processed.");
        }

        /// <summary>
        ///  Create ParallelProxyDiscoveryManager with parallel level 1 and two sources,
        ///  Overall discovery should stop, if aborting was requested
        /// </summary>
        [TestMethod]
        public void DiscoveryTestsShouldStopDiscoveryIfAbortionWasRequested()
        {
            // Since the hosts are aborted, total aggregated tests sent across will be -1
            var discoveryManagerMock = new Mock<IProxyDiscoveryManager>();
            this.createdMockManagers.Add(discoveryManagerMock);
            var parallelDiscoveryManager = this.SetupDiscoveryManager(() => discoveryManagerMock.Object, 1, true, totalTests: -1);

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
                parallelDiscoveryManager.Abort();
            });

            Assert.IsTrue(this.discoveryCompleted.Wait(taskTimeout), "Test discovery not completed.");
            Assert.AreEqual(1, processedSources.Count, "One source should be processed.");
        }

        [TestMethod]
        public void DiscoveryTestsShouldProcessAllSourceIfOneDiscoveryManagerIsStarved()
        {
            // Ensure that second discovery manager never starts. Expect 10 total tests.
            // Override DiscoveryComplete since overall aborted should be true
            var parallelDiscoveryManager = this.SetupDiscoveryManager(this.proxyManagerFunc, 2, false, totalTests: 10);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
                .Throws<NotImplementedException>();
            this.mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
                .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => { this.discoveryCompleted.Set(); });

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
            });

            // Processed sources should be 1 since the 2nd source is never discovered
            Assert.IsTrue(this.discoveryCompleted.Wait(ParallelProxyDiscoveryManagerTests.taskTimeout), "Test discovery not completed.");
            Assert.AreEqual(1, processedSources.Count, "All Sources must be processed.");
        }

        [TestMethod]
        public void DiscoveryTestsShouldCatchExceptionAndHandleLogMessageOfError()
        {
            // Ensure that second discovery manager never starts. Expect 10 total tests.
            // Override DiscoveryComplete since overall aborted should be true
            var parallelDiscoveryManager = this.SetupDiscoveryManager(this.proxyManagerFunc, 2, false, totalTests: 10);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
                .Throws<NotImplementedException>();
            this.mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
                .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => { this.discoveryCompleted.Set(); });

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
            });

            // Processed sources should be 1 since the 2nd source is never discovered
            Assert.IsTrue(this.discoveryCompleted.Wait(ParallelProxyDiscoveryManagerTests.taskTimeout), "Test discovery not completed.");
            mockHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DiscoveryTestsShouldCatchExceptionAndHandleRawMessageOfTestMessage()
        {
            // Ensure that second discovery manager never starts. Expect 10 total tests.
            // Override DiscoveryComplete since overall aborted should be true
            var parallelDiscoveryManager = this.SetupDiscoveryManager(this.proxyManagerFunc, 2, false, totalTests: 10);
            this.createdMockManagers[1].Reset();
            this.createdMockManagers[1].Setup(dm => dm.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()))
                .Throws<NotImplementedException>();
            this.mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
                .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>((t, l) => { this.discoveryCompleted.Set(); });

            Task.Run(() =>
            {
                parallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, this.mockHandler.Object);
            });

            // Processed sources should be 1 since the 2nd source is never discovered
            Assert.IsTrue(this.discoveryCompleted.Wait(ParallelProxyDiscoveryManagerTests.taskTimeout), "Test discovery not completed.");
            mockHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
        }

        [TestMethod]
        public void HandlePartialDiscoveryCompleteShouldCreateANewProxyDiscoveryManagerIfIsAbortedIsTrue()
        {
            this.proxyManagerFuncCalled = false;
            var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.mockRequestData.Object, this.proxyManagerFunc, 1, false);
            var proxyDiscovermanager = new ProxyDiscoveryManager(this.mockRequestData.Object, new Mock<ITestRequestSender>().Object, new Mock<ITestRuntimeProvider>().Object);

            parallelDiscoveryManager.HandlePartialDiscoveryComplete(proxyDiscovermanager, 20, new List<TestCase>(), isAborted: true);

            Assert.IsTrue(this.proxyManagerFuncCalled);
        }

        private IParallelProxyDiscoveryManager SetupDiscoveryManager(Func<IProxyDiscoveryManager> getProxyManager, int parallelLevel, bool abortDiscovery, int totalTests = 20)
        {
            var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.mockRequestData.Object, getProxyManager, parallelLevel, false);
            this.SetupDiscoveryTests(this.processedSources, abortDiscovery);

            // Setup a complete handler for parallel discovery manager
            this.mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null))
                .Callback<DiscoveryCompleteEventArgs, IEnumerable<TestCase>>(
                    (discoveryCompleteEventArgs, lastChunk) => { this.discoveryCompleted.Set(); });

            return parallelDiscoveryManager;
        }

        private void SetupDiscoveryTests(List<string> processedSources, bool isAbort)
        {
            var syncObject = new object();
            foreach (var manager in this.createdMockManagers)
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

                            Assert.AreEqual(this.testDiscoveryCriteria.TestCaseFilter, criteria.TestCaseFilter);
                            handler.HandleDiscoveryComplete(isAbort ? new DiscoveryCompleteEventArgs(-1, isAbort) : new DiscoveryCompleteEventArgs(10, isAbort), null);
                        });
            }
        }

        private void AssertMissingAndDuplicateSources(List<string> processedSources)
        {
            foreach (var source in this.sources)
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
            var parallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.mockRequestData.Object, this.proxyManagerFunc, concurrentManagersCount, false);

            // Action
            parallelDiscoveryManager.Initialize(skipDefaultAdapters);

            // Verify
            Assert.AreEqual(concurrentManagersCount, createdMockManagers.Count, $"Number of Concurrent Managers created should be {concurrentManagersCount}");
            createdMockManagers.ForEach(dm => dm.Verify(m => m.Initialize(skipDefaultAdapters), Times.Once));
        }
    }
}
