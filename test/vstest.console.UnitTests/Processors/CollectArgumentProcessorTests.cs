// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CollectArgumentProcessorTests
    {
        private TestableRunSettingsProvider settingsProvider;
        private CollectArgumentExecutor executor;
        private const string DefaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        public CollectArgumentProcessorTests()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
            this.executor = new CollectArgumentExecutor(this.settingsProvider);
        }

        [TestMethod]
        public void GetMetadataShouldReturnCollectArgumentProcessorCapabilities()
        {
            var processor = new CollectArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is CollectArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnCollectArgumentProcessorCapabilities()
        {
            var processor = new CollectArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is CollectArgumentExecutor);
        }

        #region CollectArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new CollectArgumentProcessorCapabilities();

            Assert.AreEqual("/Collect", capabilities.CommandName);
            Assert.AreEqual("--Collect|/Collect:<DataCollector FriendlyName>\n      Enables data diagnostic adapter(s) in the test run. Default \n      settings are used if not specified using settings file. More info here : https://aka.ms/vstest-collect", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.CollectArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region CollectArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldThrowIfArguemntIsNull()
        {
            Assert.ThrowsException<CommandLineException>(() => { this.executor.Initialize(null); });
        }

        [TestMethod]
        public void InitializeShouldNotThrowIfArgumentIsEmpty()
        {
            Assert.ThrowsException<CommandLineException>(() => { this.executor.Initialize(String.Empty); });
        }

        [TestMethod]
        public void InitializeShouldNotThrowIfArgumentIsWhiteSpace()
        {
            Assert.ThrowsException<CommandLineException>(() => { this.executor.Initialize(" "); });
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForDataCollectorInRunSettingsIfNotAlreadyPresent()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("MyDataCollector");

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldEnableDataCollectorIfDisabledInRunSettings()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("MyDataCollector");

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldDisableOtherDataCollectors()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("MyDataCollector");

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldEnableMultipleCollectors()
        {
            var runsettingsString = string.Format(DefaultRunSettings, string.Empty);
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.settingsProvider.SetActiveRunSettings(runsettings);
            this.executor.Initialize("MyDataCollector;MyDataCollector1");

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion
    }
}
