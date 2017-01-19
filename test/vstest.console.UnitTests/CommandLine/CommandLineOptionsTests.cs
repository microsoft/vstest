// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class CommandLineOptionsTests
    {
        private readonly Mock<IFileHelper> fileHelper;

        public CommandLineOptionsTests()
        {
            this.fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = this.fileHelper.Object;
        }

        [TestMethod]
        public void CommandLineOptionsDefaultBatchSizeIsTen()
        {
            Assert.AreEqual(10, CommandLineOptions.Instance.BatchSize);
        }

        [TestMethod]
        public void CommandLineOptionsDefaultTestRunStatsEventTimeoutIsOnePointFiveSec()
        {
            var timeout = new TimeSpan(0, 0, 0, 1, 500);
            Assert.AreEqual(timeout, CommandLineOptions.Instance.TestRunStatsEventTimeout);
        }

        [TestMethod]
        public void CommandLineOptionsGetForSourcesPropertyShouldReturnReadonlySourcesEnumerable()
        {
            Assert.IsTrue(CommandLineOptions.Instance.Sources is ReadOnlyCollection<string>);
        }

        [TestMethod]
        public void CommandLineOptionsGetForHasPhoneContextPropertyIfTargetDeviceIsSetReturnsTrue()
        {
            Assert.IsFalse(CommandLineOptions.Instance.HasPhoneContext);

            // Set some not null value
            CommandLineOptions.Instance.TargetDevice = "TargetDevice";
            Assert.IsTrue(CommandLineOptions.Instance.HasPhoneContext);
        }
        
        [TestMethod]
        public void CommandLineOptionsAddSourceShouldThrowCommandLineExceptionForNullSource()
        {
            Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource(null));
        }
        
        [TestMethod]
        public void CommandLineOptionsAddSourceShouldThrowCommandLineExceptionForInvalidSource()
        {
            Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource("DummySource"));
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceThrowExceptionIfDuplicateSource()
        {
            var testFilePath = "DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);
            var ex = Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource(testFilePath));
            Assert.AreEqual("Duplicate source " + Path.Combine(Directory.GetCurrentDirectory(), testFilePath) + " specified.", ex.Message);
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceForValidSource()
        {
            //rooted sources are only valid sources
            string testFilePath = @"C:\Test\DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
           
            CommandLineOptions.Instance.AddSource(testFilePath);
            
            Assert.IsTrue(CommandLineOptions.Instance.Sources.Contains(testFilePath));
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddFullPathBasedOnCurrentDirForRelativeValidSource()
        {
            string testFilePath = "DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);

            Assert.IsTrue(Path.IsPathRooted(CommandLineOptions.Instance.Sources.First()));
        }
    }
}
