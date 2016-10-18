// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System.Diagnostics;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class EnableDiagArgumentProcessorTests
    {
        private string dummyFilePath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "tmp", "foo.txt");

        private readonly EnableDiagArgumentProcessor diagProcessor;

        private readonly Mock<IFileHelper> mockFileHelper;

        public EnableDiagArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.diagProcessor = new TestableEnableDiagArgumentProcessor(this.mockFileHelper.Object);
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorMetadataShouldProvideAppropriateCapabilities()
        {
            Assert.IsFalse(this.diagProcessor.Metadata.Value.AllowMultiple);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.AlwaysExecute);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.IsAction);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.IsSpecialCommand);
            Assert.AreEqual(EnableDiagArgumentProcessor.CommandName, this.diagProcessor.Metadata.Value.CommandName);
            Assert.AreEqual(null, this.diagProcessor.Metadata.Value.ShortCommandName);
            Assert.AreEqual(ArgumentProcessorPriority.Diag, this.diagProcessor.Metadata.Value.Priority);
            Assert.AreEqual(HelpContentPriority.EnableDiagArgumentProcessorHelpPriority, this.diagProcessor.Metadata.Value.HelpPriority);
            Assert.AreEqual(CommandLineResources.EnableDiagUsage, this.diagProcessor.Metadata.Value.HelpContentResourceName);
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorThrowsIfFileNameIsNullOrEmpty()
        {
            Assert.ThrowsException<CommandLineException>(() => this.diagProcessor.Executor.Value.Initialize(null));
            Assert.ThrowsException<CommandLineException>(() => this.diagProcessor.Executor.Value.Initialize(string.Empty));
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorThrowsIfFileIsReadOnly()
        {
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyFilePath)).Returns(true);
            this.mockFileHelper.Setup(fh => fh.GetFileAttributes(this.dummyFilePath)).Returns(FileAttributes.ReadOnly);

            Assert.ThrowsException<CommandLineException>(() => this.diagProcessor.Executor.Value.Initialize(this.dummyFilePath));
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorShouldThrowIfAPathIsProvided()
        {
            Assert.ThrowsException<CommandLineException>(() => this.diagProcessor.Executor.Value.Initialize("foo"));
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorShouldCreateDirectoryOfLogFileIfNotExists()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(Path.GetDirectoryName(this.dummyFilePath))).Returns(false);
            
            this.diagProcessor.Executor.Value.Initialize(this.dummyFilePath);

            this.mockFileHelper.Verify(fh => fh.CreateDirectory(Path.GetDirectoryName(this.dummyFilePath)), Times.Once);
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorShouldNotCreateDirectoryIfAFileIsProvided()
        {
            this.diagProcessor.Executor.Value.Initialize("log.txt");

            this.mockFileHelper.Verify(fh => fh.CreateDirectory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorShouldEnableVerboseLogging()
        {
            this.diagProcessor.Executor.Value.Initialize(this.dummyFilePath);
            
            Assert.IsTrue(EqtTrace.IsVerboseEnabled);
            EqtTrace.TraceLevel = TraceLevel.Off;
        }

        private class TestableEnableDiagArgumentProcessor : EnableDiagArgumentProcessor
        {
            /// <inheritdoc/>
            public TestableEnableDiagArgumentProcessor(IFileHelper fileHelper)
                : base(fileHelper)
            {
            }
        }
    }
}