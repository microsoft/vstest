// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestHostProvider.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
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

        private readonly Mock<IMessageLogger> mockMessageLogger;

        private readonly TestRunnerConnectionInfo defaultConnectionInfo;

        private readonly string[] testSource = { "test.dll" };

        private readonly string defaultTestHostPath;
        private readonly TestProcessStartInfo defaultTestProcessStartInfo;

        private readonly TestableDotnetTestHostManager dotnetHostManager;

        private string errorMessage;
        private int maxStdErrStringLength = 22;

        private int exitCode;

        private int testHostId;

        public DotnetTestHostManagerTests()
        {
            this.mockTestHostLauncher = new Mock<ITestHostLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockMessageLogger = new Mock<IMessageLogger>();
            var mockEnvironment = new Mock<IEnvironment>();
            this.defaultConnectionInfo = new TestRunnerConnectionInfo { Port = 123, ConnectionInfo = new TestHostConnectionInfo { Endpoint = "127.0.0.1:123", Role = ConnectionRole.Client }, RunnerProcessId = 0 };

            string defaultSourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            this.defaultTestHostPath = @"\tmp\testhost.dll";
            this.dotnetHostManager = new TestableDotnetTestHostManager(
                                         this.mockProcessHelper.Object,
                                         this.mockFileHelper.Object,
                                         new DotnetHostHelper(this.mockFileHelper.Object, mockEnvironment.Object),
                                         this.maxStdErrStringLength);
            this.dotnetHostManager.Initialize(this.mockMessageLogger.Object, string.Empty);

            this.dotnetHostManager.HostExited += this.DotnetHostManagerHostExited;

            // Setup a dummy current process for tests
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);
            this.mockProcessHelper.Setup(ph => ph.GetTestEngineDirectory()).Returns(DefaultDotnetPath);
            this.mockFileHelper.Setup(ph => ph.Exists(this.defaultTestHostPath)).Returns(true);

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

            StringAssert.Contains(startInfo.Arguments, "--port " + connectionInfo.Port + " --endpoint " + connectionInfo.ConnectionInfo.Endpoint + " --role client" + " --parentprocessid 101");
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
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(expectedProcessId);
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
        public void GetTestHostProcessStartInfoOnWindowsForValidPathReturnsFullPathOfDotnetHost()
        {
            // To validate the else part, set current process to exe other than dotnet
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");

            char separator = ';';
            var dotnetExeName = "dotnet.exe";
#if !NET451
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }
#endif

            // Setup the first directory on PATH to return true for existence check for dotnet
            var paths = Environment.GetEnvironmentVariable("PATH").Split(separator);
            var acceptablePath = Path.Combine(paths[0], dotnetExeName);
            this.mockFileHelper.Setup(fh => fh.Exists(acceptablePath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            // The full path should be wrapped in quotes (in case it may contain whitespace)
            Assert.AreEqual(acceptablePath, startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldThrowExceptionWhenDotnetIsNotInstalled()
        {
            // To validate the else part, set current process to exe other than dotnet
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");

            char separator = ';';
            var dotnetExeName = "dotnet.exe";
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

            Assert.ThrowsException<FileNotFoundException>(action);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeSourceDirectoryAsWorkingDirectory()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(@"\tmp\testhost.dll")).Returns(true);
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.AreEqual($"{Path.DirectorySeparatorChar}tmp", startInfo.WorkingDirectory);
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
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            string expectedTestHostPath = @"\tmp\testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.runtimeconfig.dev.json")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(expectedTestHostPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromSourceDirectoryIfRunConfigDevFileNotFound()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            string expectedTestHostPath = @"\tmp\testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.deps.json")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(expectedTestHostPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromDepsFile()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");

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
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.runtimeconfig.dev.json", FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.runtimeconfig.dev.json")).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.deps.json", FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.deps.json")).Returns(true);

            string testHostFullPath = @"C:\packages\microsoft.testplatform.testhost/15.0.0-Dev\lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostFullPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeTestHostPathFromSourceDirectoryIfNugetpathDoesntExist()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");

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
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.runtimeconfig.dev.json", FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.runtimeconfig.dev.json")).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.deps.json", FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.deps.json")).Returns(true);

            string testHostFullPath = @"C:\packages\microsoft.testplatform.testhost/15.0.0-Dev\lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(false);

            string testHostPath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "testhost.dll");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostPath));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldSkipInvalidAdditionalProbingPaths()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");

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
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.runtimeconfig.dev.json", FileMode.Open, FileAccess.Read)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.runtimeconfig.dev.json")).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.deps.json", FileMode.Open, FileAccess.Read)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.deps.json")).Returns(true);

            string testHostFullPath = @"C:\packages\microsoft.testplatform.testhost/15.0.0-Dev\lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostFullPath));
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
        public async Task DotNetCoreErrorMessageShouldBeTruncatedToMatchErrorLength()
        {
            var errorData = "Long Custom Error Strings";
            this.ErrorCallBackTestHelper(errorData, -1);

            await this.dotnetHostManager.LaunchTestHostAsync(this.defaultTestProcessStartInfo, CancellationToken.None);

            // Ignore new line chars
            Assert.AreEqual(this.maxStdErrStringLength - Environment.NewLine.Length, this.errorMessage.Length);
            Assert.AreEqual(errorData.Substring(5), this.errorMessage);
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

            // override event listner
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
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(
                this.testSource,
                null,
                this.defaultConnectionInfo);
            return startInfo;
        }

        internal class TestableDotnetTestHostManager : DotnetTestHostManager
        {
            public TestableDotnetTestHostManager(IProcessHelper processHelper, IFileHelper fileHelper, IDotnetHostHelper dotnetTestHostHelper, int errorLength)
                : base(processHelper, fileHelper, dotnetTestHostHelper)
            {
                this.ErrorLength = errorLength;
            }
        }
    }
#pragma warning restore SA1600
}
