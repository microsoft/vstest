// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// The Local crash dump utilities tests.
    /// </summary>
    [TestClass]
    public class LocalCrashDumpUtilitiesTests
    {
        private Mock<IFileHelper> mockFileHelper;
        private TestableLocalCrashDumpUtilities localCrashDumpUtilities;

        public LocalCrashDumpUtilitiesTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.localCrashDumpUtilities = new TestableLocalCrashDumpUtilities(this.mockFileHelper.Object);
        }

        [TestMethod]
        public void GetCrashDumpFileShouldThrowExceptionIfDumpPathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.localCrashDumpUtilities.GetCrashDumpFile(null, "test.exe", 14);
            });
        }

        [TestMethod]
        public void GetCrashDumpFileShouldThrowExceptionIfApplicationNamePathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.localCrashDumpUtilities.GetCrashDumpFile("test://folder", null, 14);
            });
        }

        [TestMethod]
        public void GetCrashDumpFileShouldThrowExceptionIfProcessIdeIsNegative()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                this.localCrashDumpUtilities.GetCrashDumpFile("test://folder", "test.exe", -100);
            });
        }

        [TestMethod]
        public void GetCrashDumpFileShouldReturnLatestCrashDumpFile()
        {
            string[] dumpFiles = { "test.exe.1456.dmp", "test.exe(1).1456.dmp" };
            this.mockFileHelper.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(dumpFiles);
            DateTime dateTime1 = new DateTime(1995, 12, 21);
            DateTime dateTime2 = new DateTime(1995, 12, 25);
            this.mockFileHelper.Setup(x => x.GetLastWriteTime("test.exe.1456.dmp")).Returns(dateTime1);
            this.mockFileHelper.Setup(x => x.GetLastWriteTime("test.exe(1).1456.dmp")).Returns(dateTime2);

            string latestFile = this.localCrashDumpUtilities.GetCrashDumpFile("C:\\test", "test.exe", 1456);

            Assert.AreEqual("test.exe(1).1456.dmp", latestFile);
        }

        /// <summary>
        /// The testable local crash dump utilities.
        /// </summary>
        public class TestableLocalCrashDumpUtilities : LocalCrashDumpUtilities
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableLocalCrashDumpUtilities"/> class.
            /// </summary>
            /// <param name="fileHelper">File Helper</param>
            public TestableLocalCrashDumpUtilities(IFileHelper fileHelper)
                : base(fileHelper)
            {
            }
        }
    }
}
