// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    // Tests for ListFullyQualifiedTestsArgumentProcessor
    // </summary>
    [TestClass]
    public class ListFullyQualifiedTestsArgumentProcessorTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private InferHelper inferHelper;
        private string dummyTestFilePath = "DummyTest.dll";
        private string dummyFilePath = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid()}.txt");
        private readonly Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<ITestRunAttachmentsProcessingManager> mockAttachmentsProcessingManager;

        private static ListFullyQualifiedTestsArgumentExecutor GetExecutor(ITestRequestManager testRequestManager, IOutput output)
        {
            var runSettingsProvider = new TestableRunSettingsProvider();
            runSettingsProvider.AddDefaultRunSettings();
            var listFullyQualifiedTestsArgumentExecutor =
                new ListFullyQualifiedTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    runSettingsProvider,
                    testRequestManager,
                    output ?? ConsoleOutput.Instance);
            return listFullyQualifiedTestsArgumentExecutor;
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(dummyFilePath);
            CommandLineOptions.Instance.Reset();
        }

        public ListFullyQualifiedTestsArgumentProcessorTests()
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
        public void GetMetadataShouldReturnListFullyQualifiedTestsArgumentProcessorCapabilities()
        {
            var processor = new ListTestsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ListTestsArgumentProcessorCapabilities);
        }

        /// <summary>
        /// The help argument processor get executer should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetExecuterShouldReturnListFullyQualifiedTestsArgumentProcessorCapabilities()
        {
            var processor = new ListFullyQualifiedTestsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ListFullyQualifiedTestsArgumentExecutor);
        }

        #region ListTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new ListFullyQualifiedTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/ListFullyQualifiedTests", capabilities.CommandName);

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

            this.ResetAndAddSourceToCommandLineOptions(true);

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
            this.ResetAndAddSourceToCommandLineOptions(true);

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

            this.ResetAndAddSourceToCommandLineOptions(true);


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

            this.ResetAndAddSourceToCommandLineOptions(true);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);

            var executor = GetExecutor(testRequestManager, null);

            Assert.ThrowsException<Exception>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldOutputDiscoveredTestsAndReturnSuccess()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListFullyQualifiedTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            mockDiscoveryRequest.Verify(dr => dr.DiscoverAsync(), Times.Once);

            var fileOutput = File.ReadAllLines(this.dummyFilePath);
            Assert.IsTrue(fileOutput.Length == 2);
            Assert.IsTrue(fileOutput.Contains("Test1"));
            Assert.IsTrue(fileOutput.Contains("Test2"));
        }

        [TestMethod]
        public void DiscoveryShouldFilterCategoryTestsAndReturnSuccess()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListFullyQualifiedTestArgumentProcessorWithTraits(mockDiscoveryRequest, mockConsoleOutput);

            mockDiscoveryRequest.Verify(dr => dr.DiscoverAsync(), Times.Once);

            var fileOutput = File.ReadAllLines(this.dummyFilePath);
            Assert.IsTrue(fileOutput.Length == 1);
            Assert.IsTrue(fileOutput.Contains("Test1"));
            Assert.IsTrue(!fileOutput.Contains("Test2"));
        }

        [ExpectedException(typeof(CommandLineException))]
        [TestMethod]
        public void ExecutorExecuteShouldThrowWhenListFullyQualifiedTestsTargetPathIsEmpty()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListFullyQualifiedTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput, false);
        }

        [TestMethod]
        public void ListFullyQualifiedTestsArgumentProcessorExecuteShouldInstrumentDiscoveryRequestStart()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListFullyQualifiedTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            this.mockTestPlatformEventSource.Verify(x => x.DiscoveryRequestStart(), Times.Once);
        }

        [TestMethod]
        public void ListFullyQualifiedTestsArgumentProcessorExecuteShouldInstrumentDiscoveryRequestStop()
        {
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            this.RunListFullyQualifiedTestArgumentProcessorExecuteWithMockSetup(mockDiscoveryRequest, mockConsoleOutput);

            this.mockTestPlatformEventSource.Verify(x => x.DiscoveryRequestStop(), Times.Once);
        }
        #endregion

        private void RunListFullyQualifiedTestArgumentProcessorWithTraits(Mock<IDiscoveryRequest> mockDiscoveryRequest, Mock<IOutput> mockConsoleOutput, bool legitPath = true)
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var list = new List<TestCase>();

            var t1 = new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1");
            t1.Traits.Add(new Trait("Category", "MyCat"));
            list.Add(t1);

            var t2 = new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2");
            t2.Traits.Add(new Trait("Category", "MyBat"));
            list.Add(t2);

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions(legitPath);
            var cmdOptions = CommandLineOptions.Instance;
            cmdOptions.TestCaseFilterValue = "TestCategory=MyCat";

            var testRequestManager = new TestRequestManager(cmdOptions, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);

            GetExecutor(testRequestManager, mockConsoleOutput.Object).Execute();
        }

        private void RunListFullyQualifiedTestArgumentProcessorExecuteWithMockSetup(Mock<IDiscoveryRequest> mockDiscoveryRequest, Mock<IOutput> mockConsoleOutput, bool legitPath = true)
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions(legitPath);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask, this.mockProcessHelper.Object, this.mockAttachmentsProcessingManager.Object);

            GetExecutor(testRequestManager, mockConsoleOutput.Object).Execute();
        }

        private void ResetAndAddSourceToCommandLineOptions(bool legitPath)
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, this.mockFileHelper.Object);
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
            if (legitPath)
            {
                CommandLineOptions.Instance.ListTestsTargetPath = this.dummyFilePath;
            }
            else
            {
                CommandLineOptions.Instance.ListTestsTargetPath = string.Empty;
            }
        }
    }
}
