// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.TestPlatform.Extensions.BlameDataCollector;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDumpUtilityTests"/> class.
        /// </summary>
        public ProcessDumpUtilityTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcDumpProcess = new Mock<Process>();
            this.mockPlatformEnvironment = new Mock<IEnvironment>();

            Environment.SetEnvironmentVariable("PROCDUMP_PATH", "procdump_folder");
        }

        /// <summary>
        /// GetDumpFile will return empty list of strings if no dump files found
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillReturnEmptyIfNoDumpFile()
        {
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>()))
                               .Returns(false);

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null))
                                  .Returns(this.mockProcDumpProcess.Object);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);
            processDumpUtility.StartProcessDump(12345, "guid", "D:\\TestResults");

            Assert.AreEqual(string.Empty, processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// GetDumpFile will return empty list of strings if proc dump never started
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillReturnEmptyIfProcDumpDidntStart()
        {
            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null))
                                  .Throws(new Exception());

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);

            Assert.ThrowsException<Exception>(() => processDumpUtility.StartProcessDump(12345, "guid", "D:\\TestResults"));
            Assert.AreEqual(string.Empty, processDumpUtility.GetDumpFile());
        }

        /// <summary>
        /// GetDumpFile will wait for procdump process to exit before getting file
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillWaitForProcessToExitAndGetDumpFile()
        {
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>()))
                               .Returns(true);

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null))
                                  .Returns(this.mockProcDumpProcess.Object);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);

            processDumpUtility.StartProcessDump(12345, "guid", "D:\\TestResults");
            processDumpUtility.GetDumpFile();

            this.mockProcessHelper.Verify(x => x.WaitForProcessExit(It.IsAny<Process>()), Times.Once);
        }

        /// <summary>
        /// StartProcessDump should start procdump binary with correct arguments, while GetDumpFile returns full path
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartProcDumpExeWithCorrectParamsAndGetDumpFileReturnsFullPath()
        {
            var guid = "guid";
            var process = "process";
            var processId = 12345;
            var filename = $"{process}_{processId}_{guid}.dmp";
            var args = $"-t -g -ma {processId} {filename}";
            var testResultsDirectory = "D:\\TestResults";

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null))
                                  .Returns(this.mockProcDumpProcess.Object);
            this.mockProcessHelper.Setup(x => x.GetProcessName(processId))
                                  .Returns(process);

            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);
            processDumpUtility.StartProcessDump(processId, guid, testResultsDirectory);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), args, It.IsAny<string>(), null, null, null), Times.Once);
            Assert.AreEqual(Path.Combine(testResultsDirectory, filename), processDumpUtility.GetDumpFile());
        }

        [TestMethod]
        public void StartProcessDumpWillThrowErrorIfProcdumpEnvVarNotSet()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);

            Assert.ThrowsException<Exception>(() => processDumpUtility.StartProcessDump(1234, "guid", "D:\\"));
        }
    }
}
