// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using System.Text.RegularExpressions;

    [TestClass]
    public class RunSettingsProviderExtensionsTests
    {
        private IRunSettingsProvider runSettingsProvider;

        [TestInitialize]
        public void Init()
        {
            runSettingsProvider = new TestableRunSettingsProvider();
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateGivenSettingsXml()
        {
            string runSettingsXml = string.Join(Environment.NewLine,
                "<RunSettings>",
                "  <RunConfiguration>",
                "    <TargetPlatform>X86</TargetPlatform>",
                "  </RunConfiguration>",
                "</RunSettings>");

            runSettingsProvider.UpdateRunSettings(runSettingsXml);

            StringAssert.Contains(runSettingsProvider.ActiveRunSettings.SettingsXml, runSettingsXml);
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
                () => runSettingsProvider.UpdateRunSettings(null));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrownExceptionIfSettingsXmlIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => runSettingsProvider.UpdateRunSettings("  "));
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldSetDefaultSettingsForEmptySettings()
        {
            runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration =
                XmlRunSettingsUtilities.GetRunConfigurationNode(runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFramework.ToString(), Framework.DefaultFramework.ToString());
            Assert.AreEqual(runConfiguration.TargetPlatform, Constants.DefaultPlatform);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldAddUnspecifiedSettings()
        {
            runSettingsProvider.UpdateRunSettings(string.Join(Environment.NewLine,
                "<RunSettings>",
                "  <RunConfiguration>",
                "    <TargetPlatform>X86</TargetPlatform>",
                "  </RunConfiguration>",
                "</RunSettings>"));

            runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration =
                XmlRunSettingsUtilities.GetRunConfigurationNode(runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFramework.ToString(), Framework.DefaultFramework.ToString());
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldNotChangeSpecifiedSettings()
        {
            runSettingsProvider.UpdateRunSettings(string.Join(Environment.NewLine,
                "<RunSettings>",
                "  <RunConfiguration>",
                "    <TargetPlatform>X64</TargetPlatform> </RunConfiguration>",
                "</RunSettings>"));

            runSettingsProvider.AddDefaultRunSettings();

            var runConfiguration =
                XmlRunSettingsUtilities.GetRunConfigurationNode(runSettingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.TargetPlatform, Architecture.X64);
        }

        [TestMethod]
        public void AddDefaultRunSettingsShouldThrowExceptionIfArgumentIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                RunSettingsProviderExtensions.AddDefaultRunSettings(null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNode(null, "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNode("  ", "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfDataIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNode("Key", null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldThrowExceptionIfRunSettingsProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                RunSettingsProviderExtensions.UpdateRunSettingsNode(null, "Key", "data"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldAddNewKeyIfNotPresent()
        {
            runSettingsProvider.UpdateRunSettings(
                "<RunSettings>  <RunConfiguration> </RunConfiguration>  </RunSettings>");
            runSettingsProvider.UpdateRunSettingsNode("Key.Path", "data");

            Assert.AreEqual("data", runSettingsProvider.QueryRunSettingsNode("Key.Path"));
        }

        [TestMethod]
        public void UpdateTestRunParameterSettingsNodeShouldAddNewKeyIfNotPresent()
        {
            CheckRunSettingsAreUpdated("weburl", @"http://localhost//abc");
        }

        [TestMethod]
        public void UpdateTetsRunParameterSettingsNodeShouldOverrideValueIfKeyIsAlreadyPresent()
        {
            var runSettingsWithTestRunParameters = string.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <TestRunParameters>",
                "    <Parameter name=\"weburl\" value=\"http://localhost//abc\" />",
                "  </TestRunParameters>",
                "</RunSettings>");
            var runSettingsWithTestRunParametersOverrode = string.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <TestRunParameters>",
                "    <Parameter name=\"weburl\" value=\"http://localhost//def\" />",
                "  </TestRunParameters>",
                "</RunSettings>");

            runSettingsProvider.UpdateRunSettings(runSettingsWithTestRunParameters);
            var match = runSettingsProvider.GetTestRunParameterNodeMatch(
                "TestRunParameters.Parameter(name=\"weburl\",value=\"http://localhost//def\")");
            runSettingsProvider.UpdateTestRunParameterSettingsNode(match);

            Assert.AreEqual(runSettingsWithTestRunParametersOverrode,
                runSettingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void TestRunParameterSettingsNodeCanContainSpecialCharacters()
        {
            CheckRunSettingsAreUpdated("weburl:name", @"http://localhost//abc");
        }

        [TestMethod]
        public void TestRunParameterSettingsNodeCanBeJustASingleCharacter()
        {
            CheckRunSettingsAreUpdated("a", @"http://localhost//abc");
        }

        [TestMethod]
        public void TestRunParameterSettingsNodeCanMixSpecialCharacters()
        {
            CheckRunSettingsAreUpdated("___this_Should:be-valid.2", @"http://localhost//abc");
        }

        [TestMethod]
        public void UpdateRunSettingsNodeShouldUpdateKeyIfAlreadyPresent()
        {
            runSettingsProvider.UpdateRunSettings(
                "<RunSettings>  <RunConfiguration> <MaxCpuCount>1</MaxCpuCount></RunConfiguration>  </RunSettings>");
            runSettingsProvider.UpdateRunSettingsNode("RunConfiguration.MaxCpuCount", "0");
            Assert.AreEqual("0", runSettingsProvider.QueryRunSettingsNode("RunConfiguration.MaxCpuCount"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNodeInnerXml(null, "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNodeInnerXml("  ", "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                runSettingsProvider.UpdateRunSettingsNodeInnerXml("Key", null));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldThrowExceptionIfRunSettingsProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                RunSettingsProviderExtensions.UpdateRunSettingsNodeInnerXml(null, "Key", "<myxml/>"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldAddNewKeyIfNotPresent()
        {
            runSettingsProvider.UpdateRunSettings(
                "<RunSettings>  <RunConfiguration> </RunConfiguration>  </RunSettings>");
            runSettingsProvider.UpdateRunSettingsNodeInnerXml("Key.Path", "<myxml>myxml</myxml>");

            Assert.AreEqual("myxml", runSettingsProvider.QueryRunSettingsNode("Key.Path"));
        }

        [TestMethod]
        public void UpdateRunSettingsNodeInnerXmlShouldUpdateKeyIfAlreadyPresent()
        {
            runSettingsProvider.UpdateRunSettings(
                "<RunSettings>  <RunConfiguration> <MaxCpuCount>1</MaxCpuCount></RunConfiguration>  </RunSettings>");
            runSettingsProvider.UpdateRunSettingsNodeInnerXml("RunConfiguration", "<MaxCpuCount>0</MaxCpuCount>");
            Assert.AreEqual("0", runSettingsProvider.QueryRunSettingsNode("RunConfiguration.MaxCpuCount"));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldThrowIfKeyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => runSettingsProvider.QueryRunSettingsNode(null));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldThrowIfKeyIsEmptyOrWhiteSpace()
        {
            Assert.ThrowsException<ArgumentNullException>(() => runSettingsProvider.QueryRunSettingsNode("  "));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldReturnNullForNotExistKey()
        {
            Assert.IsNull(runSettingsProvider.QueryRunSettingsNode("RunConfiguration.TargetPlatform"));
        }

        [TestMethod]
        public void QueryRunSettingsNodeShouldReturnCorrectValue()
        {
            runSettingsProvider.UpdateRunSettings(
                "<RunSettings>  <RunConfiguration> <TargetPlatform>x86</TargetPlatform></RunConfiguration>  </RunSettings>");
            Assert.AreEqual("x86", runSettingsProvider.QueryRunSettingsNode("RunConfiguration.TargetPlatform"));
        }

        private void CheckRunSettingsAreUpdated(string parameterName, string parameterValue)
        {
            var match = runSettingsProvider.GetTestRunParameterNodeMatch(
                $@"TestRunParameters.Parameter(name=""{parameterName}"",value=""{parameterValue}"")");
            var runSettingsWithTestRunParameters = string.Join(
                Environment.NewLine,
                $@"<?xml version=""1.0"" encoding=""utf-16""?>",
                $@"<RunSettings>",
                $@"  <TestRunParameters>",
                $@"    <Parameter name=""{parameterName}"" value=""{parameterValue}"" />",
                $@"  </TestRunParameters>",
                $@"</RunSettings>");

            runSettingsProvider.UpdateRunSettings("<RunSettings>\r\n  </RunSettings>");
            runSettingsProvider.UpdateTestRunParameterSettingsNode(match);

            Assert.AreEqual(runSettingsWithTestRunParameters, runSettingsProvider.ActiveRunSettings.SettingsXml);
        }

        private class TestableRunSettingsProvider : IRunSettingsProvider
        {
            public RunSettings ActiveRunSettings { get; set; }

            public void SetActiveRunSettings(RunSettings runSettings)
            {
                ActiveRunSettings = runSettings;
            }
        }
    }
}