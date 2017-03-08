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

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ParallelProxyExecutionManagerTests
    {
        private IParallelProxyExecutionManager proxyParallelExecutionManager;

        [TestMethod]
        public void InitializeShouldCallAllConcurrentManagersOnce()
        {
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

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
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 4);
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
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 4);
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
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 2);

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var sources = new List<string>() { "1.dll", "2.dll" };

            var testRunCriteria = new TestRunCriteria(sources, 100) { TestCaseFilter = "Name~Test" };

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

                            // Duplicated testRunCriteria should match the actual one.
                            Assert.AreEqual(testRunCriteria, criteria, "Mismastch in testRunCriteria");

                            handler.HandleTestRunComplete(
                                new TestRunCompleteEventArgs(
                                    new TestRunStatistics(new Dictionary<TestOutcome, long>()),
                                    false,
                                    false,
                                    null,
                                    null,
                                    TimeSpan.Zero),
                                null,
                                null,
                                null);
                        });
            }

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()))
                .Callback<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (testRunCompleteArgs, testRunChangedEventArgs, attachmentSets, executorUris) =>
                {
                    completeEvent.Set();
                });

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, mockHandler.Object);
            completeEvent.WaitOne();

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
        public void StartTestRunShouldProcessAllTestCases()
        {
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 3);

            var mockHandler = new Mock<ITestRunEventsHandler>();

            TestCase tc1 = new TestCase("dll1.class1.test1", new Uri("hello://x/"), "1.dll");
            TestCase tc21 = new TestCase("dll2.class21.test21", new Uri("hello://x/"), "2.dll");
            TestCase tc22 = new TestCase("dll2.class21.test22", new Uri("hello://x/"), "2.dll");
            TestCase tc31 = new TestCase("dll3.class31.test31", new Uri("hello://x/"), "3.dll");
            TestCase tc32 = new TestCase("dll3.class31.test32", new Uri("hello://x/"), "3.dll");

            var tests = new List<TestCase>() { tc1, tc21, tc22, tc31, tc32 };

            var testRunCriteria = new TestRunCriteria(tests, 100);

            var processedTestCases = new List<TestCase>();
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

                            handler.HandleTestRunComplete(
                                new TestRunCompleteEventArgs(new TestRunStatistics(new Dictionary<TestOutcome, long>())
                                , false, false, null, null, TimeSpan.Zero)
                                , null, null, null);
                        });
            }

            AutoResetEvent completeEvent = new AutoResetEvent(false);

            mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()))
                .Callback<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (testRunCompleteArgs, testRunChangedEventArgs, attachmentSets, executorUris) =>
                {
                    completeEvent.Set();
                });

            this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, mockHandler.Object);
            completeEvent.WaitOne();

            Assert.AreEqual(tests.Count, processedTestCases.Count, "All Tests must be processed.");

            foreach (var test in tests)
            {
                bool matchFound = false;

                foreach (var processedTest in processedTestCases)
                {
                    if (processedTest.FullyQualifiedName.Equals(test.FullyQualifiedName))
                    {
                        if (matchFound) Assert.Fail("Concurrreny issue detected: Test['{0}'] got processed twice", test.FullyQualifiedName);
                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Test['{0}'] did NOT get processed at all", test.FullyQualifiedName);
            }

        }


        [TestMethod]
        public void StartTestRunWithSourcesShouldNotSendCompleteUntilAllSourcesAreProcessed()
        {
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 2);

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var sources = new List<string>() { "1.dll", "2.dll" };

            var testRunCriteria = new TestRunCriteria(sources, 100);

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

                            var completeArgs = new TestRunCompleteEventArgs(new
                                 TestRunStatistics(new Dictionary<TestOutcome, long>()),
                                 false, false, null, null, TimeSpan.FromMilliseconds(1));
                            handler.HandleTestRunComplete(completeArgs, null, null, null);
                        });
            }

            AutoResetEvent eventHandle = new AutoResetEvent(false);

            mockHandler.Setup(m => m.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback
                <TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (completeArgs, runChangedArgs, runAttachments, executorUris) =>
                {
                    eventHandle.Set();
                });

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, mockHandler.Object);
            });

            eventHandle.WaitOne();

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
        public void StartTestRunWithTestsShouldNotSendCompleteUntilAllTestsAreProcessed()
        {
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 3);

            var mockHandler = new Mock<ITestRunEventsHandler>();

            TestCase tc1 = new TestCase("dll1.class1.test1", new Uri("hello://x/"), "1.dll");
            TestCase tc21 = new TestCase("dll2.class21.test21", new Uri("hello://x/"), "2.dll");
            TestCase tc22 = new TestCase("dll2.class21.test22", new Uri("hello://x/"), "2.dll");
            TestCase tc31 = new TestCase("dll3.class31.test31", new Uri("hello://x/"), "3.dll");
            TestCase tc32 = new TestCase("dll3.class31.test32", new Uri("hello://x/"), "3.dll");

            var tests = new List<TestCase>() { tc1, tc21, tc22, tc31, tc32 };

            var testRunCriteria = new TestRunCriteria(tests, 100);

            var processedTestCases = new List<TestCase>();
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

                            var completeArgs = new TestRunCompleteEventArgs(new
                             TestRunStatistics(new Dictionary<TestOutcome, long>()),
                             false, false, null, null, TimeSpan.FromMilliseconds(1));
                            handler.HandleTestRunComplete(completeArgs, null, null, null);
                        });
            }

            AutoResetEvent eventHandle = new AutoResetEvent(false);

            mockHandler.Setup(m => m.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback
                <TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (completeArgs, runChangedArgs, runAttachments, executorUris) =>
                {
                    eventHandle.Set();
                });

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, mockHandler.Object);
            });

            eventHandle.WaitOne();

            Assert.AreEqual(tests.Count, processedTestCases.Count, "All Tests must be processed.");

            foreach (var test in tests)
            {
                bool matchFound = false;

                foreach (var processedTest in processedTestCases)
                {
                    if (processedTest.FullyQualifiedName.Equals(test.FullyQualifiedName))
                    {
                        if (matchFound) Assert.Fail("Concurrreny issue detected: Test['{0}'] got processed twice", test.FullyQualifiedName);
                        matchFound = true;
                    }
                }

                Assert.IsTrue(matchFound, "Concurrency issue detected: Test['{0}'] did NOT get processed at all", test.FullyQualifiedName);
            }

        }


        [TestMethod]
        public void StartTestRunShouldAggregateRunData()
        {
            var createdMockManagers = new List<Mock<IProxyExecutionManager>>();
            Func<IProxyExecutionManager> proxyManagerFunc =
                () =>
                {
                    var manager = new Mock<IProxyExecutionManager>();
                    createdMockManagers.Add(manager);
                    return manager.Object;
                };

            this.proxyParallelExecutionManager = new ParallelProxyExecutionManager(proxyManagerFunc, 2);

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var sources = new List<string>() { "1.dll", "2.dll" };

            var testRunCriteria = new TestRunCriteria(sources, 100);

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

            AutoResetEvent eventHandle = new AutoResetEvent(false);

            mockHandler.Setup(m => m.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback
                <TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>(
                (completeArgs, runChangedArgs, runAttachments, executorUris) =>
                {
                    eventHandle.Set();

                    Assert.AreEqual(TimeSpan.FromMilliseconds(200), completeArgs.ElapsedTimeInRunningTests, "Time should be max of all");
                    Assert.AreEqual(2, completeArgs.AttachmentSets.Count, "All Complete Arg attachments should return");
                    Assert.AreEqual(2, runAttachments.Count, "All RunContextAttachments should return");

                    Assert.IsTrue(completeArgs.IsAborted, "Aborted value must be OR of all values");
                    Assert.IsTrue(completeArgs.IsCanceled, "Canceled value must be OR of all values");

                    Assert.AreEqual(10, completeArgs.TestRunStatistics.ExecutedTests, "Stats must be aggregated properly");

                    Assert.AreEqual(6, completeArgs.TestRunStatistics.Stats[TestOutcome.Passed], "Stats must be aggregated properly");
                    Assert.AreEqual(4, completeArgs.TestRunStatistics.Stats[TestOutcome.Failed], "Stats must be aggregated properly");
                });

            Task.Run(() =>
            {
                this.proxyParallelExecutionManager.StartTestRun(testRunCriteria, mockHandler.Object);
            });

            eventHandle.WaitOne();

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
