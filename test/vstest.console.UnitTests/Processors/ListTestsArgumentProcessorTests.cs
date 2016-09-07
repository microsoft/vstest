// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Implementations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using ObjectModel;
    using ObjectModel.Client;
    using TestPlatform.Utilities;
    using Utilities;
    using TestPlatformHelpers;
    using Common.Logging;    // <summary>
    using CoreUtilities.Tracing;

    // Tests for ListTestsArgumentProcessor
    // </summary>
    [TestClass]
    public class ListTestsArgumentProcessorTests
    {
        MockFileHelper mockFileHelper;
        string dummyTestFilePath = "DummyTest.dll";

        private Mock<TestPlatformEventSource> mockTestPlatformEventSource;

        public ListTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new MockFileHelper();
            this.mockTestPlatformEventSource = new Mock<TestPlatformEventSource>();
            this.mockFileHelper.ExistsInvoker = (path) =>
            {
                if (string.Equals(path, this.dummyTestFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };            
        }

        /// <summary>
        /// The help argument processor get metadata should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetMetadataShouldReturnListTestsArgumentProcessorCapabilities()
        {
            ListTestsArgumentProcessor processor = new ListTestsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ListTestsArgumentProcessorCapabilities);
        }

        /// <summary>
        /// The help argument processor get executer should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetExecuterShouldReturnListTestsArgumentProcessorCapabilities()
        {
            ListTestsArgumentProcessor processor = new ListTestsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ListTestsArgumentExecutor);
        }

        #region ListTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            ListTestsArgumentProcessorCapabilities capabilities = new ListTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/ListTests", capabilities.CommandName);
            Assert.AreEqual("/lt", capabilities.ShortCommandName);
            Assert.AreEqual("/ListTests:<File Name>\n      Lists discovered tests from the given test container.", capabilities.HelpContentResourceName);

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
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper;

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            ListTestsArgumentExecutor executor = new ListTestsArgumentExecutor(
                CommandLineOptions.Instance,
                null,
                testRequestManager);

            executor.Initialize(this.dummyTestFilePath);

            Assert.IsTrue(Enumerable.Contains<string>(CommandLineOptions.Instance.Sources, this.dummyTestFilePath));
        }

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldReturnFail()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            ListTestsArgumentExecutor executor = new ListTestsArgumentExecutor(
                CommandLineOptions.Instance,
                null,
                testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            ListTestsArgumentExecutor listTestsArgumentExecutor =
                 new ListTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    null,
                    testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = listTestsArgumentExecutor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            ListTestsArgumentExecutor listTestsArgumentExecutor =
                 new ListTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    null,
                    testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = listTestsArgumentExecutor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            ListTestsArgumentExecutor listTestsArgumentExecutor =
                 new ListTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    null,
                    testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = listTestsArgumentExecutor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();

            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            var executor = new ListTestsArgumentExecutor(
                    CommandLineOptions.Instance,
                    null,
                    testRequestManager);

            Assert.ThrowsException<Exception>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldOutputDiscoveredTestsAndReturnSuccess()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            mockDiscoveryRequest.Setup(dr => dr.DiscoverAsync()).Raises(dr => dr.OnDiscoveredTests += null, new DiscoveredTestsEventArgs(list));
            
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>())).Returns(mockDiscoveryRequest.Object);
            

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object);
            new ListTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager, mockConsoleOutput.Object).Execute();

            // Assert
            mockDiscoveryRequest.Verify(dr => dr.DiscoverAsync(), Times.Once);
            
            mockConsoleOutput.Verify((IOutput co) => co.WriteLine("    Test1", OutputLevel.Information));
            mockConsoleOutput.Verify((IOutput co) => co.WriteLine("    Test2", OutputLevel.Information));
        }

        #endregion

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }
    }
}
