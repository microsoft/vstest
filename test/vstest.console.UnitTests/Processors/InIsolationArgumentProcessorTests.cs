// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InIsolationArgumentProcessorTests
    {
        private InIsolationArgumentExecutor executor;

        [TestInitialize]
        public void Init()
        {
            this.executor = new InIsolationArgumentExecutor(CommandLineOptions.Instance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnInProcessArgumentProcessorCapabilities()
        {
            var processor = new InIsolationArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is InIsolationArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnInProcessArgumentExecutor()
        {
            var processor = new InIsolationArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is InIsolationArgumentExecutor);
        }

        [TestMethod]
        public void InIsolationArgumentProcessorMetadataShouldProvideAppropriateCapabilities()
        {
            var isolationProcessor = new InIsolationArgumentProcessor();
            Assert.IsFalse(isolationProcessor.Metadata.Value.AllowMultiple);
            Assert.IsFalse(isolationProcessor.Metadata.Value.AlwaysExecute);
            Assert.IsFalse(isolationProcessor.Metadata.Value.IsAction);
            Assert.IsFalse(isolationProcessor.Metadata.Value.IsSpecialCommand);
            Assert.AreEqual(InIsolationArgumentProcessor.CommandName, isolationProcessor.Metadata.Value.CommandName);
            Assert.AreEqual(null, isolationProcessor.Metadata.Value.ShortCommandName);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, isolationProcessor.Metadata.Value.Priority);
            Assert.AreEqual(HelpContentPriority.InIsolationArgumentProcessorHelpPriority, isolationProcessor.Metadata.Value.HelpPriority);
            Assert.AreEqual("--InIsolation|/InIsolation\n      Runs the tests in an isolated process. This makes vstest.console.exe \n      process less likely to be stopped on an error in the tests, but tests \n      may run slower.", isolationProcessor.Metadata.Value.HelpContentResourceName);
        }

        
        [TestMethod]
        public void InIsolationArgumentProcessorExecutorShouldThrowIfArgumentIsProvided()
        {
            // InProcess should not have any values or arguments
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("true"),
                "Argument " + "true" + " is not expected in the 'InIsolation' command. Specify the command without the argument (Example: vstest.console.exe myTests.dll /InIsolation) and try again.");
        }

        [TestMethod]
        public void InitializeShouldSetInIsolationValue()
        {
            this.executor.Initialize(null);
            Assert.IsTrue(CommandLineOptions.Instance.InIsolation, "InProcess option must be set to true.");
        }

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            Assert.AreEqual(ArgumentProcessorResult.Success, this.executor.Execute());
        }

    }
}