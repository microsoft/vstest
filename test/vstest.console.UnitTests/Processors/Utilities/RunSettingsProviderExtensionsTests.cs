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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using vstest.console.UnitTests.Processors;

    [TestClass]
    public class RunSettingsProviderExtensionsTests
    {
        private const string DefaultRunSettingsTemplate =
            "<RunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>%ResultsDirectory%</ResultsDirectory>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n    <TargetFrameworkVersion>%DefaultFramework%</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n</RunSettings>";
        private IRunSettingsProvider runSettingsProvider;

        [TestInitialize]
        public void Init()
        {
            runSettingsProvider = new TestableRunSettingsProvider();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateGivenSettingsXml()
        {
            const string runSettingsXml = "<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>";

            this.runSettingsProvider.UpdateRunSettings(runSettingsXml);

            StringAssert.Contains(this.runSettingsProvider.ActiveRunSettings.SettingsXml, runSettingsXml);
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrownExceptionIfRunSettingsProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => RunSettingsProviderExtensions.UpdateRunSettings(null, "<RunSettings></RunSettings>"));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrownExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => this.runSettingsProvider.UpdateRunSettings(null));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrownExceptionIfSettingsXmlIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => this.runSettingsProvider.UpdateRunSettings("  "));
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldSetDefaultSettingsForEmptySettings()
        {
            this.runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(this.runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
            Assert.AreEqual(runConfiguration.TargetPlatform, Constants.DefaultPlatform);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldAddUnspecifiedSettings()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>");

            this.runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(this.runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldNotChangeSpecifiedSettings()
        {

            this.runSettingsProvider.UpdateRunSettings("<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X64</TargetPlatform>\r\n  </RunConfiguration>\r\n</RunSettings>");

            this.runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(this.runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.TargetPlatform, Architecture.X64);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldThrowExceptionIfArgumentIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => RunSettingsProviderExtensions.AddDefaultRunSettings(null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNode(null, "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNode("  ", "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfDataIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNode("Key", null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfRunSettingsProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => RunSettingsProviderExtensions.UpdateRunSettingsNode(null, "Key", "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldAddNewKeyIfNotPresent()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>  <RunConfiguration> </RunConfiguration>  </RunSettings>");
            this.runSettingsProvider.UpdateRunSettingsNode("Key.Path", "data");

            Assert.AreEqual("data", this.runSettingsProvider.QueryRunSettingsNode("Key.Path"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldUpdateKeyIfAlreadyPresent()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>  <RunConfiguration> <MaxCpuCount>1</MaxCpuCount></RunConfiguration>  </RunSettings>");
            this.runSettingsProvider.UpdateRunSettingsNode("RunConfiguration.MaxCpuCount", "0");
            Assert.AreEqual("0", this.runSettingsProvider.QueryRunSettingsNode("RunConfiguration.MaxCpuCount"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNodeInnerXml(null, "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNodeInnerXml("  ", "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.UpdateRunSettingsNodeInnerXml("Key", null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfRunSettingsProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => RunSettingsProviderExtensions.UpdateRunSettingsNodeInnerXml(null, "Key", "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldAddNewKeyIfNotPresent()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>  <RunConfiguration> </RunConfiguration>  </RunSettings>");
            this.runSettingsProvider.UpdateRunSettingsNodeInnerXml("Key.Path", "<myxml>myxml</myxml>");

            Assert.AreEqual("myxml", this.runSettingsProvider.QueryRunSettingsNode("Key.Path"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldUpdateKeyIfAlreadyPresent()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>  <RunConfiguration> <MaxCpuCount>1</MaxCpuCount></RunConfiguration>  </RunSettings>");
            this.runSettingsProvider.UpdateRunSettingsNodeInnerXml("RunConfiguration", "<MaxCpuCount>0</MaxCpuCount>");
            Assert.AreEqual("0", this.runSettingsProvider.QueryRunSettingsNode("RunConfiguration.MaxCpuCount"));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldThrowIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.QueryRunSettingsNode(null));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldThrowIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.runSettingsProvider.QueryRunSettingsNode("  "));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldReturnNullForNotExistKey()
        {
            Assert.IsNull(this.runSettingsProvider.QueryRunSettingsNode("RunConfiguration.TargetPlatform"));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldReturnCorrectValue()
        {
            this.runSettingsProvider.UpdateRunSettings("<RunSettings>  <RunConfiguration> <TargetPlatform>x86</TargetPlatform></RunConfiguration>  </RunSettings>");
            Assert.AreEqual("x86", this.runSettingsProvider.QueryRunSettingsNode("RunConfiguration.TargetPlatform"));
        }
    }
}
