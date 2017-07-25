// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestHostProvider.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.TestRunnerConnectionInfo;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

#pragma warning disable SA1600
    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private readonly TestProcessStartInfo startInfo;
        private readonly Mock<IMessageLogger> mockMessageLogger;
        private readonly Mock<IProcessHelper> mockProcessHelper;
        private readonly Mock<IDotnetHostHelper> mockDotnetHostHelper;
        private readonly Mock<IEnvironment> mockEnvironment;

        private DefaultTestHostManager testHostManager;
        private TestableTestHostManager testableTestHostManager;
        private int maxStdErrStringLength = 22;
        private string errorMessage;
        private int exitCode;
        private int testHostId;

        public DefaultTestHostManagerTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            this.mockDotnetHostHelper = new Mock<IDotnetHostHelper>();
            this.mockEnvironment = new Mock<IEnvironment>();

            this.mockMessageLogger = new Mock<IMessageLogger>();

            this.testHostManager = new DefaultTestHostManager(this.mockProcessHelper.Object, this.mockEnvironment.Object, this.mockDotnetHostHelper.Object);
            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X64}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            this.startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));
        }

        [TestMethod]
        public void ConstructorShouldSetX86ProcessForX86Architecture()
        {
            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X86}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");

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

            var startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            Assert.IsTrue(startInfo.FileName.EndsWith(Path.Combine(subFoler, "testhost.exe")));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new ConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Channel = TransportChannel.Sockets }, RunnerProcessId = 101 };
            var info = this.testHostManager.GetTestHostProcessStartInfo(
                Enumerable.Empty<string>(),
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --endpoint 127.0.0.0:123 --role client --parentprocessid 101", info.Arguments);
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
            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X86}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{true}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new ConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Channel = TransportChannel.Sockets }, RunnerProcessId = 101 };
            var source = "C:\temp\a.dll";

            var info = this.testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --endpoint 127.0.0.0:123 --role client --parentprocessid 101 --testsourcepath " + source.AddDoubleQuote(), info.Arguments);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseMonoAsHostOnNonWindowsIfNotStartedWithMono()
        {
            this.mockProcessHelper.Setup(p => p.GetCurrentProcessFileName()).Returns("/usr/bin/dotnet");
            this.mockEnvironment.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockDotnetHostHelper.Setup(d => d.GetMonoPath()).Returns("/usr/bin/mono");
            var source = "C:\temp\a.dll";

            var info = this.testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                default(TestRunnerConnectionInfo));

            Assert.AreEqual("/usr/bin/mono", info.FileName);
            StringAssert.Contains(info.Arguments, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotUseMonoAsHostOnNonWindowsIfStartedWithMono()
        {
            this.mockProcessHelper.Setup(p => p.GetCurrentProcessFileName()).Returns("/usr/bin/mono");
            this.mockEnvironment.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockDotnetHostHelper.Setup(d => d.GetMonoPath()).Returns("/usr/bin/mono");
            var source = "C:\temp\a.dll";

            var info = this.testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                default(TestRunnerConnectionInfo));

            StringAssert.Contains(info.FileName, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe");
            Assert.IsFalse(info.Arguments.Contains("TestHost" + Path.DirectorySeparatorChar + "testhost.exe"));
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
                        It.IsAny<Action<object, string>>(),
                        It.IsAny<Action<object>>())).Returns(Process.GetCurrentProcess());

            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X64}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            var startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;

            Task<bool> processId = this.testHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None);
            processId.Wait();

            Assert.IsTrue(processId.Result);

            Assert.AreEqual(Process.GetCurrentProcess().Id, this.testHostId);
        }

        [TestMethod]
        public void LaunchTestHostAsyncShouldNotStartHostProcessIfCancellationTokenIsSet()
        {
            this.testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                this.mockProcessHelper.Object,
                true,
                this.maxStdErrStringLength,
                this.mockMessageLogger.Object);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Assert.ThrowsException<System.AggregateException>(() => this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), cancellationTokenSource.Token).Wait());
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

            this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;

            Task<bool> pid = this.testHostManager.LaunchTestHostAsync(this.startInfo, CancellationToken.None);
            pid.Wait();
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);

            Assert.IsTrue(pid.Result);
            Assert.AreEqual(currentProcess.Id, this.testHostId);
        }

        [TestMethod]
        public async Task ErrorMessageShouldBeReadAsynchronously()
        {
            string errorData = "Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(errorData, this.errorMessage);
        }

        [TestMethod]
        public async Task ErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            string errorData = "Long Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            // Ignore new line chars
            Assert.AreEqual(this.maxStdErrStringLength - Environment.NewLine.Length, this.errorMessage.Length);
            Assert.AreEqual(errorData.Substring(5), this.errorMessage);
        }

        [TestMethod]
        public async Task NoErrorMessageIfExitCodeZero()
        {
            string errorData = string.Empty;
            this.ErrorCallBackTestHelper(errorData, 0);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(null, this.errorMessage);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task ErrorReceivedCallbackShouldNotLogNullOrEmptyData(string errorData)
        {
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public async Task ProcessExitedButNoErrorMessageIfNoDataWritten(int exitCode)
        {
            this.ExitCallBackTestHelper(exitCode);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(this.errorMessage, string.Empty);
            Assert.AreEqual(this.exitCode, exitCode);
        }

        [TestMethod]
        public async Task CleanTestHostAsyncShouldKillTestHostProcess()
        {
            var pid = Process.GetCurrentProcess().Id;
            bool isVerified = false;
            this.mockProcessHelper.Setup(ph => ph.TerminateProcess(It.IsAny<Process>()))
                .Callback<object>(p => isVerified = ((Process)p).Id == pid);

            this.ExitCallBackTestHelper(0);
            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);
            await this.testableTestHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        [TestMethod]
        public async Task CleanTestHostAsyncShouldNotThrowIfTestHostIsNotStarted()
        {
            var pid = Process.GetCurrentProcess().Id;
            bool isVerified = false;
            this.mockProcessHelper.Setup(ph => ph.TerminateProcess(It.IsAny<Process>())).Callback<object>(p => isVerified = ((Process)p).Id == pid).Throws<Exception>();

            this.ExitCallBackTestHelper(0);
            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);
            await this.testableTestHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        private void TestableTestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            this.exitCode = e.ErrroCode;
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void TestHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            this.testHostId = e.ProcessId;
        }

        private void ErrorCallBackTestHelper(string errorMessage, int exitCode)
        {
            this.testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                this.mockProcessHelper.Object,
                true,
                this.maxStdErrStringLength,
                this.mockMessageLogger.Object);

            this.testableTestHostManager.HostExited += this.TestHostManagerHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();

                        errorCallback(process, errorMessage);
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        private void ExitCallBackTestHelper(int exitCode)
        {
            this.testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                this.mockProcessHelper.Object,
                true,
                this.maxStdErrStringLength,
                this.mockMessageLogger.Object);

            this.testableTestHostManager.HostExited += this.TestableTestHostManagerHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
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
                IMessageLogger logger)
                : base(processHelper, new PlatformEnvironment(), new DotnetHostHelper())
            {
                this.ErrorLength = errorLength;
                this.Initialize(logger, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{architecture}</TargetPlatform> <TargetFrameworkVersion>{framework}</TargetFrameworkVersion> <DisableAppDomain>{!shared}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            }
        }
    }
#pragma warning restore SA1600
}
