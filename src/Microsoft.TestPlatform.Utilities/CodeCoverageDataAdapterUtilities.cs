// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Globalization;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The code coverage data adapter utilities.
    /// </summary>
    public static class CodeCoverageDataAdapterUtilities
    {
        #region private variables

        private const string DynamicCodeCoverageDataDiagnosticAdaterUriString = "datacollector://microsoft/CodeCoverage/2.0";
        private const string StaticCodeCoverageDataDiagnosticAdaterUriString = "datacollector://microsoft/CodeCoverage/1.0";

        private static string xPathSeperator = "/";
        private static string[] nodeNames = new string[] { Constants.RunSettingsName, Constants.DataCollectionRunSettingsName, Constants.DataCollectorsSettingName, Constants.DataCollectorSettingName };

        #region Default  CodeCoverage Settings String

        private static string codeCoverageCollectorSettingsTemplate =
@"      <DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=15.0.0.0 " + @", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" friendlyName=""Code Coverage"">" + Environment.NewLine +
@"      </DataCollector>";

        #endregion

        #endregion

        /// <summary>
        /// Updates with code coverage settings if not configured.
        /// </summary>
        /// <param name="runSettingsDocument"> The run settings document. </param>
        public static void UpdateWithCodeCoverageSettingsIfNotConfigured(IXPathNavigable runSettingsDocument)
        {
            ValidateArg.NotNull<IXPathNavigable>(runSettingsDocument, "runSettingsDocument");
            var runSettingsNavigator = runSettingsDocument.CreateNavigator();
            
            bool codeCoverageConfigured = XmlRunSettingsUtilities.ContainsDataCollector(runSettingsNavigator, DynamicCodeCoverageDataDiagnosticAdaterUriString)
                || XmlRunSettingsUtilities.ContainsDataCollector(runSettingsNavigator, StaticCodeCoverageDataDiagnosticAdaterUriString);

            if (codeCoverageConfigured == false)
            {
                var existingPath = string.Empty;
                var xpaths = new string[]
                             {
                                 string.Join(xPathSeperator, nodeNames, 0, 1),
                                 string.Join(xPathSeperator, nodeNames, 0, 2),
                                 string.Join(xPathSeperator, nodeNames, 0, 3)
                             };

                foreach (var xpath in xpaths)
                {
                    if (runSettingsNavigator.SelectSingleNode(xpath) != null)
                    {
                        existingPath = xpath;
                    }
                    else
                    {
                        break;
                    }
                }

                // If any nodes are missing to add code coverage deafult settings, add the missing xml nodes.
                XPathNavigator dataCollectorsNavigator;
                if (existingPath.Equals(xpaths[2]) == false)
                {
                    dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(existingPath);
                    var missingNodesText = GetMissingNodesTextIfAny(existingPath, xpaths[2]);
                    dataCollectorsNavigator.AppendChild(missingNodesText);
                }

                dataCollectorsNavigator = runSettingsNavigator.SelectSingleNode(xpaths[2]);
                dataCollectorsNavigator.AppendChild(codeCoverageCollectorSettingsTemplate);
            }
        }

        private static string GetMissingNodesTextIfAny(string existingPath, string fullpath)
        {
            var xmlText = "{0}";
            var nonExistingPath = fullpath.Substring(existingPath.Length);
            var requiredNodeNames = nonExistingPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var format = "<{0}>{1}</{0}>";

            foreach (var nodeName in requiredNodeNames)
            {
                xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Format(CultureInfo.InvariantCulture, format, nodeName, "{0}"));
            }

            xmlText = string.Format(CultureInfo.InvariantCulture, xmlText, string.Empty);
            return xmlText;
        }
    }
}
