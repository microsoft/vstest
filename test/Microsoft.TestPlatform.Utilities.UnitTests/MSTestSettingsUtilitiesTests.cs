// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.Tests
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using VisualStudio.TestPlatform.ObjectModel;
    using MSTest.TestFramework.AssertExtensions;

    [TestClass]
    public class MSTestSettingsUtilitiesTests
    {
        #region IsLegacyTestSettingsFile tests

        [TestMethod]
        public void IsLegacyTestSettingsFileShouldReturnTrueIfTestSettingsExtension()
        {
            Assert.IsTrue(MSTestSettingsUtilities.IsLegacyTestSettingsFile("C:\\temp\\t.testsettings"));
        }

        [TestMethod]
        public void IsLegacyTestSettingsFileShouldReturnTrueIfTestRunConfigExtension()
        {
            Assert.IsTrue(MSTestSettingsUtilities.IsLegacyTestSettingsFile("C:\\temp\\t.testrunConfig"));
        }

        [TestMethod]
        public void IsLegacyTestSettingsFileShouldReturnTrueIfVSMDIExtension()
        {
            Assert.IsTrue(MSTestSettingsUtilities.IsLegacyTestSettingsFile("C:\\temp\\t.vsmdi"));
        }

        #endregion

        #region Import tests

        [TestMethod]
        public void ImportShouldThrowIfNotLegacySettingsFile()
        {
            var defaultRunSettingsXml = "<RunSettings></RunSettings>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(defaultRunSettingsXml);

            Action action =
                () =>
                MSTestSettingsUtilities.Import(
                    "C:\\temp\\r.runsettings",
                    GetXPathNavigable(xmlDocument),
                    Architecture.X86,
                    FrameworkVersion.Framework45);
            Assert.That.Throws<XmlException>(action).WithMessage("Unexpected settings file specified.");
        }

        [TestMethod]
        public void ImportShouldThrowIfDefaultRunSettingsIsIncorrect()
        {
            var defaultRunSettingsXml = "<DataRunSettings></DataRunSettings>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(defaultRunSettingsXml);

            Action action =
                () =>
                MSTestSettingsUtilities.Import(
                    "C:\\temp\\r.testsettings",
                    GetXPathNavigable(xmlDocument),
                    Architecture.X86,
                    FrameworkVersion.Framework45);
            Assert.That.Throws<XmlException>(action).WithMessage("Could not find 'RunSettings' node.");
        }

        [TestMethod]
        public void ImportShouldEmbedTestSettingsInformation()
        {
            var defaultRunSettingsXml = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(defaultRunSettingsXml);
            var finalxPath = MSTestSettingsUtilities.Import(
                "C:\\temp\\r.testsettings",
                GetXPathNavigable(xmlDocument),
                Architecture.X86,
                FrameworkVersion.Framework45);

            var finalSettingsXml = finalxPath.CreateNavigator().OuterXml;

            var expectedSettingsXml =
                "<RunSettings>\r\n  <MSTest>\r\n    <SettingsFile>C:\\temp\\r.testsettings</SettingsFile>\r\n    <ForcedLegacyMode>true</ForcedLegacyMode>\r\n  </MSTest>\r\n  <RunConfiguration></RunConfiguration>\r\n</RunSettings>";

            Assert.AreEqual(expectedSettingsXml, finalSettingsXml);
        }

        [TestMethod]
        public void ImportShouldEmbedTestSettingsAndDefaultRunConfigurationInformation()
        {
            var defaultRunSettingsXml = "<RunSettings></RunSettings>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(defaultRunSettingsXml);
            var finalxPath = MSTestSettingsUtilities.Import(
                "C:\\temp\\r.testsettings",
                GetXPathNavigable(xmlDocument),
                Architecture.X86,
                FrameworkVersion.Framework45);

            var finalSettingsXml = finalxPath.CreateNavigator().OuterXml;
            
            var expectedSettingsXml =
                "<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>X86</TargetPlatform>\r\n    <TargetFrameworkVersion>Framework45</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n  <MSTest>\r\n    <SettingsFile>C:\\temp\\r.testsettings</SettingsFile>\r\n    <ForcedLegacyMode>true</ForcedLegacyMode>\r\n  </MSTest>\r\n</RunSettings>";

            Assert.AreEqual(expectedSettingsXml, finalSettingsXml);
        }

        #endregion

        private static IXPathNavigable GetXPathNavigable(XmlDocument doc)
        {
#if NET451
            return doc;
#else
            return doc.ToXPathNavigable();
#endif
        }
    }
}
