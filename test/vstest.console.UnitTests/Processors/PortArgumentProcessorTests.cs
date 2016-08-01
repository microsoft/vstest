// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    [TestClass]
    public class PortArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnPortArgumentProcessorCapabilities()
        {
            PortArgumentProcessor processor = new PortArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is PortArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnPortArgumentProcessorCapabilities()
        {
            PortArgumentProcessor processor = new PortArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is PortArgumentExecutor);
        }

        #region PortArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            PortArgumentProcessorCapabilities capabilities = new PortArgumentProcessorCapabilities();
            Assert.AreEqual("/Port", capabilities.CommandName);
            Assert.AreEqual("/Port:<Port>\n     The Port for socket connection and receiving the event messages.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.PortArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecuterInitializeWithNullOrEmptyPortShouldThrowCommandLineException()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The /Port argument requires the port number which is an integer. Specify the port for socket connection and receiving the event messages.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithInvalidPortShouldThrowCommandLineException()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            try
            {
                executor.Initialize("Foo");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The /Port argument requires the port number which is an integer. Specify the port for socket connection and receiving the event messages.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithValidPortShouldAddPortToCommandLineOptionsAndInitializeDesignModeManger()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            int port = 2345;
            executor.Initialize(port.ToString());
            Assert.AreEqual(port, CommandLineOptions.Instance.Port);
            Assert.IsNotNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void ExecutorExecuteForValidConnectionReturnsArgumentProcessorResultSuccess()
        {
            var testDesignModeClient = new Mock<IDesignModeClient>();
            var testRequestManager = new Mock<ITestRequestManager>();

            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, testRequestManager.Object,
                () => testDesignModeClient.Object);

            int port = 2345;
            executor.Initialize(port.ToString());
            var result = executor.Execute();

            testDesignModeClient.Verify(td => 
                td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);

            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        [TestMethod]
        public void ExecutorExecuteForFailedConnectionReturnsArgumentProcessorResultFail()
        {
            var testRequestManager = new Mock<ITestRequestManager>();
            var testDesignModeClient = new Mock<IDesignModeClient>();

            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, testRequestManager.Object,
                () => testDesignModeClient.Object);

            testDesignModeClient.Setup(td => td.ConnectToClientAndProcessRequests(It.IsAny<int>(), 
                It.IsAny<ITestRequestManager>())).Callback(() => { throw new TimeoutException(); });

            int port = 2345;
            executor.Initialize(port.ToString());
            var result = executor.Execute();

            testDesignModeClient.Verify(td => td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Fail, result);
        }
    }
}
