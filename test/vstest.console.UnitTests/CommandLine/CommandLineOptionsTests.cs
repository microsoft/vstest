// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

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
            this.fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);
            var ex = Assert.ThrowsException<CommandLineException>(() => CommandLineOptions.Instance.AddSource(testFilePath));
            Assert.AreEqual("Duplicate source " + testFilePath + " specified.", ex.Message);
        }

        [TestMethod]
        public void CommandLineOptionsAddSourceShouldAddSourceForValidSource()
        {
            string testFilePath = "DummyTestFile.txt";
            this.fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);
            
            CommandLineOptions.Instance.AddSource(testFilePath);
            
            Assert.IsTrue(CommandLineOptions.Instance.Sources.Contains(testFilePath));
        }
    }
}
