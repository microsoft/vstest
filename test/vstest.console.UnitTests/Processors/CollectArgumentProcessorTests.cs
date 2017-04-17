// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
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
        public void GetMetadataShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new CollectArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is CollectArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunSettingsArgumentProcessorCapabilities()
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
            Assert.AreEqual("--Collect|/Collect:<DataCollector FriendlyName>\n      Enables data diagnostic adapter(s) in the test run. Default \n      settings are used if not specified using settings file.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.CollectArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Collect, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region CollectArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldNotThrowIfArguemntIsNull()
        {
            this.executor.Initialize(null);
            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowIfArgumentIsEmpty()
        {
            this.executor.Initialize(string.Empty);
            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowIfArgumentIsWhiteSpace()
        {
            this.executor.Initialize(" ");
            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForDataCollectorInRunSettingsIfNotAlreadyPresent()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" />");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runsettingsString);
            this.executor.Initialize("MyDataCollector");

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>D:\\Code\\github\\harshjain2\\vstest\\test\\vstest.console.UnitTests\\bin\\Debug\\net46\\TestResults</ResultsDirectory>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n    <TargetFrameworkVersion>.NETFramework,Version=v4.6</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
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
