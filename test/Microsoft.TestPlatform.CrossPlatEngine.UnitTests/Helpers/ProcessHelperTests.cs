// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Reflection;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;

    [TestClass]
    public class ProcessHelperTests
    {
        private readonly TestableProxyOperationManager testOperationManager;

        private readonly TestableTestHostManager testHostManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        private TestableProcessHelper processHelper;

        private int connectionTimeout = 400;

        private int errorLength = 20;

        public ProcessHelperTests()
        {
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(this.connectionTimeout)).Returns(true);

            this.processHelper = new TestableProcessHelper();
            this.testHostManager = new TestableTestHostManager(Architecture.X64, Framework.DefaultFramework, this.processHelper, true);

            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.testHostManager, this.connectionTimeout, this.errorLength);
        }

        [TestMethod]
        public void ErrorMessageShouldBeReadAsynchronously()
        {
            string errorData = "Custom Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(this.testOperationManager.GetError(), errorData);
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            string errorData = "Long Custom Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(this.testOperationManager.GetError().Length, errorLength);
            Assert.AreEqual(this.testOperationManager.GetError(), errorData.Substring(5));
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedFromBeginingShouldDisplayTrailingData()
        {
            string errorData = "Error Strings";
            this.processHelper.SetErrorMessage(errorData);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(this.testOperationManager.GetError(), "StringsError Strings");
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                ITestRequestSender requestSender,
                ITestHostManager testHostManager,
                int clientConnectionTimeout,
                int errorLength) : base(requestSender, testHostManager, clientConnectionTimeout)
            {
                base.ErrorLength = errorLength;
            }

            public string GetError()
            {
                return base.GetStandardError();
            }
        }

        private class TestableTestHostManager : DefaultTestHostManager
        {
            public TestableTestHostManager(
                Architecture architecture, 
                Framework framework, 
                IProcessHelper processHelper, 
                bool shared) : base(architecture, framework, processHelper, shared)
            {
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

            public Process LaunchProcess(string processPath, string arguments, string workingDirectory, Action<Process, string> errorCallback)
            {
                var process = Process.GetCurrentProcess();

                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);
                errorCallback(process, this.ErrorMessage);
                
                return process;
            }
        }
    }
}
