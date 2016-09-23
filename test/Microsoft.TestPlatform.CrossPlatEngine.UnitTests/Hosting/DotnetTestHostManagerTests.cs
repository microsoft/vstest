// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;

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

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual(DefaultDotnetPath, startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetXPlatOnLinux()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("/tmp/dotnet", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetOnWindows()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("c:\\tmp\\vstest.console.exe");

            var startInfo = this.GetDefaultStartInfo();

            Assert.AreEqual("dotnet.exe", startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldInvokeDotnetExec()
        {
            var startInfo = this.GetDefaultStartInfo();

            StringAssert.StartsWith(startInfo.Arguments, "exec");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddRuntimeConfigJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.Arguments, "--runtimeconfig \"test.runtimeconfig.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddRuntimeConfigJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.runtimeconfig.json")).Returns(false);

            var startInfo = this.GetDefaultStartInfo();

            Assert.IsFalse(startInfo.Arguments.Contains("--runtimeconfig \"test.runtimeconfig.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldAddDepsFileJsonIfExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(true);

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.Arguments, "--depsfile \"test.deps.json\"");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldNotAddDepsFileJsonIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.Exists("test.deps.json")).Returns(false);

            var startInfo = this.GetDefaultStartInfo();

            Assert.IsFalse(startInfo.Arguments.Contains("--depsfile \"test.deps.json\""));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldProvidePathToTestHostForDesktopTarget()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("c:\\tmp\\vstest.console.exe");

            var startInfo = this.GetDefaultStartInfo();

            StringAssert.Contains(startInfo.Arguments, "c:\\tmp\\NetCore\\testhost.dll");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldProvidePathToTestHostForNetCoreTarget()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("/tmp/dotnet");
            this.mockProcessHelper.Setup(ph => ph.GetTestEngineDirectory()).Returns("/tmp/vstest");

            var startInfo = this.GetDefaultStartInfo();

            // Path.GetDirectoryName returns platform specific path separator char
            StringAssert.Contains(startInfo.Arguments, this.GetTesthostPath("/tmp/vstest"));
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123 };

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, null, connectionInfo);

            StringAssert.Contains(startInfo.Arguments, "--port " + connectionInfo.Port);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeEnvironmentVariables()
        {
            var environmentVariables = new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } };

            var startInfo = this.dotnetHostManager.GetTestHostProcessStartInfo(this.testSource, environmentVariables, this.defaultConnectionInfo);

            Assert.AreEqual(environmentVariables, startInfo.EnvironmentVariables);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithNullEnvironmentVariablesOrArgs()
        {
            this.mockTestHostLauncher.Setup(thl => thl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(111);
            var startInfo = this.GetDefaultStartInfo();

            var processId = this.dotnetHostManager.LaunchTestHost(startInfo);

            Assert.AreEqual(111, processId);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithConnectionInfo()
        {
            this.mockProcessHelper.Setup(ph => ph.GetTestEngineDirectory()).Returns("/tmp/vstest");
            var startInfo = this.GetDefaultStartInfo();
            var expectedArgs = "exec \"" + this.GetTesthostPath("/tmp/vstest") + "\" --port 0";

            this.dotnetHostManager.LaunchTestHost(startInfo);

            this.mockTestHostLauncher.Verify(thl => thl.LaunchTestHost(It.Is<TestProcessStartInfo>(x => x.Arguments.Equals(expectedArgs))), Times.Once);
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
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("testhost.exe");
            
            this.mockFileHelper.Setup(fh => fh.Exists(DefaultDotnetPath)).Returns(true);
            var startInfo = this.GetDefaultStartInfo();
            
            Assert.AreEqual(DefaultDotnetPath, startInfo.FileName);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoOnWindowsForInValidPathReturnsDotnet()
        {
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("testhost.exe");
            
            this.dotnetHostManager.envVarPathString = @"d:\hello;c:\foo";
            this.mockFileHelper.Setup(fh => fh.Exists(DefaultDotnetPath)).Returns(false);
            var startInfo = this.GetDefaultStartInfo();
            
            Assert.AreEqual("dotnet.exe", startInfo.FileName);
        }
        
        private string GetTesthostPath(string engineDirectory)
        {
            // testhost.dll will be picked up from the same path as vstest.console.dll. In the test, we are setting up
            // the path to current assembly location.
            return Path.Combine(engineDirectory, "testhost.dll");
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

        internal string envVarPathString = @"d:\hello;c:\tmp\; c:\foo";
        
        internal override string EnvVarPathString { get { return envVarPathString; } }
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