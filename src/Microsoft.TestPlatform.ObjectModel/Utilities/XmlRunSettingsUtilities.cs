// Copyright(c) Microsoft.All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// Utilities for the run settings XML.
    /// </summary>
    public class XmlRunSettingsUtilities
    {
        /// <summary>
        /// Friendly name of the data collector
        /// </summary>
        private const string FriendlyName = "UnitTestIsolation";

        /// <summary>
        /// Gets the URI of the data collector
        /// </summary>
        private const string DataCollectorUri = "datacollector://microsoft/unittestisolation/1.0";

        /// <summary>
        /// Gets the assembly qualified name of the data collector type
        /// </summary>
        private const string DataCollectorAssemblyQualifiedName = "Microsoft.VisualStudio.TraceCollector.UnitTestIsolationDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        /// <summary>
        /// Gets the os architecture of the machine where this application is running
        /// </summary>
        public static ObjectModel.Architecture OSArchitecture
        {
            get
            {
#if NET46
                var procArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                var procArch6432 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                if (string.Equals(procArch, "amd64", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(procArch, "ia64", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(procArch6432, "amd64", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(procArch6432, "ia64", StringComparison.OrdinalIgnoreCase))
                {
                    return ObjectModel.Architecture.X64;
                }
                if (string.Equals(procArch, "x86", StringComparison.OrdinalIgnoreCase))
                {
                    return ObjectModel.Architecture.X86;
                }
                return ObjectModel.Architecture.ARM;
#else
                var arch = RuntimeInformation.OSArchitecture;

                switch (arch)
                {
                    case Architecture.X64:
                        return ObjectModel.Architecture.X64;
                    case Architecture.X86:
                        return ObjectModel.Architecture.X86;
                    default:
                        return ObjectModel.Architecture.ARM;
                }
#endif
            }
        }

        /// <summary>
        /// Gets the settings to be used while creating XmlReader for runsettings.
        /// </summary>
        public static XmlReaderSettings ReaderSettings
        {
            get
            {
                return new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Prohibit };
            }
        }

        /// <summary>
        /// Examines the given XPathNavigable representation of a runsettings file and determines if it has a configuration node
        /// for the data collector (used for Fakes and CodeCoverage)
        /// </summary>
        /// <param name="runSettingDocument"> XPathNavigable representation of a runsettings file </param>
        /// <param name="dataCollectorUri"> The data Collector Uri. </param>
        /// <returns> True if there is a datacollector configured. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#")]
        public static bool ContainsDataCollector(IXPathNavigable runSettingDocument, string dataCollectorUri)
        {
            if (runSettingDocument == null)
            {
                throw new ArgumentNullException("runSettingDocument");
            }

            if (dataCollectorUri == null)
            {
                throw new ArgumentNullException("dataCollectorUri");
            }

            var navigator = runSettingDocument.CreateNavigator();
            var nodes = navigator.Select("/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector");

            foreach (XPathNavigator dataCollectorNavigator in nodes)
            {
                var uri = dataCollectorNavigator.GetAttribute("uri", string.Empty);
                if (string.Equals(dataCollectorUri, uri, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns RunConfiguration from settingsXml. 
        /// </summary>
        /// <param name="settingsXml">The run settings.</param>
        /// <returns> The RunConfiguration node as defined in the settings xml.</returns>
        public static RunConfiguration GetRunConfigurationNode(string settingsXml)
        {
            var nodeValue = GetNodeValue<RunConfiguration>(settingsXml, Constants.RunConfigurationSettingsName, RunConfiguration.FromXml);
            if (nodeValue == default(RunConfiguration))
            {
                // Return default one.
                nodeValue = new RunConfiguration();
            }
            return nodeValue;
        }

        /// <summary>
        /// Gets the set of user defined test run parameters from settings xml as key value pairs.
        /// </summary>
        /// <param name="settingsXml">The run settings xml.</param>
        /// <returns>The test run parameters defined in the run settings.</returns>
        /// <remarks>If there is no test run parameters section defined in the settings xml a blank dictionary is returned.</remarks>
        public static Dictionary<string, object> GetTestRunParameters(string settingsXml)
        {
            var nodeValue = GetNodeValue<Dictionary<string, object>>(settingsXml, Constants.TestRunParametersName, TestRunParameters.FromXml);
            if (nodeValue == default(Dictionary<string, object>))
            {
                // Return default.
                nodeValue = new Dictionary<string, object>();
            }
            return nodeValue;
        }

        /// <summary>
        /// Returns a value that indicates if the Fakes data collector is already configured  in the settings.
        /// </summary>
        /// <param name="runSettings">The run settings.</param>
        /// <returns>True if the fakes data collector is enabled.</returns>
        public static bool ContainsFakesDataCollector(IXPathNavigable runSettings)
        {
            if (runSettings == null)
            {
                throw new ArgumentNullException("runSettings");
            }

            return ContainsDataCollector(runSettings, DataCollectorUri);
        }

        /// <summary>
        /// Create a default run settings
        /// </summary>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static IXPathNavigable CreateDefaultRunSettings()
        {
            // Create a new default xml doc that looks like this:
            // <?xml version="1.0" encoding="utf-8"?>
            // <RunSettings>
            //   <DataCollectionRunSettings>
            //     <DataCollectors>
            //     </DataCollectors>
            //   </DataCollectionRunSettings>
            // </RunSettings>

            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, "", "");

            doc.AppendChild(xmlDeclaration);
            var runSettingsNode = doc.CreateElement(Constants.RunSettingsName);
            doc.AppendChild(runSettingsNode);

            var dataCollectionRunSettingsNode = doc.CreateElement(Constants.DataCollectionRunSettingsName);
            runSettingsNode.AppendChild(dataCollectionRunSettingsNode);

            var dataCollectorsNode = doc.CreateElement(Constants.DataCollectorsSettingName);
            dataCollectionRunSettingsNode.AppendChild(dataCollectorsNode);

#if NET46
            return doc;
#else
            return doc.ToXPathNavigable();
#endif
        }

        /// <summary>
        /// Returns whether data collection is enabled in the parameter settings xml or not
        /// </summary>
        /// <param name="runSettingsXml"> The run Settings Xml. </param>
        /// <returns> True if data collection is enabled. </returns>
        public static bool IsDataCollectionEnabled(string runSettingsXml)
        {
            var dataCollectionRunSettings = GetDataCollectionRunSettings(runSettingsXml);

            if (dataCollectionRunSettings == null || !dataCollectionRunSettings.IsCollectionEnabled)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get DataCollection Run settings from the settings XML.
        /// </summary>
        /// <param name="runSettingsXml"> The run Settings Xml. </param>
        /// <returns> The <see cref="DataCollectionRunSettings"/>. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static DataCollectionRunSettings GetDataCollectionRunSettings(string runSettingsXml)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).
            if (string.IsNullOrWhiteSpace(runSettingsXml))
            {
                return null;
            }

            using (var stringReader = new StringReader(runSettingsXml))
            {
                var reader = XmlReader.Create(stringReader, ReaderSettings);

                // read to the fist child
                XmlReaderUtilities.ReadToRootNode(reader);
                reader.ReadToNextElement();

                // Read till we reach DC element or reach EOF
                while (!string.Equals(reader.Name, Constants.DataCollectionRunSettingsName)
                       &&
                       !reader.EOF)
                {
                    reader.SkipToNextElement();
                }

                // If reached EOF => DC element not there
                if (reader.EOF)
                {
                    return null;
                }

                // Reached here => DC element present. 
                return DataCollectionRunSettings.FromXml(reader);
            }
        }

        /// <summary>
        /// Throws a settings exception if the node the reader is on has attributes defined.
        /// </summary>
        /// <param name="reader"> The xml reader. </param>
        internal static void ThrowOnHasAttributes(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        Constants.RunConfigurationSettingsName,
                        reader.Name));
            }
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static T GetNodeValue<T>(string settingsXml, string nodeName, Func<XmlReader, T> nodeParser)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).
            if (!string.IsNullOrWhiteSpace(settingsXml))
            {
                using (var stringReader = new StringReader(settingsXml))
                {
                    XmlReader reader = XmlReader.Create(stringReader, ReaderSettings);

                    // read to the fist child
                    XmlReaderUtilities.ReadToRootNode(reader);
                    reader.ReadToNextElement();

                    // Read till we reach nodeName element or reach EOF
                    while (!string.Equals(reader.Name, nodeName, StringComparison.OrdinalIgnoreCase)
                            &&
                            !reader.EOF)
                    {
                        reader.SkipToNextElement();
                    }

                    if (!reader.EOF)
                    {
                        // read nodeName element.
                        return nodeParser(reader);
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Get InProc DataCollection Run settings
        /// </summary>
        /// <param name="runSettingsXml">
        /// The run Settings Xml.
        /// </param>
        /// <returns>Data collection run settings.</returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static DataCollectionRunSettings GetInProcDataCollectionRunSettings(string runSettingsXml)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).            
            if (!string.IsNullOrWhiteSpace(runSettingsXml))
            {
                runSettingsXml = runSettingsXml.Trim();
                using (StringReader stringReader1 = new StringReader(runSettingsXml))
                {
                    XmlReader reader = XmlReader.Create(stringReader1, ReaderSettings);

                    // read to the fist child
                    XmlReaderUtilities.ReadToRootNode(reader);
                    reader.ReadToNextElement();

                    // Read till we reach In Proc IDC element or reach EOF
                    while (!string.Equals(reader.Name, Constants.InProcDataCollectionRunSettingsName)
                            &&
                            !reader.EOF)
                    {
                        reader.SkipToNextElement();
                    }

                    // If reached EOF => IDC element not there
                    if (reader.EOF)
                    {
                        return null;
                    }

                    // Reached here => In Proc IDC element present. 
                    return DataCollectionRunSettings.FromXml(reader, Constants.InProcDataCollectionRunSettingsName, Constants.InProcDataCollectorsSettingName, Constants.InProcDataCollectorSettingName);
                }
            }

            return null;
        }
    }
}
