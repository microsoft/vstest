// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using ObjectModel;

    [TestClass]
    public class RunSettingsUtilitiesTests
    {
        private const string DefaultRunSettingsTemplate =
            "<RunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>%ResultsDirectory%</ResultsDirectory>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n    <TargetFrameworkVersion>%DefaultFramework%</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n</RunSettings>";

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
            RunSettingsManager.Instance = null;
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateGivenSettingsXml()
        {
            var runSettingsManager = RunSettingsManager.Instance;
            const string runSettingsXml = "<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>";

            RunSettingsUtilities.UpdateRunSettings(runSettingsManager, runSettingsXml);

            StringAssert.Contains(runSettingsManager.ActiveRunSettings.SettingsXml, runSettingsXml);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldSetDefaultSettingsForEmptySettings()
        {
            var runSettingsManager = RunSettingsManager.Instance;

            RunSettingsUtilities.AddDefaultRunSettings(runSettingsManager);

            RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
            Assert.AreEqual(runConfiguration.TargetPlatform, Constants.DefaultPlatform);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldAddUnspecifiedSettings()
        {
            var runSettingsManager = RunSettingsManager.Instance;
            RunSettingsUtilities.UpdateRunSettings(runSettingsManager, "<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>");

            RunSettingsUtilities.AddDefaultRunSettings(runSettingsManager);

            RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldNotChangeSpecifiedSettings()
        {
            var runSettingsManager = RunSettingsManager.Instance;
            RunSettingsUtilities.UpdateRunSettings(runSettingsManager, "<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X64</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>");

            RunSettingsUtilities.AddDefaultRunSettings(runSettingsManager);

            RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.TargetPlatform, Architecture.X64);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldThrowExceptionIfArgumentIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => RunSettingsUtilities.AddDefaultRunSettings(null));
        }

        #region Testable Implementations

        private string GetDefaultRunSettings()
        {
            var defaultResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
            return DefaultRunSettingsTemplate.Replace("%ResultsDirectory%", defaultResultsDirectory).Replace("%DefaultFramework%", Framework.DefaultFramework.Name);
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
