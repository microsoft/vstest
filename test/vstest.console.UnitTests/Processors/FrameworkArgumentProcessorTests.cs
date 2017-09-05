// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;
    using vstest.console.UnitTests.Processors;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

    using ExceptionUtilities = Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.ExceptionUtilities;

    [TestClass]
    public class FrameworkArgumentProcessorTests
    {
        private FrameworkArgumentExecutor executor;
        private TestableRunSettingsProvider runSettingsProvider;

        [TestInitialize]
        public void Init()
        {
            this.runSettingsProvider = new TestableRunSettingsProvider();
            this.executor = new FrameworkArgumentExecutor(CommandLineOptions.Instance, runSettingsProvider);
        }
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
        public void GetExecuterShouldReturnFrameworkArgumentExecutor()
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
            StringAssert.Contains(capabilities.HelpContentResourceName, "Valid values are \".NETFramework,Version=v4.5.1\", \".NETCoreApp,Version=v1.0\"");

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

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize(null),
                "The /Framework argument requires the target .Net Framework version for the test run.   Example:  /Framework:\".NETFramework,Version=v4.5.1\"");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsEmpty()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize("  "),
                "The /Framework argument requires the target .Net Framework version for the test run.   Example:  /Framework:\".NETFramework,Version=v4.5.1\"");
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsInvalid()
        {
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => this.executor.Initialize("foo"),
                "Invalid .Net Framework version:{0}. Please give the fullname of the TargetFramework. Other supported .Net Framework versions are Framework35, Framework40, Framework45 and FrameworkCore10.",
                "foo");
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsAndRunSettingsFramework()
        {
            this.executor.Initialize(".NETCoreApp,Version=v1.0");
            Assert.AreEqual(".NETCoreApp,Version=v1.0", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
            Assert.AreEqual(".NETCoreApp,Version=v1.0", this.runSettingsProvider.QueryRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath));
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsFrameworkForOlderFrameworks()
        {
            this.executor.Initialize("Framework35");
            Assert.AreEqual(".NETFramework,Version=v3.5", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
            Assert.AreEqual(".NETFramework,Version=v3.5", this.runSettingsProvider.QueryRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath));
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsFrameworkForCaseInsensitiveFramework()
        {
            this.executor.Initialize(".netcoreApp,Version=v1.0");
            Assert.AreEqual(".netcoreApp,Version=v1.0", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
            Assert.AreEqual(".netcoreApp,Version=v1.0", this.runSettingsProvider.QueryRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath));
        }

        [TestMethod]
        public void InitializeShouldNotSetFrameworkIfSettingsFileIsLegacy()
        {
            this.runSettingsProvider.UpdateRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath, FrameworkVersion.Framework45.ToString());
            CommandLineOptions.Instance.SettingsFile = @"c:\tmp\settings.testsettings";
            this.executor.Initialize(".NETFramework,Version=v3.5");
            Assert.AreEqual(".NETFramework,Version=v3.5", CommandLineOptions.Instance.TargetFrameworkVersion.Name);
            Assert.AreEqual(FrameworkVersion.Framework45.ToString(), this.runSettingsProvider.QueryRunSettingsNode(FrameworkArgumentExecutor.RunSettingsPath));
        }

        #endregion

        #region FrameworkArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            Assert.AreEqual(ArgumentProcessorResult.Success, this.executor.Execute());
        }

        #endregion

    }
}
