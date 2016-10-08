// Copyright (c) Microsoft. All rights reserved.

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

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersOnce()
        {
            var createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            Func<IProxyDiscoveryManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyDiscoveryManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(proxyManagerFunc, 3, false);
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
            var createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            Func<IProxyDiscoveryManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyDiscoveryManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(proxyManagerFunc, 4, false);
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
            var createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            Func<IProxyDiscoveryManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyDiscoveryManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(proxyManagerFunc, 2, false);

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var sources = new List<string>() { "1.dll", "2.dll" };
            var testDiscoveryCriteria = new DiscoveryCriteria(sources, 100, null);

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
            var createdMockManagers = new List<Mock<IProxyDiscoveryManager>>();
            Func<IProxyDiscoveryManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyDiscoveryManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            proxyParallelDiscoveryManager = new ParallelProxyDiscoveryManager(proxyManagerFunc, 2, false);

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

            var sources = new List<string>() { "1.dll", "2.dll" };

            var discoveryCriteria = new DiscoveryCriteria(sources, 100, null);

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

            AutoResetEvent eventHandle = new AutoResetEvent(false);

            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(20, null, false))
                 .Callback<long, IEnumerable<TestCase>, bool>(
                 (totalTests, lastChunk, aborted) =>
                 {
                     eventHandle.Set();
                 });

            Task.Run(() =>
            {
                proxyParallelDiscoveryManager.DiscoverTests(discoveryCriteria, mockHandler.Object);
            });

            Assert.IsTrue(eventHandle.WaitOne(5000), "eventHandle was not set");

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
    }
}
