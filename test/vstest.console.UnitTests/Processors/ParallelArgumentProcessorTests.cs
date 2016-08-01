// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class ParallelArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnParallelArgumentProcessorCapabilities()
        {
            var processor = new ParallelArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ParallelArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnPlatformArgumentProcessorCapabilities()
        {
            var processor = new ParallelArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ParallelArgumentExecutor);
        }

        #region ParallelArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new ParallelArgumentProcessorCapabilities();
            Assert.AreEqual("/Parallel", capabilities.CommandName);
            Assert.AreEqual("/Parallel\nSpecifies that the tests be executed in parallel. By default up to all available cores on the machine may be used. The number of cores to use may be configured using a settings file.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.ParallelArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region ParallelArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNonNull()
        {
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance);

            // Parallel should not have any values or arguments
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("123"),
                "Argument " + 123 + " is not expected in the 'Parallel' command. Specify the command without the argument (Example: vstest.console.exe myTests.dll /Parallel) and try again.");
        }

        [TestMethod]
        public void InitializeShouldSetParallelValue()
        {
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance);
            executor.Initialize(null);
            Assert.IsTrue(CommandLineOptions.Instance.Parallel, "Parallel option must be set to true.");
        }

        #endregion

        #region ParallelArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        #endregion
    }
}
