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

    [TestClass]
    public class InferRunSettingsHelperTests
    {
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

        #region private methods

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
