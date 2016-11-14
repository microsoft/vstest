// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClientUtilitiesTests
    {
        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldThrowIfDocumentIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => ClientUtilities.FixRelativePathsInRunSettings(null, "c:\\temp"));
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldThrowIfPathIsNullOrEmpty()
        {
            Assert.ThrowsException<ArgumentNullException>(() => ClientUtilities.FixRelativePathsInRunSettings(new XmlDocument(), null));
            Assert.ThrowsException<ArgumentNullException>(() => ClientUtilities.FixRelativePathsInRunSettings(new XmlDocument(), ""));
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldNotModifyAnEmptyRunSettings()
        {
            var runSettingsXML = "<RunSettings></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            Assert.AreEqual(runSettingsXML, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldModifyRelativeTestSettingsFilePath()
        {
            var runSettingsXML = "<RunSettings><MSTest><SettingsFile>..\\remote.testsettings</SettingsFile></MSTest></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            var expectedPath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(currentAssemblyLocation), "..\\remote.testsettings"));
            var expectedSettingsXml = string.Concat(
                "<RunSettings><MSTest><SettingsFile>",
                expectedPath,
                "</SettingsFile></MSTest></RunSettings>");

            Assert.AreEqual(expectedSettingsXml, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldNotModifyAbsoluteTestSettingsFilePath()
        {
            var runSettingsXML = "<RunSettings><MSTest><SettingsFile>C:\\temp\\remote.testsettings</SettingsFile></MSTest></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            Assert.AreEqual(runSettingsXML, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldNotModifyEmptyTestSettingsFilePath()
        {
            var runSettingsXML = "<RunSettings><MSTest><SettingsFile></SettingsFile></MSTest></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            Assert.AreEqual(runSettingsXML, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldModifyRelativeResultsDirectory()
        {
            var runSettingsXML = "<RunSettings><RunConfiguration><ResultsDirectory>..\\results</ResultsDirectory></RunConfiguration></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            var expectedPath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(currentAssemblyLocation), "..\\results"));
            var expectedSettingsXml = string.Concat(
                "<RunSettings><RunConfiguration><ResultsDirectory>",
                expectedPath,
                "</ResultsDirectory></RunConfiguration></RunSettings>");

            Assert.AreEqual(expectedSettingsXml, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldNotModifyAbsoluteResultsDirectory()
        {
            var runSettingsXML = "<RunSettings><RunConfiguration><ResultsDirectory>C:\\temp\\results</ResultsDirectory></RunConfiguration></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            Assert.AreEqual(runSettingsXML, finalSettingsXml);
        }

        [TestMethod]
        public void FixRelativePathsInRunSettingsShouldNotModifyEmptyResultsDirectory()
        {
            var runSettingsXML = "<RunSettings><RunConfiguration><ResultsDirectory></ResultsDirectory></RunConfiguration></RunSettings>";

            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXML);

            var currentAssemblyLocation = typeof(ClientUtilitiesTests).GetTypeInfo().Assembly.Location;

            ClientUtilities.FixRelativePathsInRunSettings(doc, currentAssemblyLocation);

            var finalSettingsXml = doc.OuterXml;

            Assert.AreEqual(runSettingsXML, finalSettingsXml);
        }
    }
}
