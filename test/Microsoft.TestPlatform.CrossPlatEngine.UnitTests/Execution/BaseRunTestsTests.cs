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

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OMTestResult =  Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;


    using Moq;

    [TestClass]
    public class BaseRunTestsTests
    {
        private const string BaseRunTestsExecutorUri = "executor://BaseRunTestsExecutor/";
        private const string BadBaseRunTestsExecutorUri = "executor://BadBaseRunTestsExecutor/";

        private TestExecutionContext testExecutionContext;
        private Mock<ITestRunEventsHandler> mockTestRunEventsHandler;

        private TestableBaseRunTests runTestsInstance;

        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Mock<IThread> mockThread;

        private Mock<IRequestData> mockRequestData;

        private Mock<IMetricsCollection> mockMetricsCollection;

        private Mock<IDataSerializer> mockDataSerializer;

        private TestRunChangedEventArgs receivedRunStatusArgs;
        private TestRunCompleteEventArgs receivedRunCompleteArgs;
        private ICollection<AttachmentSet> receivedattachments;
        private ICollection<string> receivedExecutorUris;
        private TestCase inProgressTestCase;

        public BaseRunTestsTests()
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
                          testCaseFilter: string.Empty,
                          filterOptions: null);
            this.mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();

            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockThread = new Mock<IThread>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);

            this.runTestsInstance = new TestableBaseRunTests(
                this.mockRequestData.Object,
                null,
                null,
                this.testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.mockTestPlatformEventSource.Object,
                null,
                new PlatformThread(),
                this.mockDataSerializer.Object);

            TestPluginCacheTests.SetupMockExtensions(new string[] { typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location }, () => { });
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestExecutorExtensionManager.Destroy();
            TestPluginCacheTests.ResetExtensionsCache();
        }

        #region Constructor tests

        [TestMethod]
        public void ConstructorShouldInitializeRunContext()
        {
            var runContext = this.runTestsInstance.GetRunContext;
            Assert.IsNotNull(runContext);
            Assert.IsFalse(runContext.KeepAlive);
            Assert.IsFalse(runContext.InIsolation);
            Assert.IsFalse(runContext.IsDataCollectionEnabled);
            Assert.IsFalse(runContext.IsBeingDebugged);
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
                    (
                     TestRunCompleteEventArgs complete,
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
                    (
                     TestRunCompleteEventArgs complete,
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
                    (
                     TestRunCompleteEventArgs complete,
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
                    (
                     TestRunCompleteEventArgs complete,
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
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
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

            // var runtimeVersion = string.Concat(PlatformServices.Default.Runtime.RuntimeType, " ",
            //            PlatformServices.Default.Runtime.RuntimeVersion);
            var runtimeVersion = " ";

            var expectedWarningMessage = string.Format(
                expectedWarningMessageFormat,
                "executor://nonexistent/",
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

            var expectedUris = new string[] { BaseRunTestsExecutorUri.ToLower() };
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
            this.mockTestRunEventsHandler.Verify(
                treh => treh.HandleLogMessage(TestMessageLevel.Error, message),
                Times.Once);

            // Also validate that a test run complete is called.
            this.mockTestRunEventsHandler.Verify(
                treh => treh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
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
            this.SetUpTestRunEvents();

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
        public void RunTestsShouldNotCloneTestCaseAndTestResultsObjectForNonPackageSource()
        {
            this.SetUpTestRunEvents();

            this.runTestsInstance.RunTests();

            this.mockDataSerializer.Verify(d => d.Clone<TestCase>(It.IsAny<TestCase>()), Times.Never);
            this.mockDataSerializer.Verify(d => d.Clone<OMTestResult>(It.IsAny<OMTestResult>()), Times.Never);
        }

        [TestMethod]
        public void RunTestsShouldUpdateTestResultsTestCaseSourceWithPackageIfTestSourceIsPackage()
        {
            const string package = @"C:\Projects\UnitTestApp1\AppPackages\UnitTestApp1\UnitTestApp1_1.0.0.0_Win32_Debug_Test\UnitTestApp1_1.0.0.0_Win32_Debug.appx";
            this.SetUpTestRunEvents(package);

            // Act.
            this.runTestsInstance.RunTests();

            // Test run changed event assertions
            Assert.IsNotNull(receivedRunStatusArgs.NewTestResults);
            Assert.IsTrue(receivedRunStatusArgs.NewTestResults.Count() > 0);

            // verify TC.Source is updated with package
            foreach (var tr in receivedRunStatusArgs.NewTestResults)
            {
                Assert.AreEqual(tr.TestCase.Source, package);
            }
        }

        [TestMethod]
        public void RunTestsShouldUpdateActiveTestCasesSourceWithPackageIfTestSourceIsPackage()
        {
            const string package = @"C:\Porjects\UnitTestApp3\Debug\UnitTestApp3\UnitTestApp3.build.appxrecipe";
            this.mockDataSerializer.Setup(d => d.Clone<TestCase>(It.IsAny<TestCase>()))
                .Returns<TestCase>(t => JsonDataSerializer.Instance.Clone<TestCase>(t));
            this.SetUpTestRunEvents(package, setupHandleTestRunComplete: false);

            // Act.
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedRunStatusArgs.ActiveTests);
            Assert.AreEqual(1, receivedRunStatusArgs.ActiveTests.Count());


            foreach (var tc in receivedRunStatusArgs.ActiveTests)
            {
                Assert.AreEqual(tc.Source, package);
            }
        }

        [TestMethod]
        public void RunTestsShouldCloneTheActiveTestCaseObjectsIfTestSourceIsPackage()
        {
            const string package = @"C:\Porjects\UnitTestApp3\Debug\UnitTestApp3\UnitTestApp3.build.appxrecipe";

            this.SetUpTestRunEvents(package, setupHandleTestRunComplete: false);

            // Act.
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedRunStatusArgs.ActiveTests);
            Assert.AreEqual(1, receivedRunStatusArgs.ActiveTests.Count());

            this.mockDataSerializer.Verify(d => d.Clone<TestCase>(It.IsAny<TestCase>()), Times.Exactly(2));
        }

        [TestMethod]
        public void RunTestsShouldCloneTheTestResultsObjectsIfTestSourceIsPackage()
        {
            const string package = @"C:\Porjects\UnitTestApp3\Debug\UnitTestApp3\UnitTestApp3.build.appxrecipe";

            this.SetUpTestRunEvents(package, setupHandleTestRunComplete: false);

            // Act.
            this.runTestsInstance.RunTests();

            Assert.IsNotNull(receivedRunStatusArgs.NewTestResults);
            Assert.AreEqual(1, receivedRunStatusArgs.ActiveTests.Count());

            this.mockDataSerializer.Verify(d => d.Clone<OMTestResult>(It.IsAny<OMTestResult>()), Times.Exactly(2));
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

        [TestMethod]
        public void RunTestsShouldCreateSTAThreadIfExecutionThreadApartmentStateIsSTA()
        {
            this.SetupForExecutionThreadApartmentStateTests(PlatformApartmentState.STA);
            this.runTestsInstance.RunTests();
            this.mockThread.Verify(t => t.Run(It.IsAny<Action>(), PlatformApartmentState.STA, true));
        }

        [TestMethod]
        public void RunTestsShouldSendMetricsOnTestRunComplete()
        {
            TestRunCompleteEventArgs receivedRunCompleteArgs = null;
            var mockMetricsCollector = new Mock<IMetricsCollection>();

            var dict = new Dictionary<string, object>();
            dict.Add("DummyMessage", "DummyValue");

            // Setup mocks.
            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            this.mockTestRunEventsHandler.Setup(
                    treh =>
                        treh.HandleTestRunComplete(
                            It.IsAny<TestRunCompleteEventArgs>(),
                            It.IsAny<TestRunChangedEventArgs>(),
                            It.IsAny<ICollection<AttachmentSet>>(),
                            It.IsAny<ICollection<string>>()))
                .Callback(
                    (
                        TestRunCompleteEventArgs complete,
                        TestRunChangedEventArgs stats,
                        ICollection<AttachmentSet> attachments,
                        ICollection<string> executorUris) =>
                    {
                        receivedRunCompleteArgs = complete;
                    });

            // Act.
            this.runTestsInstance.RunTests();

            // Assert.
            Assert.IsNotNull(receivedRunCompleteArgs.Metrics);
            Assert.IsTrue(receivedRunCompleteArgs.Metrics.Any());
            Assert.IsTrue(receivedRunCompleteArgs.Metrics.ContainsKey("DummyMessage"));
        }

        [TestMethod]
        public void RunTestsShouldCollectMetrics()
        {
            var mockMetricsCollector = new Mock<IMetricsCollection>();
            var dict = new Dictionary<string, object>();
            dict.Add("DummyMessage", "DummyValue");
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
            mockMetricsCollector.Setup(mc => mc.Metrics).Returns(dict);
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

            // Act.
            this.runTestsInstance.RunTests();

            // Verify.
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TotalTestsRun, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.RunState, It.IsAny<string>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterUsedToRunTests, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenByAllAdaptersInSec, It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(string.Concat(TelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, ".", new Uri(BadBaseRunTestsExecutorUri)), It.IsAny<object>()), Times.Once);
            mockMetricsCollector.Verify(rd => rd.Add(string.Concat(TelemetryDataConstants.TotalTestsRanByAdapter, ".", new Uri(BadBaseRunTestsExecutorUri)), It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsShouldNotCreateThreadIfExecutionThreadApartmentStateIsMTA()
        {
            this.SetupForExecutionThreadApartmentStateTests(PlatformApartmentState.MTA);
            this.runTestsInstance.RunTests();

            this.mockThread.Verify(t => t.Run(It.IsAny<Action>(), PlatformApartmentState.STA, true), Times.Never);
        }

        [TestMethod]
        public void RunTestsShouldRunTestsInMTAThreadWhenRunningInSTAThreadFails()
        {
            this.SetupForExecutionThreadApartmentStateTests(PlatformApartmentState.STA);
            this.mockThread.Setup(
                mt => mt.Run(It.IsAny<Action>(), PlatformApartmentState.STA, It.IsAny<bool>())).Throws<ThreadApartmentStateNotSupportedException>();
            bool isInvokeExecutorCalled = false;
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriTuple, runcontext, frameworkHandle) =>
                {
                    isInvokeExecutorCalled = true;
                };
            this.runTestsInstance.RunTests();

            Assert.IsTrue(isInvokeExecutorCalled, "InvokeExecutor() should be called when STA thread creation fails.");
            this.mockThread.Verify(t => t.Run(It.IsAny<Action>(), PlatformApartmentState.STA, true), Times.Once);
        }

        [TestMethod]
        public void CancelShouldCreateSTAThreadIfExecutionThreadApartmentStateIsSTA()
        {
            this.SetupForExecutionThreadApartmentStateTests(PlatformApartmentState.STA);
            this.mockThread.Setup(mt => mt.Run(It.IsAny<Action>(), PlatformApartmentState.STA, It.IsAny<bool>()))
                .Callback<Action, PlatformApartmentState, bool>((action, start, waitForCompletion) =>
                {
                    if (waitForCompletion)
                    {
                        // Callback for RunTests().
                        this.runTestsInstance.Cancel();
                    }
                });

            this.runTestsInstance.RunTests();
            this.mockThread.Verify(
                t => t.Run(It.IsAny<Action>(), PlatformApartmentState.STA, It.IsAny<bool>()),
                Times.Exactly(2),
                "Both RunTests() and Cancel() should create STA thread.");
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
        }

        private void SetupForExecutionThreadApartmentStateTests(PlatformApartmentState apartmentState)
        {
            this.mockThread = new Mock<IThread>();

            this.runTestsInstance = new TestableBaseRunTests(
                this.mockRequestData.Object,
                null,
                $@"<RunSettings>
                  <RunConfiguration>
                     <ExecutionThreadApartmentState>{apartmentState}</ExecutionThreadApartmentState>
                   </RunConfiguration>
                </RunSettings>",
                this.testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.mockTestPlatformEventSource.Object,
                null,
                this.mockThread.Object,
                this.mockDataSerializer.Object);

            TestPluginCacheTests.SetupMockExtensions(new string[] { typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location }, () => { });
            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
        }

        private void SetUpTestRunEvents(string package = null, bool setupHandleTestRunComplete = true)
        {
            if (setupHandleTestRunComplete)
            {
                this.SetupHandleTestRunComplete();
            }
            else
            {
                this.SetupHandleTestRunStatsChange();
            }

            this.SetupDataSerializer();

            this.runTestsInstance = this.runTestsInstance = new TestableBaseRunTests(
                this.mockRequestData.Object,
                package,
                null,
                this.testExecutionContext,
                null,
                this.mockTestRunEventsHandler.Object,
                this.mockTestPlatformEventSource.Object,
                null,
                new PlatformThread(),
                this.mockDataSerializer.Object);

            var assemblyLocation = typeof(BaseRunTestsTests).GetTypeInfo().Assembly.Location;
            var executorUriExtensionMap = new List<Tuple<Uri, string>>
            {
                new Tuple<Uri, string>(new Uri(BadBaseRunTestsExecutorUri), assemblyLocation),
                new Tuple<Uri, string>(new Uri(BaseRunTestsExecutorUri), assemblyLocation)
            };

            // Setup mocks.
            this.SetupExecutorCallback(executorUriExtensionMap);
        }

        private void SetupExecutorCallback(List<Tuple<Uri, string>> executorUriExtensionMap)
        {
            this.runTestsInstance.GetExecutorUriExtensionMapCallback = (fh, rc) => { return executorUriExtensionMap; };
            this.runTestsInstance.InvokeExecutorCallback =
                (executor, executorUriExtensionTuple, runContext, frameworkHandle) =>
                {
                    var testCase = new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll");
                    var testResult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
                    this.inProgressTestCase = new TestCase("x.y.z2", new Uri("uri://dummy"), "x.dll");

                    this.runTestsInstance.GetTestRunCache.OnTestStarted(inProgressTestCase);
                    this.runTestsInstance.GetTestRunCache.OnNewTestResult(testResult);
                };
        }

        private void SetupDataSerializer()
        {
            this.mockDataSerializer.Setup(d => d.Clone<TestCase>(It.IsAny<TestCase>()))
                .Returns<TestCase>(t => JsonDataSerializer.Instance.Clone<TestCase>(t));

            this.mockDataSerializer.Setup(d => d.Clone<OMTestResult>(It.IsAny<OMTestResult>()))
                .Returns<OMTestResult>(t => JsonDataSerializer.Instance.Clone<OMTestResult>(t));
        }

        private void SetupHandleTestRunStatsChange()
        {
            this.testExecutionContext.FrequencyOfRunStatsChangeEvent = 2;
            this.mockTestRunEventsHandler
                .Setup(treh => treh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()))
                .Callback((TestRunChangedEventArgs stats) => { receivedRunStatusArgs = stats; });
        }

        private void SetupHandleTestRunComplete()
        {
            this.mockTestRunEventsHandler.Setup(
                    treh =>
                        treh.HandleTestRunComplete(
                            It.IsAny<TestRunCompleteEventArgs>(),
                            It.IsAny<TestRunChangedEventArgs>(),
                            It.IsAny<ICollection<AttachmentSet>>(),
                            It.IsAny<ICollection<string>>()))
                .Callback(
                    (
                        TestRunCompleteEventArgs complete,
                        TestRunChangedEventArgs stats,
                        ICollection<AttachmentSet> attachments,
                        ICollection<string> executorUris) =>
                    {
                        receivedRunCompleteArgs = complete;
                        this.receivedRunStatusArgs = stats;
                        receivedattachments = attachments;
                        receivedExecutorUris = executorUris;
                    });
        }

        #endregion

        #region Testable Implementation

        private class TestableBaseRunTests : BaseRunTests
        {
            public TestableBaseRunTests(
                IRequestData requestData,
                string package,
                string runSettings,
                TestExecutionContext testExecutionContext,
                ITestCaseEventsHandler testCaseEventsHandler,
                ITestRunEventsHandler testRunEventsHandler,
                ITestPlatformEventSource testPlatformEventSource,
                ITestEventsPublisher testEventsPublisher,
                IThread platformThread,
                IDataSerializer dataSerializer)
                : base(
                    requestData,
                    package,
                    runSettings,
                    testExecutionContext,
                    testCaseEventsHandler,
                    testRunEventsHandler,
                    testPlatformEventSource,
                    testEventsPublisher,
                    platformThread,
                    dataSerializer)
            {
                this.testCaseEventsHandler = testCaseEventsHandler;
            }

            private ITestCaseEventsHandler testCaseEventsHandler;

            public Action<bool> BeforeRaisingTestRunCompleteCallback { get; set; }

            public Func<IFrameworkHandle, RunContext, IEnumerable<Tuple<Uri, string>>> GetExecutorUriExtensionMapCallback { get; set; }

            public
                Action
                    <LazyExtension<ITestExecutor, ITestExecutorCapabilities>, Tuple<Uri, string>, RunContext,
                        IFrameworkHandle> InvokeExecutorCallback
            { get; set; }

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

            protected override void SendSessionEnd()
            {
                this.testCaseEventsHandler?.SendSessionEnd();
            }

            protected override void SendSessionStart()
            {
                this.testCaseEventsHandler?.SendSessionStart(new Dictionary<string, object> { { "TestSources", new List<string>() { "1.dll" } } });
            }

            protected override bool ShouldAttachDebuggerToTestHost(
                LazyExtension<ITestExecutor, ITestExecutorCapabilities> executor,
                Tuple<Uri, string> executorUri,
                RunContext runContext)
            {
                return false;
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
