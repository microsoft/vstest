// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProcessHelperTests
    {
        private readonly TestableProxyOperationManager testOperationManager;

        private readonly TestableTestHostManager testHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private TestableProcessHelper processHelper;

        private int connectionTimeout = 400;

        private int errorLength = 20;

        private TestSessionMessageLogger sessionMessageLogger;

        private string errorMessage;

        public ProcessHelperTests()
        {
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);

            this.processHelper = new TestableProcessHelper();
            this.testHostManager = new TestableTestHostManager(Architecture.X64, Framework.DefaultFramework, this.processHelper, true, errorLength);

            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.testHostManager, this.connectionTimeout, this.errorLength);
            this.sessionMessageLogger = TestSessionMessageLogger.Instance;
            this.sessionMessageLogger.TestRunMessage += this.TestSessionMessageHandler;
        }

        private void TestSessionMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            errorMessage = e.Message;
        }

        [TestMethod]
        public void ErrorMessageShouldBeReadAsynchronously()
        {
            string errorData = "Custom Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, errorData);
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            string errorData = "Long Custom Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage.Length, errorLength);
            Assert.AreEqual(errorMessage, errorData.Substring(5));
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedFromBeginingShouldDisplayTrailingData()
        {
            string errorData = "Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, "StringsError Strings");
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                ITestRequestSender requestSender,
                ITestRuntimeProvider testHostManager,
                int clientConnectionTimeout,
                int errorLength) : base(requestSender, testHostManager, clientConnectionTimeout)
            {
                base.ErrorLength = errorLength;
            }
        }

        private class TestableTestHostManager : DefaultTestHostManager
        {
            public TestableTestHostManager(
                Architecture architecture,
                Framework framework,
                IProcessHelper processHelper,
                bool shared,
                int errorLength) : base(architecture, framework, processHelper, shared)
            {
                base.TimeOut = 30000;
                base.ErrorLength = errorLength;
                base.Initialize(TestSessionMessageLogger.Instance);
            }

            public override TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                return new TestProcessStartInfo();
            }
        }
        private class TestableProcessHelper : IProcessHelper
        {
            private string ErrorMessage;

            public void SetErrorMessage(string errorMessage)
            {
                this.ErrorMessage = errorMessage;
            }
            public string GetCurrentProcessFileName()
            {
                throw new NotImplementedException();
            }

            public int GetCurrentProcessId()
            {
                throw new NotImplementedException();
            }

            public string GetTestEngineDirectory()
            {
                throw new NotImplementedException();
            }

            public Process LaunchProcess(string processPath, string arguments, string workingDirectory, IDictionary<string, string> envVariables, Action<Process, string> errorCallback)
            {
                var process = Process.GetCurrentProcess();

                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);

                return process;
            }

            public string GetProcessName(int processId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
