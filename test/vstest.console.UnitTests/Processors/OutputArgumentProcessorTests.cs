// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Implementations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Processors;
    using System;
    using TestPlatform.CommandLine.Processors;
    using TestPlatform.Utilities.Helpers.Interfaces;

    [TestClass]
    public class OutputArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnOutputArgumentProcessorCapabilities()
        {
            OutputArgumentProcessor processor = new OutputArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is OutputArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnOutputArgumentProcessorCapabilities()
        {
            OutputArgumentProcessor processor = new OutputArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is OutputArgumentExecutor);
        }

        #region OutputArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            OutputArgumentProcessorCapabilities capabilities = new OutputArgumentProcessorCapabilities();
            Assert.AreEqual("/Output", capabilities.CommandName);
            Assert.AreEqual("-o|--Output|/o|/Output:<Output>\n     The directory containing the binaries to run.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.OutputArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecuterInitializeWithNullOrEmptyOutputShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            OutputArgumentExecutor executor = new OutputArgumentExecutor(options);

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(@"The Output path was not found, provide a valid path and try again.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithInvalidOutputShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            OutputArgumentExecutor executor = new OutputArgumentExecutor(options);

            try
            {
                executor.Initialize(@"C:\Foo.txt");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(@"The Output path was not found, provide a valid path and try again.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithValidOutputShouldAddOutputToCommandLineOptions()
        {
            var options = CommandLineOptions.Instance;
            OutputArgumentExecutor executor = new OutputArgumentExecutor(options);
            string testOutput = @"C:\OutputDir";
            var mockFileHelper = new MockFileHelper();
            mockFileHelper.ExistsInvoker = (path) =>
            {
                return string.Equals(path, testOutput);
            };
            executor.FileHelper = mockFileHelper;

            executor.Initialize(testOutput);
            Assert.AreEqual(testOutput, options.Output);
        }

        [TestMethod]
        public void ExecutorExecuteReturnArgumentProcessorResultSuccess()
        {
            var executor = new OutputArgumentExecutor(CommandLineOptions.Instance);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}
