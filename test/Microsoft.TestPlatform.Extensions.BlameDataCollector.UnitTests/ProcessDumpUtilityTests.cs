// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
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
        private Mock<IDumperFactory> mockDumperFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDumpUtilityTests"/> class.
        /// </summary>
        public ProcessDumpUtilityTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockDumperFactory = new Mock<IDumperFactory>();
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

            this.mockDumperFactory.Setup(x => x.Create(It.IsAny<Version>()))
                .Returns(new Mock<IDumper>().Object);

            var processDumpUtility = new ProcessDumpUtility(
                this.mockProcessHelper.Object,
                this.mockFileHelper.Object,
                this.mockDumperFactory.Object);

            processDumpUtility.StartTriggerBasedProcessDump(processId, guid, testResultsDirectory);

            var ex = Assert.ThrowsException<FileNotFoundException>(() => processDumpUtility.GetDumpFile());
            Assert.AreEqual(ex.Message, Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }
    }
}