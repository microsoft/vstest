// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using System;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

    [TestClass]
    public class ParallelProxyDiscoveryManagerTests
    {
        private IParallelProxyDiscoveryManager proxyParallelDiscoveryManager;
        private List<Mock<IProxyDiscoveryManager>> createdMockManagers;
        private Func<IProxyDiscoveryManager> proxyManagerFunc;
        private Mock<ITestDiscoveryEventsHandler> mockHandler;
        private List<string> sources = new List<string>() { "1.dll", "2.dll" };
        private DiscoveryCriteria testDiscoveryCriteria;

        [TestInitialize]
        public void SetUp()
        {
            this.createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            this.proxyManagerFunc = () =>
            {
                var manager = new Mock<IProxyDiscoveryManager>();
                this.createdMockManagers.Add(manager);
                return manager.Object;
            };
            this.mockHandler =  new Mock<ITestDiscoveryEventsHandler>();
            this.testDiscoveryCriteria = new DiscoveryCriteria(sources, 100, null);
        }

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersOnce()
        {
            this.proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.proxyManagerFunc, 3, false);
            this.proxyParallelDiscoveryManager.Initialize();

            Assert.AreEqual(3, createdMockManagers.Count, "Number of Concurrent Managers created should be 3");

            foreach (var manager in createdMockManagers)
            {
                manager.Verify(m => m.Initialize(), Times.Once);
            }
        }

        [TestMethod]
        public void AbortShouldCallAllConcurrentManagersOnce()
        {
            this.proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.proxyManagerFunc, 4, false);
            this.proxyParallelDiscoveryManager.Abort();

            Assert.AreEqual(4, createdMockManagers.Count, "Number of Concurrent Managers created should be 4");

            foreach (var manager in createdMockManagers)
            {
                manager.Verify(m => m.Abort(), Times.Once);
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldProcessAllSources()
        {
            proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.proxyManagerFunc, 2, false);

            var processedSources = new List<string>();
            var syncObject = new object();
            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler>())).
                    Callback<DiscoveryCriteria, ITestDiscoveryEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                processedSources.AddRange(criteria.Sources);
                            }

                            Task.Delay(100).Wait();

                            handler.HandleDiscoveryComplete(10, null, false);
                        });
            }

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(20, null, false))
                .Callback<long, IEnumerable<TestCase>, bool>(
                (totalTests, lastChunk, aborted) =>
                {
                    completeEvent.Set();
                });

            proxyParallelDiscoveryManager.DiscoverTests(testDiscoveryCriteria, mockHandler.Object);
            Assert.IsTrue(completeEvent.WaitOne(5000), "CompleteEvent was not set" );

            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");

            foreach (var source in sources)
            {
                bool matchFound = false;

                foreach (var processedSrc in processedSources)
                {
                    if (processedSrc.Equals(source))
                    {
                        if (matchFound) Assert.Fail("Concurrreny issue detected: Source['{0}'] got processed twice", processedSrc);
                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Source['{0}'] did NOT get processed at all", source);
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldNotSendCompleteUntilAllSourcesAreProcessed()
        {
            proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(this.proxyManagerFunc, 2, false);

            var processedSources = new List<string>();
            SetupDiscoveryTests(processedSources, false);

            AutoResetEvent eventHandle = new AutoResetEvent(false);

            SetupHandleDiscoveyComplete(eventHandle, false);

            Task.Run(() =>
            {
                proxyParallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, mockHandler.Object);
            });

            Assert.IsTrue(eventHandle.WaitOne(15000), "eventHandle was not set");

            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");

            foreach (var source in sources)
            {
                bool matchFound = false;

                foreach (var processedSrc in processedSources)
                {
                    if (processedSrc.Equals(source))
                    {
                        if (matchFound) Assert.Fail("Concurrreny issue detected: Source['{0}'] got processed twice", processedSrc);
                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Source['{0}'] did NOT get processed at all", source);
            }
        }

        /// <summary>
        ///  Create ParallelProxyDiscoveryManager with parallel level 1 and two source,
        ///  Abort in any source should not stop discovery for other sources.
        /// </summary>
        [TestMethod]
        public void DiscoveryTestsShouldProcessAllSourcesOnDiscoveryAbortsForAnySource()
        {
            var discoveryManagerMock = new Mock<IProxyDiscoveryManager>();
            proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(() => discoveryManagerMock.Object, 1, false);
            this.createdMockManagers.Add(discoveryManagerMock);
            var processedSources = new List<string>();
            SetupDiscoveryTests(processedSources, true);
            AutoResetEvent eventHandle = new AutoResetEvent(false);
            SetupHandleDiscoveyComplete(eventHandle, true);

            Task.Run(() =>
            {
                proxyParallelDiscoveryManager.DiscoverTests(this.testDiscoveryCriteria, mockHandler.Object);
            });

            eventHandle.WaitOne(15000);
            Assert.AreEqual(sources.Count, processedSources.Count, "All Sources must be processed.");
        }

        private void SetupHandleDiscoveyComplete(AutoResetEvent eventHandle, bool isAbort)
        {
            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(20, null, isAbort))
                .Callback<long, IEnumerable<TestCase>, bool>(
                    (totalTests1, lastChunk, aborted) => { eventHandle.Set(); });
        }

        private void SetupDiscoveryTests(List<string> processedSources, bool isAbort)
        {
            var syncObject = new object();
            foreach (var manager in createdMockManagers)
            {
                manager.Setup(m => m.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler>())).
                    Callback<DiscoveryCriteria, ITestDiscoveryEventsHandler>(
                        (criteria, handler) =>
                        {
                            lock (syncObject)
                            {
                                processedSources.AddRange(criteria.Sources);
                            }

                            Task.Delay(100).Wait();

                            handler.HandleDiscoveryComplete(10, null, isAbort);
                        });
            }
        }
    }
}
