// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DefaultTestHostManagerTests
    {
        private DefaultTestHostManager testHostManager;
        
        /// <summary>
        /// The mock process helper.
        /// </summary>
        /// <remarks>Doing this only because mocks currently does not support internalVisibleTo on signed assemblies yet for .Net Core.</remarks>
        private Mock<IProcessHelper> mockProcessHelper;

        private TestProcessStartInfo startInfo;

        public DefaultTestHostManagerTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("vstest.console.exe");

            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper.Object);
            this.startInfo = this.testHostManager.GetTestHostProcessStartInfo(null, default(TestRunnerConnectionInfo));
        }

        [TestMethod]
        public void ConstructorShouldSetX86ProcessForX86Architecture()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X86, Framework.DefaultFramework, this.mockProcessHelper.Object);

            var info = this.testHostManager.GetTestHostProcessStartInfo(null, default(TestRunnerConnectionInfo));

            StringAssert.EndsWith(info.FileName, "testhost.x86.exe");
        }

        [TestMethod]
        public void ConstructorShouldSetX64ProcessForX64Architecture()
        {
            StringAssert.EndsWith(this.startInfo.FileName, "testhost.exe");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeConnectionInfo()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123 };

            var info = this.testHostManager.GetTestHostProcessStartInfo(null, connectionInfo);

            Assert.AreEqual(" --port 123", info.Arguments);
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

            var info = this.testHostManager.GetTestHostProcessStartInfo(environmentVariables, default(TestRunnerConnectionInfo));

            Assert.AreEqual(environmentVariables, info.EnvironmentVariables);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldIncludeCurrentWorkingDirectory()
        {
            Assert.AreEqual(Directory.GetCurrentDirectory(), this.startInfo.WorkingDirectory);
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            this.mockProcessHelper.Setup(ph => ph.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Process.GetCurrentProcess());

            var processId = this.testHostManager.LaunchTestHost(this.startInfo);

            Assert.AreEqual(Process.GetCurrentProcess().Id, processId);
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

            var pid = this.testHostManager.LaunchTestHost(this.startInfo);

            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            Assert.AreEqual(currentProcess.Id, pid);
        }
    }
}
