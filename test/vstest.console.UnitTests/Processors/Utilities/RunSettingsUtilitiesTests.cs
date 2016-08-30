// Copyright(c) Microsoft.All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ObjectModel;

    [TestClass]
    public class RunSettingsUtilitiesTests
    {
        private const string DefaultRunSettingsTemplate =
            "<RunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>%ResultsDirectory%</ResultsDirectory>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n    <TargetFrameworkVersion>.NETFramework,Version=v4.6</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n</RunSettings>";

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnDefaultRunSettingsIfProviderIsNull()
        {
            var runSettings = RunSettingsUtilities.GetRunSettings(null, null);

            Assert.AreEqual(this.GetDefaultRunSettings(), runSettings);
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnDefaultRunSettingsIfActiveRunSettingsIsNull()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(runSettings: null);

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, null);

            Assert.AreEqual(this.GetDefaultRunSettings(), runSettings);
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnRunSettingsFromTheProvider()
        {
            var settingsProvider = new TestableRunSettingsProvider();

            var settings = "<RunSettings>\r\n  <RunConfiguration>\r\n    <RandomNumer>432423</RandomNumer>\r\n  </RunConfiguration>\r\n</RunSettings>";
            settingsProvider.SetActiveRunSettings(settings);

            var receivedRunSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, null);

            StringAssert.Contains(receivedRunSettings, "<RandomNumer>432423</RandomNumer>");
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnSettingsWithPlatformSpecifiedInCommandLineOptions()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(runSettings: null);

            CommandLineOptions.Instance.TargetArchitecture = ObjectModel.Architecture.X64;

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, CommandLineOptions.Instance);

            var defaultRunSettings = this.GetDefaultRunSettings();
            //Replace with the platform specified.
            var expectedSettings = defaultRunSettings.Replace("X86", "X64");
            Assert.AreEqual(expectedSettings, runSettings);
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnSettingsWithFrameworkSpecifiedInCommandLineOptions()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(runSettings: null);

            CommandLineOptions.Instance.TargetFrameworkVersion = Framework.FromString(".NETFramework,Version=v3.5");

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, CommandLineOptions.Instance);

            var defaultRunSettings = this.GetDefaultRunSettings();
            
            //Replace with the framework specified.
            var expectedSettings = defaultRunSettings.Replace("Version=v4.6", "Version=v3.5");
            Assert.AreEqual(expectedSettings, runSettings);
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnSettingsWithoutParallelOptionWhenParallelIsOff()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(runSettings: null);

            // Do not have to explicitly set - but for readability
            CommandLineOptions.Instance.Parallel = false;

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, CommandLineOptions.Instance);

            Assert.IsTrue(!runSettings.Contains("MaxCpuCount"), "MaxCpuCount must not be set if parallel setting is false.");
        }


        [TestMethod]
        public void GetRunSettingsShouldReturnSettingsWithParallelOptionWhenParallelIsOn()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(runSettings: null);

            CommandLineOptions.Instance.Parallel = true;

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, CommandLineOptions.Instance);
            StringAssert.Contains(runSettings, "<MaxCpuCount>0</MaxCpuCount>", "MaxCpuCount must be set to 0 if Parallel Enabled.");
        }

        [TestMethod]
        public void GetRunSettingsShouldReturnWithoutChangeIfUserProvidesBothParallelSwitchAndSettings()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.SetActiveRunSettings(settingXml);

            CommandLineOptions.Instance.Parallel = true;

            var runSettings = RunSettingsUtilities.GetRunSettings(settingsProvider, CommandLineOptions.Instance);

            var parallelValue = Environment.ProcessorCount;
            StringAssert.Contains(runSettings, "<MaxCpuCount>2</MaxCpuCount>", "RunSettings Parallel value should take precendence over parallel switch.");
        }


        #region Testable Implementations

        private string GetDefaultRunSettings()
        {
            var defaultResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
            return DefaultRunSettingsTemplate.Replace("%ResultsDirectory%", defaultResultsDirectory);
        }

        private class TestableRunSettingsProvider : IRunSettingsProvider
        {
            public RunSettings ActiveRunSettings { get; private set; }

            public void SetActiveRunSettings(RunSettings runSettings)
            {
                this.ActiveRunSettings = runSettings;
            }

            public void SetActiveRunSettings(string settingsXml)
            {
                var runSettings = new RunSettings();
                runSettings.LoadSettingsXml(settingsXml);

                SetActiveRunSettings(runSettings);
            }
        }

        #endregion
    }
}
