// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Runtime.Versioning;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using vstest.console.UnitTests.Processors;

    /// <summary>
    /// Tests for RunTestsArgumentProcessor
    /// </summary>
    [TestClass]
    public class RunTestsArgumentProcessorTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<IOutput> mockOutput;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private InferHelper inferHelper;
        private string dummyTestFilePath = "DummyTest.dll";
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;

        public RunTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockOutput = new Mock<IOutput>();
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyTestFilePath)).Returns(true);
            this.mockFileHelper.Setup(x => x.GetCurrentDirectory()).Returns("");
            this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
            this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
            this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);
            SetupMockExtensions();
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.X86);
            this.mockAssemblyMetadataProvider.Setup(x => x.GetFrameWork(It.IsAny<string>())).Returns(new FrameworkName(Constants.DotNetFramework40));
        }

        [TestMethod]
        public void GetMetadataShouldReturnRunTestsArgumentProcessorCapabilities()
        {
            RunTestsArgumentProcessor processor = new RunTestsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is RunTestsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunTestsArgumentProcessorCapabilities()
        {
            RunTestsArgumentProcessor processor = new RunTestsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is RunTestsArgumentExecutor);
        }

        #region RunTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            RunTestsArgumentProcessorCapabilities capabilities = new RunTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/RunTests", capabilities.CommandName);
            Assert.AreEqual("[TestFileNames]\r\n      Run tests from the specified files. Separate multiple test file names\r\n      by spaces.\r\n      Examples: mytestproject.dll\r\n                mytestproject.dll myothertestproject.exe", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.RunTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(true, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(true, capabilities.IsSpecialCommand);
        }
        #endregion

        #region RunTestsArgumentExecutorTests

        [TestMethod]
        public void ExecutorExecuteShouldReturnSuccessWithoutExecutionInDesignMode()
        {
            var runSettingsProvider = new TestableRunSettingsProvider();
            runSettingsProvider.UpdateRunSettings("<RunSettings/>");

            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.IsDesignMode = true;
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, runSettingsProvider, testRequestManager, this.mockOutput.Object);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldThrowCommandLineException()
        {
            CommandLineOptions.Instance.Reset();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        private RunTestsArgumentExecutor GetExecutor(ITestRequestManager testRequestManager)
        {
            var runSettingsProvider = new TestableRunSettingsProvider();
            runSettingsProvider.AddDefaultRunSettings();
            var executor = new RunTestsArgumentExecutor(
                CommandLineOptions.Instance,
                runSettingsProvider,
                testRequestManager,
                this.mockOutput.Object
                );
            return executor;
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<Exception>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldForListOfTestsReturnSuccess()
        {
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            var result = this.RunRunArgumentProcessorExecuteWithMockSetup(mockTestRunRequest.Object);

            mockTestRunRequest.Verify(tr => tr.ExecuteAsync(), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        [TestMethod]
        public void TestRunRequestManagerShouldInstrumentExecutionRequestStart()
        {
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            var result = this.RunRunArgumentProcessorExecuteWithMockSetup(mockTestRunRequest.Object);

            this.mockTestPlatformEventSource.Verify(x => x.ExecutionRequestStart(), Times.Once);
        }

        [TestMethod]
        public void TestRunRequestManagerShouldInstrumentExecutionRequestStop()
        {
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            var result = this.RunRunArgumentProcessorExecuteWithMockSetup(mockTestRunRequest.Object);

            this.mockTestPlatformEventSource.Verify(x => x.ExecutionRequestStop(), Times.Once);
        }

        #endregion

        private ArgumentProcessorResult RunRunArgumentProcessorExecuteWithMockSetup(ITestRunRequest testRunRequest)
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockConsoleOutput = new Mock<IOutput>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            var mockTestRunStats = new Mock<ITestRunStatistics>();

            var args = new TestRunCompleteEventArgs(mockTestRunStats.Object, false, false, null, null, new TimeSpan());

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(testRunRequest);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            return executor.Execute();
        }

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }

        public static void SetupMockExtensions()
        {
            SetupMockExtensions(() => { });
        }

        public static void SetupMockExtensions(Action callback)
        {
            SetupMockExtensions(new string[] { typeof(RunTestsArgumentProcessorTests).GetTypeInfo().Assembly.Location, typeof(ConsoleLogger).GetTypeInfo().Assembly.Location }, callback);
        }

        public static void SetupMockExtensions(string[] extensions, Action callback)
        {
            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, new[] { ".dll" }))
                .Callback(callback)
                .Returns(extensions);

            var testableTestPluginCache = new TestableTestPluginCache();

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
        }

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger3 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                events.TestRunMessage += TestMessageHandler;
                events.TestRunComplete += Events_TestRunComplete;
                events.TestResult += Events_TestResult;
            }

            private void Events_TestResult(object sender, TestResultEventArgs e)
            {
            }

            private void Events_TestRunComplete(object sender, TestRunCompleteEventArgs e)
            {

            }

            private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
            {
            }
        }
    }

    #region Testable implementation

    public class TestableTestPluginCache : TestPluginCache
    {
    }

    #endregion

}