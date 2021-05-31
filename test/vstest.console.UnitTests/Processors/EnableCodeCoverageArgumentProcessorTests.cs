// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class EnableCodeCoverageArgumentProcessorTests
    {
        private TestableRunSettingsProvider settingsProvider;
        private EnableCodeCoverageArgumentExecutor executor;

        private readonly string DefaultRunSettings = string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors >{0}</DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>");

        public EnableCodeCoverageArgumentProcessorTests()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
            this.executor = new EnableCodeCoverageArgumentExecutor(CommandLineOptions.Instance, this.settingsProvider,
                new Mock<IFileHelper>().Object);
            CollectArgumentExecutor.EnabledDataCollectors.Clear();
        }

        [TestMethod]
        public void GetMetadataShouldReturnEnableCodeCoverageArgumentProcessorCapabilities()
        {
            var processor = new EnableCodeCoverageArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is EnableCodeCoverageArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnEnableCodeCoverageArgumentProcessorCapabilities()
        {
            var processor = new EnableCodeCoverageArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is EnableCodeCoverageArgumentExecutor);
        }

        #region EnableCodeCoverageArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new EnableCodeCoverageArgumentProcessorCapabilities();

            Assert.AreEqual("/EnableCodeCoverage", capabilities.CommandName);
            Assert.IsFalse(capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.IsFalse(capabilities.AllowMultiple);
            Assert.IsFalse(capabilities.AlwaysExecute);
            Assert.IsFalse(capabilities.IsSpecialCommand);
        }

        #endregion

        #region EnableCodeCoverageArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldSetEnableCodeCoverageOfCommandLineOption()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            CommandLineOptions.Instance.EnableCodeCoverage = false;

            this.executor.Initialize(string.Empty);

            Assert.IsTrue(CommandLineOptions.Instance.EnableCodeCoverage,
                "/EnableCoverage should set CommandLineOption.EnableCodeCoverage to true");
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForCodeCoverageInRunSettingsIfNotAlreadyPresent()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize(string.Empty);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            var dataCollectorsFriendlyNames =
                XmlRunSettingsUtilities.GetDataCollectorsFriendlyName(this.settingsProvider.ActiveRunSettings
                    .SettingsXml);
            Assert.IsTrue(dataCollectorsFriendlyNames.Contains("Code Coverage"),
                "Code coverage setting in not available in runsettings");
        }

        [TestMethod]
        public void InitializeShouldEnableCodeCoverageIfDisabledInRunSettings()
        {
            var runsettingsString = string.Format(DefaultRunSettings,
                "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize(string.Empty);

            Assert.AreEqual(string.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <DataCollectionRunSettings>",
                "    <DataCollectors>",
                "      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />",
                "    </DataCollectors>",
                "  </DataCollectionRunSettings>",
                "</RunSettings>"), this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldNotDisableOtherDataCollectors()
        {
            CollectArgumentExecutor.EnabledDataCollectors.Add("mydatacollector1");
            var runsettingsString = string.Format(DefaultRunSettings,
                "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize(string.Empty);

            Assert.AreEqual(string.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <DataCollectionRunSettings>",
                "    <DataCollectors>",
                "      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />",
                "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
                "    </DataCollectors>",
                "  </DataCollectionRunSettings>",
                "</RunSettings>"), this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldNotEnableOtherDataCollectors()
        {
            var runsettingsString = string.Format(DefaultRunSettings,
                "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize(string.Empty);

            Assert.AreEqual(string.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <DataCollectionRunSettings>",
                "    <DataCollectors>",
                "      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />",
                "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />",
                "    </DataCollectors>",
                "  </DataCollectionRunSettings>",
                "</RunSettings>"), this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion
    }
}