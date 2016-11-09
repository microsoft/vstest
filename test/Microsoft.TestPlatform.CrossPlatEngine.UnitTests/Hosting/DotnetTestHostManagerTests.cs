// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DotnetTestHostManagerTests
    {
        private const string DefaultDotnetPath = "c:\\tmp\\dotnet.exe";

        private readonly Mock<ITestHostLauncher> mockTestHostLauncher;

        private readonly TestableDotnetTestHostManager dotnetHostManager;

        private readonly Mock<IProcessHelper> mockProcessHelper;

        private readonly Mock<IFileHelper> mockFileHelper;

        private readonly TestRunnerConnectionInfo defaultConnectionInfo;

        private readonly string[] testSource = { "test.dll" };

        public DotnetTestHostManagerTests()
        {
            this.mockTestHostLauncher = new Mock<ITestHostLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.defaultConnectionInfo = default(TestRunnerConnectionInfo);
            this.dotnetHostManager = new TestableDotnetTestHostManager(
                                         this.mockTestHostLauncher.Object,
                                         this.mockProcessHelper.Object,
                                         this.mockFileHelper.Object);

            // Setup a dummy current process for tests
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);
            this.mockProcessHelper.Setup(ph => ph.GetTestEngineDirectory()).Returns(DefaultDotnetPath);
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

            Assert.AreEqual("\"" + DefaultDotnetPath + "\"", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetXPlatOnLinux()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("\"/tmp/dotnet\"", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetOnWindows()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("c:\\tmp\\vstest.console.exe");
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("\"dotnet.exe\"", startInfo.FileName);
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
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123, RunnerProcessId = 101 };
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, null, connectionInfo);

            StringAssert.Contains(startInfo.Arguments, "--port " + connectionInfo.Port + " --parentprocessid 101");
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
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(expectedProcessId);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            var startInfo = this.GetDefaultStartInfo();

            var processId = this.dotnetHostManager.LaunchTestHost(startInfo);

            Assert.AreEqual(expectedProcessId, processId);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithEnvironmentVariables()
        {
            var variables = new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } };
            var startInfo = new TestProcessStartInfo { EnvironmentVariables = variables };

            this.dotnetHostManager.LaunchTestHost(startInfo);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.EnvironmentVariables.Equals(variables))), Times.Once);
        }

        [TestMethod]
        public void DotnetTestHostManagedShouldNotBeShared()
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
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }

            // Setup the first directory on PATH to return true for existence check for dotnet
            var paths = Environment.GetEnvironmentVariable("PATH").Split(separator);
            var acceptablePath = Path.Combine(paths[0], dotnetExeName);
            this.mockFileHelper.Setup(fh => fh.Exists(acceptablePath)).Returns(true);
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            // The full path should be wrapped in quotes (in case it may contain whitespace)
            Assert.AreEqual("\"" + acceptablePath + "\"", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoOnWindowsForInvalidPathReturnsDotnet()
        {
            // To validate the else part, set current process to exe other than dotnet
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            this.mockFileHelper.Setup(ph => ph.Exists("testhost.dll")).Returns(true);
            var dotnetExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("\"" + dotnetExeName + "\"", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeCurrentDirectoryAsWorkingDirectory()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            this.mockFileHelper.Setup(ph => ph.Exists(@"\tmp\testhost.dll")).Returns(true);
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.AreEqual(Directory.GetCurrentDirectory(), startInfo.WorkingDirectory);
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnEmptySetIfSourceDirectoryDoesNotExist()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(new[] { $".{Path.DirectorySeparatorChar}foo.dll" });

            Assert.AreEqual(0, extensions.Count());
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnLibariesFromSourceDirectory()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.TopDirectoryOnly)).Returns(new[] { "foo.TestAdapter.dll" });
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(new[] { $".{Path.DirectorySeparatorChar}foo.dll" });

            CollectionAssert.AreEqual(new[] { "foo.TestAdapter.dll" }, extensions.ToArray());
        }

        [TestMethod]
        public void GetTestPlatformExtensionsShouldReturnEmptySetIfSourceDirectoryIsEmpty()
        {
            // Parent directory is empty since the input source is file "test.dll"
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(fh => fh.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.TopDirectoryOnly)).Returns(new[] { "foo.dll" });
            var extensions = this.dotnetHostManager.GetTestPlatformExtensions(this.testSource);

            Assert.AreEqual(0, extensions.Count());
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithConnectionInfo()
        {
            // Absolute path to the source directory
            var sourcePath = Path.Combine($"{Path.DirectorySeparatorChar}tmp", "test.dll");
            string expectedTestHostPath = @"\tmp\testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(expectedTestHostPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            var expectedArgs = "exec \"" + expectedTestHostPath + "\" --port 0 --parentprocessid 0";

            this.dotnetHostManager.LaunchTestHost(startInfo);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.Arguments.Equals(expectedArgs))), Times.Once);
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
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.runtimeconfig.dev.json", FileMode.Open, FileAccess.ReadWrite)).Returns(runtimeConfigStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.runtimeconfig.dev.json")).Returns(true);

            MemoryStream depsFileStream = new MemoryStream(Encoding.UTF8.GetBytes(depsFileContent));
            this.mockFileHelper.Setup(ph => ph.GetStream("\\tmp\\test.deps.json", FileMode.Open, FileAccess.ReadWrite)).Returns(depsFileStream);
            this.mockFileHelper.Setup(ph => ph.Exists("\\tmp\\test.deps.json")).Returns(true);

            string testHostFullPath = @"C:\packages\microsoft.testplatform.testhost/15.0.0-Dev\lib/netstandard1.5/testhost.dll";
            this.mockFileHelper.Setup(ph => ph.Exists(testHostFullPath)).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(new[] { sourcePath }, null, this.defaultConnectionInfo);

            Assert.IsTrue(startInfo.Arguments.Contains(testHostFullPath));
        }

        private TestProcessStartInfo GetDefaultStartInfo()
        {
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(
                this.testSource,
                null,
                this.defaultConnectionInfo);
            return startInfo;
        }
    }

    internal class TestableDotnetTestHostManager : DotnetTestHostManager
    {
        public TestableDotnetTestHostManager(ITestHostLauncher testHostLauncher, IProcessHelper processHelper, IFileHelper fileHelper)
            : base(testHostLauncher, processHelper, fileHelper)
        { }
    }

    [TestClass]
    public class DefaultTestHostLauncherTests
    {
        [TestMethod]
        public void DefaultTestHostLauncherIsDebugShouldBeFalse()
        {
            var hostLauncher = new DefaultTestHostLauncher();

            Assert.IsFalse(hostLauncher.IsDebug);
        }

        [TestMethod]
        public void DefaultTestHostLauncherShouldStartTestProcess()
        {
            var startInfo = new TestProcessStartInfo { FileName = "testhost.exe", Arguments = "a1", WorkingDirectory = "w" };
            var currentProcess = Process.GetCurrentProcess();
            var mockProcessHelper = new Mock<IProcessHelper>();
            mockProcessHelper.Setup(ph => ph.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(currentProcess);
            var hostLauncher = new DefaultTestHostLauncher(mockProcessHelper.Object);

            var processId = hostLauncher.LaunchTestHost(startInfo);

            Assert.AreEqual(currentProcess.Id, processId);
            mockProcessHelper.Verify(ph => ph.LaunchProcess("testhost.exe", "a1", "w"), Times.Once);
        }
    }
}
