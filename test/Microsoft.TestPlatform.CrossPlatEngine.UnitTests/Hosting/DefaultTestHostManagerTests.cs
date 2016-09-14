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
        private MockProcessHelper mockProcessHelper;

        [TestInitialize]
        public void TestInit()
        {
            this.mockProcessHelper = new MockProcessHelper();
        }

        [TestMethod]
        public void ConstructorShouldSetX86ProcessForX86Architecture()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X86, Framework.DefaultFramework, this.mockProcessHelper);
            
            // Setup mocks.
            var processPath = string.Empty;
            var times = 0;

            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
                {
                    times++;
                    processPath = path;
                    return Process.GetCurrentProcess();
                };
                
            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            StringAssert.EndsWith(processPath, "testhost.x86.exe");
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void ConstructorShouldSetX64ProcessForX64Architecture()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            // Setup mocks.
            var processPath = string.Empty;
            var times = 0;

            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                times++;
                processPath = path;
                return Process.GetCurrentProcess();
            };
            
            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            StringAssert.EndsWith(processPath, "testhost.exe");
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithOneArgument()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            // Setup mocks.
            var cliargs = string.Empty;
            var times = 0;

            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                times++;
                cliargs = args;
                return Process.GetCurrentProcess();
            };

            var arguments = new List<string> { "-p" };
            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), arguments);

            var commandLineArgumentsString = string.Join(" ", arguments);

            Assert.AreEqual(commandLineArgumentsString, cliargs);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithMultipleArguments()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            // Setup mocks.
            var cliargs = string.Empty;
            var times = 0;

            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                times++;
                cliargs = args;
                return Process.GetCurrentProcess();
            };

            var arguments = new List<string> { "-p", "23453" };
            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), arguments);

            var commandLineArgumentsString = string.Join(" ", arguments);

            Assert.AreEqual(commandLineArgumentsString, cliargs);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchProcessWithCurrentWorkingDirectory()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            // Setup mocks.
            var pwd = string.Empty;
            var times = 0;

            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                times++;
                pwd = wd;
                return Process.GetCurrentProcess();
            };

            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            var workingDirectory = Directory.GetCurrentDirectory();
            
            Assert.AreEqual(workingDirectory, pwd);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper); ;

            // Setup mocks.
            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                return Process.GetCurrentProcess();
            };

            var processID = this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            Assert.AreEqual(Process.GetCurrentProcess().Id, processID);
        }

        [TestMethod]
        public void LaunchTestHostShouldLaunchDotnetExeIfRunningUnderDotnetCLIContext()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            string processPath = null;

            // Setup mocks.
            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                processPath = path;
                return Process.GetCurrentProcess();
            };
            var currentProcessPath = "c:\\temp\\dotnet.exe";
            this.mockProcessHelper.CurrentProcessName = currentProcessPath;

            this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            Assert.AreEqual(currentProcessPath, processPath);
        }

        [TestMethod]
        public void PropertiesShouldReturnEmptyDictionary()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            Assert.AreEqual(0, this.testHostManager.Properties.Count);
        }

        [TestMethod]
        public void LaunchTestHostShouldUseCustomHostIfSet()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);
            var mockCustomLauncher = new Mock<ITestHostLauncher>();
            this.testHostManager.SetCustomLauncher(mockCustomLauncher.Object);

            var isProcessHelperCalled = false;
            var processToReturn = Process.GetCurrentProcess();
            // Setup mocks.
            this.mockProcessHelper.LaunchProcessInvoker = (path, args, wd) =>
            {
                isProcessHelperCalled = true;
                return processToReturn;
            };

            mockCustomLauncher.Setup(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(processToReturn.Id);

            var processID = this.testHostManager.LaunchTestHost(new Dictionary<string, string>(), new List<string>());

            Assert.IsFalse(isProcessHelperCalled, "ProcessHelper must not be called if custom launcher is set.");
            mockCustomLauncher.Verify(mc => mc.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom launcher must be called if set.");
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldSetWorkingDirectoryAsParentProcess()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            TestProcessStartInfo testProcessStartInfo = this.testHostManager.GetTestHostProcessStartInfo(null, new List<string>());

            Assert.AreEqual(Directory.GetCurrentDirectory(), testProcessStartInfo.WorkingDirectory);
        }

        [TestMethod]
        public void GetTestHostProcessStartInfoShouldSetWorkingDirectoryAsParentProcessIfRunningUnderDotnetCLIContext()
        {
            this.testHostManager = new DefaultTestHostManager(Architecture.X64, Framework.DefaultFramework, this.mockProcessHelper);

            this.mockProcessHelper.CurrentProcessName = "dotnet.exe";

            TestProcessStartInfo testProcessStartInfo = this.testHostManager.GetTestHostProcessStartInfo(null, new List<string>());

            Assert.AreEqual(Directory.GetCurrentDirectory(), testProcessStartInfo.WorkingDirectory);
        }

        #region implementations

        private class MockProcessHelper : IProcessHelper
        {
            public MockProcessHelper()
            {
                this.CurrentProcessName = "testhost.exe";
            }

            public Func<string,string,string,Process> LaunchProcessInvoker { get; set; }

            public string CurrentProcessName { get; set; }

            public string GetCurrentProcessFileName()
            {
                return this.CurrentProcessName;
            }

            public Process LaunchProcess(string processPath, string arguments, string workingDirectory)
            {
                return this.LaunchProcessInvoker?.Invoke(processPath, arguments, workingDirectory);
            }
        }

        #endregion
    }
}
