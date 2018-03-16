// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility : IProcessDumpUtility
    {
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private IEnvironment environment;
        private Process procDumpProcess;
        private string testResultsDirectory;
        private string dumpFileName;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.environment = environment;
        }

        /// <inheritdoc/>
        public string GetDumpFile()
        {
            if (this.procDumpProcess == null)
            {
                return string.Empty;
            }

            this.processHelper.WaitForProcessExit(this.procDumpProcess);

            // Dump files can never be more than 1 because procdump will generate single file, but GetFiles function returns an array
            var dumpFiles = this.fileHelper.GetFiles(this.testResultsDirectory, this.dumpFileName + "*", SearchOption.TopDirectoryOnly);
            if (dumpFiles.Length > 0)
            {
                // Log to diagnostics if multiple files just in case
                if (dumpFiles.Length != 1)
                {
                    EqtTrace.Warning("ProcessDumpUtility.GetDumpFile: Multiple dump files found.");
                }

                return dumpFiles[0];
            }

            if (EqtTrace.IsErrorEnabled)
            {
                int exitCode;
                EqtTrace.Error("ProcessDumpUtility.GetDumpFile: No dump file generated.");
                if (this.processHelper.TryGetExitCode(this.procDumpProcess, out exitCode))
                {
                    EqtTrace.Error("ProcessDumpUtility.GetDumpFile: Procdump exited with code: {0}", exitCode);
                }
            }

            throw new FileNotFoundException(Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }

        /// <inheritdoc/>
        public void StartProcessDump(int processId, string dumpFileGuid, string testResultsDirectory)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}";
            this.testResultsDirectory = testResultsDirectory;

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(),
                                            ProcessDumpUtility.BuildProcDumpArgs(processId, this.dumpFileName),
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
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
            // -accepteula: Auto accept end-user license agreement
            // -t: Write a dump when the process terminates.
            // This will create a minidump of the process with specified filename
            return "-accepteula -t " + processId + " " + filename + ".dmp";
        }

        /// <summary>
        /// Get procdump executable path
        /// </summary>
        /// <returns>procdump executable path</returns>
        private string GetProcDumpExecutable()
        {
            var procdumpPath = Environment.GetEnvironmentVariable("PROCDUMP_PATH");
            if (!string.IsNullOrWhiteSpace(procdumpPath))
            {
                string filename = string.Empty;

                if (this.environment.Architecture == PlatformArchitecture.X64)
                {
                    filename = "procdump64.exe";
                }
                else if (this.environment.Architecture == PlatformArchitecture.X86)
                {
                    filename = "procdump.exe";
                }

                var procDumpExe = Path.Combine(procdumpPath, filename);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("Using procdump at: {0}", procDumpExe);
                }

                return procDumpExe;
            }
            else
            {
                throw new TestPlatformException(Resources.Resources.ProcDumpEnvVarEmpty);
            }
        }
    }
}
