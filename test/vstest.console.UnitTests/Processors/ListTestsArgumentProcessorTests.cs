// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Runtime.Versioning;

    using Common.Logging;

    using CoreUtilities.Tracing.Interfaces;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ObjectModel;
    using ObjectModel.Client;

    using TestPlatform.Utilities;

    using TestPlatformHelpers;

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
            Assert.AreEqual("-lt|--ListTests|/lt|/ListTests:<File Name>\n      Lists discovered tests from the given test container.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.ListTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(true, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }
        #endregion

        #region ListTestsArgumentExecutorTests

        [TestMethod]
        public void ExecutorInitializeWithValidSourceShouldAddItToTestSources()
        {
            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager, null);

            executor.Initialize(this.dummyTestFilePath);

            Assert.IsTrue(Enumerable.Contains<string>(CommandLineOptions.Instance.Sources, this.dummyTestFilePath));
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldReturnFail()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager, null);

            var argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var listTestsArgumentExecutor = GetExecutor(testRequestManager, null);

            var argumentProcessorResult = listTestsArgumentExecutor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var listTestsArgumentExecutor = GetExecutor(testRequestManager, null);

            var argumentProcessorResult = listTestsArgumentExecutor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<Exception>(() => executor.Execute());
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

            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);


            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            GetExecutor(testRequestManager, mockConsoleOutput.Object).Execute();
        }

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }
    }
}
