// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
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
        private string dummyFilePath = Path.Combine(Path.DirectorySeparatorChar.ToString(), $"{System.Guid.NewGuid()}", $"{System.Guid.NewGuid()}.txt");

        private readonly EnableDiagArgumentProcessor diagProcessor;

        private readonly Mock<IFileHelper> mockFileHelper;

        private TraceLevel traceLevel;
        private string traceFileName;

        public EnableDiagArgumentProcessorTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.diagProcessor = new TestableEnableDiagArgumentProcessor(this.mockFileHelper.Object);

            // Saving the EqtTrace state
#if NETFRAMEWORK
            traceLevel = EqtTrace.TraceLevel;
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            traceLevel = (TraceLevel)EqtTrace.TraceLevel;
            EqtTrace.TraceLevel = (PlatformTraceLevel)TraceLevel.Off;
#endif

            traceFileName = EqtTrace.LogFile;
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restoring to initial state for EqtTrace
            EqtTrace.InitializeTrace(traceFileName, PlatformTraceLevel.Verbose);
#if NETFRAMEWORK
            EqtTrace.TraceLevel = traceLevel;
#else
            EqtTrace.TraceLevel = (PlatformTraceLevel)traceLevel;
#endif
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorMetadataShouldProvideAppropriateCapabilities()
        {
            Assert.IsFalse(this.diagProcessor.Metadata.Value.AllowMultiple);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.AlwaysExecute);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.IsAction);
            Assert.IsFalse(this.diagProcessor.Metadata.Value.IsSpecialCommand);
            Assert.AreEqual(EnableDiagArgumentProcessor.CommandName, this.diagProcessor.Metadata.Value.CommandName);
            Assert.IsNull(this.diagProcessor.Metadata.Value.ShortCommandName);
            Assert.AreEqual(ArgumentProcessorPriority.Diag, this.diagProcessor.Metadata.Value.Priority);
            Assert.AreEqual(HelpContentPriority.EnableDiagArgumentProcessorHelpPriority, this.diagProcessor.Metadata.Value.HelpPriority);
            Assert.AreEqual(CommandLineResources.EnableDiagUsage, this.diagProcessor.Metadata.Value.HelpContentResourceName);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void EnableDiagArgumentProcessorExecutorThrowsIfDiagArgumentIsNullOrEmpty(string argument)
        {
            string exceptionMessage = string.Format(CultureInfo.CurrentUICulture, CommandLineResources.InvalidDiagArgument, argument);
            EnableDiagArgumentProcessorExecutorShouldThrowIfInvalidArgument(argument, exceptionMessage);
        }

        [TestMethod]
        public void EnableDiagArgumentProcessorExecutorDoesNotThrowsIfFileDotOpenThrow()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(Path.GetDirectoryName(this.dummyFilePath))).Returns(true);

            this.diagProcessor.Executor.Value.Initialize(this.dummyFilePath);
        }

        [TestMethod]
        [DataRow("abs;dfsdc.txt;verbosity=normal", "abs")] // ; in file path is not supported
        [DataRow("\"abst;dfsdc.txt\";verbosity=normal", "abst")] // Even though in escaped double quotes, semi colon is not supported in file path
        [DataRow("foo", "foo")]
        public void EnableDiagArgumentProcessorExecutorShouldThrowIfDirectoryPathIsProvided(string argument, string filePath)
        {
            var exceptionMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidDiagFilePath, filePath);

            EnableDiagArgumentProcessorExecutorShouldThrowIfInvalidArgument(argument, exceptionMessage);
        }

        [TestMethod]
        [DataRow("abc.txt;verbosity=normal=verbose")] // Multiple '=' in parameter
        [DataRow("abc.txt;verbosity;key1=value1")] // No '=' in parameter
        public void EnableDiagArgumentProcessorExecutorShouldThrowIfInvalidArgument(string argument)
        {
            string exceptionMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidDiagArgument, argument);
            EnableDiagArgumentProcessorExecutorShouldThrowIfInvalidArgument(argument, exceptionMessage);
        }

        [TestMethod]
        [DataRow("abc.txt")]
        [DataRow("abc.txt;verbosity=normal")]
        [DataRow("abc.txt;tracelevel=info;newkey=newvalue")]
        [DataRow("\"abc.txt\";verbosity=normal;newkey=newvalue")] //escaped double quotes are allowed for file path.
        [DataRow(";;abc.txt;;;;verbosity=normal;;;;")]
        public void EnableDiagArgumentProcessorExecutorShouldNotThrowIfValidArgument(string argument)
        {
            try
            {
                this.diagProcessor.Executor.Value.Initialize(argument);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        [DataRow("abc.txt;tracelevel=info;newkey=newvalue")]
        [DataRow("abc.txt;tracelevel=info;")]
        [DataRow("abc.txt;tracelevel=INfO")]
        [DataRow("abc.txt;traCELevel=info")]
        [DataRow("abc.txt;traCELevel=INfO")]
        public void EnableDiagArgumentProcessorExecutorShouldInitializeTraceWithCorrectTraceLevel(string argument)
        {
            // Setting any trace level  other than info.
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Verbose;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Verbose;
#endif

            this.diagProcessor.Executor.Value.Initialize(argument);

            Assert.AreEqual(TraceLevel.Info, (TraceLevel)EqtTrace.TraceLevel);
            Assert.IsTrue(EqtTrace.LogFile.Contains("abc.txt"));
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
        public void EnableDiagArgumentProcessorExecutorShouldDisableVerboseLoggingIfEqtTraceThowException()
        {
            this.mockFileHelper.Setup(fh => fh.DirectoryExists(Path.GetDirectoryName(this.dummyFilePath))).Returns(true);
            this.diagProcessor.Executor.Value.Initialize(this.dummyFilePath);

            Assert.IsTrue(!EqtTrace.IsVerboseEnabled);
#if NETFRAMEWORK
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            EqtTrace.TraceLevel = PlatformTraceLevel.Off;
#endif
        }

        private class TestableEnableDiagArgumentProcessor : EnableDiagArgumentProcessor
        {
            /// <inheritdoc/>
            public TestableEnableDiagArgumentProcessor(IFileHelper fileHelper)
                : base(fileHelper)
            {
            }
        }

        private void EnableDiagArgumentProcessorExecutorShouldThrowIfInvalidArgument(string argument, string exceptionMessage)
        {
            try
            {
                this.diagProcessor.Executor.Value.Initialize(argument);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType().Equals(typeof(CommandLineException)));
                Assert.IsTrue(e.Message.Contains(exceptionMessage));
            }
        }
    }
}