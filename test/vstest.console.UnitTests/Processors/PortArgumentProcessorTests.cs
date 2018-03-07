// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Moq;
    using System;
    using System.Diagnostics;

    [TestClass]
    public class PortArgumentProcessorTests
    {
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<IDesignModeClient> testDesignModeClient;
        private Mock<ITestRequestManager> testRequestManager;
        private PortArgumentExecutor executor;

        public PortArgumentProcessorTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.testDesignModeClient = new Mock<IDesignModeClient>();
            this.testRequestManager = new Mock<ITestRequestManager>();
            this.executor = new PortArgumentExecutor(CommandLineOptions.Instance, this.testRequestManager.Object);
        }

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
            Assert.AreEqual("--Port|/Port:<Port>" + Environment.NewLine + "      The Port for socket connection and receiving the event messages.", capabilities.HelpContentResourceName);

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
            try
            {
                this.executor.Initialize("Foo");
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
            int port = 2345;
            CommandLineOptions.Instance.ParentProcessId = 0;

            this.executor.Initialize(port.ToString());

            Assert.AreEqual(port, CommandLineOptions.Instance.Port);
            Assert.IsNotNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void ExecutorInitializeShouldSetDesignMode()
        {
            int port = 2345;
            CommandLineOptions.Instance.ParentProcessId = 0;

            this.executor.Initialize(port.ToString());

            Assert.IsTrue(CommandLineOptions.Instance.IsDesignMode);
        }

        [TestMethod]
        public void ExecutorInitializeShouldSetProcessExitCallback()
        {
            this.executor = new PortArgumentExecutor(CommandLineOptions.Instance, this.testRequestManager.Object, this.mockProcessHelper.Object);
            int port = 2345;
            int processId = Process.GetCurrentProcess().Id;
            CommandLineOptions.Instance.ParentProcessId = processId;

            this.executor.Initialize(port.ToString());

            this.mockProcessHelper.Verify(ph => ph.SetExitCallback(processId, It.IsAny<Action<object>>()), Times.Once);
        }

        [TestMethod]
        public void ExecutorExecuteForValidConnectionReturnsArgumentProcessorResultSuccess()
        {
            this.executor = new PortArgumentExecutor(CommandLineOptions.Instance, this.testRequestManager.Object,
                (parentProcessId, ph) => this.testDesignModeClient.Object, this.mockProcessHelper.Object);

            int port = 2345;
            this.executor.Initialize(port.ToString());
            var result = executor.Execute();

            this.testDesignModeClient.Verify(td =>
                td.ConnectToClientAndProcessRequests(port, this.testRequestManager.Object), Times.Once);

            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        [TestMethod]
        public void ExecutorExecuteForFailedConnectionShouldThrowCommandLineException()
        {
            this.executor = new PortArgumentExecutor(CommandLineOptions.Instance, this.testRequestManager.Object,
                (parentProcessId, ph) => testDesignModeClient.Object, this.mockProcessHelper.Object);

            testDesignModeClient.Setup(td => td.ConnectToClientAndProcessRequests(It.IsAny<int>(),
                It.IsAny<ITestRequestManager>())).Callback(() => { throw new TimeoutException(); });

            int port = 2345;
            this.executor.Initialize(port.ToString());
            Assert.ThrowsException<CommandLineException>(() => executor.Execute());

            testDesignModeClient.Verify(td => td.ConnectToClientAndProcessRequests(port, this.testRequestManager.Object), Times.Once);
        }


        [TestMethod]
        public void ExecutorExecuteSetsParentProcessIdOnDesignModeInitializer()
        {
            var parentProcessId = 2346;
            var parentProcessIdArgumentExecutor = new ParentProcessIdArgumentExecutor(CommandLineOptions.Instance);
            parentProcessIdArgumentExecutor.Initialize(parentProcessId.ToString());

            int actualParentProcessId = -1;
            this.executor = new PortArgumentExecutor(CommandLineOptions.Instance,
                this.testRequestManager.Object,
                (ppid, ph) =>
                {
                    actualParentProcessId = ppid;
                    return testDesignModeClient.Object;
                },
                this.mockProcessHelper.Object
                );

            int port = 2345;
            this.executor.Initialize(port.ToString());
            var result = executor.Execute();

            testDesignModeClient.Verify(td =>
                td.ConnectToClientAndProcessRequests(port, testRequestManager.Object), Times.Once);

            Assert.AreEqual(parentProcessId, actualParentProcessId, "Parent process Id must be set correctly on design mode initializer");

            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}