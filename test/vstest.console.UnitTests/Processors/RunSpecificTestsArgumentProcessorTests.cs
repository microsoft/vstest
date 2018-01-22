// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Runtime.Versioning;

    using CommandLineUtilities;
    using CoreUtilities.Tracing.Interfaces;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using vstest.console.UnitTests.Processors;

    [TestClass]
    public class RunSpecificTestsArgumentProcessorTests
    {
        private const string NoDiscoveredTestsWarning = @"No test is available in DummyTest.dll. Make sure that installed test discoverers & executors, platform & framework version settings are appropriate and try again.";
        private const string TestAdapterPathSuggestion = @"Additionally, path to test adapters can be specified using /TestAdapterPath command. Example  /TestAdapterPath:<pathToCustomAdapters>.";
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<IOutput> mockOutput;
        private readonly InferHelper inferHelper;
        private string dummyTestFilePath = "DummyTest.dll";
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;

        private RunSpecificTestsArgumentExecutor GetExecutor(ITestRequestManager testRequestManager)
        {
            var runSettingsProvider = new TestableRunSettingsProvider();
            runSettingsProvider.AddDefaultRunSettings();
            return new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, runSettingsProvider, testRequestManager, this.mockOutput.Object);
        }

        public RunSpecificTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockOutput = new Mock<IOutput>();
            this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
            this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);
            this.mockAssemblyMetadataProvider.Setup(x => x.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X64);
            this.mockAssemblyMetadataProvider.Setup(x => x.GetFrameWork(It.IsAny<string>())).Returns(new FrameworkName(Constants.DotNetFramework40));
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyTestFilePath)).Returns(true);
            this.mockFileHelper.Setup(x => x.GetCurrentDirectory()).Returns("");
            this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
            this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
        }

        [TestMethod]
        public void GetMetadataShouldReturnRunSpecificTestsArgumentProcessorCapabilities()
        {
            RunSpecificTestsArgumentProcessor processor = new RunSpecificTestsArgumentProcessor();

            Assert.IsTrue(processor.Metadata.Value is RunSpecificTestsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnRunSpecificTestsArgumentExecutor()
        {
            RunSpecificTestsArgumentProcessor processor = new RunSpecificTestsArgumentProcessor();

            Assert.IsTrue(processor.Executor.Value is RunSpecificTestsArgumentExecutor);
        }

        #region RunSpecificTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            RunSpecificTestsArgumentProcessorCapabilities capabilities = new RunSpecificTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/Tests", capabilities.CommandName);
            StringAssert.Contains(capabilities.HelpContentResourceName, "/Tests:<Test Names>\n      Run tests with names that match the provided values.");

            Assert.AreEqual(HelpContentPriority.RunSpecificTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(true, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }
        #endregion

        #region RunSpecificTestsArgumentExecutorTests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => { executor.Initialize(null); });
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsEmpty()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => { executor.Initialize(String.Empty); });
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsWhiteSpace()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => { executor.Initialize(" "); });
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentsAreEmpty()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => { executor.Initialize(" , "); });
        }

        [TestMethod]
        public void ExecutorShouldSplitTestsSeparatedByComma()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldThrowCommandLineException()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteForValidSourceWithTestCaseFilterShouldThrowCommandLineException()
        {
            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);
            CommandLineOptions.Instance.TestCaseFilterValue = "Filter";
            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionThrownDuringDiscoveryAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionThrownDuringDiscoveryAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionThrownDuringDiscoveryAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionThrownDuringExecutionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestRunRequest.Setup(dr => dr.ExecuteAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionThrownDuringExecutionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestRunRequest.Setup(dr => dr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionThrownDuringExecutionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestRunRequest.Setup(dr => dr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldForValidSourcesAndNoTestsDiscoveredShouldLogWarningAndReturnSuccess()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            this.ResetAndAddSourceToCommandLineOptions();

            // Setting some testdapterpath
            CommandLineOptions.Instance.TestAdapterPath = @"C:\Foo";

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(new List<TestCase>()));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            this.mockOutput.Verify(o => o.WriteLine("Starting test discovery, please wait...", OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine(NoDiscoveredTestsWarning, OutputLevel.Warning), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldForValidSourcesAndNoTestsDiscoveredShouldLogAppropriateWarningIfTestAdapterPathIsNotSetAndReturnSuccess()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            this.ResetAndAddSourceToCommandLineOptions();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(new List<TestCase>()));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            this.mockOutput.Verify(o => o.WriteLine("Starting test discovery, please wait...", OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine(NoDiscoveredTestsWarning + " " + TestAdapterPathSuggestion, OutputLevel.Warning), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldForValidSourcesAndValidSelectedTestsRunsTestsAndReturnSuccess()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            this.ResetAndAddSourceToCommandLineOptions();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorShouldRunTestsWhenTestsAreCommaSeparated()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            ResetAndAddSourceToCommandLineOptions();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri1"), "Source1"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1, Test2");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            mockOutput.Verify(o => o.WriteLine(It.IsAny<string>(), OutputLevel.Warning), Times.Never);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorShouldRunTestsWhenTestsAreFiltered()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            ResetAndAddSourceToCommandLineOptions();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri1"), "Source1"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            mockOutput.Verify(o => o.WriteLine(It.IsAny<string>(), OutputLevel.Warning), Times.Never);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorShouldWarnWhenTestsAreNotAvailable()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            ResetAndAddSourceToCommandLineOptions();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri1"), "Source1"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1, Test2");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            mockOutput.Verify(o => o.WriteLine("A total of 1 tests were discovered but some tests do not match the specified selection criteria(Test1). Use right value(s) and try again.", OutputLevel.Warning), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorShouldRunTestsWhenTestsAreCommaSeparatedWithEscape()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            ResetAndAddSourceToCommandLineOptions();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1(a,b)", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2(c,d)", new Uri("http://FooTestUri1"), "Source1"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            executor.Initialize("Test1(a\\,b), Test2(c\\,d)");
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            mockOutput.Verify(o => o.WriteLine(It.IsAny<string>(), OutputLevel.Warning), Times.Never);
            Assert.AreEqual(ArgumentProcessorResult.Success, argumentProcessorResult);
        }

        #endregion

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.TestCaseFilterValue = null;
            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }
    }
}
