// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.IO;
    using Microsoft.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.Win32;

    /// <summary>
    /// BlameDumpFolder class to get dump file name
    /// </summary>
    public class BlameDumpFolder : IBlameDumpFolder
    {
        /// <summary>
        /// Registry for the local crash dumps.
        /// </summary>
        private const string LocalCrashDumpRegistryKey = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps";

        /// <summary>
        /// Registry value name for the dumps folder.
        /// </summary>
        private const string DumpFolderValueName = @"DumpFolder";

        /// <summary>
        /// Default dumps folder.
        /// </summary>
        private const string DefaultCrashDumpFolder = @"%LOCALAPPDATA%\\CrashDumps";

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameDumpFolder"/> class.
        /// </summary>
        public BlameDumpFolder()
        {
        }

        /// <summary>
        /// Gets the local crash dump path
        /// </summary>
        /// <param name="applicationName">Application Name</param>
        /// <param name="crashDumpPath">CrashDumpPath</param>
        /// <returns>LocalCrashDumpEnabed</returns>
        public bool GetCrashDumpFolderPath(string applicationName, out string crashDumpPath)
        {
            if (applicationName == null)
            {
                throw new ArgumentNullException(applicationName);
            }

            bool localCrashDumpsEnabled = false;
            crashDumpPath = string.Empty;

            try
            {
                using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var localCrashDumpSubKey = hklmKey.OpenSubKey(LocalCrashDumpRegistryKey))
                {
                    localCrashDumpsEnabled = localCrashDumpSubKey != null;
                    if (localCrashDumpsEnabled)
                    {
                        string dumpFolder = string.Empty;

                        using (var applicatonCrashDumpSubKey = localCrashDumpSubKey.OpenSubKey(applicationName))
                        {
                            if (applicatonCrashDumpSubKey != null)
                            {
                                dumpFolder = this.ReadDumpsFolderPath(applicatonCrashDumpSubKey);
                            }
                            else
                            {
                                dumpFolder = this.ReadDumpsFolderPath(localCrashDumpSubKey);
                            }
                        }

                        if (!string.IsNullOrEmpty(dumpFolder) && Directory.Exists(dumpFolder))
                        {
                            crashDumpPath = dumpFolder as string;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return localCrashDumpsEnabled;
        }

        /// <summary>
        /// Reads Dumps Folder path from registry key
        /// </summary>
        /// <param name="registryKey">Dumps Registry Key</param>
        /// <returns>Folder Path</returns>
        private string ReadDumpsFolderPath(RegistryKey registryKey)
        {
            string dumpFolder = registryKey.GetValue(DumpFolderValueName) as string;
            if (string.IsNullOrEmpty(dumpFolder))
            {
                // Fallback to default dumps folder path, if none is given explicitly...
                dumpFolder = DefaultCrashDumpFolder;
            }

            dumpFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dumpFolder));

            return dumpFolder;
        }
    }
}
