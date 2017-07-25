// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.Win32;

    /// <summary>
    /// LocalCrashDumpUtilities class to get dump file name
    /// </summary>
    public class LocalCrashDumpUtilities
    {
        /// <summary>
        /// Forward link for guidance to enable local crash dumps.
        /// </summary>
        private const string EnableLocalCrashDumpFwLink = "http://go.microsoft.com/fwlink/?linkid=232477";

        /// <summary>
        /// The file helper
        /// </summary>
        private IFileHelper fileHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCrashDumpUtilities"/> class.
        /// </summary>
        public LocalCrashDumpUtilities()
            : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCrashDumpUtilities"/> class.
        /// </summary>
        /// <param name="fileHelper">File Helper</param>
        protected LocalCrashDumpUtilities(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets the forward link for guidance to enable the local crash dumps.
        /// </summary>
        public static string EnableLocalCrashDumpForwardLink
        {
            get
            {
                return EnableLocalCrashDumpFwLink;
            }
        }

        /// <summary>
        /// Gets the crash dump file from paath
        /// </summary>
        /// <param name="dumpPath">Path for Dump Folder</param>
        /// <param name="applicationName">Application Name</param>
        /// <param name="processId">Process Id</param>
        /// <returns>Latest Crash Dump File</returns>
        public virtual string GetCrashDumpFile(string dumpPath, string applicationName, int processId)
        {
            ValidateArg.NotNull(dumpPath, "dumpPath");
            ValidateArg.NotNull(applicationName, "applicationName");
            ValidateArg.NotNegative(processId, "processId");

            string latestCrashDump = string.Empty;
            string searchPattern = string.Empty;
            if (!string.IsNullOrEmpty(applicationName))
            {
                // Dump file names are in format <applicationName>.<ProcessId>.dmp...
                // Dump file names are in format <applicationName>(1).<ProcessId>.dmp...
                searchPattern = applicationName + "*." + processId + "*";
            }

            if (!string.IsNullOrEmpty(dumpPath)
                    && this.fileHelper.DirectoryExists(dumpPath))
            {
                string[] dumpfiles = this.fileHelper.GetFiles(dumpPath, searchPattern, SearchOption.TopDirectoryOnly);

                if (dumpfiles.Length > 0)
                {
                    latestCrashDump = dumpfiles[0];
                    if (dumpfiles.Length > 1)
                    {
                        if (EqtTrace.IsWarningEnabled)
                        {
                            EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "LocalCrashDumpUtilities: GetCrashDumpFile: Mulitple crash dump file with name '{0}' found.", searchPattern));
                        }

                        // Find the latest one...
                        latestCrashDump = this.FindLatestFile(dumpfiles);
                    }
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Verbose(string.Format(CultureInfo.InvariantCulture, "LocalCrashDumpUtilities: GetCrashDumpFile: Latest crash dump file is: '{0}'.", latestCrashDump));
            }

            return latestCrashDump;
        }

        /// <summary>
        /// Finds Latest File from list of files
        /// </summary>
        /// <param name="files">files</param>
        /// <returns>Lates File</returns>
        private string FindLatestFile(string[] files)
        {
            string latestFile = files[0];
            DateTime latestFileModified = this.fileHelper.GetLastWriteTime(latestFile);

            DateTime fileModified = DateTime.MinValue;
            for (int i = 1; i < files.Length; ++i)
            {
                fileModified = this.fileHelper.GetLastWriteTime(files[i]);
                if (fileModified > latestFileModified)
                {
                    latestFile = files[i];
                    latestFileModified = fileModified;
                }
            }

            return latestFile;
        }
    }
}
