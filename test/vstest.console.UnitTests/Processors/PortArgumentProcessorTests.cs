// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    [TestClass]
    public class PortArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnPortArgumentProcessorCapabilities()
        {
            var processor = new PortArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is PortArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnPortArgumentProcessorCapabilities()
        {
            var processor = new PortArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is PortArgumentExecutor);
        }

        #region PortArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            var capabilities = new PortArgumentProcessorCapabilities();
            Assert.AreEqual("/Port", capabilities.CommandName);
            Assert.AreEqual(("--Port|/Port:<Port>" + Environment.NewLine + "      The Port for socket connection and receiving the event messages.").Replace("\r", string.Empty), capabilities.HelpContentResourceName.Replace("\r", string.Empty));

            Assert.AreEqual(HelpContentPriority.PortArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.DesignMode, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecutorInitializeWithNullOrEmptyPortShouldThrowCommandLineException()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The --Port|/Port argument requires the port number which is an integer. Specify the port for socket connection and receiving the event messages.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecutorInitializeWithInvalidPortShouldThrowCommandLineException()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            try
            {
                executor.Initialize("Foo");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The --Port|/Port argument requires the port number which is an integer. Specify the port for socket connection and receiving the event messages.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecutorInitializeWithValidPortShouldAddPortToCommandLineOptionsAndInitializeDesignModeManager()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            int port = 2345;
            CommandLineOptions.Instance.ParentProcessId = 0;

            executor.Initialize(port.ToString());

            Assert.AreEqual(port, CommandLineOptions.Instance.Port);
            Assert.IsNotNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void ExecutorInitializeShouldSetDesignMode()
        {
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance);
            int port = 2345;
            CommandLineOptions.Instance.ParentProcessId = 0;

            executor.Initialize(port.ToString());

            Assert.IsTrue(CommandLineOptions.Instance.IsDesignMode);
        }

        [TestMethod]
        public void ExecutorExecuteForValidConnectionReturnsArgumentProcessorResultSuccess()
        {
            var testDesignModeClient = new Mock<IDesignModeClient>();
            var testRequestManager = new Mock<ITestRequestManager>();

            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, testRequestManager.Object,
                (parentProcessId) => testDesignModeClient.Object);

            int port = 2345;
            executor.Initialize(port.ToString());
            var result = executor.Execute();

            testDesignModeClient.Verify(td =>
                td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);

            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        [TestMethod]
        public void ExecutorExecuteForFailedConnectionShouldThrowCommandLineException()
        {
            var testRequestManager = new Mock<ITestRequestManager>();
            var testDesignModeClient = new Mock<IDesignModeClient>();

            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, testRequestManager.Object,
                (parentProcessId) => testDesignModeClient.Object);

            testDesignModeClient.Setup(td => td.ConnectToClientAndProcessRequests(It.IsAny<int>(),
                It.IsAny<ITestRequestManager>())).Callback(() => { throw new TimeoutException(); });

            int port = 2345;
            executor.Initialize(port.ToString());
            Assert.ThrowsException<CommandLineException>(() => executor.Execute());

            testDesignModeClient.Verify(td => td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);
        }


        [TestMethod]
        public void ExecutorExecuteSetsParentProcessIdOnDesignModeInitializer()
        {
            var testDesignModeClient = new Mock<IDesignModeClient>();
            var testRequestManager = new Mock<ITestRequestManager>();

            var parentProcessId = 2346;
            var parentProcessIdArgumentExecutor = new ParentProcessIdArgumentExecutor(CommandLineOptions.Instance);
            parentProcessIdArgumentExecutor.Initialize(parentProcessId.ToString());

            int actualParentProcessId = -1;
            var executor = new PortArgumentExecutor(CommandLineOptions.Instance, testRequestManager.Object,
                (ppid) =>
                {
                    actualParentProcessId = ppid;
                    return testDesignModeClient.Object;
                });

            int port = 2345;
            executor.Initialize(port.ToString());
            var result = executor.Execute();

            testDesignModeClient.Verify(td =>
                td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);

            Assert.AreEqual(parentProcessId, actualParentProcessId, "Parent process Id must be set correctly on design mode initializer");

            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}