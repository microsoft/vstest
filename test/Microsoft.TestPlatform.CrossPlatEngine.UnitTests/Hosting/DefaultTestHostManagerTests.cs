// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System.Threading.Tasks;
    using System.Threading;
    using System;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private DefaultTestHostManager testHostManager;
        private readonly TestProcessStartInfo startInfo;

        private TestableProxyOperationManager testOperationManager;

        private TestableTestHostManager testableTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;
        private Mock<IMessageLogger> mockMessageLogger;
        private Mock<IProcessHelper> mockProcessHelper;

        private int connectionTimeout = 400;
        private int errorLength = 20;
        
        private string errorMessage;

        public DefaultTestHostManagerTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            
            this.mockMessageLogger = new Mock<IMessageLogger>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, true);
            this.startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));
        }

        public void ErrorCallBackTestHelper(string errorMessage, int exitCode)
        {
            this.testableTestHostManager = new TestableTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, 
                true, errorLength, this.mockMessageLogger.Object);

            this.testableTestHostManager.HostExited += TestHostManager_HostExited;
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.mockProcessHelper.Setup(ph => ph.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<Process, string>>())).
                Callback<string, string, string, IDictionary<string, string>, Action<Process, string>>((var1, var2, var3, dictionary, errorCallback) =>
                {
                    var process = Process.GetCurrentProcess();

                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<Process>(), out exitCode)).Returns(true);


            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.testableTestHostManager, this.connectionTimeout, this.errorLength);
        }

        private void TestHostManager_HostExited(object sender, HostProviderEventArgs e)
        {
            this.errorMessage = e.Data;
        }

        [TestMethod]
        public void ConstructorShouldSetX86ProcessForX86Architecture()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X86, Framework.DefaultFramework, this.mockProcessHelper.Object, true);

            var info = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            StringAssert.EndsWith(info.FileName, "testhost.x86.exe");
        }

        [TestMethod]
        public void ConstructorShouldSetX64ProcessForX64Architecture()
        {
            StringAssert.EndsWith(this.startInfo.FileName, "testhost.exe");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeFileNameFromSubFolderTestHostWhenCurrentProcessIsDotnet()
        {
            string subFoler = "TestHost";
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("dotnet.exe");

            var startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            Assert.IsTrue(startInfo.FileName.EndsWith(Path.Combine(subFoler, "testhost.exe")));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, RunnerProcessId = 101 };
            var info = this.testHostManager.GetTestHostProcessStartInfo(
                Enumerable.Empty<string>(),
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --parentprocessid 101", info.Arguments);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEmptyEnvironmentVariables()
        {
            Assert.AreEqual(0, this.startInfo.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEnvironmentVariables()
        {
            var environmentVariables = new Dictionary<string, string> { { "k1", "v1" } };

            var info = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), environmentVariables, default(TestRunnerConnectionInfo));

            Assert.AreEqual(environmentVariables, info.EnvironmentVariables);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeCurrentWorkingDirectory()
        {
            Assert.AreEqual(Directory.GetCurrentDirectory(), this.startInfo.WorkingDirectory);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestSourcePathInArgumentsIfNonShared()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, shared: false);
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, RunnerProcessId = 101 };

            var source = "C:\temp\a.dll";
            var info = this.testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --parentprocessid 101 --testsourcepath " + "\"" + source + "\"", info.Arguments);
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            this.mockProcessHelper.Setup(ph => ph.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<Process, string>>())).
                Returns(Process.GetCurrentProcess());

            var testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, true);
            var startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            Task<int> processId = testHostManager.LaunchTestHostAsync(startInfo);

            try
            {
                processId.Wait();
            }
            catch (AggregateException) { }

            Assert.AreEqual(Process.GetCurrentProcess().Id, processId.Result);
        }

        [TestMethod]
        public void PropertiesShouldReturnEmptyDictionary()
        {
            Assert.AreEqual(0, this.testHostManager.Properties.Count);
        }

        [TestMethod]
        public void DefaultTestHostManagerShouldBeShared()
        {
            Assert.IsTrue(this.testHostManager.Shared);
        }

        [TestMethod]
        public void LaunchTestHostShouldUseCustomHostIfSet()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.testHostManager.SetCustomLauncher(mockCustomLauncher.Object);
            var currentProcess = Process.GetCurrentProcess();
            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(currentProcess.Id);

            Task<int> pid = this.testHostManager.LaunchTestHostAsync(this.startInfo);
            try
            {
                pid.Wait(new CancellationTokenSource(3000).Token);
            }
            catch (Exception) { }

            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            Assert.AreEqual(currentProcess.Id, pid.Result);
        }

        [TestMethod]
        public void ErrorMessageShouldBeReadAsynchronously()
        {
            string errorData = "Custom Error Strings";            
            ErrorCallBackTestHelper(errorData, -1);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, errorData);
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            string errorData = "Long Custom Error Strings";
            ErrorCallBackTestHelper(errorData, -1);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage.Length, errorLength);
            Assert.AreEqual(errorMessage, errorData.Substring(5));
        }

        [TestMethod]
        public void ErrorMessageShouldBeTruncatedFromBeginingShouldDisplayTrailingData()
        {
            string errorData = "Error Strings";
            ErrorCallBackTestHelper(errorData, -1);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, "StringsError Strings");
        }

        [TestMethod]
        public void NoErrorMessageIfExitCodeZero()
        {
            string errorData = "";
            ErrorCallBackTestHelper(errorData, 0);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(null, errorMessage);
        }

        [TestMethod]
        public void NoErrorMessageIfDataIsNull()
        {
            string errorData = null;
            ErrorCallBackTestHelper(errorData, -1);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, "");
        }

        [TestMethod]
        public void NoErrorMessageIfDataIsEmpty()
        {
            string errorData = string.Empty;
            ErrorCallBackTestHelper(errorData, -1);

            this.testOperationManager.SetupChannel(Enumerable.Empty<string>());

            Assert.AreEqual(errorMessage, "");
        }

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager(
                ITestRequestSender requestSender,
                ITestRuntimeProvider testableTestHostManager,
                int clientConnectionTimeout,
                int errorLength) : base(requestSender, testableTestHostManager, clientConnectionTimeout)
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
                int errorLength,
                IMessageLogger logger) : base(architecture, framework, processHelper, shared)
            {
                base.TimeOut = 30000;
                base.ErrorLength = errorLength;
                base.Initialize(logger);
            }

            public override TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                return new TestProcessStartInfo();
            }
        }
    }
}
