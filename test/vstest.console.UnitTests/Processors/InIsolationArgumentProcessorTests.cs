// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public class InIsolationArgumentProcessorTests
    {
        private readonly InIsolationArgumentProcessor isolationProcessor;

        public InIsolationArgumentProcessorTests()
        {
            this.isolationProcessor = new InIsolationArgumentProcessor();
        }

        [TestMethod]
        public void InIsolationArgumentProcessorMetadataShouldProvideAppropriateCapabilities()
        {
            Assert.IsFalse(this.isolationProcessor.Metadata.Value.AllowMultiple);
            Assert.IsFalse(this.isolationProcessor.Metadata.Value.AlwaysExecute);
            Assert.IsFalse(this.isolationProcessor.Metadata.Value.IsAction);
            Assert.IsFalse(this.isolationProcessor.Metadata.Value.IsSpecialCommand);
            Assert.AreEqual(InIsolationArgumentProcessor.CommandName, this.isolationProcessor.Metadata.Value.CommandName);
            Assert.AreEqual(null, this.isolationProcessor.Metadata.Value.ShortCommandName);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, this.isolationProcessor.Metadata.Value.Priority);
            Assert.AreEqual(HelpContentPriority.InIsolationArgumentProcessorHelpPriority, this.isolationProcessor.Metadata.Value.HelpPriority);
        }

        
        [TestMethod]
        public void InIsolationArgumentProcessorExecutorShouldThrowIfArgumentIsProvided()
        {
            Assert.ThrowsException<CommandLineException>(() => this.isolationProcessor.Executor.Value.Initialize("foo"));
        }

    }
}