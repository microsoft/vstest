// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class EnableBlameArgumentProcessorTests
    {
        private Mock<IEnvironment> mockEnvronment;
        private Mock<IOutput> mockOutput;
        private TestableRunSettingsProvider settingsProvider;
        private EnableBlameArgumentExecutor executor;
        private const string DefaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors ></DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration><ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory></RunConfiguration>\r\n  </RunSettings>";

        public EnableBlameArgumentProcessorTests()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
            this.mockEnvronment = new Mock<IEnvironment>();
            this.mockOutput = new Mock<IOutput>();

            this.executor = new TestableEnableBlameArgumentExecutor(this.settingsProvider, this.mockEnvronment.Object, this.mockOutput.Object);
            CollectArgumentExecutor.EnabledDataCollectors.Clear();
        }

        [TestMethod]
        public void GetMetadataShouldReturnEnableBlameArgumentProcessorCapabilities()
        {
            var processor = new EnableBlameArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is EnableBlameArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnEnableBlameArgumentProcessorCapabilities()
        {
            var processor = new EnableBlameArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is EnableBlameArgumentExecutor);
        }

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new EnableBlameArgumentProcessorCapabilities();

            Assert.AreEqual("/Blame", capabilities.CommandName);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Logging, capabilities.Priority);
            Assert.AreEqual(HelpContentPriority.EnableDiagArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(CommandLineResources.EnableBlameUsage, capabilities.HelpContentResourceName);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForBlameInRunSettingsIfNotAlreadyPresent()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(DefaultRunSettings);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.executor.Initialize("");

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"blame\" enabled=\"True\">\r\n        <Configuration>\r\n          <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n        </Configuration>\r\n      </DataCollector>\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n  </RunConfiguration>\r\n  <LoggerRunSettings>\r\n    <Loggers>\r\n      <Logger friendlyName=\"blame\" enabled=\"True\" />\r\n    </Loggers>\r\n  </LoggerRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldWarnIfPlatformNotSupportedForCollectDumpOption()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(DefaultRunSettings);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.mockEnvronment.SetupSequence(x => x.OperatingSystem)
                               .Returns(PlatformOperatingSystem.Unix)
                               .Returns(PlatformOperatingSystem.Windows)
                               .Returns(PlatformOperatingSystem.Windows);

            this.mockEnvronment.SetupSequence(x => x.Architecture)
                               .Returns(PlatformArchitecture.ARM)
                               .Returns(PlatformArchitecture.ARM64);

            for (var i = 0; i < 3; i++)
            {
                this.executor.Initialize("collectdump");
                this.mockOutput.Verify(x => x.WriteLine(CommandLineResources.BlameCollectDumpNotSupportedForPlatform, OutputLevel.Warning));
            }
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForBlameAlongWithCollectDumpEntryIfEnabled()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(DefaultRunSettings);
            this.settingsProvider.SetActiveRunSettings(runsettings);

            this.mockEnvronment.Setup(x => x.OperatingSystem)
                               .Returns(PlatformOperatingSystem.Windows);
            this.mockEnvronment.Setup(x => x.Architecture)
                               .Returns(PlatformArchitecture.X64);

            this.executor.Initialize("CollectDump");

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"blame\" enabled=\"True\">\r\n        <Configuration>\r\n          <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n          <CollectDump>true</CollectDump>\r\n        </Configuration>\r\n      </DataCollector>\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n  </RunConfiguration>\r\n  <LoggerRunSettings>\r\n    <Loggers>\r\n      <Logger friendlyName=\"blame\" enabled=\"True\" />\r\n    </Loggers>\r\n  </LoggerRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        internal class TestableEnableBlameArgumentExecutor : EnableBlameArgumentExecutor
        {
            internal TestableEnableBlameArgumentExecutor(IRunSettingsProvider runSettingsManager, IEnvironment environment, IOutput output)
                : base(runSettingsManager, environment)
            {
                this.Output = output;
            }
        }
    }
}
