// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// The blame collector tests.
    /// </summary>
    [TestClass]
    public class ProcessDumpUtilityTests
    {
        private Mock<IFileHelper> mockFileHelper;
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<Process> mockProcDumpProcess;
        private Mock<IEnvironment> mockPlatformEnvironment;
        private Mock<INativeMethodsHelper> mockNativeMethodsHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDumpUtilityTests"/> class.
        /// </summary>
        public ProcessDumpUtilityTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcDumpProcess = new Mock<Process>();
            this.mockPlatformEnvironment = new Mock<IEnvironment>();
            this.mockNativeMethodsHelper = new Mock<INativeMethodsHelper>();

            Environment.SetEnvironmentVariable("PROCDUMP_PATH", "D:\\procdump");
        }

        /// <summary>
        /// GetDumpFile will return empty list of strings if no dump files found
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillThrowExceptionIfNoDumpfile()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var testResultsDirectory = "D:\\TestResults";

            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(new string[] { });
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);
            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Returns(this.mockProcDumpProcess.Object);

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory);

            var ex = Assert.ThrowsException<FileNotFoundException>(() => processDumpUtility.GetDumpFile());
            Assert.AreEqual(ex.Message, Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }

        /// <summary>
        /// GetDumpFile will return empty list of strings if proc dump never started
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillReturnEmptyIfProcDumpDidntStart()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var testResultsDirectory = "D:\\TestResults";

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Throws(new Exception());
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);

            var processDumpUtility = new ProcessDumpUtility(
                 this.mockProcessHelper.Object,
                 this.mockFileHelper.Object,
                 this.mockPlatformEnvironment.Object,
                 this.mockNativeMethodsHelper.Object);

            Assert.ThrowsException<Exception>(() => processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory));
            Assert.AreEqual(string.Empty, processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// GetDumpFile will wait for proc dump process to exit before getting file
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillWaitForProcessToExitAndGetDumpFile()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var testResultsDirectory = "D:\\TestResults";

            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(new string[] { "dump.dmp" });
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);
            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Returns(this.mockProcDumpProcess.Object);

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory);
            processDumpUtility.GetDumpFile();

            this.mockProcessHelper.Verify(x => x.WaitForProcessExit(It.IsAny<Process>()), Times.Once);
        }

        /// <summary>
        /// StartProcessDump should start proc dump binary with correct arguments, while GetDumpFile returns full path
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartProcDumpExeWithCorrectParamsAndGetDumpFileReturnsFullPath()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var filename = $"{process}_{processId}_{guid}.dmp";
            var args = $"-accepteula -e 1 -g -t -f STACK_OVERFLOW -f ACCESS_VIOLATION {processId} {filename}";
            var testResultsDirectory = "D:\\TestResults";

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Returns(this.mockProcDumpProcess.Object);
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);

            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(new string[] { Path.Combine(testResultsDirectory, filename) });

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), args, It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()), Times.Once);
            Assert.AreEqual(Path.Combine(testResultsDirectory, filename), processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// StartProcessDump should start proc dump binary with correct full dump arguments, while GetDumpFile returns full path
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartProcDumpExeWithCorrectParamsForFullDump()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var filename = $"{process}_{processId}_{guid}.dmp";
            var args = $"-accepteula -e 1 -g -t -ma -f STACK_OVERFLOW -f ACCESS_VIOLATION {processId} {filename}";
            var testResultsDirectory = "D:\\TestResults";

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Returns(this.mockProcDumpProcess.Object);
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);
            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(new string[] { Path.Combine(testResultsDirectory, filename) });

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory, isFullDump: true);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), args, It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()), Times.Once);
            Assert.AreEqual(Path.Combine(testResultsDirectory, filename), processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// StartProcessDump should start proc dump binary with correct arguments for hang based dump, while GetDumpFile returns full path
        /// </summary>
        [TestMethod]
        public void StartProcessDumpForHangWillStartProcDumpExeWithCorrectParams()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var filename = $"{process}_{processId}_{guid}_hangdump.dmp";
            var args = $"-accepteula -n 1 -ma {processId} {filename}";
            var testResultsDirectory = "D:\\TestResults";

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()))
                .Returns(this.mockProcDumpProcess.Object);
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                .Returns(process);
            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(new string[] { Path.Combine(testResultsDirectory, filename) });

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartHangBasedProcessDump(processId, guid, testResultsDirectory, isFullDump: true);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), args, It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()), Times.Once);
            Assert.AreEqual(Path.Combine(testResultsDirectory, filename), processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// Start process dump will throw error if PROCDUMP_PATH env variable is not set
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillThrowErrorIfProcdumpEnvVarNotSet()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            var ex = Assert.ThrowsException<TestPlatformException>(() => processDumpUtility.StartTriggerBasedProcessDump(1234, "guid", "D:\\"));
            Assert.AreEqual(ex.Message, Resources.Resources.ProcDumpEnvVarEmpty);
        }

        /// <summary>
        /// Start process dump will start exe according to Test host Process in 32Bit OS
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartExeCorrespondingToTestHostProcessIn32BitOS()
        {
            var guid = "guid";

            // var process = "process";
            var processId = 12345;
            var testResultsDirectory = "D:\\TestResults";

            this.mockPlatformEnvironment.Setup(x => x.Architecture).Returns(PlatformArchitecture.X86);
            this.mockProcessHelper.Setup(x => x.GetProcessHandle(processId))
                                .Returns(new IntPtr(0));

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockPlatformEnvironment.Object,
                this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(Path.Combine("D:\\procdump", "procdump.exe"), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()));
        }

        /// <summary>
        /// Start process dump will start exe according to 64 Bit Test host Process in 64Bit OS
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartExeCorrespondingTo64BitTestHostProcessIn64BitOS()
        {
            IntPtr x64ProcessHandle = new IntPtr(0);
            int processId = 1234;
            this.mockPlatformEnvironment.Setup(x => x.Architecture).Returns(PlatformArchitecture.X64);

            this.mockProcessHelper.Setup(x => x.GetProcessHandle(processId))
                                .Returns(x64ProcessHandle);
            this.mockNativeMethodsHelper.Setup(x => x.Is64Bit(x64ProcessHandle))
                                .Returns(true);

            var processDumpUtility = new ProcessDumpUtility(
            this.mockProcessHelper.Object,
            this.mockFileHelper.Object,
            this.mockPlatformEnvironment.Object,
            this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, "guid", "D:\\");

            this.mockProcessHelper.Verify(x => x.LaunchProcess(Path.Combine("D:\\procdump", "procdump64.exe"), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()));
        }

        /// <summary>
        /// Start process dump will start exe according to 32 Bit Test host Process in 64 bit OS
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartExeCorrespondingTo32BitTestHostProcessIn64BitOS()
        {
            IntPtr x86ProcessHandle = new IntPtr(0);
            int processId = 12345;
            this.mockPlatformEnvironment.Setup(x => x.Architecture).Returns(PlatformArchitecture.X64);

            this.mockProcessHelper.Setup(x => x.GetProcessHandle(processId))
                                .Returns(x86ProcessHandle);
            this.mockNativeMethodsHelper.Setup(x => x.Is64Bit(x86ProcessHandle))
                                .Returns(false);

            var processDumpUtility = new ProcessDumpUtility(
            this.mockProcessHelper.Object,
            this.mockFileHelper.Object,
            this.mockPlatformEnvironment.Object,
            this.mockNativeMethodsHelper.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, "guid", "D:\\");

            this.mockProcessHelper.Verify(x => x.LaunchProcess(Path.Combine("D:\\procdump", "procdump.exe"), It.IsAny<string>(), It.IsAny<string>(), null, null, null, It.IsAny<Action<object, string>>()));
        }

        /// <summary>
        /// Ensure terminate process calls terminate on proc dump process
        /// </summary>
        [TestMethod]
        public void TerminateProcessDumpShouldCallTerminateOnProcDumpProcess()
        {
            var processDumpUtility = new ProcessDumpUtility(
            this.mockProcessHelper.Object,
            this.mockFileHelper.Object,
            this.mockPlatformEnvironment.Object,
            this.mockNativeMethodsHelper.Object);

            // Mock process helper
            this.mockProcessHelper.Setup(x => x.TerminateProcess(It.IsAny<object>()));

            // Raise
            processDumpUtility.TerminateProcess();

            // Verify
            this.mockProcessHelper.Verify(x => x.TerminateProcess(It.IsAny<object>()), Times.Once);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);
        }
    }
}