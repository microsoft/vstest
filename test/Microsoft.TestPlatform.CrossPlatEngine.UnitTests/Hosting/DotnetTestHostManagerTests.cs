// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DotnetTestHostManagerTests
    {
        private const string DefaultDotnetPath = "c:\\tmp\\dotnet.exe";

        private const string DefaultTestHostPath = "c:\\tmp\\testhost.dll";

        private readonly Mock<ITestHostLauncher> mockTestHostLauncher;

        private readonly TestableDotnetTestHostManager dotnetHostManager;

        private readonly Mock<IProcessHelper> mockProcessHelper;

        private readonly Mock<IFileHelper> mockFileHelper;

        public DotnetTestHostManagerTests()
        {
            this.mockTestHostLauncher = new Mock<ITestHostLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.dotnetHostManager = new TestableDotnetTestHostManager(
                                         this.mockTestHostLauncher.Object,
                                         this.mockProcessHelper.Object,
                                         this.mockFileHelper.Object);

            // Setup a dummy current process for tests
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetCommandline()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns(DefaultDotnetPath);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);

            Assert.AreEqual(DefaultDotnetPath, startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetXPlatOnLinux()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);

            Assert.AreEqual("/tmp/dotnet", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetOnWindows()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("c:\\tmp\\vstest.console.exe");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);

            Assert.AreEqual("dotnet", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetExec()
        {
            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);
            
            StringAssert.StartsWith(startInfo.Arguments, "exec");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddRuntimeConfigJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);
            
            StringAssert.Contains(startInfo.Arguments, "--runtimeconfig \"test.runtimeconfig.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddRuntimeConfigJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(false);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);
            
            Assert.IsFalse(startInfo.Arguments.Contains("--runtimeconfig \"test.runtimeconfig.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddDepsFileJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(true);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);
            
            StringAssert.Contains(startInfo.Arguments, "--depsfile \"test.deps.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddDepsFileJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(false);

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);
            
            Assert.IsFalse(startInfo.Arguments.Contains("--depsfile \"test.deps.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldProvidePathToTestHostForDesktopTarget()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("c:\\tmp\\vstest.console.exe");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);

            StringAssert.Contains(startInfo.Arguments, "c:\\tmp\\NetCore\\testhost.dll");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldProvidePathToTestHostForNetCoreTarget()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(null, null);

            // Path.GetDirectoryName returns platform specific path separator char
            StringAssert.Contains(startInfo.Arguments, this.GetTesthostPath());
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithNullEnvironmentVariablesOrArgs()
        {
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(111);

            var processId = this.dotnetHostManager.LaunchTestHost(null, null);

            Assert.AreEqual(111, processId);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithArguments()
        {
            var args = new List<string> { "arg1", "arg2" };
            var expectedArgs = "exec \"" + this.GetTesthostPath() + "\" arg1 arg2";

            this.dotnetHostManager.LaunchTestHost(null, args);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.Arguments.Equals(expectedArgs))), Times.Once);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithEnvironmentVariables()
        {
            var variables = new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } };

            this.dotnetHostManager.LaunchTestHost(variables, null);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.EnvironmentVariables.Equals(variables))), Times.Once);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithWorkingDirectorySetToTestAssembly()
        {
        }

        [TestMethod]
        public void LaunchTestHostShouldUseCustomHostIfSet()
        {
        }

        private string GetTesthostPath()
        {
            // testhost.dll will be picked up from the same path as vstest.console.dll. In the test, we are setting up
            // the path to current assembly location.
            var testhostPath = Path.Combine(
                Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location),
                "testhost.dll");
            return testhostPath;
        }
    }

    internal class TestableDotnetTestHostManager : DotnetTestHostManager
    {
        public TestableDotnetTestHostManager(ITestHostLauncher testHostLauncher, IProcessHelper processHelper, IFileHelper fileHelper)
            : base(testHostLauncher, processHelper, fileHelper)
        {
        }
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