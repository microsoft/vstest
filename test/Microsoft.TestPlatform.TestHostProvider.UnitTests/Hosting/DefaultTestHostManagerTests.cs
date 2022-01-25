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

    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private readonly TestProcessStartInfo startInfo;
        private readonly Mock<IMessageLogger> mockMessageLogger;
        private readonly Mock<IProcessHelper> mockProcessHelper;
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<IDotnetHostHelper> mockDotnetHostHelper;
        private readonly Mock<IEnvironment> mockEnvironment;

        private readonly DefaultTestHostManager testHostManager;
        private TestableTestHostManager testableTestHostManager;
        private string errorMessage;
        private int exitCode;
        private int testHostId;

        public DefaultTestHostManagerTests()
        {
            mockProcessHelper = new Mock<IProcessHelper>();
            mockFileHelper = new Mock<IFileHelper>();
            mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            mockDotnetHostHelper = new Mock<IDotnetHostHelper>();
            mockEnvironment = new Mock<IEnvironment>();

            mockMessageLogger = new Mock<IMessageLogger>();

            testHostManager = new DefaultTestHostManager(mockProcessHelper.Object, mockFileHelper.Object, mockEnvironment.Object, mockDotnetHostHelper.Object);
            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X64}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default);
        }

        [TestMethod]
        public void ConstructorShouldSetX86ProcessForX86Architecture()
        {
            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X86}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");

            var info = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default);

            StringAssert.EndsWith(info.FileName, "testhost.x86.exe");
        }

        [TestMethod]
        public void ConstructorShouldSetX64ProcessForX64Architecture()
        {
            StringAssert.EndsWith(startInfo.FileName, "testhost.exe");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeFileNameFromSubFolderTestHostWhenCurrentProcessIsDotnet()
        {
            string subFoler = "TestHost";

            var startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default);

            Assert.IsTrue(startInfo.FileName.EndsWith(Path.Combine(subFoler, "testhost.exe")));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets }, RunnerProcessId = 101 };
            var info = testHostManager.GetTestHostProcessStartInfo(
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

            var info = testHostManager.GetTestHostConnectionInfo();

            Assert.AreEqual(connectionInfo, info);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEmptyEnvironmentVariables()
        {
            Assert.AreEqual(0, startInfo.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEnvironmentVariables()
        {
            var environmentVariables = new Dictionary<string, string> { { "k1", "v1" } };

            var info = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), environmentVariables, default);

            Assert.AreEqual(environmentVariables, info.EnvironmentVariables);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeCurrentWorkingDirectory()
        {
            Assert.AreEqual(Directory.GetCurrentDirectory(), startInfo.WorkingDirectory);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestSourcePathInArgumentsIfNonShared()
        {
            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X86}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{true}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets }, RunnerProcessId = 101 };
            var source = "C:\temp\a.dll";

            var info = testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                connectionInfo);

            Assert.AreEqual(" --port 123 --endpoint 127.0.0.0:123 --role client --parentprocessid 101 --testsourcepath " + source.AddDoubleQuote(), info.Arguments);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseMonoAsHostOnNonWindowsIfNotStartedWithMono()
        {
            mockProcessHelper.Setup(p => p.GetCurrentProcessFileName()).Returns("/usr/bin/dotnet");
            mockEnvironment.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            mockDotnetHostHelper.Setup(d => d.GetMonoPath()).Returns("/usr/bin/mono");
            var source = "C:\temp\a.dll";

            var info = testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                default);

            Assert.AreEqual("/usr/bin/mono", info.FileName);
            StringAssert.Contains(info.Arguments, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotUseMonoAsHostOnNonWindowsIfStartedWithMono()
        {
            mockProcessHelper.Setup(p => p.GetCurrentProcessFileName()).Returns("/usr/bin/mono");
            mockEnvironment.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            mockDotnetHostHelper.Setup(d => d.GetMonoPath()).Returns("/usr/bin/mono");
            var source = @"C:\temp\a.dll";

            var info = testHostManager.GetTestHostProcessStartInfo(
                new List<string>() { source },
                null,
                default);

            StringAssert.Contains(info.FileName, "TestHost" + Path.DirectorySeparatorChar + "testhost.exe");
            Assert.IsFalse(info.Arguments.Contains("TestHost" + Path.DirectorySeparatorChar + "testhost.exe"));
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnExtensionsListAsIsIfSourcesListIsEmpty()
        {
            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration></RunConfiguration> </RunSettings>");
            List<string> currentList = new() { @"FooExtension.dll" };

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(new List<string>(), currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnExtensionsListAsIsIfSourcesListIsNull()
        {
            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration></RunConfiguration> </RunSettings>");
            List<string> currentList = new() { @"FooExtension.dll" };

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(null, currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldNotExcludeOutputDirectoryExtensionsIfTestAdapterPathIsSet()
        {
            List<string> sourcesDir = new() { @"C:\Source1" };
            List<string> sources = new() { @"C:\Source1\source1.dll" };

            List<string> extensionsList1 = new() { @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);

            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[1])).Returns(new Version(5, 5));

            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TestAdaptersPaths>C:\\Foo</TestAdaptersPaths></RunConfiguration> </RunSettings>");
            List<string> currentList = new() { @"FooExtension.dll", @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(sources, currentList).ToList();

            // Verify
            CollectionAssert.AreEqual(currentList, resultExtensions);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestPlatformExtensionsShouldIncludeOutputDirectoryExtensionsIfTestAdapterPathIsNotSet()
        {
            List<string> sourcesDir = new() { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new() { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new() { @"C:\Source1\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            List<string> extensionsList2 = new() { @"C:\Source2\ext1.TestAdapter.dll", @"C:\Source2\ext2.TestAdapter.dll" };

            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[1])).Returns(new Version(5, 5));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 2));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[1])).Returns(new Version(5, 0));

            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            List<string> expectedList = new() { @"C:\Source2\ext1.TestAdapter.dll", @"C:\Source1\ext2.TestAdapter.dll" };
            CollectionAssert.AreEqual(expectedList, resultExtensions);
            mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, "Multiple versions of same extension found. Selecting the highest version." + Environment.NewLine + "  ext1.TestAdapter : 2.2\n  ext2.TestAdapter : 5.5"), Times.Once);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestPlatformExtensionsShouldReturnPathTheHigherVersionedFileExtensions()
        {
            List<string> sourcesDir = new() { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new() { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new() { @"C:\Source1\ext1.TestAdapter.dll" };
            List<string> extensionsList2 = new() { @"C:\Source2\ext1.TestAdapter.dll" };

            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 2));

            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            CollectionAssert.AreEqual(extensionsList2, resultExtensions);
            mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, "Multiple versions of same extension found. Selecting the highest version." + Environment.NewLine + "  ext1.TestAdapter : 2.2"), Times.Once);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestPlatformExtensionsShouldReturnPathToSingleFileExtensionOfATypeIfVersionsAreSame()
        {
            List<string> sourcesDir = new() { "C:\\Source1", "C:\\Source2" };
            List<string> sources = new() { @"C:\Source1\source1.dll", @"C:\Source2\source2.dll" };

            List<string> extensionsList1 = new() { @"C:\Source1\ext1.TestAdapter.dll" };
            List<string> extensionsList2 = new() { @"C:\Source2\ext1.TestAdapter.dll" };

            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList1[0])).Returns(new Version(2, 0));
            mockFileHelper.Setup(fh => fh.GetFileVersion(extensionsList2[0])).Returns(new Version(2, 0));

            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[0], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList1);
            mockFileHelper.Setup(fh => fh.EnumerateFiles(sourcesDir[1], SearchOption.TopDirectoryOnly, "TestAdapter.dll")).Returns(extensionsList2);

            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration><TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion></RunConfiguration> </RunSettings>");

            // Act
            var resultExtensions = testHostManager.GetTestPlatformExtensions(sources, new List<string>()).ToList();

            // Verify
            CollectionAssert.AreEqual(extensionsList1, resultExtensions);
            mockMessageLogger.Verify(ml => ml.SendMessage(TestMessageLevel.Warning, It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            mockProcessHelper.Setup(
                ph =>
                    ph.LaunchProcess(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Action<object, string>>(),
                        It.IsAny<Action<object>>(),
                         It.IsAny<Action<object, string>>())).Returns(Process.GetCurrentProcess());

            testHostManager.Initialize(mockMessageLogger.Object, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{Architecture.X64}</TargetPlatform> <TargetFrameworkVersion>{Framework.DefaultFramework}</TargetFrameworkVersion> <DisableAppDomain>{false}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            var startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default);

            testHostManager.HostLaunched += TestHostManagerHostLaunched;

            Task<bool> processId = testHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None);
            processId.Wait();

            Assert.IsTrue(processId.Result);

            Assert.AreEqual(Process.GetCurrentProcess().Id, testHostId);
        }

        [TestMethod]
        public void LaunchTestHostAsyncShouldNotStartHostProcessIfCancellationTokenIsSet()
        {
            testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                mockProcessHelper.Object,
                true,
                mockMessageLogger.Object);

            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            Assert.ThrowsException<AggregateException>(() => testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), cancellationTokenSource.Token).Wait());
        }

        [TestMethod]
        public void PropertiesShouldReturnEmptyDictionary()
        {
            Assert.AreEqual(0, testHostManager.Properties.Count);
        }

        [TestMethod]
        public void DefaultTestHostManagerShouldBeShared()
        {
            Assert.IsTrue(testHostManager.Shared);
        }

        [TestMethod]
        public void LaunchTestHostShouldUseCustomHostIfSet()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testHostManager.SetCustomLauncher(mockCustomLauncher.Object);
            var currentProcess = Process.GetCurrentProcess();
            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(currentProcess.Id);

            testHostManager.HostLaunched += TestHostManagerHostLaunched;

            Task<bool> pid = testHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None);
            pid.Wait();
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(pid.Result);
            Assert.AreEqual(currentProcess.Id, testHostId);
        }

        [TestMethod]
        public void LaunchTestHostShouldSetExitCallbackInCaseCustomHost()
        {
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            testHostManager.SetCustomLauncher(mockCustomLauncher.Object);
            var currentProcess = Process.GetCurrentProcess();
            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(currentProcess.Id);
            testHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None).Wait();

            mockProcessHelper.Verify(ph => ph.SetExitCallback(currentProcess.Id, It.IsAny<Action<object>>()));
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestSourcesShouldReturnAppropriateSourceIfAppxRecipeIsProvided()
        {
            var sourcePath = Path.Combine(Path.GetDirectoryName(typeof(TestableTestHostManager).GetTypeInfo().Assembly.GetAssemblyLocation()), @"..\..\..\..\TestAssets\UWPTestAssets\UnitTestApp8.build.appxrecipe");
            IEnumerable<string> sources = testHostManager.GetTestSources(new List<string> { sourcePath });
            Assert.IsTrue(sources.Any());
            Assert.IsTrue(sources.FirstOrDefault().EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        [TestCategory("Windows")]
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
            ErrorCallBackTestHelper(errorData, -1);

            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(errorData, errorMessage);
        }

        [TestMethod]
        public async Task NoErrorMessageIfExitCodeZero()
        {
            string errorData = string.Empty;
            ErrorCallBackTestHelper(errorData, 0);

            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);

            Assert.IsNull(errorMessage);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task ErrorReceivedCallbackShouldNotLogNullOrEmptyData(string errorData)
        {
            ErrorCallBackTestHelper(errorData, -1);

            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(errorMessage, string.Empty);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public async Task ProcessExitedButNoErrorMessageIfNoDataWritten(int exitCode)
        {
            ExitCallBackTestHelper(exitCode);

            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);

            Assert.AreEqual(errorMessage, string.Empty);
            Assert.AreEqual(this.exitCode, exitCode);
        }

        [TestMethod]
        public async Task CleanTestHostAsyncShouldKillTestHostProcess()
        {
            var pid = Process.GetCurrentProcess().Id;
            bool isVerified = false;
            mockProcessHelper.Setup(ph => ph.TerminateProcess(It.IsAny<Process>()))
                .Callback<object>(p => isVerified = ((Process)p).Id == pid);

            ExitCallBackTestHelper(0);
            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);
            await testableTestHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        [TestMethod]
        public async Task CleanTestHostAsyncShouldNotThrowIfTestHostIsNotStarted()
        {
            var pid = Process.GetCurrentProcess().Id;
            bool isVerified = false;
            mockProcessHelper.Setup(ph => ph.TerminateProcess(It.IsAny<Process>())).Callback<object>(p => isVerified = ((Process)p).Id == pid).Throws<Exception>();

            ExitCallBackTestHelper(0);
            await testableTestHostManager.LaunchTestHostAsync(GetDefaultStartInfo(), CancellationToken.None);
            await testableTestHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        private void TestableTestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            exitCode = e.ErrroCode;
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void TestHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            testHostId = e.ProcessId;
        }

        private void ErrorCallBackTestHelper(string errorMessage, int exitCode)
        {
            testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                mockProcessHelper.Object,
                true,
                mockMessageLogger.Object);

            testableTestHostManager.HostExited += TestHostManagerHostExited;

            mockProcessHelper.Setup(
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

            mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        private void ExitCallBackTestHelper(int exitCode)
        {
            testableTestHostManager = new TestableTestHostManager(
                Architecture.X64,
                Framework.DefaultFramework,
                mockProcessHelper.Object,
                true,
                mockMessageLogger.Object);

            testableTestHostManager.HostExited += TestableTestHostManagerHostExited;

            mockProcessHelper.Setup(
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

            mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
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
                Initialize(logger, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings> <RunConfiguration> <TargetPlatform>{architecture}</TargetPlatform> <TargetFrameworkVersion>{framework}</TargetFrameworkVersion> <DisableAppDomain>{!shared}</DisableAppDomain> </RunConfiguration> </RunSettings>");
            }
        }
    }
}
