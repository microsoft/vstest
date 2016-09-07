// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.IO;
    using Common.UnitTests.ExtensionFramework;
    using System.Reflection;
    //using Microsoft.Extensions.PlatformAbstractions;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

    [TestClass]
    public class BaseRunTestsTests
    {
        private TestableTestRunCache testableTestRunCache;
        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private TestableBaseRunTests runTestsInstance;

        private Mock<TestPlatformEventSource> mockTestPlatformEventSource;

        private const string BaseRunTestsExecutorUri = "executor://BaseRunTestsExecutor/";
        private const string BadBaseRunTestsExecutorUri = "executor://BadBaseRunTestsExecutor/";


        [TestInitialize]
        public void TestInit()
        {
            this.testableTestRunCache = new TestableTestRunCache();
            this.testExecutionContext = new TestExecutionContext(
                100,
                TimeSpan.MaxValue,
                inIsolation: false,
                keepAlive: false,
                areTestCaseLevelEventsRequired: false,
                isDebug: false,
                testCaseFilter: null);
            this.mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.mockTestPlatformEventSource = new Mock<TestPlatformEventSource>();

            this.runTestsInstance = new TestableBaseRunTests(
                testableTestRunCache,
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.mockTestPlatformEventSource.Object);
        }

        #region Constructor tests

        [TestMethod]
        public void ConstructorShouldInitializeRunContext()
        {
            var runContext = this.runTestsInstance.GetRunContext;
            Assert.IsNotNull(runContext);
            Assert.AreEqual(false, runContext.KeepAlive);
            Assert.AreEqual(false, runContext.InIsolation);
            Assert.AreEqual(false, runContext.IsDataCollectionEnabled);
            Assert.AreEqual(false, runContext.IsBeingDebugged);
        }

        [TestMethod]
        public void ConstructorShouldInitializeFrameworkHandle()
        {
            var frameworkHandle = this.runTestsInstance.GetFrameworkHandle;
            Assert.IsNotNull(frameworkHandle);
        }

        [TestMethod]
        public void ConstructorShouldInitializeExecutorUrisThatRanTests()
        {
            var executorUris = this.runTestsInstance.GetExecutorUrisThatRanTests;
            Assert.IsNotNull(executorUris);
        }

        #endregion
        
        #region RunTests tests

        [TestMethod]
        public void RunTestsShouldRaiseTestRunCompleteBeforeThrowingExceptionOnAnException()
        {
            TestRunCompleteEventArgs receivedCompleteArgs = null;
            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { throw new NotImplementedException(); };
            this.mockTestRunEventsHandler.Setup(
                treh =>
                treh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                        {
                            receivedCompleteArgs = complete;
                        });

            Assert.ThrowsException<NotImplementedException>(() => this.runTestsInstance.RunTests());

            Assert.IsNotNull(receivedCompleteArgs);
            Assert.IsTrue(receivedCompleteArgs.IsAborted);
        }

        [TestMethod]
        public void RunTestsShouldNotThrowIfExceptionIsAFileNotFoundException()
        {
            TestRunCompleteEventArgs receivedCompleteArgs = null;
            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { throw new FileNotFoundException(); };
            this.mockTestRunEventsHandler.Setup(
                treh =>
                treh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                    {
                        receivedCompleteArgs = complete;
                    });

            // This should not throw.
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedCompleteArgs);
            Assert.IsTrue(receivedCompleteArgs.IsAborted);
        }

        [TestMethod]
        public void RunTestsShouldNotThrowIfExceptionIsAnArgumentException()
        {
            TestRunCompleteEventArgs receivedCompleteArgs = null;
            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { throw new ArgumentException(); };
            this.mockTestRunEventsHandler.Setup(
                treh =>
                treh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                    {
                        receivedCompleteArgs = complete;
                    });

            // This should not throw.
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedCompleteArgs);
            Assert.IsTrue(receivedCompleteArgs.IsAborted);
        }

        [TestMethod]
        public void RunTestsShouldThrowIfExecutorUriExtensionMapIsNull()
        {
            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return null; };
            
            // This should not throw.
            Assert.ThrowsException<NullReferenceException>(() => this.runTestsInstance.RunTests());
        }

        [TestMethod]
        public void RunTestsShouldInvokeTheTestExecutorIfAdapterAssemblyIsKnown()
        {
            var assemblyLocation = typeof (BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> receivedExecutor = null;

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    receivedExecutor = executor;
                };
            
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedExecutor);
            Assert.AreEqual(BaseRunTestsExecutorUri, receivedExecutor.Metadata.ExtensionUri);
        }

        [TestMethod]
        public void RunTestsShouldInvokeTheTestExecutorIfAdapterAssemblyIsUnknown()
        {
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.UnspecifiedAdapterPath)
            };
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> receivedExecutor = null;

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    receivedExecutor = executor;
                };
            TestPluginCacheTests.SetupMockExtensions(
                new string[] { typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location },
                () => { });

            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedExecutor);
            Assert.AreEqual(BaseRunTestsExecutorUri, receivedExecutor.Metadata.ExtensionUri);
        }

        [TestMethod]
        public void RunTestsShouldReportAWarningIfExecutorUriIsNotDefinedInExtensionAssembly()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri("executor://nonexistent/"), assemblyLocation)
            };
            LazyExtension<ITestExecutor, ITestExecutorCapabilities> receivedExecutor = null;

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    receivedExecutor = executor;
                };

            this.runTestsInstance.RunTests();

            var expectedWarningMessageFormat =
                "Could not find test executor with URI '{0}'.  Make sure that the test executor is installed and supports .net runtime version {1}.";
            //var runtimeVersion = string.Concat(PlatformServices.Default.Runtime.RuntimeType, " ",
            //            PlatformServices.Default.Runtime.RuntimeVersion);
            var runtimeVersion = " ";

            var expectedWarningMessage = string.Format(expectedWarningMessageFormat, "executor://nonexistent/",
                runtimeVersion);
            this.mockTestRunEventsHandler.Verify(
                treh => treh.HandleLogMessage(TestMessageLevel.Warning, expectedWarningMessage), Times.Once);
            
            // Should not have been called.
            Assert.IsNull(receivedExecutor);
        }

        [TestMethod]
        public void RunTestsShouldNotAddExecutorUriToExecutorUriListIfNoTestsAreRun()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            
            this.runTestsInstance.RunTests();

            Assert.AreEqual(0, this.runTestsInstance.GetExecutorUrisThatRanTests.Count);
        }

        [TestMethod]
        public void RunTestsShouldAddExecutorUriToExecutorUriListIfExecutorHasRunTests()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    this.testableTestRunCache.TotalExecutedTests += 1;
                };

            this.runTestsInstance.RunTests();

            var expectedUris = new string[] {BaseRunTestsExecutorUri.ToLower()};
            CollectionAssert.AreEqual(expectedUris, this.runTestsInstance.GetExecutorUrisThatRanTests.ToArray());
        }

        [TestMethod]
        public void RunTestsShouldReportWarningIfExecutorThrowsAnException()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    throw new ArgumentException("Test influenced.");
                };

            this.runTestsInstance.RunTests();

            var messageFormat = "An exception occurred while invoking executor '{0}': {1}";
            var message = string.Format(messageFormat, BaseRunTestsExecutorUri.ToLower(), "Test influenced.");
            this.mockTestRunEventsHandler.Verify(treh => treh.HandleLogMessage(TestMessageLevel.Error, message),
                Times.Once);

            // Also validate that a test run complete is called.
            this.mockTestRunEventsHandler.Verify(
                treh => treh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsShouldNotFailOtherExecutorsRunIfOneExecutorThrowsAnException()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BadBaseRunTestsExecutorUri), assemblyLocation),
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    if (string.Equals(BadBaseRunTestsExecutorUri, executor.Metadata.ExtensionUri))
                    {
                        throw new Exception();
                    }
                    else
                    {
                        this.testableTestRunCache.TotalExecutedTests++;
                    }
                };

            this.runTestsInstance.RunTests();

            var expectedUris = new string[] { BaseRunTestsExecutorUri.ToLower() };
            CollectionAssert.AreEqual(expectedUris, this.runTestsInstance.GetExecutorUrisThatRanTests.ToArray());
        }

        [TestMethod]
        public void RunTestsShouldIterateThroughAllExecutors()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BadBaseRunTestsExecutorUri), assemblyLocation),
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    this.testableTestRunCache.TotalExecutedTests++;
                };

            this.runTestsInstance.RunTests();

            var expectedUris = new string[] { BadBaseRunTestsExecutorUri.ToLower(), BaseRunTestsExecutorUri.ToLower() };
            CollectionAssert.AreEqual(expectedUris, this.runTestsInstance.GetExecutorUrisThatRanTests.ToArray());
        }

        [TestMethod]
        public void RunTestsShouldRaiseTestRunComplete()
        {
            TestRunCompleteEventArgs receivedRunCompleteArgs = null;
            TestRunChangedEventArgs receivedRunStatusArgs = null;
            ICollection<AttachmentSet> receivedattachments = null;
            ICollection<string> receivedExecutorUris = null;

            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BadBaseRunTestsExecutorUri), assemblyLocation),
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    var tr = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(new TestCase("A.C.M", new Uri("default://dummy/"), "A"));
                    this.testableTestRunCache.TestResultList.Add(tr);
                    this.testableTestRunCache.TotalExecutedTests++;
                };
            this.mockTestRunEventsHandler.Setup(
                treh =>
                treh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                    {
                        receivedRunCompleteArgs = complete;
                        receivedRunStatusArgs = stats;
                        receivedattachments = attachments;
                        receivedExecutorUris = executorUris;
                    });

            // Act.
            this.runTestsInstance.RunTests();

            // Test run complete assertions.
            Assert.IsNotNull(receivedRunCompleteArgs);
            Assert.IsNull(receivedRunCompleteArgs.Error);
            Assert.IsFalse(receivedRunCompleteArgs.IsAborted);
            Assert.AreEqual(this.testableTestRunCache.TestRunStatistics, receivedRunCompleteArgs.TestRunStatistics);

            // Test run changed event assertions
            Assert.IsNotNull(receivedRunStatusArgs);
            Assert.AreEqual(this.testableTestRunCache.TestRunStatistics, receivedRunStatusArgs.TestRunStatistics);
            Assert.IsNotNull(receivedRunStatusArgs.NewTestResults);
            Assert.IsTrue(receivedRunStatusArgs.NewTestResults.Count() > 0);
            Assert.IsNull(receivedRunStatusArgs.ActiveTests);

            // Attachments
            Assert.IsNotNull(receivedattachments);

            // Executor Uris
            var expectedUris = new string[] { BadBaseRunTestsExecutorUri.ToLower(), BaseRunTestsExecutorUri.ToLower() };
            CollectionAssert.AreEqual(expectedUris, receivedExecutorUris.ToArray());
        }

        [TestMethod]
        public void RunTestsShouldNotifyItsImplementersOfAnyExceptionThrownByTheExecutors()
        {
            bool? isExceptionThrown = null;
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    throw new Exception();
                };
            this.runTestsInstance.BeforeRaisingTestRunCompleteCallback = (isEx) => { isExceptionThrown = isEx; };

            this.runTestsInstance.RunTests();

            Assert.IsTrue(isExceptionThrown.HasValue && isExceptionThrown.Value);
        }

        #endregion

        #region Testable Implementation

        private class TestableBaseRunTests : BaseRunTests
        {
            public TestableBaseRunTests(
                ITestRunCache testRunCache,
                string runSettings,
                TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler,
                ITestRunEventsHandler testRunEventsHandler,
                TestPlatformEventSource testPlatformEventSource)
                : base(testRunCache, runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, testPlatformEventSource)
            {
            }

            public Action<bool> BeforeRaisingTestRunCompleteCallback { get; set; }

            public Func<IFrameworkHandle, RunContext, IEnumerable<Tuple<Uri, string>>> GetExecutorUriExtensionMapCallback { get; set; }

            public
                Action
                    <LazyExtension<ITestExecutor, ITestExecutorCapabilities>, Tuple<Uri, string>, RunContext,
                        IFrameworkHandle> InvokeExecutorCallback { get; set; }

            /// <summary>
            /// Gets the run settings.
            /// </summary>
            public string GetRunSettings => this.RunSettings;

            /// <summary>
            /// Gets the test execution context.
            /// </summary>
            public TestExecutionContext GetTestExecutionContext => this.TestExecutionContext;

            /// <summary>
            /// Gets the test run events handler.
            /// </summary>
            public ITestRunEventsHandler GetTestRunEventsHandler => this.TestRunEventsHandler;

            /// <summary>
            /// Gets the test run cache.
            /// </summary>
            public ITestRunCache GetTestRunCache => this.TestRunCache;

            public bool GetIsCancellationRequested => this.IsCancellationRequested;

            public RunContext GetRunContext => this.RunContext;

            public FrameworkHandle GetFrameworkHandle => this.FrameworkHandle;

            public ICollection<string> GetExecutorUrisThatRanTests => this.ExecutorUrisThatRanTests;

            protected override void BeforeRaisingTestRunComplete(bool exceptionsHitDuringRunTests)
            {
                this.BeforeRaisingTestRunCompleteCallback?.Invoke(exceptionsHitDuringRunTests);
            }

            protected override IEnumerable<Tuple<Uri, string>> GetExecutorUriExtensionMap(IFrameworkHandle testExecutorFrameworkHandle, RunContext runContext)
            {
                return this.GetExecutorUriExtensionMapCallback?.Invoke(testExecutorFrameworkHandle, runContext);
            }

            protected override void InvokeExecutor(LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor, Tuple<Uri, string> executorUriExtensionTuple, RunContext runContext, IFrameworkHandle frameworkHandle)
            {
                this.InvokeExecutorCallback?.Invoke(executor, executorUriExtensionTuple, runContext, frameworkHandle);
            }
        }

        [ExtensionUri(BaseRunTestsExecutorUri)]
        private class TestExecutor : ITestExecutor
        {
            public void Cancel()
            {
                
            }

            public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                
            }

            public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                
            }
        }

        [ExtensionUri(BadBaseRunTestsExecutorUri)]
        private class BadTestExecutor : ITestExecutor
        {
            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                throw new NotImplementedException();
            }

            public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

    }
}
