// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CLIRunSettingsArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new CLIRunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is CLIRunSettingsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new CLIRunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is CLIRunSettingsArgumentExecutor);
        }

        #region CLIRunSettingsArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new CLIRunSettingsArgumentProcessorCapabilities();

            Assert.AreEqual("--", capabilities.CommandName);
            Assert.AreEqual("RunSettings Args:\n      Any runsettings parameter(s) that should be passed (in key value format)", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.CLIRunSettingsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.CLIRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region CLIRunSettingsArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsNull()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(null);
            executor.Initialize(null);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsWhiteSpace()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(" ");

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsEmpty()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor=new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(string.Empty);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldSetActiveRunSettings()
        {
            var args = "MSTest.DeploymentEnabled False";
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreKeyIfValueIsNotPassed()
        {
            var args = "MSTest.DeploymentEnabled False MSTest1";
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldAddNodeIfNotPresent()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            var defaultSettingsXml = ((XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings()).OuterXml;
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled False ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldOverriteValueIfNotAlreadyExists()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var defaultSettingsXml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>";
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled True  ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>True</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldHandleEmptyStringsPassedAsArguments()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            var defaultSettingsXml = ((XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings()).OuterXml;
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled False   ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion

        #region private
        private class TestableRunSettingsProvider : IRunSettingsProvider
        {
            public RunSettings ActiveRunSettings
            {
                get;
                set;
            }

            public void SetActiveRunSettings(RunSettings runSettings)
            {
                this.ActiveRunSettings = runSettings;
            }
        }
        #endregion
    }
}
