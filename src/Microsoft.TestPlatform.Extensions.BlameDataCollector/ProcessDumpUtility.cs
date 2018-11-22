// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.TestPlatform.Extensions.BlameDataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility : IProcessDumpUtility
    {
        private static List<string> procDumpExceptionsList;
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private IEnvironment environment;
        private Process procDumpProcess;
        private string testResultsDirectory;
        private string dumpFileName;
        private INativeMethodsHelper nativeMethodsHelper;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment(), new NativeMethodsHelper())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment, INativeMethodsHelper nativeMethodsHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.environment = environment;
            this.nativeMethodsHelper = nativeMethodsHelper;
            ProcessDumpUtility.procDumpExceptionsList = new List<string>()
            {
                "STACK_OVERFLOW",
                "ACCESS_VIOLATION"
            };
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
        public void StartProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}";
            this.testResultsDirectory = testResultsDirectory;

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(processId),
                                            ProcessDumpUtility.BuildProcDumpArgs(processId, this.dumpFileName, isFullDump),
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
        }

        /// <inheritdoc/>
        public void TerminateProcess()
        {
            try
            {
                EqtTrace.Info("ProcessDumpUtility : Attempting to kill proc dump process.");
                this.processHelper.TerminateProcess(this.procDumpProcess);
            }
            catch (Exception e)
            {
                EqtTrace.Warning($"ProcessDumpUtility : Failed to kill proc dump process with exception {e}");
            }
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
        /// <param name="isFullDump">
        /// Is full dump enabled
        /// </param>
        /// <returns>Arguments</returns>
        private static string BuildProcDumpArgs(int processId, string filename, bool isFullDump = false)
        {
            // -accepteula: Auto accept end-user license agreement
            // -e: Write a dump when the process encounters an unhandled exception. Include the 1 to create dump on first chance exceptions.
            // -g: Run as a native debugger in a managed process (no interop).
            // -t: Write a dump when the process terminates.
            // -ma: Full dump argument.
            // -f: Filter the exceptions.
            StringBuilder procDumpArgument = new StringBuilder("-accepteula -e 1 -g -t ");
            if (isFullDump)
            {
                procDumpArgument.Append("-ma ");
            }

            foreach (var exceptionFilter in ProcessDumpUtility.procDumpExceptionsList)
            {
                procDumpArgument.Append($"-f {exceptionFilter} ");
            }

            procDumpArgument.Append($"{processId} {filename}.dmp");
            var argument = procDumpArgument.ToString();

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"ProcessDumpUtility : The procdump argument is {argument}");
            }

            return argument;
        }

        /// <summary>
        /// Get procdump executable path
        /// </summary>
        /// <param name="processId">
        /// Process Id
        /// </param>
        /// <returns>procdump executable path</returns>
        private string GetProcDumpExecutable(int processId)
        {
            var procdumpPath = Environment.GetEnvironmentVariable("PROCDUMP_PATH");

            if (!string.IsNullOrWhiteSpace(procdumpPath))
            {
                string filename = string.Empty;

                // Launch procdump according to process architecture
                if (this.environment.Architecture == PlatformArchitecture.X86)
                {
                    filename = Constants.ProcdumpProcess;
                }
                else
                {
                     filename = this.nativeMethodsHelper.Is64Bit(this.processHelper.GetProcessHandle(processId)) ?
                     Constants.Procdump64Process : Constants.ProcdumpProcess;
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