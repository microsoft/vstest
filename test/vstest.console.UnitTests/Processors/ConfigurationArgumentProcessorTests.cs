// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class ConfigurationArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnConfigurationArgumentProcessorCapabilities()
        {
            ConfigurationArgumentProcessor processor = new ConfigurationArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ConfigurationArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnConfigurationArgumentProcessorCapabilities()
        {
            ConfigurationArgumentProcessor processor = new ConfigurationArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ConfigurationArgumentExecutor);
        }

        #region ConfigurationArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            ConfigurationArgumentProcessorCapabilities capabilities = new ConfigurationArgumentProcessorCapabilities();
            Assert.AreEqual("/Configuration", capabilities.CommandName);
            Assert.AreEqual("-c|--Configuration|/c|/Configuration:<Configuration>\n     The configuration the project is built for i.e. Debug/Release", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.ConfigurationArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecuterInitializeWithNullOrEmptyConfigurationShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            ConfigurationArgumentExecutor executor = new ConfigurationArgumentExecutor(options);

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The given configuration is invalid.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithInvalidConfigurationShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            ConfigurationArgumentExecutor executor = new ConfigurationArgumentExecutor(options);

            try
            {
                executor.Initialize("Foo");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The given configuration is invalid.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithValidConfigurationShouldAddConfigurationToCommandLineOptions()
        {
            var options = CommandLineOptions.Instance;
            ConfigurationArgumentExecutor executor = new ConfigurationArgumentExecutor(options);
            
            executor.Initialize("Debug");
            Assert.AreEqual("Debug", options.Configuration);
        }

        [TestMethod]
        public void ExecutorExecuteReturnArgumentProcessorResultSuccess()
        {
            var executor = new ConfigurationArgumentExecutor(CommandLineOptions.Instance);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}
