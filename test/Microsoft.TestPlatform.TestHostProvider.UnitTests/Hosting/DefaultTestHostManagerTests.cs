// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestHostProvider.Hosting.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.DesktopTestHostRuntimeProvider;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

#pragma warning disable SA1600
    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private readonly TestProcessStartInfo startInfo;
        private readonly Mock<IMessageLogger> mockMessageLogger;
        private readonly Mock<IProcessHelper> mockProcessHelper;
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<IDotnetHostHelper> mockDotnetHostHelper;
        private readonly Mock<IEnvironment> mockEnvironment;

        private DefaultTestHostManager testHostManager;
        private TestableTestHostManager testableTestHostManager;
        private string errorMessage;
        private int exitCode;
        private int testHostId;

        public DefaultTestHostManagerTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            this.mockDotnetHostHelper = new Mock<IDotnetHostHelper>();
            this.mockEnvironment = new Mock<IEnvironment>();

            this.mockMessageLogger = new Mock<IMessageLogger>();

            this.testHostManager = new DefaultTestHostManager(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockEnvironment.Object, this.mockDotnetHostHelper.Object);
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
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets }, RunnerProcessId = 101 };
            var info = this.testHostManager.GetTestHostProcessStartInfo(
                Enumerable.Empty<string>(),
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --endpoint 127.0.0.0:123 --role client --parentprocessid 101", info.Arguments);
        }

        [TestMethod]
        public void GetTestHostConnectionInfoShouldIncludeEndpointRoleAndChannelType()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = "127.0.0.1:0",
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };

            var info = this.testHostManager.GetTestHostConnectionInfo();

            Assert.AreEqual(connectionInfo, info);
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
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets }, RunnerProcessId = 101 };
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
            StringAssert.Contains(info.Arguments, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotUseMonoAsHostOnNonWindowsIfStartedWithMono()
        {
            this.mockProcessHelper.Setup(p => p.GetCurrentProcessFileName()).Returns("/usr/bin/mono");
            this.mockEnvironment.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockDotnetHostHelper.Setup(d => d.GetMonoPath()).Returns("/usr/bin/mono");
            var source = @"C:\temp\a.dll";

            var info = this.testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                default(TestRunnerConnectionInfo));

            StringAssert.Contains(info.FileName, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe");
            Assert.IsFalse(info.Arguments.Contains("TestHost" + Path.DirectorySeparatorChar + "testhost.exe"));
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnExtensionsListAsIsIfSourcesListIsEmpty()
        {
            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration></RunConfiguration> </RunSettings>");
            List<string> currentList = new List<string> { @"FooExtension.dll" };

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(new List<string>(), currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnExtensionsListAsIsIfSourcesListIsNull()
        {
            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration></RunConfiguration> </RunSettings>");
            List<string> currentList = new List<string> { @"FooExtension.dll" };

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(null, currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldNotExcludeOutputDirectoryExtensionsIfTestAdapterPathIsSet()
        {
            List<string> sourcesDir = new List<string> { @"C:\Source1" };
            List<string> sources = new List<string> { @"C:\Source1\source1.dll" };

            List<string> extensionsList1 = new List<string> { @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);

            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[1])).Returns(new Version(5, 5));

            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TestAdaptersPaths>C:\\Foo</TestAdaptersPaths></RunConfiguration> </RunSettings>");
            List<string> currentList = new List<string> { @"FooExtension.dll", @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(sources, currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldIncludeOutputDirectoryExtensionsIfTestAdapterPathIsNotSet()
        {
            List<string> sourcesDir = new List<string> { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new List<string> { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new List<string> { @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            List<string> extensionsList2 = new List<string> { @"C:\Source2\ext1.TestAdapter.dll", @"C:\Source2\ext2.TestAdapter.dll" };

            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[1])).Returns(new Version(5, 5));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 2));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[1])).Returns(new Version(5, 0));

            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            List<string> expectedList = new List<string> { @"C:\Source2\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            CollectionAssert.AreEqual(expectedList, resultExtensions);
            this.mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, "Multiple versions of same extension found. Selecting the highest version." + Environment.NewLine + "  ext1.TestAdapter : 2.2\n  ext2.TestAdapter : 5.5"), Times.Once);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnPathTheHigherVersionedFileExtensions()
        {
            List<string> sourcesDir = new List<string> { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new List<string> { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new List<string> { @"C:\Source1\ext1.TestAdapter.dll" };
            List<string> extensionsList2 = new List<string> { @"C:\Source2\ext1.TestAdapter.dll" };

            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 2));

            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            CollectionAssert.AreEqual(extensionsList2, resultExtensions);
            this.mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, "Multiple versions of same extension found. Selecting the highest version." + Environment.NewLine + "  ext1.TestAdapter : 2.2"), Times.Once);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnPathToSingleFileExtensionOfATypeIfVersionsAreSame()
        {
            List<string> sourcesDir = new List<string> { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new List<string> { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new List<string> { @"C:\Source1\ext1.TestAdapter.dll" };
            List<string> extensionsList2 = new List<string> { @"C:\Source2\ext1.TestAdapter.dll" };

            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            this.mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 0));

            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            this.testHostManager.Initialize(this.mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = this.testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            CollectionAssert.AreEqual(extensionsList1, resultExtensions);
            this.mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, It.IsAny<string>()), Times.Never);
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
                        It.IsAny<Action<object>>(),
                         It.IsAny<Action<object, string>>())).Returns(Process.GetCurrentProcess());

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
            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(currentProcess.Id);

            this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;

            Task<bool> pid = this.testHostManager.LaunchTestHostAsync(this.startInfo, CancellationToken.None);
            pid.Wait();
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(pid.Result);
            Assert.AreEqual(currentProcess.Id, this.testHostId);
        }

        [TestMethod]
        public void LaunchTestHostShouldSetExitCallbackInCaseCustomHost()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.testHostManager.SetCustomLauncher(mockCustomLauncher.Object);
            var currentProcess = Process.GetCurrentProcess();
            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(currentProcess.Id);
            this.testHostManager.LaunchTestHostAsync(this.startInfo, CancellationToken.None).Wait();

            this.mockProcessHelper.Verify(ph => ph.SetExitCallback(currentProcess.Id, It.IsAny<Action<object>>()));
        }

        [TestMethod]
        public void GetTestSourcesShouldReturnAppropriateSourceIfAppxRecipeIsProvided()
        {
            var sourcePath = Path.Combine(Path.GetDirectoryName(typeof(TestableTestHostManager).GetTypeInfo().Assembly.GetAssemblyLocation()), @"..\..\..\..\TestAssets\UWPTestAssets\UnitTestApp8.build.appxrecipe");
            IEnumerable<string> sources = this.testHostManager.GetTestSources(new List<string> { sourcePath });
            Assert.IsTrue(sources.Any());
            Assert.IsTrue(sources.FirstOrDefault().EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void AppxManifestFileShouldReturnAppropriateSourceIfAppxManifestIsProvided()
        {
            var appxManifestPath = Path.Combine(Path.GetDirectoryName(typeof(TestableTestHostManager).GetTypeInfo().Assembly.GetAssemblyLocation()), @"..\..\..\..\TestAssets\UWPTestAssets\AppxManifest.xml");
            string source = AppxManifestFile.GetApplicationExecutableName(appxManifestPath);
            Assert.AreEqual("UnitTestApp8.exe", source);
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
        public async Task NoErrorMessageIfExitCodeZero()
        {
            string errorData = string.Empty;
            this.ErrorCallBackTestHelper(errorData, 0);

            await this.testableTestHostManager.LaunchTestHostAsync(this.GetDefaultStartInfo(), CancellationToken.None);

            Assert.IsNull(this.errorMessage);
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
                            It.IsAny<Action<object>>(),
                            It.IsAny<Action<object, string>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>, Action<object, string>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback, outputCallback) =>
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
                            It.IsAny<Action<object>>(),
                            It.IsAny<Action<object, string>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>, Action<object, string>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback, outputCallback) =>
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
                IMessageLogger logger)
                : base(processHelper, new FileHelper(), new PlatformEnvironment(), new DotnetHostHelper())
            {
                this.Initialize(logger, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{architecture}</TargetPlatform> <TargetFrameworkVersion>{framework}</TargetFrameworkVersion> <DisableAppDomain>{!shared}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            }
        }
    }
#pragma warning restore SA1600
}
