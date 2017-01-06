// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;

    /// <summary>
    /// Utilities to get the run settings from the provider and the commandline options specified.
    /// </summary>
    internal class RunSettingsUtilities
    {
        private const string EmptyRunSettings = @"<RunSettings></RunSettings>";

        public static void UpdateRunSettings(IRunSettingsProvider runSettingsManager, string runsettingsXml)
        {
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(runsettingsXml);
            runSettingsManager.SetActiveRunSettings(runSettings);
        }

        public static void AddDefaultRunSettings(IRunSettingsProvider runSettingsProvider)
        {
            ValidateArg.NotNull(runSettingsProvider, nameof(runSettingsProvider));

            var runSettingsXml = runSettingsProvider?.ActiveRunSettings?.SettingsXml;

            if (string.IsNullOrWhiteSpace(runSettingsXml))
            {
                runSettingsXml = EmptyRunSettings;
            }

            runSettingsXml = AddDefaultRunSettings(runSettingsXml);
            RunSettingsUtilities.UpdateRunSettings(runSettingsProvider, runSettingsXml);
        }

        public static XmlDocument GetRunSettingXmlDocument(IRunSettingsProvider runSettingsProvider)
        {
            ValidateArg.NotNull(runSettingsProvider, nameof(runSettingsProvider));

            var doc = new XmlDocument();

            if (runSettingsProvider.ActiveRunSettings != null &&
                !string.IsNullOrEmpty(runSettingsProvider.ActiveRunSettings.SettingsXml))
            {
                var settingsXml = runSettingsProvider.ActiveRunSettings.SettingsXml;

#if net46
                    using (var reader = XmlReader.Create(new StringReader(settingsXml), new XmlReaderSettings() { XmlResolver = null, CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                    {
#else
                using (
                    var reader = XmlReader.Create(new StringReader(settingsXml),
                        new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
#endif
                    doc.Load(reader);
                }
            }
            else
            {
#if net46
                    doc = (XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings();
#else
                using (
                    var reader =
                        XmlReader.Create(
                            new StringReader(XmlRunSettingsUtilities.CreateDefaultRunSettings().CreateNavigator().OuterXml),
                            new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
                {
                    doc.Load(reader);
                }
#endif
            }
            return doc;
        }

        public static void UpdateRunSettingsXmlDocument(XmlDocument xmlDocument, string key, string value)
        {
            var node = GetXmlNode(xmlDocument, key) ?? RunSettingsUtilities.CreateNode(xmlDocument, key.Split('.'));
            node.InnerText = value;
        }

        public static void UpdateRunSettingsNode(IRunSettingsProvider runSettingsProvider, string key, string value)
        {
            ValidateArg.NotNull(runSettingsProvider, nameof(runSettingsProvider));
            var xmlDocument = RunSettingsUtilities.GetRunSettingXmlDocument(runSettingsProvider);
            RunSettingsUtilities.UpdateRunSettingsXmlDocument(xmlDocument, key, value);
            RunSettingsUtilities.UpdateRunSettings(runSettingsProvider, xmlDocument.OuterXml);
        }

        public static string QueryRunSettingsNode(IRunSettingsProvider runSettingsProvider, string key)
        {
            ValidateArg.NotNull(runSettingsProvider, nameof(runSettingsProvider));
            var xmlDocument = RunSettingsUtilities.GetRunSettingXmlDocument(runSettingsProvider);
            var node = GetXmlNode(xmlDocument, key);
            return node?.InnerText;
        }

        /// <summary>
        /// Gets the effective run settings adding the default run settings if not already present.
        /// </summary>
        /// <param name="runSettings"> The run settings XML. </param>
        /// <returns> Effective run settings. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static string AddDefaultRunSettings(string runSettings)
        {
            var architecture = Constants.DefaultPlatform;
            var framework = Framework.DefaultFramework;
            var defaultResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ResultsDirectoryName);

            using (var stream = new StringReader(runSettings))
            using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);

                var navigator = document.CreateNavigator();

                InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, architecture, framework, defaultResultsDirectory);
                return navigator.OuterXml;
            }
        }

        private static XmlNode CreateNode(XmlDocument doc, string[] xPath)
        {
            XmlNode node = null;
            XmlNode parent = doc.DocumentElement;

            for (int i = 0; i < xPath.Length; i++)
            {
                node = parent.SelectSingleNode(xPath[i]);

                if (node == null)
                {
                    node = parent.AppendChild(doc.CreateElement(xPath[i]));
                }

                parent = node;
            }

            return node;
        }

        private static XmlNode GetXmlNode(XmlDocument xmlDocument, string key)
        {
            var xPath = key.Replace('.', '/');
            var node = xmlDocument.SelectSingleNode(string.Format("//RunSettings/{0}", xPath));
            return node;
        }
    }
}
