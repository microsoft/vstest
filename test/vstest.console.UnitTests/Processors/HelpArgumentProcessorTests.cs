// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HelpArgumentProcessorTests
    {
        /// <summary>
        /// The help argument processor get metadata should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetMetadataShouldReturnHelpArgumentProcessorCapabilities()
        {
            HelpArgumentProcessor processor = new HelpArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is HelpArgumentProcessorCapabilities);
        }

        /// <summary>
        /// The help argument processor get executer should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetExecuterShouldReturnHelpArgumentProcessorCapabilities()
        {
            HelpArgumentProcessor processor = new HelpArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is HelpArgumentExecutor);
        }

        #region HelpArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            HelpArgumentProcessorCapabilities capabilities = new HelpArgumentProcessorCapabilities();
            Assert.AreEqual("/?", capabilities.CommandName);
            Assert.AreEqual("/?\n      Display this usage message.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.HelpArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Help, capabilities.Priority);

            Assert.AreEqual(true, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion
   
        [TestMethod]
        public void ExecuterExecuteReturnArgumentProcessorResultAbort()
        {
            HelpArgumentExecutor executor = new HelpArgumentExecutor();
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Abort, result);
        }

        [TestMethod]
        public void ExecuterExecuteWritesAppropriateDataToConsole()
        {
            HelpArgumentExecutor executor = new HelpArgumentExecutor();
            var output = new DummyConsoleOutput();
            executor.Output = output;
            var result = executor.Execute();
            Assert.IsTrue(output.Lines.Contains("Usage: vstest.console.exe [TestFileNames] [Options]"));
            Assert.IsTrue(output.Lines.Contains("Options:"));
            Assert.IsTrue(output.Lines.Contains("Description: Runs tests from the specified files."));
            Assert.IsTrue(output.Lines.Contains("  To run tests in the same process:\n    >vstest.console.exe tests.dll \n  To run tests in a separate process:\n    >vstest.console.exe /inIsolation tests.dll\n  To run tests with additional settings such as  data collectors:\n    >vstest.console.exe  tests.dll /Settings:Local.RunSettings"));
        }
    }

    internal class DummyConsoleOutput : IOutput
    {
        /// <summary>
        /// The lines.
        /// </summary>
        internal List<string> Lines;

        public DummyConsoleOutput()
        {
            this.Lines = new List<string>();
        }

        public void WriteLine(string message, OutputLevel level)
        {
            this.Lines.Add(message);
        }

        public void Write(string message, OutputLevel level)
        {
            throw new System.NotImplementedException();
        }
    }
}
