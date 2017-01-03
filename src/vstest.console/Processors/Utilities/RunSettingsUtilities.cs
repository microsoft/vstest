// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Utilities to get the run settings from the provider and the commandline options specified.
    /// </summary>
    internal class RunSettingsUtilities
    {
        private const string EmptyRunSettings = @"<RunSettings></RunSettings>";

        /// <summary>
        /// Gets the run settings to be used for the session.
        /// </summary>
        /// <param name="runSettingsProvider"> The current provider of run settings.</param>
        /// <param name="commandlineOptions"> The command line options specified. </param>
        /// <returns></returns>
        internal static string GetRunSettings(IRunSettingsProvider runSettingsProvider, CommandLineOptions commandlineOptions)
        {
            var runSettings = runSettingsProvider?.ActiveRunSettings?.SettingsXml;

            if (string.IsNullOrWhiteSpace(runSettings))
            {
                runSettings = EmptyRunSettings;
            }

            //runSettings = GetEffectiveRunSettings(runSettings, commandlineOptions);

            return runSettings;
        }

        /// <summary>
        /// Gets the effective run settings adding the commandline options to the run settings if not already present.
        /// </summary>
        /// <param name="runSettings"> The run settings XML. </param>
        /// <param name="commandLineOptions"> The command line options. </param>
        /// <returns> Effective run settings. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static string GetEffectiveRunSettings(string runSettings, CommandLineOptions commandLineOptions)
        {
            var architecture = Constants.DefaultPlatform;

            if (commandLineOptions != null && commandLineOptions.ArchitectureSpecified)
            {
                architecture = commandLineOptions.TargetArchitecture;
            }

            var framework = Framework.DefaultFramework;

            if (commandLineOptions != null && commandLineOptions.FrameworkVersionSpecified)
            {
                framework = commandLineOptions.TargetFrameworkVersion;
            }
            var resultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ResultsDirectoryName);

            if (commandLineOptions != null && !string.IsNullOrEmpty(commandLineOptions.ResultsDirectory))
            {
                resultsDirectory = commandLineOptions.ResultsDirectory;
            }

            using (var stream = new StringReader(runSettings))
            using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);

                var navigator = document.CreateNavigator();

                InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, architecture, framework, resultsDirectory);

                if (commandLineOptions != null && commandLineOptions.Parallel)
                {
                    ParallelRunSettingsUtilities.UpdateRunSettingsWithParallelSettingIfNotConfigured(navigator);
                }

                return navigator.OuterXml;
            }
        }

        public static XmlNode CreateNode(XmlDocument doc, string[] xPath)
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

        public static XmlDocument GetRunSettingXmlDocument(IRunSettingsProvider runSettingsManager)
        {
            var doc = new XmlDocument();

            if (runSettingsManager.ActiveRunSettings != null &&
                !string.IsNullOrEmpty(runSettingsManager.ActiveRunSettings.SettingsXml))
            {
                var settingsXml = runSettingsManager.ActiveRunSettings.SettingsXml;

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

        public static void SetRunSettingXmlDocument(IRunSettingsProvider runSettingsManager, XmlDocument xmlDocument)
        {
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(xmlDocument.OuterXml);
            runSettingsManager.SetActiveRunSettings(runSettings);
        }

        public static void UpdateRunSettingsXmlDocument(XmlDocument xmlDocument, string key, string value)
        {
            var xPath = key.Replace('.', '/');
            var node = xmlDocument.SelectSingleNode(string.Format("//RunSettings/{0}", xPath));

            if (node == null)
            {
                node = RunSettingsUtilities.CreateNode(xmlDocument, key.Split('.'));
            }

            node.InnerText = value;
        }

        public static void UpdateRunSettings(IRunSettingsProvider runSettingsManager, string key, string value)
        {
            var xmlDocument = RunSettingsUtilities.GetRunSettingXmlDocument(runSettingsManager);
            RunSettingsUtilities.UpdateRunSettingsXmlDocument(xmlDocument, key, value);
            RunSettingsUtilities.SetRunSettingXmlDocument(runSettingsManager, xmlDocument);
        }
    }
}
