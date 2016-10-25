// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using UtilitiesResources = Microsoft.VisualStudio.TestPlatform.Utilities.Resources.Resources;

    /// <summary>
    /// The legacy mstest.exe settings utilities.
    /// </summary>
    public static class MSTestSettingsUtilities
    {
        /// <summary>
        /// Imports the parameter settings file in the default runsettings. 
        /// </summary>
        /// <param name="settingsFile">
        /// Settings file which need to be imported. The file extension of the settings file will be specified by <paramref name="SettingsFileExtension"/> property.
        /// </param>
        /// <param name="defaultRunSettings"> Input RunSettings document to which settings file need to be imported. </param>
        /// <param name="architecture"> The architecture. </param>
        /// <param name="frameworkVersion"> The framework Version. </param>
        /// <returns> Updated RunSetting Xml document with imported settings. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static IXPathNavigable Import(string settingsFile, IXPathNavigable defaultRunSettings, Architecture architecture, FrameworkVersion frameworkVersion)
        {
            ValidateArg.NotNull(settingsFile, "settingsFile");
            ValidateArg.NotNull(defaultRunSettings, "defaultRunSettings");

            if (IsLegacyTestSettingsFile(settingsFile) == false)
            {
                throw new XmlException(string.Format(CultureInfo.CurrentCulture, UtilitiesResources.UnExpectedSettingsFile));
            }

            var navigator = defaultRunSettings.CreateNavigator();

            if (!navigator.MoveToChild(Constants.RunSettingsName, string.Empty))
            {
                throw new XmlException(UtilitiesResources.NoRunSettingsNodeFound);
            }

            var settingsNode = GenerateMSTestXml(settingsFile);
            settingsNode.MoveToRoot();
            navigator.PrependChild(settingsNode);

            // Adding RunConfig 
            if (!navigator.MoveToChild(Constants.RunConfigurationSettingsName, string.Empty))
            {
                var doc = new XmlDocument();
                var runConfigurationNode = doc.CreateElement(Constants.RunConfigurationSettingsName);

                var targetPlatformNode = doc.CreateElement("TargetPlatform");
                targetPlatformNode.InnerXml = architecture.ToString();
                runConfigurationNode.AppendChild(targetPlatformNode);

                var targetFrameworkVersionNode = doc.CreateElement("TargetFrameworkVersion");
                targetFrameworkVersionNode.InnerXml = frameworkVersion.ToString();
                runConfigurationNode.AppendChild(targetFrameworkVersionNode);

                var runConfigNodeNavigator = runConfigurationNode.CreateNavigator();
                runConfigNodeNavigator.MoveToRoot();
                navigator.PrependChild(runConfigNodeNavigator);
            }

            navigator.MoveToRoot();
            return navigator;
        }

        public static bool IsLegacyTestSettingsFile(string settingsFile)
        {
            return string.Equals(Path.GetExtension(settingsFile), ".testSettings", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(Path.GetExtension(settingsFile), ".testrunConfig", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(Path.GetExtension(settingsFile), ".vsmdi", StringComparison.OrdinalIgnoreCase);
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static XPathNavigator GenerateMSTestXml(string settingsFile)
        {
            // Generate the MSTest xml
            //
            // <MSTest>
            //   <TestSettingsFile>C:\local.testsettings</TestSettingsFile>
            //   <ForcedLegacyMode>true</ForcedLegacyMode>
            // </MSTest>
            //
            XmlDocument doc = new XmlDocument();
            XmlElement mstestNode = doc.CreateElement("MSTest");

            XmlElement testSettingsFileNode = doc.CreateElement("SettingsFile");
            testSettingsFileNode.InnerXml = settingsFile;
            mstestNode.AppendChild(testSettingsFileNode);

            XmlElement forcedLegacyModeNode = doc.CreateElement("ForcedLegacyMode");
            forcedLegacyModeNode.InnerXml = "true";
            mstestNode.AppendChild(forcedLegacyModeNode);

            return mstestNode.CreateNavigator();
        }
    }
}
