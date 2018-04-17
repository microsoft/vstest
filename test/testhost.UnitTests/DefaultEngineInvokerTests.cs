// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace testhost.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
            { "--diag", @"C:\Users\samadala\src\vstest\log_3.host.18-04-17_20-25-45_48171_1.txt"},
            { "--telemetryoptedin", "false"},
            { "--datacollectionport", "21290"}
        };

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

            Assert.AreEqual(message,
                string.Format(
                    CultureInfo.CurrentUICulture,
                    CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                    CoreUtilitiesConstants.TesthostProcessName,
                    CoreUtilitiesConstants.DatacollectorProcessName,
                    EnvironmentHelper.DefaultConnectionTimeout,
                    EnvironmentHelper.VstestConnectionTimeout));
        }

        [TestMethod]
        public void InvokeShouldSetParentProcessExistCallback()
        {
            this.engineInvoker.Invoke(argsDictionary);

            this.mockProcssHelper.Verify(h => h.SetExitCallback(ParentProcessId, It.IsAny<Action<object>>()));
        }
    }
}
