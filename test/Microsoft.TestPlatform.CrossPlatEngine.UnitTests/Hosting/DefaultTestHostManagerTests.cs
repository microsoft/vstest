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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System.Threading.Tasks;
    using System.Threading;
    using System;

    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private DefaultTestHostManager testHostManager;
        private readonly Mock<IProcessHelper> mockProcessHelper;
        private readonly TestProcessStartInfo startInfo;

        public DefaultTestHostManagerTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");

            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object, true);
            this.startInfo = this.testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));
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
            var mockProcessHelper = new TestableProcessHelper();
            
            var testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, mockProcessHelper, true);
            var startInfo = testHostManager.GetTestHostProcessStartInfo(Enumerable.Empty<string>(), null, default(TestRunnerConnectionInfo));

            Task<int> processId = testHostManager.LaunchTestHost(startInfo);

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

            Task<int> pid = this.testHostManager.LaunchTestHost(this.startInfo);
            try
            {
                pid.Wait(new CancellationTokenSource(3000).Token);
            }
            catch (Exception) { }
            
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            Assert.AreEqual(currentProcess.Id, pid.Result);
        }

        private class TestableProcessHelper : IProcessHelper
        {
            private string ErrorMessage;

            public void SetErrorMessage(string errorMessage)
            {
                this.ErrorMessage = errorMessage;
            }
            public string GetCurrentProcessFileName()
            {
                return "vstest.console.exe";
            }

            public int GetCurrentProcessId()
            {
                throw new NotImplementedException();
            }

            public string GetTestEngineDirectory()
            {
                throw new NotImplementedException();
            }

            public Process LaunchProcess(string processPath, string arguments, string workingDirectory, Action<Process, string> errorCallback)
            {
                return Process.GetCurrentProcess();
            }

            public string GetProcessName(int processId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
