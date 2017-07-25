// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DisableAutoFakesArgumentProcessorTests
    {
        private readonly DisableAutoFakesArgumentProcessor disableAutoFakesArgumentProcessor;

        public DisableAutoFakesArgumentProcessorTests()
        {
            this.disableAutoFakesArgumentProcessor = new DisableAutoFakesArgumentProcessor();
        }

        [TestMethod]
        public void DisableAutoFakesArgumentProcessorMetadataShouldProvideAppropriateCapabilities()
        {
            Assert.IsFalse(this.disableAutoFakesArgumentProcessor.Metadata.Value.AllowMultiple);
            Assert.IsFalse(this.disableAutoFakesArgumentProcessor.Metadata.Value.AlwaysExecute);
            Assert.IsFalse(this.disableAutoFakesArgumentProcessor.Metadata.Value.IsAction);
            Assert.IsFalse(this.disableAutoFakesArgumentProcessor.Metadata.Value.IsSpecialCommand);
            Assert.AreEqual(DisableAutoFakesArgumentProcessor.CommandName, this.disableAutoFakesArgumentProcessor.Metadata.Value.CommandName);
            Assert.AreEqual(null, this.disableAutoFakesArgumentProcessor.Metadata.Value.ShortCommandName);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, this.disableAutoFakesArgumentProcessor.Metadata.Value.Priority);
            Assert.AreEqual(HelpContentPriority.DisableAutoFakesArgumentProcessorHelpPriority, this.disableAutoFakesArgumentProcessor.Metadata.Value.HelpPriority);
        }


        [TestMethod]
        public void DisableAutoFakesArgumentProcessorExecutorShouldThrowIfArgumentIsNullOrEmpty()
        {
            Assert.ThrowsException<CommandLineException>(() => this.disableAutoFakesArgumentProcessor.Executor.Value.Initialize(string.Empty));
            Assert.ThrowsException<CommandLineException>(() => this.disableAutoFakesArgumentProcessor.Executor.Value.Initialize(" "));
        }

        [TestMethod]
        public void DisableAutoFakesArgumentProcessorExecutorShouldThrowIfArgumentIsNotBooleanString()
        {
            Assert.ThrowsException<CommandLineException>(() => this.disableAutoFakesArgumentProcessor.Executor.Value.Initialize("DisableAutoFakes"));
        }

        [TestMethod]
        public void DisableAutoFakesArgumentProcessorExecutorShouldSetCommandLineDisableAutoFakeValueAsPerArgumentProvided()
        {
            this.disableAutoFakesArgumentProcessor.Executor.Value.Initialize("true");
            Assert.AreEqual(CommandLineOptions.Instance.DisableAutoFakes, true);
        }
    }
}