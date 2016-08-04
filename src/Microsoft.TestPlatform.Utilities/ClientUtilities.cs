// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    using Microsoft.Win32;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Utilities used by the client to understand the environment of the current run.
    /// </summary>
    public static class ClientUtilities
    {
        private const string TestSettingsFileXPath = "RunSettings/MSTest/SettingsFile";
        private const string ResultsDirectoryXPath = "RunSettings/RunConfiguration/ResultsDirectory";

        /// <summary>
        /// Manifest file name to check if vstest.console.exe is running in portable mode
        /// </summary>
        private const string PortableVsTestManifestFilename = "Portable.VsTest.Manifest";

        /// <summary>
        /// Registry Subkey for VisualStudio Root under HKLM Registry node for 32 bit process.
        /// </summary>
        private const string TaRootRegKey32 = @"Software\Microsoft\VisualStudio\15.0\Setup\VSTF\TestAgent";

        /// <summary>
        /// Registry Subkey for VisualStudio Root under HKLM Registry node for 64 bit process.
        /// </summary>
        private const string VsRootRegKey64 = @"Software\Wow6432Node\Microsoft\VisualStudio\15.0";


        /// <summary>
        /// Registry Subkey for VisualStudio Root under HKLM Registry node for 32 bit process.
        /// </summary>
        private const string VsRootRegKey32 = @"Software\Microsoft\VisualStudio\15.0";

        /// <summary>
        /// Registry Subkey for VisualStudio Root under HKLM Registry node for 64 bit process.
        /// </summary>
        private const string TaRootRegKey64 = @"Software\Wow6432Node\Microsoft\VisualStudio\15.0\Setup\VSTF\TestAgent";

        /// <summary>
        /// Registry key name for the TestAgent Install Directory.
        /// </summary>
        private const string ProductDirRegistryKeyName = "ProductDir";

        /// <summary>
        /// Environment variable which specifies the registry root
        /// (This key will be primarily used in rascalPro)
        /// </summary>
        private const string RegistryRootEnvironmentVariableName = @"VisualStudio_RootRegistryKey";

        /// <summary>
        /// Registry key name for the visual studio Install Directory.
        /// </summary>
        private const string InstallDirRegistryKeyName = "InstallDir";

        /// <summary>
        /// Converts the relative paths in a runsetting file to absolute ones.
        /// </summary>

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
        public static bool CheckIfTestProcessIsRunningInXcopyableMode()
        {
            return CheckIfTestProcessIsRunningInXcopyableMode(Process.GetCurrentProcess().MainModule.FileName);
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
        public static bool CheckIfTestProcessIsRunningInXcopyableMode(string exeName)
        {
            // Get the directory of the exe 
            var exeDir = Path.GetDirectoryName(exeName);
            return File.Exists(Path.Combine(exeDir, PortableVsTestManifestFilename));
        }

        /// <summary>
        /// Gets the Visual studio install location.
        /// </summary>
        /// <returns>VS install path</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static string GetVSInstallPath()
        {
            // Try custom Vs install path if available. This is done for rascal pro. 
            var vsInstallPathFromCustomRoot = GetVsInstallPathFromCustomRoot();
            if (!string.IsNullOrEmpty(vsInstallPathFromCustomRoot))
            {
                return vsInstallPathFromCustomRoot;
            }

            string path = null;
            var subKey = VsRootRegKey32;
            // Changing the key appropriately for 64 bit process.
            if (Is64BitProcess())
            {
                subKey = VsRootRegKey64;
            }
            using (var hklmKey = Registry.LocalMachine)
            {
                try
                {
                    RegistryKey visualstudioSubKey = hklmKey.OpenSubKey(subKey);
                    var registryValue = visualstudioSubKey.GetValue(InstallDirRegistryKeyName).ToString();
                    if (Directory.Exists(registryValue))
                    {
                        path = registryValue;
                    }
                }
                catch (Exception)
                {
                    //ignore the exception.
                }
            }

            return path;
        }

        /// <summary>
        /// Gets the TestAgent install location.
        /// </summary>
        /// <returns>TestAgent install path</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static string GetTestAgentInstallPath()
        {
            string path = null;
            var subKey = TaRootRegKey32;

            // Changing the key appropriately for 64 bit process.
            if (Is64BitProcess())
            {
                subKey = TaRootRegKey64;
            }
            using (var hklmKey = Registry.LocalMachine)
            {
                try
                {
                    using (RegistryKey visualstudioSubKey = hklmKey.OpenSubKey(subKey))
                    {
                        string registryValue = visualstudioSubKey.GetValue(ProductDirRegistryKeyName).ToString();
                        var installLocation = Path.Combine(registryValue, @"Common7\IDE");
                        if (Directory.Exists(installLocation))
                        {
                            path = installLocation;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore the exception.
                    EqtTrace.Verbose("Got following exception while searching for testAgent key {0}", ex.Message);
                }
            }

            return path;
        }



        /// <summary>
        /// Return true if the process executing is 64 bit process.
        /// In 32 bit processes, IntPtr size is 4 and not 8
        /// </summary>
        /// <returns>The bool</returns>
        private static bool Is64BitProcess()
        {
            return IntPtr.Size == 8;
        }


        /// <summary>
        /// Get Vs install path from custom root
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to ignore failures to read the registry settings")]
        private static string GetVsInstallPathFromCustomRoot()
        {
            try
            {
                var registryKeyWhichContainsVsInstallPath = GetEnvironmentVariable(RegistryRootEnvironmentVariableName);

                if (string.IsNullOrEmpty(registryKeyWhichContainsVsInstallPath))
                {
                    return null;
                }

                // Rascal always uses currentUser hive
                // todo: OpenremoteBaseKey method doesn't exist in dotnet core.
#if NET46
                using (RegistryKey hiveKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.CurrentUser, string.Empty))
                {
                    var visualstudioSubKey = hiveKey.OpenSubKey(registryKeyWhichContainsVsInstallPath);
                    var registryValue = visualstudioSubKey.GetValue("InstallDir").ToString();
                    if (Directory.Exists(registryValue))
                    {
                        return registryValue;
                    }
                }
#endif
            }
            catch (Exception)
            {
                // ignore the exception.
            }

            return null;
        }

        /// <summary>
        /// Returns the value of the environment variable
        /// </summary>
        /// <param name="keyName">
        /// The key Name.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetEnvironmentVariable(string keyName)
        {
            var value = Environment.GetEnvironmentVariable(keyName);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            using (var key = Registry.CurrentUser.OpenSubKey("Environment", false))
            {
                return key?.GetValue(keyName) as string;
            }
        }

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