// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace testhost.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.TestHost;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    using CommunicationUtilitiesResources =
        Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    [TestClass]
    public class DefaultEngineInvokerTests
    {
        private const int ParentProcessId = 27524;
        private static readonly IDictionary<string, string> argsDictionary = new Dictionary<string, string>
        {
            { "--port", "21291" },
            { "--endpoint", "127.0.0.1:021291"  },
            { "--role", "client"},
            { "--parentprocessid", ParentProcessId.ToString() },
            { "--diag", "temp.txt"},
            { "--tracelevel", "3"},
            { "--telemetryoptedin", "false"},
            { "--datacollectionport", "21290"}
        };
        private static readonly string TimoutErrorMessage =
            "testhost process failed to connect to datacollector process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";

        private Mock<ITestRequestHandler> mockTestRequestHandler;
        private Mock<IDataCollectionTestCaseEventSender> mockDataCollectionTestCaseEventSender;
        private Mock<IProcessHelper> mockProcssHelper;
        private DefaultEngineInvoker engineInvoker;

        public DefaultEngineInvokerTests()
        {
            this.mockDataCollectionTestCaseEventSender = new Mock<IDataCollectionTestCaseEventSender>();
            this.mockTestRequestHandler = new Mock<ITestRequestHandler>();
            this.mockProcssHelper = new Mock<IProcessHelper>();
            this.engineInvoker = new DefaultEngineInvoker(
                this.mockTestRequestHandler.Object,
                this.mockDataCollectionTestCaseEventSender.Object,
                this.mockProcssHelper.Object);
            this.mockTestRequestHandler.Setup(h => h.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(true);
            this.mockDataCollectionTestCaseEventSender.Setup(s => s.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
        }

        [TestMethod]
        public void InvokeShouldWaitForDefaultTimeoutIfNoEnvVariableSetDuringDataCollectorConnection()
        {
            this.engineInvoker.Invoke(argsDictionary);

            this.mockDataCollectionTestCaseEventSender.Verify(s => s.WaitForRequestSenderConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000));
        }

        [TestMethod]
        public void InvokeShouldWaitBasedOnTimeoutEnvVariableDuringDataCollectorConnection()
        {
            var timeout = 10;
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, timeout.ToString());
            this.engineInvoker.Invoke(argsDictionary);

            this.mockDataCollectionTestCaseEventSender.Verify(s => s.WaitForRequestSenderConnection(timeout * 1000));
        }

        [TestMethod]
        public void InvokeShouldThrowExceptionIfDataCollectorConnection()
        {
            this.mockDataCollectionTestCaseEventSender.Setup(s => s.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(false);
            var message = Assert.ThrowsException<TestPlatformException>(() => this.engineInvoker.Invoke(argsDictionary)).Message;

            Assert.AreEqual(message, DefaultEngineInvokerTests.TimoutErrorMessage);
        }

        [TestMethod]
        public void InvokeShouldSetParentProcessExistCallback()
        {
            this.engineInvoker.Invoke(argsDictionary);

            this.mockProcssHelper.Verify(h => h.SetExitCallback(ParentProcessId, It.IsAny<Action<object>>()));
        }

        [TestMethod]
        public void InvokeShouldInitializeTraceWithCorrectTraceLevel()
        {
            // Setting EqtTrace.TraceLevel to a value other than info.
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif

            this.engineInvoker.Invoke(argsDictionary);

            // Verify
            Assert.AreEqual(TraceLevel.Info, (TraceLevel)EqtTrace.TraceLevel);
        }

        [TestMethod]
        public void InvokeShouldInitializeTraceWithVerboseTraceLevelIfInvalidTraceLevelPassed()
        {
            // Setting EqtTrace.TraceLevel to a value other than info.
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Warning;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Warning;
#endif

            try
            {
                argsDictionary["--tracelevel"] = "5"; // int value which is not defined in TraceLevel.
                this.engineInvoker.Invoke(argsDictionary);
            }
            finally{
                argsDictionary["--tracelevel"] = "3"; // Setting to default value of 3.
            }

            // Verify
            Assert.AreEqual(TraceLevel.Verbose, (TraceLevel)EqtTrace.TraceLevel);
        }
    }
}
