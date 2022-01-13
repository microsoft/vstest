// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestHostProvider.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

#pragma warning disable SA1600

    [TestClass]
    public class DotnetTestHostManagerTests
    {
        private const string DefaultDotnetPath = "c:\\tmp\\dotnet.exe";

        private readonly Mock<ITestHostLauncher> mockTestHostLauncher;

        private readonly Mock<IProcessHelper> mockProcessHelper;

        private readonly Mock<IFileHelper> mockFileHelper;

        private readonly Mock<IWindowsRegistryHelper> mockWindowsRegistry;

        private readonly Mock<IMessageLogger> mockMessageLogger;

        private readonly Mock<IEnvironment> mockEnvironment;

        private readonly Mock<IRunSettingsHelper> mockRunsettingHelper;

        private readonly TestRunnerConnectionInfo defaultConnectionInfo;

        private readonly string[] testSource = { "test.dll" };

        private readonly string defaultTestHostPath;

        private readonly TestProcessStartInfo defaultTestProcessStartInfo;

        private readonly TestableDotnetTestHostManager dotnetHostManager;

        private Mock<IEnvironmentVariableHelper> mockEnvironmentVariable;

        private string errorMessage;

        private int exitCode;

        private int testHostId;

        private string temp = Path.GetTempPath();

        public DotnetTestHostManagerTests()
        {
            this.mockTestHostLauncher = new Mock<ITestHostLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockMessageLogger = new Mock<IMessageLogger>();
            this.mockEnvironment = new Mock<IEnvironment>();
            this.mockWindowsRegistry = new Mock<IWindowsRegistryHelper>();
            this.mockEnvironmentVariable = new Mock<IEnvironmentVariableHelper>();
            this.mockRunsettingHelper = new Mock<IRunSettingsHelper>();
            this.defaultConnectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.1:123", Role = ConnectionRole.Client }, RunnerProcessId = 0 };

            this.mockEnvironment.SetupGet(e => e.Architecture).Returns((PlatformArchitecture)Enum.Parse(typeof(PlatformArchitecture), Constants.DefaultPlatform.ToString()));
            this.mockRunsettingHelper.SetupGet(r => r.IsDefaultTargetArchitecture).Returns(true);
            string defaultSourcePath = Path.Combine(this.temp, "test.dll");
            this.defaultTestHostPath = Path.Combine(this.temp, "testhost.dll");
            this.dotnetHostManager = new TestableDotnetTestHostManager(
                                         this.mockProcessHelper.Object,
                                         this.mockFileHelper.Object,
                                         new DotnetHostHelper(this.mockFileHelper.Object, this.mockEnvironment.Object, this.mockWindowsRegistry.Object, this.mockEnvironmentVariable.Object, this.mockProcessHelper.Object),
                                         this.mockEnvironment.Object,
                                         this.mockRunsettingHelper.Object,
                                         this.mockWindowsRegistry.Object,
                                         this.mockEnvironmentVariable.Object);
            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, string.Empty);

            this.dotnetHostManager.HostExited += this.DotnetHostManagerHostExited;

            // Setup a dummy current process for tests
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);
            this.mockProcessHelper.Setup(ph => ph.GetTestEngineDirectory()).Returns(DefaultDotnetPath);
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessArchitecture()).Returns(PlatformArchitecture.X64);
            this.mockEnvironmentVariable.Setup(ev => ev.GetEnvironmentVariable(It.IsAny<string>())).Returns(Path.GetDirectoryName(DefaultDotnetPath));
            this.mockFileHelper.Setup(ph => ph.Exists(this.defaultTestHostPath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists(DefaultDotnetPath)).Returns(true);

            this.mockTestHostLauncher
                .Setup(th => th.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Returns(Process.GetCurrentProcess().Id);

            this.mockTestHostLauncher
                .Setup(th => th.LaunchTestHost(It.IsAny<TestProcessStartInfo>()))
                .Returns(Process.GetCurrentProcess().Id);

            this.defaultTestProcessStartInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { defaultSourcePath }, null, this.defaultConnectionInfo);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldThrowIfSourceIsNull()
        {
            Action action = () => this.dotnetHostManager.GetTestHostProcessStartInfo(null, null, this.defaultConnectionInfo);

            Assert.ThrowsException<ArgumentNullException>(action);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldThrowIfMultipleSourcesAreProvided()
        {
            var sources = new[] { "test1.dll", "test2.dll" };
            Action action = () => this.dotnetHostManager.GetTestHostProcessStartInfo(sources, null, this.defaultConnectionInfo);

            Assert.ThrowsException<InvalidOperationException>(action);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetCommandline()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual(DefaultDotnetPath, startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetXPlatOnLinux()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("/tmp/dotnet", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetExec()
        {
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            var startInfo = this.GetDefaultStartInfo();

            StringAssert.StartsWith(startInfo.Arguments, "exec");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddRuntimeConfigJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.Arguments, "--runtimeconfig \"test.runtimeconfig.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddRuntimeConfigJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(false);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.IsFalse(startInfo.Arguments.Contains("--runtimeconfig \"test.runtimeconfig.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddDepsFileJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.Arguments, "--depsfile \"test.deps.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddDepsFileJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(false);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.IsFalse(startInfo.Arguments.Contains("--depsfile \"test.deps.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.0:123", Role = ConnectionRole.Client, Transport = Transport.Sockets }, RunnerProcessId = 101 };
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, null, connectionInfo);

            StringAssert.Contains(startInfo.Arguments, "--port " + connectionInfo.Port + " --endpoint " + connectionInfo.ConnectionInfo.Endpoint + " --role client --parentprocessid 101");
        }

        [TestMethod]
        public void GetTestHostConnectionInfoShouldIncludeEndpointRoleAndChannelType()
        {
            var connectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.1:0", Role = ConnectionRole.Client, Transport = Transport.Sockets };

            var info = this.dotnetHostManager.GetTestHostConnectionInfo();

            Assert.AreEqual(connectionInfo, info);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEnvironmentVariables()
        {
            var environmentVariables = new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } };
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, environmentVariables, this.defaultConnectionInfo);

            Assert.AreEqual(environmentVariables, startInfo.EnvironmentVariables);
        }

        [TestMethod]
        public void GetTestHostProcessStartIfDepsFileNotFoundAndTestHostFoundShouldNotThrowException()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(false);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();
            StringAssert.Contains(startInfo.Arguments, "testhost.dll");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseTestHostX64ExePresentOnWindows()
        {
            var testhostExePath = "testhost.exe";
            this.mockFileHelper.Setup(ph => ph.Exists(testhostExePath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.FileName, testhostExePath);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseDotnetExeOnUnixWithTestHostDllPath()
        {
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.x86.exe")).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Unix);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.FileName, "dotnet");
            StringAssert.Contains(startInfo.Arguments, "testhost.dll");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseTestHostExeIfPresentOnWindows()
        {
            var testhostExePath = "testhost.exe";
            this.mockFileHelper.Setup(ph => ph.Exists(testhostExePath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);

            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, "<RunSettings><RunConfiguration><TargetPlatform>x64</TargetPlatform></RunConfiguration></RunSettings>");
            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.FileName, testhostExePath);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldUseDotnetHostPathFromRunsettings()
        {
            var dotnetHostPath = @"C:\dotnet.exe";
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, $"<RunSettings><RunConfiguration><DotNetHostPath>{dotnetHostPath}</DotNetHostPath></RunConfiguration></RunSettings>");
            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.FileName, dotnetHostPath);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestHostProcessStartInfoShouldUseTestHostExeFromNugetIfNotFoundInSourceLocation()
        {
            var testhostExePath = "testhost.exe";
            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, "<RunSettings><RunConfiguration><TargetPlatform>x64</TargetPlatform></RunConfiguration></RunSettings>");
            this.mockFileHelper.Setup(ph => ph.Exists(testhostExePath)).Returns(false);
            this.mockFileHelper.Setup(ph => ph.Exists("C:\\packages\\microsoft.testplatform.testhost\\15.0.0-Dev\\build\\netcoreapp2.1\\x64\\testhost.exe")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            var sourcePath = Path.Combine(this.temp, "test.dll");

            string runtimeConfigFileContent =
@"{
    ""runtimeOptions"": {
        ""additionalProbingPaths"": [
            ""C:\\packages""
            ]
        }
}";

            string depsFileContent =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0"",
        ""signature"": ""8f25843f8e35a3e80ef4ae98b95117ea5c468b3f""
    },
    ""compilationOptions"": {},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""microsoft.testplatform.testhost/15.0.0-Dev"": {
                ""dependencies"": {
                    ""Microsoft.TestPlatform.ObjectModel"": ""15.0.0-Dev"",
                    ""Newtonsoft.Json"": ""9.0.1""
                },
                ""runtime"": {
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CommunicationUtilities.dll"": { },
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CrossPlatEngine.dll"": { },
                    ""lib/netstandard1.5/Microsoft.VisualStudio.TestPlatform.Common.dll"": { },
                    ""lib/netstandard1.5/testhost.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""microsoft.testplatform.testhost/15.0.0-Dev"": {
        ""type"": ""package"",
        ""serviceable"": true,
        ""sha512"": ""sha512-enO8sZmjbhXOfiZ6hV2ncaknaHnQbrGVsHUJzzu2Dmoh4fHFro4BF1Y4+sb4LOQhu4b3DFYPRj1ncd1RQK6HmQ=="",
        ""path"": ""microsoft.testplatform.testhost/15.0.0-Dev"",
        ""hashPath"": ""microsoft.testplatform.testhost.15.0.0-Dev""
        }
    }
}";

            MemoryStream runtimeConfigStream = new MemoryStream(Encoding.UTF8.GetBytes(runtimeConfigFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.runtimeconfig.dev.json"), FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.deps.json"), FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            string testHostFullPath = @"C:\packages\microsoft.testplatform.testhost/15.0.0-Dev\lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            StringAssert.Contains(startInfo.FileName, "C:\\packages\\microsoft.testplatform.testhost\\15.0.0-Dev\\build\\netcoreapp2.1\\x64\\testhost.exe");
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void GetTestHostProcessStartInfoShouldUseTestHostX86ExeFromNugetIfNotFoundInSourceLocation()
        {
            var testhostExePath = "testhost.x86.exe";
            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, "<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>");
            this.mockFileHelper.Setup(ph => ph.Exists(testhostExePath)).Returns(false);
            this.mockFileHelper.Setup(ph => ph.Exists($"C:\\packages{Path.DirectorySeparatorChar}microsoft.testplatform.testhost\\15.0.0-Dev{Path.DirectorySeparatorChar}build\\netcoreapp2.1\\x86\\testhost.x86.exe")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            var sourcePath = Path.Combine(this.temp, "test.dll");

            string runtimeConfigFileContent =
@"{
    ""runtimeOptions"": {
        ""additionalProbingPaths"": [
            ""C:\\packages""
            ]
        }
}";

            string depsFileContent =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0"",
        ""signature"": ""8f25843f8e35a3e80ef4ae98b95117ea5c468b3f""
    },
    ""compilationOptions"": {},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""microsoft.testplatform.testhost/15.0.0-Dev"": {
                ""dependencies"": {
                    ""Microsoft.TestPlatform.ObjectModel"": ""15.0.0-Dev"",
                    ""Newtonsoft.Json"": ""9.0.1""
                },
                ""runtime"": {
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CommunicationUtilities.dll"": { },
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CrossPlatEngine.dll"": { },
                    ""lib/netstandard1.5/Microsoft.VisualStudio.TestPlatform.Common.dll"": { },
                    ""lib/netstandard1.5/testhost.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""microsoft.testplatform.testhost/15.0.0-Dev"": {
        ""type"": ""package"",
        ""serviceable"": true,
        ""sha512"": ""sha512-enO8sZmjbhXOfiZ6hV2ncaknaHnQbrGVsHUJzzu2Dmoh4fHFro4BF1Y4+sb4LOQhu4b3DFYPRj1ncd1RQK6HmQ=="",
        ""path"": ""microsoft.testplatform.testhost/15.0.0-Dev"",
        ""hashPath"": ""microsoft.testplatform.testhost.15.0.0-Dev""
        }
    }
}";

            MemoryStream runtimeConfigStream = new MemoryStream(Encoding.UTF8.GetBytes(runtimeConfigFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.runtimeconfig.dev.json"), FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.deps.json"), FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            string testHostFullPath = $@"C:\packages{Path.DirectorySeparatorChar}microsoft.testplatform.testhost/15.0.0-Dev{Path.DirectorySeparatorChar}lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            StringAssert.Contains(startInfo.FileName, "C:\\packages\\microsoft.testplatform.testhost\\15.0.0-Dev\\build\\netcoreapp2.1\\x86\\testhost.x86.exe");
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithNullEnvironmentVariablesOrArgs()
        {
            var expectedProcessId = Process.GetCurrentProcess().Id;
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(expectedProcessId);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            var startInfo = this.GetDefaultStartInfo();
            this.dotnetHostManager.SetCustomLauncher(this.mockTestHostLauncher.Object);

            this.dotnetHostManager.HostLaunched += this.DotnetHostManagerHostLaunched;

            Task<bool> processId = this.dotnetHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None);
            processId.Wait();

            Assert.IsTrue(processId.Result);
            Assert.AreEqual(expectedProcessId, this.testHostId);
        }

        [TestMethod]
        public void LaunchTestHostAsyncShouldNotStartHostProcessIfCancellationTokenIsSet()
        {
            var expectedProcessId = Process.GetCurrentProcess().Id;
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(expectedProcessId);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            var startInfo = this.GetDefaultStartInfo();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Assert.ThrowsException<AggregateException>(() => this.dotnetHostManager.LaunchTestHostAsync(startInfo, cancellationTokenSource.Token).Wait());
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithEnvironmentVariables()
        {
            var variables = new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } };
            var startInfo = new TestProcessStartInfo { EnvironmentVariables = variables };
            this.dotnetHostManager.SetCustomLauncher(this.mockTestHostLauncher.Object);

            this.dotnetHostManager.HostLaunched += this.DotnetHostManagerHostLaunched;

            Task<bool> processId = this.dotnetHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None);
            processId.Wait();

            Assert.IsTrue(processId.Result);
            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.EnvironmentVariables.Equals(variables)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void DotnetTestHostManagerShouldNotBeShared()
        {
            Assert.IsFalse(this.dotnetHostManager.Shared);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldThrowExceptionWhenDotnetIsNotInstalled()
        {
            // To validate the else part, set current process to exe other than dotnet
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");

            // Reset the muxer
            this.mockEnvironmentVariable.Setup(ev => ev.GetEnvironmentVariable(It.IsAny<string>())).Returns(string.Empty);

            char separator = ';';
            var dotnetExeName = "dotnet.exe";
            if (!System.Environment.OSVersion.Platform.ToString().StartsWith("Win"))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }

            var paths = Environment.GetEnvironmentVariable("PATH").Split(separator);

            foreach (string path in paths)
            {
                string dotnetExeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                this.mockFileHelper.Setup(fh => fh.Exists(dotnetExeFullPath)).Returns(false);
            }

            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            Action action = () => this.GetDefaultStartInfo();

            Assert.ThrowsException<TestPlatformException>(action);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeSourceDirectoryAsWorkingDirectory()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "testhost.dll"))).Returns(true);
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.AreEqual(Path.GetDirectoryName(this.temp), startInfo.WorkingDirectory);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnEmptySetIfSourceDirectoryDoesNotExist()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(new[] { $".{Path.DirectorySeparatorChar}foo.dll" }, Enumerable.Empty<string>());

            Assert.AreEqual(0, extensions.Count());
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnLibariesFromSourceDirectory()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(new[] { "foo.TestAdapter.dll" });
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(new[] { $".{Path.DirectorySeparatorChar}foo.dll" }, Enumerable.Empty<string>());

            CollectionAssert.AreEqual(new[] { "foo.TestAdapter.dll" }, extensions.ToArray());
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnEmptySetIfSourceDirectoryIsEmpty()
        {
            // Parent directory is empty since the input source is file "test.dll"
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(new[] { "foo.dll" });
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(this.testSource, Enumerable.Empty<string>());

            Assert.AreEqual(0, extensions.Count());
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldNotAddNonCoverletDataCollectorsExtensionsIfPresent()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), SearchOption.TopDirectoryOnly, It.IsAny<string[]>())).Returns(new[] { "foo.dll" });
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(this.testSource, new List<string> { "abc.dataollector.dll" });

            Assert.AreEqual(0, extensions.Count());
        }

        [TestMethod]
        public async Task LaunchTestHostShouldLaunchProcessWithConnectionInfo()
        {
            var expectedArgs = "exec \"" + this.defaultTestHostPath + "\" --port 123 --endpoint 127.0.0.1:123 --role client --parentprocessid 0";
            this.dotnetHostManager.SetCustomLauncher(this.mockTestHostLauncher.Object);
            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.Arguments.Equals(expectedArgs)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void LaunchTestHostShouldSetExitCallBackInCaseCustomHost()
        {
            var expectedProcessId = Process.GetCurrentProcess().Id;
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(expectedProcessId);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();
            this.dotnetHostManager.SetCustomLauncher(this.mockTestHostLauncher.Object);
            this.dotnetHostManager.LaunchTestHostAsync(startInfo, CancellationToken.None).Wait();

            this.mockProcessHelper.Verify(ph => ph.SetExitCallback(expectedProcessId, It.IsAny<Action<object>>()));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromSourceDirectoryIfDepsFileNotFound()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");
            string expectedTestHostPath = Path.Combine(this.temp, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            StringAssert.Contains(startInfo.Arguments, expectedTestHostPath);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathNextToTestRunnerIfTesthostDllIsNoFoundAndDepsFileNotFound()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");
            string testhostNextToTestDll = Path.Combine(this.temp, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(testhostNextToTestDll)).Returns(false);

            var here = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var expectedTestHostPath = Path.Combine(here, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            StringAssert.Contains(startInfo.Arguments, expectedTestHostPath);
            var expectedAdditionalDepsPath = Path.Combine(here, "testhost.deps.json");
            StringAssert.Contains(startInfo.Arguments, $"--additional-deps \"{expectedAdditionalDepsPath}\"");
            var expectedAdditionalProbingPath = here;
            StringAssert.Contains(startInfo.Arguments, $"--additionalprobingpath \"{expectedAdditionalProbingPath}\"");
            var expectedRuntimeConfigPath = Path.Combine(here, "testhost-latest.runtimeconfig.json");
            StringAssert.Contains(startInfo.Arguments, $"--runtimeconfig \"{expectedRuntimeConfigPath}\"");
        }

        [TestMethod]

        // we can't put in a "default" value, and we don't have other way to determine if this provided value is the
        // runtime default or the actual value that user provided, so right now the default will use the latest, instead
        // or the more correct 1.0, it should be okay, as that version is not supported anymore anyway
        [DataRow("netcoreapp1.0", "latest")]
        [DataRow("netcoreapp2.1", "2.1")]
        [DataRow("netcoreapp3.1", "3.1")]
        [DataRow("net5.0", "5.0")]

        // net6.0 is currently the latest released version, but it still has it's own runtime config, it is not the same as
        // "latest" which means the latest you have on system. So if you have only 5.0 SDK then net6.0 will fail because it can't find net6.0,
        // but latest would use net5.0 because that is the latest one on your system.
        [DataRow("net6.0", "6.0")]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathNextToTestRunnerIfTesthostDllIsNoFoundAndDepsFileNotFoundWithTheCorrectTfm(string tfm, string suffix)
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");
            string testhostNextToTestDll = Path.Combine(this.temp, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(testhostNextToTestDll)).Returns(false);

            var here = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var testhostNextToRunner = Path.Combine(here, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(testhostNextToRunner)).Returns(true);

            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, $"<RunSettings><RunConfiguration><TargetFrameworkVersion>{tfm}</TargetFrameworkVersion></RunConfiguration></RunSettings>");
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            var expectedRuntimeConfigPath = Path.Combine(here, $"testhost-{suffix}.runtimeconfig.json");
            StringAssert.Contains(startInfo.Arguments, $"--runtimeconfig \"{expectedRuntimeConfigPath}\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromSourceDirectoryIfRunConfigDevFileNotFound()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");
            string expectedTestHostPath = Path.Combine(this.temp, "testhost.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(expectedTestHostPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromDepsFile()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");

            string runtimeConfigFileContent =
@"{
    ""runtimeOptions"": {
        ""additionalProbingPaths"": [
            ""C:\\packages""
            ]
        }
}";

            string depsFileContent =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0"",
        ""signature"": ""8f25843f8e35a3e80ef4ae98b95117ea5c468b3f""
    },
    ""compilationOptions"": {},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""microsoft.testplatform.testhost/15.0.0-Dev"": {
                ""dependencies"": {
                    ""Microsoft.TestPlatform.ObjectModel"": ""15.0.0-Dev"",
                    ""Newtonsoft.Json"": ""9.0.1""
                },
                ""runtime"": {
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CommunicationUtilities.dll"": { },
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CrossPlatEngine.dll"": { },
                    ""lib/netstandard1.5/Microsoft.VisualStudio.TestPlatform.Common.dll"": { },
                    ""lib/netstandard1.5/testhost.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""microsoft.testplatform.testhost/15.0.0-Dev"": {
        ""type"": ""package"",
        ""serviceable"": true,
        ""sha512"": ""sha512-enO8sZmjbhXOfiZ6hV2ncaknaHnQbrGVsHUJzzu2Dmoh4fHFro4BF1Y4+sb4LOQhu4b3DFYPRj1ncd1RQK6HmQ=="",
        ""path"": ""microsoft.testplatform.testhost/15.0.0-Dev"",
        ""hashPath"": ""microsoft.testplatform.testhost.15.0.0-Dev""
        }
    }
}";

            MemoryStream runtimeConfigStream = new MemoryStream(Encoding.UTF8.GetBytes(runtimeConfigFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.runtimeconfig.dev.json"), FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.deps.json"), FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            string testHostFullPath = $@"C:\packages{Path.DirectorySeparatorChar}microsoft.testplatform.testhost/15.0.0-Dev{Path.DirectorySeparatorChar}lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostFullPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromSourceDirectoryIfNugetpathDoesntExist()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");

            string runtimeConfigFileContent =
@"{
    ""runtimeOptions"": {
        ""additionalProbingPaths"": [
            ""C:\\packages""
            ]
        }
}";

            string depsFileContent =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0"",
        ""signature"": ""8f25843f8e35a3e80ef4ae98b95117ea5c468b3f""
    },
    ""compilationOptions"": {},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""microsoft.testplatform.testhost/15.0.0-Dev"": {
                ""dependencies"": {
                    ""Microsoft.TestPlatform.ObjectModel"": ""15.0.0-Dev"",
                    ""Newtonsoft.Json"": ""9.0.1""
                },
                ""runtime"": {
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CommunicationUtilities.dll"": { },
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CrossPlatEngine.dll"": { },
                    ""lib/netstandard1.5/Microsoft.VisualStudio.TestPlatform.Common.dll"": { },
                    ""lib/netstandard1.5/testhost.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""microsoft.testplatform.testhost/15.0.0-Dev"": {
        ""type"": ""package"",
        ""serviceable"": true,
        ""sha512"": ""sha512-enO8sZmjbhXOfiZ6hV2ncaknaHnQbrGVsHUJzzu2Dmoh4fHFro4BF1Y4+sb4LOQhu4b3DFYPRj1ncd1RQK6HmQ=="",
        ""path"": ""microsoft.testplatform.testhost/15.0.0-Dev"",
        ""hashPath"": ""microsoft.testplatform.testhost.15.0.0-Dev""
        }
    }
}";

            MemoryStream runtimeConfigStream = new MemoryStream(Encoding.UTF8.GetBytes(runtimeConfigFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.runtimeconfig.dev.json"), FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.deps.json"), FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            string testHostFullPath = $@"C:\packages{Path.DirectorySeparatorChar}microsoft.testplatform.testhost/15.0.0-Dev{Path.DirectorySeparatorChar}lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(false);

            string testHostPath = Path.Combine(this.temp, "testhost.dll");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldSkipInvalidAdditionalProbingPaths()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine(this.temp, "test.dll");

            string runtimeConfigFileContent =
@"{
    ""runtimeOptions"": {
        ""additionalProbingPaths"": [
            ""C:\\Users\\bob\\.dotnet\\store\\|arch|\\|tfm|"",
            ""C:\\packages""
            ]
        }
}";

            string depsFileContent =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0"",
        ""signature"": ""8f25843f8e35a3e80ef4ae98b95117ea5c468b3f""
    },
    ""compilationOptions"": {},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""microsoft.testplatform.testhost/15.0.0-Dev"": {
                ""dependencies"": {
                    ""Microsoft.TestPlatform.ObjectModel"": ""15.0.0-Dev"",
                    ""Newtonsoft.Json"": ""9.0.1""
                },
                ""runtime"": {
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CommunicationUtilities.dll"": { },
                    ""lib/netstandard1.5/Microsoft.TestPlatform.CrossPlatEngine.dll"": { },
                    ""lib/netstandard1.5/Microsoft.VisualStudio.TestPlatform.Common.dll"": { },
                    ""lib/netstandard1.5/testhost.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""microsoft.testplatform.testhost/15.0.0-Dev"": {
        ""type"": ""package"",
        ""serviceable"": true,
        ""sha512"": ""sha512-enO8sZmjbhXOfiZ6hV2ncaknaHnQbrGVsHUJzzu2Dmoh4fHFro4BF1Y4+sb4LOQhu4b3DFYPRj1ncd1RQK6HmQ=="",
        ""path"": ""microsoft.testplatform.testhost/15.0.0-Dev"",
        ""hashPath"": ""microsoft.testplatform.testhost.15.0.0-Dev""
        }
    }
}";

            MemoryStream runtimeConfigStream = new MemoryStream(Encoding.UTF8.GetBytes(runtimeConfigFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.runtimeconfig.dev.json"), FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.runtimeconfig.dev.json"))).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream(Path.Combine(this.temp, "test.deps.json"), FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists(Path.Combine(this.temp, "test.deps.json"))).Returns(true);

            string testHostFullPath = $@"C:\packages{Path.DirectorySeparatorChar}microsoft.testplatform.testhost/15.0.0-Dev{Path.DirectorySeparatorChar}lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostFullPath));
        }

        [TestMethod]
        [DataRow("DOTNET_ROOT(x86)", "x86")]
        [DataRow("DOTNET_ROOT", "x64")]
        [DataRow("DOTNET_ROOT_WRONG", "")]
        [TestCategory("Windows")]
        public void GetTestHostProcessStartInfoShouldForwardDOTNET_ROOTEnvVarsForAppHost(string envVar, string expectedValue)
        {
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.exe")).Returns(true);
            this.mockEnvironment.Setup(ev => ev.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.mockEnvironmentVariable.Reset();
            this.mockEnvironmentVariable.Setup(x => x.GetEnvironmentVariable($"VSTEST_WINAPPHOST_{envVar}")).Returns(expectedValue);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, null, this.defaultConnectionInfo);
            if (!string.IsNullOrEmpty(expectedValue))
            {
                Assert.AreEqual(1, startInfo.EnvironmentVariables.Count);
                Assert.IsNotNull(startInfo.EnvironmentVariables[envVar]);
                Assert.AreEqual(startInfo.EnvironmentVariables[envVar], expectedValue);
            }
            else
            {
                Assert.AreEqual(0, startInfo.EnvironmentVariables.Count);
            }
        }

        [TestMethod]
        public async Task DotNetCoreErrorMessageShouldBeReadAsynchronouslyAsync()
        {
            var errorData = "Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            Assert.AreEqual(errorData, this.errorMessage);
        }

        [TestMethod]
        public async Task DotNetCoreNoErrorMessageIfExitCodeZero()
        {
            string errorData = string.Empty;
            this.ErrorCallBackTestHelper(errorData, 0);

            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            Assert.IsNull(this.errorMessage);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task DotNetCoreErrorReceivedCallbackShouldNotLogNullOrEmptyData(string errorData)
        {
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public async Task DotNetCoreProcessExitedButNoErrorMessageIfNoDataWritten(int exitCode)
        {
            var errorData = string.Empty;
            this.ExitCallBackTestHelper(exitCode);

            // override event listener
            this.dotnetHostManager.HostExited += this.DotnetHostManagerExitCodeTesterHostExited;

            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

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
            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            await this.dotnetHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        [TestMethod]
        public async Task CleanTestHostAsyncShouldNotThrowIfTestHostIsNotStarted()
        {
            var pid = Process.GetCurrentProcess().Id;
            bool isVerified = false;
            this.mockProcessHelper.Setup(ph => ph.TerminateProcess(It.IsAny<Process>())).Callback<object>(p => isVerified = ((Process)p).Id == pid).Throws<Exception>();

            this.ExitCallBackTestHelper(0);
            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            await this.dotnetHostManager.CleanTestHostAsync(CancellationToken.None);

            Assert.IsTrue(isVerified);
        }

        private void DotnetHostManagerExitCodeTesterHostExited(object sender, HostProviderEventArgs e)
        {
            this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            this.exitCode = e.ErrroCode;
        }

        private void DotnetHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void DotnetHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            this.testHostId = e.ProcessId;
        }

        private void ErrorCallBackTestHelper(string errorMessage, int exitCode)
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
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(
                this.testSource,
                null,
                this.defaultConnectionInfo);
            return startInfo;
        }

        internal class TestableDotnetTestHostManager : DotnetTestHostManager
        {
            public TestableDotnetTestHostManager(
                IProcessHelper processHelper,
                IFileHelper fileHelper,
                IDotnetHostHelper dotnetTestHostHelper,
                IEnvironment environment,
                IRunSettingsHelper runsettingsHelper,
                IWindowsRegistryHelper windowsRegistryHelper,
                IEnvironmentVariableHelper environmentVariableHelper)
                : base(processHelper, fileHelper, dotnetTestHostHelper, environment, runsettingsHelper, windowsRegistryHelper, environmentVariableHelper)
            {
            }
        }
    }
#pragma warning restore SA1600
}
