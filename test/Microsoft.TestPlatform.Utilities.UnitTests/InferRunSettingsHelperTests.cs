// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using System;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.TestPlatform.Utilities.Tests;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
            var navigator = this.GetNavigator(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

            Assert.That.Throws<XmlException>(action)
                        .WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
                                            "Could not find 'RunSettings' node."));
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfPlatformNodeIsInvalid()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>foo</TargetPlatform></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

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
            var navigator = this.GetNavigator(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

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
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateWithFrameworkSettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldUpdateWithResultsDirectorySettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X86, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdatePlatformIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdateFrameworkIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>Framework40</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
        }
        //TargetFrameworkMoniker

        [TestMethod]
        public void UpdateRunSettingsShouldAllowTargetFrameworkMonikerValue()
        {

            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdateResultsDirectoryIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldNotUpdatePlatformOrFrameworkOrResultsDirectoryIfRunSettingsAlreadyHasIt()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform><TargetFrameworkVersion>Framework40</TargetFrameworkVersion><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
            StringAssert.Contains(xml, "<TargetFrameworkVersion>Framework40</TargetFrameworkVersion>");
            StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsWithAnEmptyRunSettingsShouldAddValuesSpecifiedInRunConfiguration()
        {
            var settings = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;

            StringAssert.Contains(xml, "<TargetPlatform>X64</TargetPlatform>");
            StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
            StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
        }

        [TestMethod]
        public void UpdateRunSettingsShouldReturnBackACompleteRunSettings()
        {
            var settings = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.X64, Framework.DefaultFramework, "temp");

            var xml = navigator.OuterXml;
            var expectedRunSettings = string.Format("<RunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>temp</ResultsDirectory>\r\n    <TargetPlatform>X64</TargetPlatform>\r\n    <TargetFrameworkVersion>{0}</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n</RunSettings>", Framework.DefaultFramework.Name);

            Assert.AreEqual(expectedRunSettings, xml);
        }

        [TestMethod]
        public void UpdateRunSettingsShouldThrowIfArchitectureSetIsIncompatibleWithCurrentSystemArchitecture()
        {
            var settings = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settings);

            Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, Architecture.ARM, Framework.DefaultFramework, "temp");

            Assert.That.Throws<SettingsException>(action)
                .WithMessage(string.Format(
                        "Incompatible Target platform settings '{0}' with system architecture '{1}'.",
                        "ARM",
                        XmlRunSettingsUtilities.OSArchitecture.ToString()));
        }

        [DataTestMethod]
        [DataRow("DesignMode")]
        [DataRow("CollectSourceInformation")]
        public void UpdateRunSettingsShouldNotModifyXmlIfNavigatorIsNotAtRootNode(string settingName)
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);
            navigator.MoveToFirstChild();

            switch (settingName.ToUpperInvariant())
            {
                case "DESIGNMODE":
                    InferRunSettingsHelper.UpdateDesignMode(navigator, true);
                    break;

                case "COLLECTSOURCEINFORMATION":
                    InferRunSettingsHelper.UpdateCollectSourceInformation(navigator, true);
                    break;
            };

            navigator.MoveToRoot();
            Assert.IsTrue(navigator.InnerXml.IndexOf(settingName, StringComparison.OrdinalIgnoreCase) < 0);
        }

        [TestMethod]
        public void UpdateDesignModeOrCsiShouldNotModifyXmlIfNodeIsAlreadyPresent()
        {
            var settings = @"<RunSettings><RunConfiguration><DesignMode>False</DesignMode><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateDesignMode(navigator, true);
            InferRunSettingsHelper.UpdateCollectSourceInformation(navigator, true);

            Assert.AreEqual("False", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/DesignMode"));
            Assert.AreEqual("False", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/CollectSourceInformation"));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UpdateDesignModeOrCsiShouldModifyXmlToValueProvided(bool val)
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateDesignMode(navigator, val);
            InferRunSettingsHelper.UpdateCollectSourceInformation(navigator, val);

            Assert.AreEqual(val.ToString(), this.GetValueOf(navigator, "/RunSettings/RunConfiguration/DesignMode"));
            Assert.AreEqual(val.ToString(), this.GetValueOf(navigator, "/RunSettings/RunConfiguration/CollectSourceInformation"));
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

            var navigator = this.GetNavigator(settings);

            var result = InferRunSettingsHelper.TryGetDeviceXml(navigator, out string deviceXml);
            Assert.IsTrue(result);

            InferRunSettingsHelper.UpdateTargetDevice(navigator, deviceXml);
            Assert.AreEqual(deviceXml.ToString(), this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetDevice"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(navigator, "X64", overwrite: false);

            Assert.AreEqual("x86", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(navigator, "X64", overwrite: true);

            Assert.AreEqual("X64", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetPlatformShouldAddPlatformXmlNodeIfNotPresent()
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetPlatform(navigator, "X64");

            Assert.AreEqual("X64", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetPlatform"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetFramework(navigator, ".NETCoreApp,Version=v1.0", overwrite: false);

            Assert.AreEqual(".NETFramework,Version=v4.5", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
        {
            var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetFramework(navigator, ".NETCoreApp,Version=v1.0", overwrite: true);

            Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void UpdateTargetFrameworkShouldAddFrameworkXmlNodeIfNotPresent()
        {
            var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var navigator = this.GetNavigator(settings);

            InferRunSettingsHelper.UpdateTargetFramework(navigator, ".NETCoreApp,Version=v1.0");

            Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(navigator, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
        }

        [TestMethod]
        public void FilterCompatiableSourcesShouldIdentifyIncomaptiableSourcesAndConstructWarningMessage()
        {
            #region Arrange
            sourceArchitectures["AnyCPU1net46.dll"] = Architecture.AnyCPU;
            sourceArchitectures["x64net47.exe"] = Architecture.X64;
            sourceArchitectures["x86net45.dll"] = Architecture.X86;

            sourceFrameworks["AnyCPU1net46.dll"] = frameworkNet46;
            sourceFrameworks["x64net47.exe"] = frameworkNet47;
            sourceFrameworks["x86net45.dll"] = frameworkNet45;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine(GetSourceIncompatibleMessage("AnyCPU1net46.dll"));
            sb.AppendLine(GetSourceIncompatibleMessage("x64net47.exe"));
            sb.AppendLine(GetSourceIncompatibleMessage("x86net45.dll"));

            var expected = string.Format(CultureInfo.CurrentCulture, UtilitiesResources.DisplayChosenSettings, frameworkNet47, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");
            #endregion

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, frameworkNet47, sourceArchitectures, sourceFrameworks, out warningMessage);

            // None of the DLLs passed are compatiable to the choosen settings
            Assert.AreEqual(0, compatibleSources.Count());
            Assert.AreEqual(expected, warningMessage);
        }

        [TestMethod]
        public void FilterCompatiableSourcesShouldIdentifyCompatiableSources()
        {
            sourceArchitectures["x64net45.exe"] = Architecture.X64;
            sourceArchitectures["x86net45.dll"] = Architecture.X86;

            sourceFrameworks["x64net45.exe"] = frameworkNet45;
            sourceFrameworks["x86net45.dll"] = frameworkNet45;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine(GetSourceIncompatibleMessage("x64net45.exe"));

            var expected = string.Format(CultureInfo.CurrentCulture, UtilitiesResources.DisplayChosenSettings, frameworkNet45, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

            // only "x86net45.dll" is the compatiable source
            Assert.AreEqual(1, compatibleSources.Count());
            Assert.AreEqual(expected, warningMessage);
        }

        [TestMethod]
        public void FilterCompatiableSourcesShouldRetrunnullAsWarningMessageIfNoConflict()
        {
            sourceArchitectures["x64net45.exe"] = Architecture.X64;
            sourceFrameworks["x64net45.exe"] = frameworkNet45;

            string warningMessage = string.Empty;
            var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Architecture.X64, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

            Assert.IsNull(warningMessage);
        }

        #region private methods

        private string GetSourceIncompatibleMessage(string source)
        {
            return string.Format(CultureInfo.CurrentCulture, UtilitiesResources.SourceIncompatible, source, sourceFrameworks[source].Version, sourceArchitectures[source]);
        }

        private XPathNavigator GetNavigator(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            return doc.CreateNavigator();
        }

        private string GetValueOf(XPathNavigator navigator, string xpath)
        {
            navigator.MoveToRoot();
            return navigator.SelectSingleNode(xpath).Value;
        }
        #endregion
    }
}
