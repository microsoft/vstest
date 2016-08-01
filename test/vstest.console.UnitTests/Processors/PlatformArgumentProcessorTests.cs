// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class PlatformArgumentProcessorTests
    {
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
        public void GetExecuterShouldReturnPlatformArgumentProcessorCapabilities()
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
            Assert.AreEqual("/Platform:<Platform type>\n      Target platform architecture to be used for test execution. \n      Valid values are x86, x64 and ARM.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.PlatformArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region PlatformArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize(null),
                "The /Platform argument requires the target platform type for the test run to be provided.   Example:  /Platform:x86");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsEmpty()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("  "),
                "The /Platform argument requires the target platform type for the test run to be provided.   Example:  /Platform:x86");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNotAnArchitecture()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("foo"),
                "Invalid platform type:{0}. Valid platform types are x86, x64 and Arm.",
                "foo");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNotASupportedArchitecture()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("AnyCPU"),
                "Invalid platform type:{0}. Valid platform types are x86, x64 and Arm.",
                "AnyCPU");
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsArchitecture()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            executor.Initialize("x64");
            Assert.AreEqual(ObjectModel.Architecture.X64, CommandLineOptions.Instance.TargetArchitecture);
        }

        [TestMethod]
        public void InitializeShouldNotConsiderCaseSensitivityOfTheArgumentPassed()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            executor.Initialize("ArM");
            Assert.AreEqual(ObjectModel.Architecture.ARM, CommandLineOptions.Instance.TargetArchitecture);
        }

        #endregion

        #region PlatformArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            var executor = new PlatformArgumentExecutor(CommandLineOptions.Instance);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        #endregion
    }
}
