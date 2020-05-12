// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcDumpCrashDumper : ICrashDumper
    {
        private static readonly IEnumerable<string> ProcDumpExceptionsList = new List<string>()
        {
            "STACK_OVERFLOW",
            "ACCESS_VIOLATION"
        };

        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private IEnvironment environment;
        private Process procDumpProcess;
        private string tempDirectory;
        private string dumpFileName;
        private INativeMethodsHelper nativeMethodsHelper;

        public ProcDumpCrashDumper()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment(), new NativeMethodsHelper())
        {
        }

        public ProcDumpCrashDumper(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment, INativeMethodsHelper nativeMethodsHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.environment = environment;
            this.nativeMethodsHelper = nativeMethodsHelper;
        }

        protected Action<object, string> OutputReceivedCallback => (process, data) =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(data);
            Console.ForegroundColor = ConsoleColor.White;
            // Log all standard output message of procdump in diag files.
            // Otherwise they end up coming on console in pipleine.
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("ProcDumpCrashDumper.OutputReceivedCallback: Output received from procdump process: " + data);
            }
        };

        /// <inheritdoc/>
        public void WaitForDumpToFinish()
        {
            if (this.procDumpProcess == null)
            {
                return;
            }

            this.processHelper.WaitForProcessExit(this.procDumpProcess);
        }

        /// <inheritdoc/>
        //public void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump, string targetFramework)
        public void AttachToTargetProcess(int processId, string outputFile, DumpTypeOption dumpType)
        {
            EqtTrace.Info($"ProcDumpCrashDumper.AttachToTargetProcess: Attaching to process '{processId}' to dump into '{outputFile}'.");

            // Procdump will append .dmp at the end of the dump file. We generate this internally so it is rather a safety check.
            if (!outputFile.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("Procdump crash dump file must end with .dmp extension.");
            }
            this.tempDirectory = Path.GetDirectoryName(outputFile);
            this.dumpFileName = Path.GetFileNameWithoutExtension(outputFile);

            string procDumpArgs = new ProcDumpArgsBuilder().BuildTriggerBasedProcDumpArgs(
                processId,
                this.dumpFileName,
                ProcDumpExceptionsList,
                isFullDump: dumpType == DumpTypeOption.Full);


            EqtTrace.Info($"ProcDumpCrashDumper.AttachToTargetProcess: Running ProcDump with arguments: '{procDumpArgs}'.");
            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(processId),
                                            procDumpArgs,
                                            tempDirectory,
                                            null,
                                            null,
                                            null,
                                            this.OutputReceivedCallback) as Process;

             EqtTrace.Info($"ProcDumpCrashDumper.AttachToTargetProcess: ProcDump started as process with id '{this.procDumpProcess.Id}'.");
        }


        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            if (this.procDumpProcess == null)
            {
                return;
            }

            try
            {
                EqtTrace.Info($"ProcDumpCrashDumper.DetachFromTargetProcess: ProcDump detaching from target process '{targetProcessId}'.");
                new Win32NamedEvent($"Procdump-{targetProcessId}").Set();
            }
            finally
            {
                try
                {
                    EqtTrace.Info("ProcDumpCrashDumper.DetachFromTargetProcess: Attempting to kill proc dump process.");
                    this.processHelper.TerminateProcess(this.procDumpProcess);
                }
                catch (Exception e)
                {
                    EqtTrace.Warning($"ProcDumpCrashDumper.DetachFromTargetProcess: Failed to kill proc dump process with exception {e}");
                }
            }
        }

        /// <summary>
        /// Get proc dump executable path
        /// </summary>
        /// <param name="processId">
        /// Process Id
        /// </param>
        /// <returns>proc dump executable path</returns>
        private string GetProcDumpExecutable(int processId)
        {
            var here = Path.GetDirectoryName(typeof(ProcDumpCrashDumper).Assembly.Location);
            var procdumpPath = Path.Combine(here, "procdump"); // Environment.GetEnvironmentVariable("PROCDUMP_PATH");

            if (string.IsNullOrWhiteSpace(procdumpPath))
            {
                throw new TestPlatformException(Resources.Resources.ProcDumpEnvVarEmpty);

            }

            string filename;
            if (this.environment.OperatingSystem == PlatformOperatingSystem.Windows)
            {
                // Launch proc dump according to process architecture
                if (this.environment.Architecture == PlatformArchitecture.X86)
                {
                    filename = Constants.ProcdumpProcess;
                }
                else
                {
                    filename = this.nativeMethodsHelper.Is64Bit(this.processHelper.GetProcessHandle(processId)) ?
                    Constants.Procdump64Process : Constants.ProcdumpProcess;
                }
            }
            //else if (this.environment.OperatingSystem == PlatformOperatingSystem.Unix) {
            //    filename = Constants.ProcdumpUnixProcess;
            //}
            else
            {
                throw new NotSupportedException($"Not supported platform {this.environment.OperatingSystem}");
            }

            var procDumpExe = Path.Combine(procdumpPath, filename);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ProcDumpCrashDumper.GetProcDumpExecutable: Using proc dump at: {0}", procDumpExe);
            }

            return procDumpExe;
        }
    }
}