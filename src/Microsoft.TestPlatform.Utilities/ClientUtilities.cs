// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Utilities used by the client to understand the environment of the current run.
    /// </summary>
    public static class ClientUtilities
    {
        private const string TestSettingsFileXPath = "RunSettings/MSTest/SettingsFile";
        private const string ResultsDirectoryXPath = "RunSettings/RunConfiguration/ResultsDirectory";

        /// <summary>
        /// Converts the relative paths in a runsetting file to absolue ones.
        /// </summary>
        /// <param name="xmlDocument">Xml Document containing Runsettings xml</param>
        /// <param name="path">Path of the .runsettings xml file</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public static void FixRelativePathsInRunSettings(XmlDocument xmlDocument, string path)
        {
            if (xmlDocument == null)
            {
                throw new ArgumentNullException("xPathNavigator");
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string root = Path.GetDirectoryName(path);
            var testRunSettingsNode = xmlDocument.SelectSingleNode(TestSettingsFileXPath);
            if (testRunSettingsNode != null)
            {
                FixNodeFilePath(testRunSettingsNode, root);
            }

            var resultsDirectoryNode = xmlDocument.SelectSingleNode(ResultsDirectoryXPath);
            if (resultsDirectoryNode != null)
            {
                FixNodeFilePath(resultsDirectoryNode, root);
            }
        }

        private static void FixNodeFilePath(XmlNode node, string root)
        {
            string fileName = node.InnerXml;
            fileName = Environment.ExpandEnvironmentVariables(fileName);

            if (!string.IsNullOrEmpty(fileName)
                    && !Path.IsPathRooted(fileName))
            {
                // We have a relative file path...
                fileName = Path.Combine(root, fileName);
                fileName = Path.GetFullPath(fileName);
            }

            node.InnerXml = fileName;
        }
    }
}
