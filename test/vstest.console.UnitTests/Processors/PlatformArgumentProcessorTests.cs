// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;
    using vstest.console.UnitTests.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using ExceptionUtilities = Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.ExceptionUtilities;
    using System;

    [TestClass]
    public class PlatformArgumentProcessorTests
    {
        private PlatformArgumentExecutor executor;
        private TestableRunSettingsProvider runSettingsProvider;

        [TestInitialize]
        public void Init()
        {
            this.runSettingsProvider = new TestableRunSettingsProvider();
            this.executor = new PlatformArgumentExecutor(CommandLineOptions.Instance, runSettingsProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnPlatformArgumentProcessorCapabilities()
        {
            var processor = new PlatformArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is PlatformArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnPlatformArgumentExecutor()
        {
            var processor = new PlatformArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is PlatformArgumentExecutor);
        }

        #region PlatformArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new PlatformArgumentProcessorCapabilities();
            Assert.AreEqual("/Platform", capabilities.CommandName);
            var expected = "--Platform|/Platform:<Platform type>\r\n      Target platform architecture to be used for test execution. \r\n      Valid values are x86, x64 and ARM.";
            Assert.AreEqual(expected: expected.NormalizeLineEndings().ShowWhiteSpace(), capabilities.HelpContentResourceName.NormalizeLineEndings().ShowWhiteSpace());

            Assert.AreEqual(HelpContentPriority.PlatformArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.IsFalse(capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.IsFalse(capabilities.AllowMultiple);
            Assert.IsFalse(capabilities.AlwaysExecute);
            Assert.IsFalse(capabilities.IsSpecialCommand);
        }

        #endregion

        #region PlatformArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize(null),
                "The /Platform argument requires the target platform type for the test run to be provided.   Example:  /Platform:x86");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsEmpty()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("  "),
                "The /Platform argument requires the target platform type for the test run to be provided.   Example:  /Platform:x86");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNotAnArchitecture()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("foo"),
                "Invalid platform type:{0}. Valid platform types are x86, x64 and Arm.",
                "foo");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNotASupportedArchitecture()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("AnyCPU"),
                "Invalid platform type:{0}. Valid platform types are x86, x64 and Arm.",
                "AnyCPU");
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsArchitecture()
        {
            this.executor.Initialize("x64");
            Assert.AreEqual(ObjectModel.Architecture.X64, CommandLineOptions.Instance.TargetArchitecture);
            Assert.AreEqual(nameof(ObjectModel.Architecture.X64), this.runSettingsProvider.QueryRunSettingsNode(PlatformArgumentExecutor.RunSettingsPath));
        }

        [TestMethod]
        public void InitializeShouldNotConsiderCaseSensitivityOfTheArgumentPassed()
        {
            executor.Initialize("ArM");
            Assert.AreEqual(ObjectModel.Architecture.ARM, CommandLineOptions.Instance.TargetArchitecture);
            Assert.AreEqual(nameof(ObjectModel.Architecture.ARM), this.runSettingsProvider.QueryRunSettingsNode(PlatformArgumentExecutor.RunSettingsPath));
        }

        #endregion

        #region PlatformArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            Assert.AreEqual(ArgumentProcessorResult.Success, this.executor.Execute());
        }

        #endregion
    }
}
