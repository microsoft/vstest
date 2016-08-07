// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Utilities used by the client to understand the environment of the current run.
    /// </summary>
    public static class ClientUtilities
    {
        /// <summary>
        /// Manifest file name to check if vstest.console.exe is running in portable mode
        /// </summary>
        internal const string PortableVsTestManifestFilename = "Portable.VsTest.Manifest";

        /// <summary>
        /// The test settings file x path.
        /// </summary>
        private const string TestSettingsFileXPath = "RunSettings/MSTest/SettingsFile";

        /// <summary>
        /// The results directory x path.
        /// </summary>
        private const string ResultsDirectoryXPath = "RunSettings/RunConfiguration/ResultsDirectory";

        /// <summary>
        /// Converts the relative paths in a runsetting file to absolute ones.
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

            var root = Path.GetDirectoryName(path);
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

        /// <summary>
        /// Check if Vstest.console is running in xcopyable mode
        /// </summary>
        /// <returns>true if vstest is running in xcopyable mode</returns>
        public static bool IsTestProcessRunningInXcopyableMode()
        {
            return IsTestProcessRunningInXcopyableMode(Process.GetCurrentProcess().MainModule.FileName);
        }

        /// <summary>
        /// Check if Vstest.console is running in xcopyable mode given exe path
        /// </summary>
        /// <param name="exeName">
        /// The exe Name.
        /// </param>
        /// <returns>
        /// true if vstest is running in xcopyable mode 
        /// </returns>
        public static bool IsTestProcessRunningInXcopyableMode(string exeName)
        {
            // Get the directory of the exe 
            var exeDir = Path.GetDirectoryName(exeName);
            return File.Exists(Path.Combine(exeDir, PortableVsTestManifestFilename));
        }

        /// <summary>
        /// The fix node file path.
        /// </summary>
        /// <param name="node">
        /// The node.
        /// </param>
        /// <param name="root">
        /// The root.
        /// </param>
        private static void FixNodeFilePath(XmlNode node, string root)
        {
            var fileName = node.InnerXml;

            if (!string.IsNullOrEmpty(fileName)
                    && !Path.IsPathRooted(fileName))
            {
                // We have a relative file path...
                fileName = Path.Combine(root, fileName);
                fileName = Path.GetFullPath(fileName);

                node.InnerXml = fileName;
            }
        }
    }
}