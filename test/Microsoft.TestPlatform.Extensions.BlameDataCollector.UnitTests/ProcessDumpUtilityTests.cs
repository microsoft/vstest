// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.TestPlatform.Extensions.BlameDataCollector;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDumpUtilityTests"/> class.
        /// </summary>
        public ProcessDumpUtilityTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockProcDumpProcess = new Mock<Process>();
            this.mockPlatformEnvironment = new Mock<IEnvironment>();

            Environment.SetEnvironmentVariable("PROCDUMP_PATH", "D:\\procdump");
        }

        /// <summary>
        /// GetDumpFile will return empty list of strings if no dump files found
        /// </summary>
        [TestMethod]
        public void GetDumpFileWillThrowExceptionIfNoDumpfile()
        {
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>()))
                               .Returns(false);

            this.mockProcessHelper.Setup(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null))
                                  .Returns(this.mockProcDumpProcess.Object);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);
            processDumpUtility.StartProcessDump(12345, "guid", "D:\\TestResults");

            var ex = Assert.ThrowsException<FileNotFoundException>(() => processDumpUtility.GetDumpFile());
            Assert.AreEqual(ex.Message, Resources.Resources.DumpFileNotGeneratedErrorMessage);
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
            var args = $"-t -ma {processId} {filename}";
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

        /// <summary>
        /// Start process dump will throw error if PROCDUMP_PATH env variable is not set
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillThrowErrorIfProcdumpEnvVarNotSet()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);

            var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);

            var ex = Assert.ThrowsException<TestPlatformException>(() => processDumpUtility.StartProcessDump(1234, "guid", "D:\\"));
            Assert.AreEqual(ex.Message, Resources.Resources.ProcDumpEnvVarEmpty);
        }

        /// <summary>
        /// Start process dump will start exe corresponding to platform architecture
        /// </summary>
        [TestMethod]
        public void StartProcessDumpWillStartExeCorrespondingToPlatformArchitecture()
        {
            PlatformArchitecture[] platformArchitecture = { PlatformArchitecture.X64, PlatformArchitecture.X86 };

            Dictionary<PlatformArchitecture, string> architectureExeMap = new Dictionary<PlatformArchitecture, string>()
            {
                { PlatformArchitecture.X86, "procdump.exe" },
                { PlatformArchitecture.X64, "procdump64.exe" }
            };

            foreach (var architecture in architectureExeMap)
            {
                this.mockPlatformEnvironment.Setup(x => x.Architecture).Returns(architecture.Key);

                var processDumpUtility = new ProcessDumpUtility(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockPlatformEnvironment.Object);
                processDumpUtility.StartProcessDump(1234, "guid", "D:\\");

                this.mockProcessHelper.Verify(x => x.LaunchProcess(Path.Combine("D:\\procdump", architecture.Value), It.IsAny<string>(), It.IsAny<string>(), null, null, null));
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);
        }
    }
}
