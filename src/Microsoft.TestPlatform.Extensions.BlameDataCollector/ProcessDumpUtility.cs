// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility : IProcessDumpUtility
    {
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private Process procDumpProcess;
        private string testResultsDirectory;
        private string dumpFileName;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.procDumpProcess = null;
        }

        /// <inheritdoc/>
        public string GetDumpFile()
        {
            if (this.procDumpProcess == null)
            {
                return string.Empty;
            }

            this.processHelper.WaitForProcessExit(this.procDumpProcess);

            var dumpFile = Path.Combine(this.testResultsDirectory, this.dumpFileName);
            if (this.fileHelper.Exists(dumpFile))
            {
                return dumpFile;
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Verbose(string.Format(CultureInfo.InvariantCulture, "ProcessDumpUtility: GetCrashDumpFile: No dump file generated."));
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public void StartProcessDump(int processId, string dumpFileGuid, string testResultsDirectory)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{dumpFileGuid}.dmp";
            this.testResultsDirectory = testResultsDirectory;

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            ProcessDumpUtility.GetProcDumpExecutable(),
                                            ProcessDumpUtility.BuildProcDumpArgs(processId, this.dumpFileName),
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
        }

        /// <summary>
        /// Get procdump executable path
        /// </summary>
        /// <returns>procdump executable path</returns>
        private static string GetProcDumpExecutable()
        {
            var procDumpExe = Path.Combine(Path.GetDirectoryName(typeof(BlameCollector).GetTypeInfo().Assembly.GetAssemblyLocation()), "Extensions", "procdump64.exe");
            return procDumpExe;
        }

        /// <summary>
        /// Arguments for procdump.exe
        /// </summary>
        /// <param name="processId">
        /// Process Id
        /// </param>
        /// <param name="filename">
        /// Filename for dump file
        /// </param>
        /// <returns>Arguments</returns>
        private static string BuildProcDumpArgs(int processId, string filename)
        {
            return "-t -g -ma " + processId + " " + filename;
        }
    }
}
