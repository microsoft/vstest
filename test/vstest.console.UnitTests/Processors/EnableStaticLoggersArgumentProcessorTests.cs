// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EnableStaticLoggerArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnEnableLoggerArgumentProcessorCapabilities()
        {
            EnableStaticLoggersArgumentProcessor processor = new EnableStaticLoggersArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is EnableStaticLoggersArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnEnableStaticLoggersArgumentExecutor()
        {
            var processor = new EnableStaticLoggersArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is EnableStaticLoggersArgumentExecutor);
        }

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            EnableStaticLoggersArgumentProcessorCapabilities capabilities = new EnableStaticLoggersArgumentProcessorCapabilities();
            Assert.AreEqual("/EnableStaticLoggers", capabilities.CommandName);
            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Logging, capabilities.Priority);
        }

        public void ExecutorExecuteShouldReturnArgumentProcessorResultSuccess()
        {
            var executor = new EnableLoggerArgumentExecutor(null);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        // todo : Add test cases for NET451 and dotnet core
    }
}
