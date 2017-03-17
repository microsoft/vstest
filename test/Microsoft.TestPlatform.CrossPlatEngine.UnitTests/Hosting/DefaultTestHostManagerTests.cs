// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private readonly TestProcessStartInfo startInfo;        
        private readonly Mock<ITestRequestSender> mockRequestSender;
        private readonly Mock<IMessageLogger> mockMessageLogger;
        private readonly Mock<IProcessHelper> mockProcessHelper;

        private DefaultTestHostManager testHostManager;
        private TestableTestHostManager testableTestHostManager;
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
            this.testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                this.mockProcessHelper.Object,
                true,
                this.errorLength,
                this.mockMessageLogger.Object);

            this.testableTestHostManager.HostExited += this.TestHostManagerHostExited;
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(123);

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<Process, string>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<Process, string>>(
                    (var1, var2, var3, dictionary, errorCallback) =>
                        {
                    var process = Process.GetCurrentProcess();

                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                    errorCallback(process, errorMessage);
                }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<Process>(), out exitCode)).Returns(true);
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
            this.mockProcessHelper.Setup(
                ph =>
                    ph.LaunchProcess(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Action<Process, string>>())).Returns(Process.GetCurrentProcess());

            var testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, true);
            var startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            Task<int> processId = testHostManager.LaunchTestHostAsync(startInfo);
            processId.Wait();

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
            pid.Wait();
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            Assert.AreEqual(currentProcess.Id, pid.Result);
        }

        [TestMethod]
        public async Task ErrorMessageShouldBeReadAsynchronously()
        {
            string errorData = "Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(this.errorMessage, errorData);
        }

        [TestMethod]
        public async Task ErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            string errorData = "Long Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(this.errorMessage.Length, this.errorLength);
            Assert.AreEqual(this.errorMessage, errorData.Substring(5));
        }

        [TestMethod]
        public async Task ErrorMessageShouldBeTruncatedFromBeginingShouldDisplayTrailingData()
        {
            string errorData = "Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(this.errorMessage, "StringsError Strings");
        }

        [TestMethod]
        public async Task NoErrorMessageIfExitCodeZero()
        {
            string errorData = string.Empty;
            this.ErrorCallBackTestHelper(errorData, 0);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(null, this.errorMessage);
        }

        [TestMethod]
        public async Task NoErrorMessageIfDataIsNullAsync()
        {
            this.ErrorCallBackTestHelper(null, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        [TestMethod]
        public async Task NoErrorMessageIfDataIsEmpty()
        {
            string errorData = string.Empty;
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                this.errorMessage = e.Data;
            }
        }

        private TestProcessStartInfo GetDefaultStartInfo()
        {
            return new TestProcessStartInfo();
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
        }
    }
}
