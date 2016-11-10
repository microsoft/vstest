// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class FrameworkArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnFrameworkArgumentProcessorCapabilities()
        {
            var processor = new FrameworkArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is FrameworkArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnFrameworkArgumentProcessorCapabilities()
        {
            var processor = new FrameworkArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is FrameworkArgumentExecutor);
        }

        #region FrameworkArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new FrameworkArgumentProcessorCapabilities();
            Assert.AreEqual("/Framework", capabilities.CommandName);
            StringAssert.Contains(capabilities.HelpContentResourceName, "Valid values are \".NETFramework,Version=v4.6\", \".NETCoreApp,Version=v1.0\"");

            Assert.AreEqual(HelpContentPriority.FrameworkArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region FrameworkArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize(null),
                "The /Framework argument requires the target .Net Framework version for the test run.   Example:  /Framework:\".NETFramework,Version=v4.6\"");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsEmpty()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("  "),
                "The /Framework argument requires the target .Net Framework version for the test run.   Example:  /Framework:\".NETFramework,Version=v4.6\"");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsInvalid()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("foo"),
                "Invalid .Net Framework version:{0}. Please give the fullname of the TargetFramework. Other supported .Net Framework versions are Framework35, Framework40 and Framework45.",
                "foo");
        }
        
        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsFramework()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            executor.Initialize(".NETCoreApp,Version=v1.0");
            Assert.AreEqual(".NETCoreApp,Version=v1.0", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsFrameworkForOlderFrameworks()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            executor.Initialize("Framework35");
            Assert.AreEqual(".NETFramework,Version=v3.5", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsFrameworkForCaseInsensitiveFramework()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            executor.Initialize(".netcoreApp,Version=v1.0");
            Assert.AreEqual(".netcoreApp,Version=v1.0", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
        }

        #endregion

        #region FrameworkArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            var executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        #endregion
    }
}
