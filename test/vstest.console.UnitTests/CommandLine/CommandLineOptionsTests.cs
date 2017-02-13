// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.IO;

    [TestClass]
    public class CommandLineOptionsTests
    {
        private readonly Mock<IFileHelper> fileHelper;
        private readonly string currentDirectory = @"C:\\Temp";

        public CommandLineOptionsTests()
        {
            this.fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = this.fileHelper.Object;
            this.fileHelper.Setup(fh => fh.GetCurrentDirectory()).Returns(currentDirectory);
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
            Assert.AreEqual(timeout, CommandLineOptions.Instance.TestStatsEventTimeout);
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
        public void CommandLineOptionsAddSourceShouldConvertRelativePathToAbsolutePath()
        {
            string relativeTestFilePath = "DummyTestFile.txt";
            var absolutePath = Path.Combine(currentDirectory, relativeTestFilePath);
            this.fileHelper.Setup(fh => fh.Exists(absolutePath)).Returns(true);

            // Pass relative path
            CommandLineOptions.Instance.AddSource(relativeTestFilePath);
            Assert.IsTrue(CommandLineOptions.Instance.Sources.Contains(absolutePath));
        }
        
        [TestMethod]
        public void CommandLineOptionsAddSourceShouldThrowCommandLineExceptionForInvalidSource()
        {
            Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource("DummySource"));
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceThrowExceptionIfDuplicateSource()
        {
            var testFilePath = "C:\\DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);
            var ex = Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource(testFilePath));
            Assert.AreEqual("Duplicate source " + testFilePath + " specified.", ex.Message);
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceForValidSource()
        {
            string testFilePath = "C:\\DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);
            
            CommandLineOptions.Instance.AddSource(testFilePath);
            
            Assert.IsTrue(CommandLineOptions.Instance.Sources.Contains(testFilePath));
        }
    }
}
