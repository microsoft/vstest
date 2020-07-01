// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Threading.Tasks;
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using vstest.console.Internal;
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
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<ITestRunAttachmentsProcessingManager> mockAttachmentsProcessingManager;

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
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockAttachmentsProcessingManager = new Mock<ITestRunAttachmentsProcessingManager>();
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
            Assert.AreEqual("[TestFileNames]" + Environment.NewLine + "      Run tests from the specified files or wild card pattern. Separate multiple test file names or pattern" + Environment.NewLine + "      by spaces. Set console logger verbosity to detailed to view matched test files." + Environment.NewLine + "      Examples: mytestproject.dll" + Environment.NewLine + "                mytestproject.dll myothertestproject.exe" + Environment.NewLine + @"                testproject*.dll my*project.dll", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.RunTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.IsTrue(capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.IsFalse(capabilities.AllowMultiple);
            Assert.IsFalse(capabilities.AlwaysExecute);
            Assert.IsTrue(capabilities.IsSpecialCommand);
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
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, runSettingsProvider, testRequestManager, this.mockOutput.Object);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldThrowCommandLineException()
        {
            CommandLineOptions.Instance.Reset();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
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
        public void ExecutorExecuteShouldThrowTestPlatformException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<TestPlatformException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowSettingsException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<SettingsException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowInvalidOperationException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<InvalidOperationException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
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

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(testRunRequest);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager);

            return executor.Execute();
        }

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, this.mockFileHelper.Object);
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