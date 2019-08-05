// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.TestPlatform.Extensions.BlameDataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility : IProcessDumpUtility
    {
        private static readonly List<string> ProcDumpExceptionsList = new List<string>()
        {
            "STACK_OVERFLOW",
            "ACCESS_VIOLATION"
        };

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
        public void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}";
            this.testResultsDirectory = testResultsDirectory;

            string procDumpArgs = new ProcDumpArgsBuilder().BuildTriggerBasedProcDumpArgs(
                processId,
                this.dumpFileName,
                ProcDumpExceptionsList,
                isFullDump);

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"ProcessDumpUtility : The procdump argument is {procDumpArgs}");
            }

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(processId),
                                            procDumpArgs,
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
        }

        /// <inheritdoc/>
        public void StartHangBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}_hangdump";
            this.testResultsDirectory = testResultsDirectory;

            string procDumpArgs = new ProcDumpArgsBuilder().BuildHangBasedProcDumpArgs(
                processId,
                this.dumpFileName,
                isFullDump);

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"ProcessDumpUtility : The hang based procdump invocation argument is {procDumpArgs}");
            }

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(processId),
                                            procDumpArgs,
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
        }

        /// <inheritdoc/>
        public void DetachFromTargetProcess()
        {
            new Win32NamedEvent($"Procdump-{this.procDumpProcess.Id}").Set();
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