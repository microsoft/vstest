// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using System;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.TestPlatform.Utilities.Tests;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using OMResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.CommonResources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSTest.TestFramework.AssertExtensions;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using UtilitiesResources = Microsoft.VisualStudio.TestPlatform.Utilities.Resources.Resources;
    using System.Globalization;
    using System.Text;

    [TestClass]
    public class InferRunSettingsHelperTests
    {
        private IDictionary<string, Architecture> sourceArchitectures;
        private IDictionary<string, Framework> sourceFrameworks;
        private readonly Framework frameworkNet45 = Framework.FromString(".NETFramework,Version=4.5");
        private readonly Framework frameworkNet46 = Framework.FromString(".NETFramework,Version=4.6");
        private readonly Framework frameworkNet47 = Framework.FromString(".NETFramework,Version=4.7");

        public InferRunSettingsHelperTests()
        {
            sourceArchitectures = new Dictionary<string, Architecture>();
            sourceFrameworks = new Dictionary<string, Framework>();
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfRunSettingsNodeDoesNotExist()
        {
            var settings = @"<RandomSettings></RandomSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            Assert.That.Throws<XmlException>(action)
                        .WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
                                            "Could not find 'RunSettings' node."));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfPlatformNodeIsInvalid()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>foo</TargetPlatform></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            Assert.That.Throws<XmlException>(action)
                        .WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
                                        string.Format("Invalid setting '{0}'. Invalid value '{1}' specified for '{2}'",
                                            "RunConfiguration",
                                            "foo",
                                            "TargetPlatform")));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfFrameworkNodeIsInvalid()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>foo</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            Assert.That.Throws<XmlException>(action)
                        .WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
                                        string.Format("Invalid setting '{0}'. Invalid value '{1}' specified for '{2}'",
                                        "RunConfiguration",
                                        "foo",
                                        "TargetFrameworkVersion")));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateWithPlatformSettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateWithFrameworkSettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateWithResultsDirectorySettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdatePlatformIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdateFrameworkIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>Framework40</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
        }
        //TargetFrameworkMoniker

        [TestMethod]
        public void UpdateRunSettingsShouldAllowTargetFrameworkMonikerValue()
        {

            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdateResultsDirectoryIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdatePlatformOrFrameworkOrResultsDirectoryIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform><TargetFrameworkVersion>Framework40</TargetFrameworkVersion><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
            StringAssert.Contains(xml, "<TargetFrameworkVersion>Framework40</TargetFrameworkVersion>");
            StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsWithAnEmptyRunSettingsShouldAddValuesSpecifiedInRunConfiguration()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X64</TargetPlatform>");
            StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
            StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldReturnBackACompleteRunSettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = xmlDocument.OuterXml;
            var expectedRunSettings = string.Format("<RunSettings><RunConfiguration><ResultsDirectory>temp</ResultsDirectory><TargetPlatform>X64</TargetPlatform><TargetFrameworkVersion>{0}</TargetFrameworkVersion></RunConfiguration></RunSettings>", Framework.DefaultFramework.Name);

            Assert.AreEqual(expectedRunSettings, xml);
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfArchitectureSetIsIncompatibleWithCurrentSystemArchitecture()
        {
            var settings = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.ARM, Framework.DefaultFramework, "temp");

            Assert.That.Throws<SettingsException>(action)
                .WithMessage(string.Format(
                        "Incompatible Target platform settings '{0}' with system architecture '{1}'.",
                        "ARM",
                        XmlRunSettingsUtilities.OSArchitecture.ToString()));
        }

        [TestMethod]
        public void UpdateDesignModeOrCsiShouldNotModifyXmlIfNodeIsAlreadyPresent()
        {
            var settings = @"<RunSettings><RunConfiguration><DesignMode>False</DesignMode><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateDesignMode(xmlDocument, true);
            InferRunSettingsHelper.UpdateCollectSourceInformation(xmlDocument, true);

            Assert.AreEqual("False", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/DesignMode"));
            Assert.AreEqual("False", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/CollectSourceInformation"));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UpdateDesignModeOrCsiShouldModifyXmlToValueProvided(bool val)
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateDesignMode(xmlDocument, val);
            InferRunSettingsHelper.UpdateCollectSourceInformation(xmlDocument, val);

            Assert.AreEqual(val.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/DesignMode"));
            Assert.AreEqual(val.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/CollectSourceInformation"));
        }

        [TestMethod]
        public void MakeRunsettingsCompatibleShouldDeleteNewlyAddedRunConfigurationNode()
        {
            var settings = @"<RunSettings><RunConfiguration><DesignMode>False</DesignMode><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>";

            var result = InferRunSettingsHelper.MakeRunsettingsCompatible(settings);

            Assert.IsTrue(result.IndexOf("DesignMode", StringComparison.OrdinalIgnoreCase) < 0);
        }

        [TestMethod]
        public void MakeRunsettingsCompatibleShouldNotDeleteOldRunConfigurationNode()
        {
            var settings = @"<RunSettings>
                                <RunConfiguration>
                                    <DesignMode>False</DesignMode>
                                    <CollectSourceInformation>False</CollectSourceInformation>
                                    <TargetPlatform>x86</TargetPlatform>
                                    <TargetFrameworkVersion>net46</TargetFrameworkVersion>
                                    <TestAdaptersPaths>dummypath</TestAdaptersPaths>
                                    <ResultsDirectory>dummypath</ResultsDirectory>
                                    <SolutionDirectory>dummypath</SolutionDirectory>
                                    <MaxCpuCount>2</MaxCpuCount>
                                    <DisableParallelization>False</DisableParallelization>
                                    <DisableAppDomain>False</DisableAppDomain>
                                </RunConfiguration>
                            </RunSettings>";

            var result = InferRunSettingsHelper.MakeRunsettingsCompatible(settings);

            Assert.IsTrue(result.IndexOf("TargetPlatform", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("TargetFrameworkVersion", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("TestAdaptersPaths", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("ResultsDirectory", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("SolutionDirectory", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("MaxCpuCount", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("DisableParallelization", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.IsTrue(result.IndexOf("DisableAppDomain", StringComparison.OrdinalIgnoreCase) > 0);
        }

        [TestMethod]
        public void UpdateTargetDeviceValueFromOldMsTestSettings()
        {
            var settings = @"<RunSettings>
                                <RunConfiguration>
                                    <MaxCpuCount>2</MaxCpuCount>
                                    <DisableParallelization>False</DisableParallelization>
                                    <DisableAppDomain>False</DisableAppDomain>
                                </RunConfiguration>
                                <MSPhoneTest>
                                  <TargetDevice>169.254.193.190</TargetDevice>
                                </MSPhoneTest>
                            </RunSettings>";

            var xmlDocument = this.GetXmlDocument(settings);

            var result = InferRunSettingsHelper.TryGetDeviceXml(xmlDocument.CreateNavigator(), out string deviceXml);
            Assert.IsTrue(result);

            InferRunSettingsHelper.UpdateTargetDevice(xmlDocument, deviceXml);
            Assert.AreEqual(deviceXml.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetDevice"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64", overwrite: false);

            Assert.AreEqual("x86", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64", overwrite: true);

            Assert.AreEqual("X64", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldAddPlatformXmlNodeIfNotPresent()
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64");

            Assert.AreEqual("X64", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0", overwrite: false);

            Assert.AreEqual(".NETFramework,Version=v4.5", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0", overwrite: true);

            Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldAddFrameworkXmlNodeIfNotPresent()
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settings);

            InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0");

            Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void FilterCompatibleSourcesShouldIdentifyIncomaptiableSourcesAndConstructWarningMessage()
        {
            #region Arrange
            sourceArchitectures["AnyCPU1net46.dll"] = Architecture.AnyCPU;
            sourceArchitectures["x64net47.exe"] = Architecture.X64;
            sourceArchitectures["x86net45.dll"] = Architecture.X86;

            sourceFrameworks["AnyCPU1net46.dll"] = frameworkNet46;
            sourceFrameworks["x64net47.exe"] = frameworkNet47;
            sourceFrameworks["x86net45.dll"] = frameworkNet45;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(GetSourceIncompatibleMessage("AnyCPU1net46.dll"));
            sb.AppendLine(GetSourceIncompatibleMessage("x64net47.exe"));
            sb.AppendLine(GetSourceIncompatibleMessage("x86net45.dll"));

            var expected = string.Format(CultureInfo.CurrentCulture, OMResources.DisplayChosenSettings, frameworkNet47, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");
            #endregion

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, frameworkNet47, sourceArchitectures, sourceFrameworks, out warningMessage);

            // None of the DLLs passed are compatible to the chosen settings
            Assert.AreEqual(0, compatibleSources.Count());
            Assert.AreEqual(expected, warningMessage);
        }

        [TestMethod]
        public void FilterCompatibleSourcesShouldIdentifyCompatibleSources()
        {
            sourceArchitectures["x64net45.exe"] = Architecture.X64;
            sourceArchitectures["x86net45.dll"] = Architecture.X86;

            sourceFrameworks["x64net45.exe"] = frameworkNet45;
            sourceFrameworks["x86net45.dll"] = frameworkNet45;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(GetSourceIncompatibleMessage("x64net45.exe"));

            var expected = string.Format(CultureInfo.CurrentCulture, OMResources.DisplayChosenSettings, frameworkNet45, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

            // only "x86net45.dll" is the compatible source
            Assert.AreEqual(1, compatibleSources.Count());
            Assert.AreEqual(expected, warningMessage);
        }

        [TestMethod]
        public void FilterCompatibleSourcesShouldNotComposeWarningIfSettingsAreCorrect()
        {
            sourceArchitectures["x86net45.dll"] = Architecture.X86;
            sourceFrameworks["x86net45.dll"] = frameworkNet45;

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

            // only "x86net45.dll" is the compatible source
            Assert.AreEqual(1, compatibleSources.Count());
            Assert.IsTrue(string.IsNullOrEmpty(warningMessage));
        }

        [TestMethod]
        public void FilterCompatibleSourcesShouldRetrunWarningMessageIfNoConflict()
        {
            sourceArchitectures["x64net45.exe"] = Architecture.X64;
            sourceFrameworks["x64net45.exe"] = frameworkNet45;

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Architecture.X64, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

            Assert.IsTrue(string.IsNullOrEmpty(warningMessage));
        }
        
        [TestMethod]
        public void IsTestSettingsEnabledShouldReturnTrueIfRunsettingsHasTestSettings()
        {
            string runsettingsString = @"<RunSettings>
                                        <MSTest>
                                            <SettingsFile>C:\temp.testsettings</SettingsFile>
                                            <ForcedLegacyMode>true</ForcedLegacyMode>
                                        </MSTest>
                                    </RunSettings>";

            Assert.IsTrue(InferRunSettingsHelper.IsTestSettingsEnabled(runsettingsString));
        }

        [TestMethod]
        public void IsTestSettingsEnabledShouldReturnFalseIfRunsettingsDoesnotHaveTestSettings()
        {
            string runsettingsString = @"<RunSettings>
                                        <MSTest>
                                            <ForcedLegacyMode>true</ForcedLegacyMode>
                                        </MSTest>
                                    </RunSettings>";

            Assert.IsFalse(InferRunSettingsHelper.IsTestSettingsEnabled(runsettingsString));
        }

        #region private methods

        private string GetSourceIncompatibleMessage(string source)
        {
            return string.Format(CultureInfo.CurrentCulture, OMResources.SourceIncompatible, source, sourceFrameworks[source].Version, sourceArchitectures[source]);
        }

        private XmlDocument GetXmlDocument(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            return doc;
        }

        private string GetValueOf(XmlDocument xmlDocument, string xpath)
        {
            return xmlDocument.SelectSingleNode(xpath).InnerText;
        }
        #endregion
    }
}
