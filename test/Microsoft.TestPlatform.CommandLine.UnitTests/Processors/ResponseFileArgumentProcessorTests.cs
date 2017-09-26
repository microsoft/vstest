// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class ResponseFileArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnResponseFileArgumentProcessorCapabilities()
        {
            var processor = new ResponseFileArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ResponseFileArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnNull()
        {
            var processor = new ResponseFileArgumentProcessor();
            Assert.IsNull(processor.Executor);
        }

        #region ResponseFileArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new ResponseFileArgumentProcessorCapabilities();
            Assert.AreEqual("@", capabilities.CommandName);
            StringAssert.Contains(capabilities.HelpContentResourceName, "Read response file for more options");

            Assert.AreEqual(HelpContentPriority.ResponseFileArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(true, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(true, capabilities.IsSpecialCommand);
        }

        #endregion
    }
}
