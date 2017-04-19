// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EnableCodeCoverageArgumentProcessorTests
    {
        private TestableRunSettingsProvider settingsProvider;
        private EnableCodeCoverageArgumentExecutor executor;
        private const string DefaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        public EnableCodeCoverageArgumentProcessorTests()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
            this.executor = new EnableCodeCoverageArgumentExecutor(this.settingsProvider);
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
            //Assert.AreEqual("/EnableCodeCoverage\n      Enables data diagnostic adapter 'CodeCoverage' in the test run. Default \n      settings are used if not specified using settings file.", capabilities.HelpContentResourceName);
            //Assert.AreEqual(HelpContentPriority.EnableCodeCoverageArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region EnableCodeCoverageArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldCreateEntryForCodeCoverageInRunSettingsIfNotAlreadyPresent()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("Code Coverage");

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldEnableCodeCoverageIfDisabledInRunSettings()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("Code Coverage");

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldNotDisableOtherDataCollectors()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("Code Coverage");            

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldNotEnableOtherDataCollectors()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"Code Coverage\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("Code Coverage");

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Code Coverage\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion
    }
}
