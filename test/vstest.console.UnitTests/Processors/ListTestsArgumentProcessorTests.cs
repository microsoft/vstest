// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading.Tasks;

    using CoreUtilities.Tracing.Interfaces;
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using ObjectModel;
    using ObjectModel.Client;
    using TestPlatform.Utilities;
    using TestPlatformHelpers;
    using vstest.console.Internal;
    using vstest.console.UnitTests.Processors;

    // <summary>
    // Tests for ListTestsArgumentProcessor
    // </summary>
    [TestClass]
    public class ListTestsArgumentProcessorTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private InferHelper inferHelper;
        private string dummyTestFilePath = "DummyTest.dll";
        private readonly Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<ITestRunAttachmentsProcessingManager> mockAttachmentsProcessingManager;

        private static ListTestsArgumentExecutor GetExecutor(ITestRequestManager testRequestManager, IOutput output)
        {
            var runSettingsProvider = new TestableRunSettingsProvider();

            runSettingsProvider.AddDefaultRunSettings();
            var listTestsArgumentExecutor =
                new ListTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    runSettingsProvider,
                    testRequestManager,
                    output ?? ConsoleOutput.Instance);
            return listTestsArgumentExecutor;
        }

        [TestCleanup]
        public void Cleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        public ListTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyTestFilePath)).Returns(true);
            this.mockFileHelper.Setup(x => x.GetCurrentDirectory()).Returns("");
            this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
            this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
            this.mockAssemblyMetadataProvider.Setup(x => x.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X64);
            this.mockAssemblyMetadataProvider.Setup(x => x.GetFrameWork(It.IsAny<string>())).Returns(new FrameworkName(Constants.DotNetFramework40));
            this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockAttachmentsProcessingManager = new Mock<ITestRunAttachmentsProcessingManager>();
        }

        /// <summary>
        /// The help argument processor get metadata should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetMetadataShouldReturnListTestsArgumentProcessorCapabilities()
        {
            var processor = new ListTestsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ListTestsArgumentProcessorCapabilities);
        }

        /// <summary>
        /// The help argument processor get executer should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetExecuterShouldReturnListTestsArgumentProcessorCapabilities()
        {
            var processor = new ListTestsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ListTestsArgumentExecutor);
        }

        #region ListTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new ListTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/ListTests", capabilities.CommandName);
            Assert.AreEqual("/lt", capabilities.ShortCommandName);
            Assert.AreEqual("-lt|--ListTests|/lt|/ListTests:<File Name>" + Environment.NewLine + "      Lists all discovered tests from the given test container.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.ListTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.IsTrue(capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.IsFalse(capabilities.AllowMultiple);
            Assert.IsFalse(capabilities.AlwaysExecute);
            Assert.IsFalse(capabilities.IsSpecialCommand);
        }
        #endregion

        #region ListTestsArgumentExecutorTests

        [TestMethod]
        public void ExecutorInitializeWithValidSourceShouldAddItToTestSources()
        {
            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, this.mockFileHelper.Object);
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager, null);

            executor.Initialize(this.dummyTestFilePath);

            Assert.IsTrue(Enumerable.Contains<string>(CommandLineOptions.Instance.Sources, this.dummyTestFilePath));
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldReturnFail()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, TestPlatformFactory.GetTestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowTestPlatformException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<TestPlatformException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowSettingsException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var listTestsArgumentExecutor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<SettingsException>(() => listTestsArgumentExecutor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowInvalidOperationException()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var listTestsArgumentExecutor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<InvalidOperationException>(() => listTestsArgumentExecutor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<Exception>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldOutputDiscoveredTestsAndReturnSuccess()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            // Assert
            mockDiscoveryRequest.Verify(dr => dr.DiscoverAsync(), Times.Once);

            mockConsoleOutput.Verify((IOutput co) => co.WriteLine("    Test1", OutputLevel.Information));
            mockConsoleOutput.Verify((IOutput co) => co.WriteLine("    Test2", OutputLevel.Information));
        }

        [TestMethod]
        public void ListTestArgumentProcessorExecuteShouldInstrumentDiscoveryRequestStart()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            this.mockTestPlatformEventSource.Verify(x => x.DiscoveryRequestStart(), Times.Once);
        }

        [TestMethod]
        public void ListTestArgumentProcessorExecuteShouldInstrumentDiscoveryRequestStop()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            this.mockTestPlatformEventSource.Verify(x => x.DiscoveryRequestStop(), Times.Once);
        }
        #endregion

        private void RunListTestArgumentProcessorExecuteWithMockSetup(Mock<IDiscoveryRequest> mockDiscoveryRequest, Mock<IOutput> mockConsoleOutput)
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));

            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);


            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);
            GetExecutor(testRequestManager, mockConsoleOutput.Object).Execute();
        }

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, this.mockFileHelper.Object);
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }
    }
}
