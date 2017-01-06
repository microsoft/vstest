// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;

    using Moq;
    using CoreUtilities.Tracing;
    using CoreUtilities.Tracing.Interfaces;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    [TestClass]
    public class RunSpecificTestsArgumentProcessorTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;
        private string dummyTestFilePath = "DummyTest.dll";

        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;

        [TestInitialize]
        public void Init()
        {
            RunSettingsUtilities.AddDefaultRunSettings(RunSettingsManager.Instance);
        }

        [TestCleanup]
        public void Cleanup()
        {
            RunSettingsUtilities.AddDefaultRunSettings(RunSettingsManager.Instance);
        }


        public RunSpecificTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyTestFilePath)).Returns(true);
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
        public void ExecutorExecuteForNoSourcesShouldThrowCommandLineException()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            RunSpecificTestsArgumentExecutor executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);
            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteForValidSourceWithTestCaseFilterShouldThrowCommandLineException()
        {
            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);
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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

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
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
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

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new RunSpecificTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            executor.Initialize("Test1");

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
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
