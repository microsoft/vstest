// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Common.UnitTests.ExtensionFramework;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;

    [TestClass]
    public class BaseRunTestsTests
    {
        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private TestableBaseRunTests runTestsInstance;

        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;

        private const string BaseRunTestsExecutorUri = "executor://BaseRunTestsExecutor/";
        private const string BadBaseRunTestsExecutorUri = "executor://BadBaseRunTestsExecutor/";

        [TestInitialize]
        public void TestInit()
        {
            this.testExecutionContext = new TestExecutionContext(
                          frequencyOfRunStatsChangeEvent: 100,
                          runStatsChangeEventTimeout: TimeSpan.MaxValue,
                          inIsolation: false,
                          keepAlive: false,
                          isDataCollectionEnabled: false,
                          areTestCaseLevelEventsRequired: false,
                          hasTestRun: false,
                          isDebug: false,
                          testCaseFilter: string.Empty);
            this.mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();

            this.runTestsInstance = new TestableBaseRunTests(
                null,
                testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.mockTestPlatformEventSource.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestExecutorExtensionManager.Destroy();
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
        public void RunTestsShouldRaiseTestRunCompleteWithAbortedAsTrueOnException()
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

            this.runTestsInstance.RunTests();

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
        public void RunTestsShouldAbortIfExecutorUriExtensionMapIsNull()
        {
            TestRunCompleteEventArgs receivedCompleteArgs = null; 

            // Setup mocks.
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return null; };
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
        public void RunTestsShouldInstrumentExecutionStart()
        {
            this.runTestsInstance.RunTests();
                    
            this.mockTestPlatformEventSource.Verify(x => x.ExecutionStart(), Times.Once);
        }

        [TestMethod]
        public void RunTestsShouldInstrumentExecutionStop()
        {

            this.SetupExecutorUriMock();

            this.runTestsInstance.RunTests();     
                   
            this.mockTestPlatformEventSource.Verify(x => x.ExecutionStop(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsShouldInstrumentAdapterExecutionStart()
        {
            this.SetupExecutorUriMock();

            this.runTestsInstance.RunTests();

            this.mockTestPlatformEventSource.Verify(x => x.AdapterExecutionStart(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void RunTestsShouldInstrumentAdapterExecutionStop()
        {
            this.SetupExecutorUriMock();

            this.runTestsInstance.RunTests();

            this.mockTestPlatformEventSource.Verify(x => x.AdapterExecutionStop(It.IsAny<long>()), Times.AtLeastOnce);
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
                    var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
                    var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
                    this.runTestsInstance.GetTestRunCache.OnNewTestResult(testResult);
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
                        var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
                        var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
                        this.runTestsInstance.GetTestRunCache.OnNewTestResult(testResult);
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
                    var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
                    var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
                    this.runTestsInstance.GetTestRunCache.OnNewTestResult(testResult);
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
                    var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
                    var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
                    this.runTestsInstance.GetTestRunCache.OnNewTestResult(testResult);
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
            Assert.AreEqual(this.runTestsInstance.GetTestRunCache.TestRunStatistics.ExecutedTests, receivedRunCompleteArgs.TestRunStatistics.ExecutedTests);

            // Test run changed event assertions
            Assert.IsNotNull(receivedRunStatusArgs);
            Assert.AreEqual(this.runTestsInstance.GetTestRunCache.TestRunStatistics.ExecutedTests, receivedRunStatusArgs.TestRunStatistics.ExecutedTests);
            Assert.IsNotNull(receivedRunStatusArgs.NewTestResults);
            Assert.IsTrue(receivedRunStatusArgs.NewTestResults.Count() > 0);
            Assert.IsTrue(receivedRunStatusArgs.ActiveTests == null || receivedRunStatusArgs.ActiveTests.Count() == 0);

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

        [TestMethod]
        public void RunTestsShouldReportLogMessagesFromExecutors()
        {
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => executorUriExtensionMap;
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriTuple, runcontext, frameworkHandle) =>
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "DummyMessage");
                };

            this.runTestsInstance.RunTests();

            this.mockTestRunEventsHandler.Verify(re => re.HandleLogMessage(TestMessageLevel.Error, "DummyMessage"));
        }

        #endregion

        #region Private Methods

        private void SetupExecutorUriMock()
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
        }
        #endregion

        #region Testable Implementation

        private class TestableBaseRunTests : BaseRunTests
        {
            public TestableBaseRunTests(
                string runSettings,
                TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler,
                ITestRunEventsHandler testRunEventsHandler,
                ITestPlatformEventSource testPlatformEventSource)
                : base(runSettings, testExecutionContext, testCaseEventsHandler, testRunEventsHandler, testPlatformEventSource)
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
