// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;

    [TestClass]
    public class ParallelArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
            RunSettingsManager.Instance = null;
        }

        [TestMethod]
        public void GetMetadataShouldReturnParallelArgumentProcessorCapabilities()
        {
            var processor = new ParallelArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ParallelArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnParallelArgumentExecutor()
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
            Assert.AreEqual("--Parallel|/Parallel\n      Specifies that the tests be executed in parallel. By default up\n      to all available cores on the machine may be used.\n      The number of cores to use may be configured using a settings file.", capabilities.HelpContentResourceName);

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
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            // Parallel should not have any values or arguments
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("123"),
                "Argument " + 123 + " is not expected in the 'Parallel' command. Specify the command without the argument (Example: vstest.console.exe myTests.dll /Parallel) and try again.");
        }

        [TestMethod]
        public void InitializeShouldSetParallelValue()
        {
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);
            executor.Initialize(null);
            Assert.IsTrue(CommandLineOptions.Instance.Parallel, "Parallel option must be set to true.");
            Assert.AreEqual("0", RunSettingsUtilities.QueryRunSettingsNode(RunSettingsManager.Instance, ParallelArgumentExecutor.RunSettingsPath));
        }

        #endregion

        #region ParallelArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            var executor = new ParallelArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        #endregion
    }
}
