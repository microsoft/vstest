// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;
    using vstest.console.UnitTests.Processors;

    [TestClass]
    public class InProcessArgumentProcessorTests
    {
        private InProcessArgumentExecutor executor;
        private TestableRunSettingsProvider runSettingsProvider;

        [TestInitialize]
        public void Init()
        {
            this.runSettingsProvider = new TestableRunSettingsProvider();
            this.executor = new InProcessArgumentExecutor(CommandLineOptions.Instance, this.runSettingsProvider);
        }
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnInProcessArgumentProcessorCapabilities()
        {
            var processor = new InProcessArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is InProcessArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnInProcessArgumentExecutor()
        {
            var processor = new InProcessArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is InProcessArgumentExecutor);
        }

        #region InProcessArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new InProcessArgumentProcessorCapabilities();
            Assert.AreEqual("/InProcess", capabilities.CommandName);
            Assert.AreEqual("--InProcess|/InProcess\n      Runs the tests in vstest.console.exe process.\n      This is supported for framework \".NETFramework,Version=v4.*\", Framework40 and Framework45.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.InProcessArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region InProcessArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNonNull()
        {
            // InProcess should not have any values or arguments
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("true"),
                "Argument " + "true" + " is not expected in the 'InProcess' command. Specify the command without the argument (Example: vstest.console.exe myTests.dll /InProcess) and try again.");
        }

        [TestMethod]
        public void InitializeShouldSetInProcessValue()
        {
            this.executor.Initialize(null);
            Assert.IsTrue(CommandLineOptions.Instance.InProcess, "InProcess option must be set to true.");
            Assert.AreEqual("true", this.runSettingsProvider.QueryRunSettingsNode(InProcessArgumentExecutor.RunSettingsPath));
        }

        #endregion

        #region InProcessArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            Assert.AreEqual(ArgumentProcessorResult.Success, this.executor.Execute());
        }

        #endregion
    }
}
