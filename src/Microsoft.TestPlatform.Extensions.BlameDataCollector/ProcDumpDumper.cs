// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcDumpDumper : ICrashDumper, IHangDumper
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
        private bool collectAlways;
        private string outputDirectory;
        private Process process;
        private string outputFilePrefix;

        public ProcDumpDumper()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment(), new NativeMethodsHelper())
        {
        }

        public ProcDumpDumper(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment, INativeMethodsHelper nativeMethodsHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.environment = environment;
            this.nativeMethodsHelper = nativeMethodsHelper;
        }

        protected Action<object, string> OutputReceivedCallback => (process, data) =>
        {
            // useful for visibility when debugging this tool
            // Console.ForegroundColor = ConsoleColor.Cyan;
            // Console.WriteLine(data);
            // Console.ForegroundColor = ConsoleColor.White;
            // Log all standard output message of procdump in diag files.
            // Otherwise they end up coming on console in pipleine.
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("ProcDumpDumper.OutputReceivedCallback: Output received from procdump process: " + data);
            }
        };

        /// <inheritdoc/>
        public void WaitForDumpToFinish()
        {
            if (this.processHelper == null)
            {
                EqtTrace.Info($"ProcDumpDumper.WaitForDumpToFinish: ProcDump was not previously attached, this might indicate error during setup, look for ProcDumpDumper.AttachToTargetProcess.");
            }

            this.processHelper?.WaitForProcessExit(this.procDumpProcess);
        }

        /// <inheritdoc/>
        public void AttachToTargetProcess(int processId, string outputDirectory, DumpTypeOption dumpType, bool collectAlways)
        {
            this.collectAlways = collectAlways;
            this.outputDirectory = outputDirectory;
            this.process = Process.GetProcessById(processId);
            this.outputFilePrefix = $"{this.process.ProcessName}_{this.process.Id}_{DateTime.Now:yyyyMMddTHHmmss}_crashdump";
            var outputFile = Path.Combine(outputDirectory, $"{this.outputFilePrefix}.dmp");
            EqtTrace.Info($"ProcDumpDumper.AttachToTargetProcess: Attaching to process '{processId}' to dump into '{outputFile}'.");

            // Procdump will append .dmp at the end of the dump file. We generate this internally so it is rather a safety check.
            if (!outputFile.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Procdump crash dump file must end with .dmp extension.");
            }

            if (!this.TryGetProcDumpExecutable(processId, out var procDumpPath))
            {
                var err = $"{procDumpPath} could not be found, please set PROCDUMP_PATH environment variable to a directory that contains {procDumpPath} executable, or make sure that the executable is available on PATH.";
                ConsoleOutput.Instance.Warning(false, err);
                EqtTrace.Error($"ProcDumpDumper.AttachToTargetProcess: {err}");
                return;
            }

            this.tempDirectory = Path.GetDirectoryName(outputFile);
            this.dumpFileName = Path.GetFileNameWithoutExtension(outputFile);

            string procDumpArgs = new ProcDumpArgsBuilder().BuildTriggerBasedProcDumpArgs(
                processId,
                this.dumpFileName,
                ProcDumpExceptionsList,
                isFullDump: dumpType == DumpTypeOption.Full);

            EqtTrace.Info($"ProcDumpDumper.AttachToTargetProcess: Running ProcDump with arguments: '{procDumpArgs}'.");
            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            procDumpPath,
                                            procDumpArgs,
                                            this.tempDirectory,
                                            null,
                                            null,
                                            null,
                                            this.OutputReceivedCallback) as Process;

            EqtTrace.Info($"ProcDumpDumper.AttachToTargetProcess: ProcDump started as process with id '{this.procDumpProcess.Id}'.");
        }

        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            if (this.procDumpProcess == null)
            {
                EqtTrace.Info($"ProcDumpDumper.DetachFromTargetProcess: ProcDump was not previously attached, this might indicate error during setup, look for ProcDumpDumper.AttachToTargetProcess.");
                return;
            }

            try
            {
                EqtTrace.Info($"ProcDumpDumper.DetachFromTargetProcess: ProcDump detaching from target process '{targetProcessId}'.");
                new Win32NamedEvent($"Procdump-{targetProcessId}").Set();
            }
            finally
            {
                try
                {
                    EqtTrace.Info("ProcDumpDumper.DetachFromTargetProcess: Attempting to kill proc dump process.");
                    this.processHelper.TerminateProcess(this.procDumpProcess);
                }
                catch (Exception e)
                {
                    EqtTrace.Warning($"ProcDumpDumper.DetachFromTargetProcess: Failed to kill proc dump process with exception {e}");
                }
            }
        }

        public IEnumerable<string> GetDumpFiles(bool processCrashed)
        {
            var allDumps = this.fileHelper.DirectoryExists(this.outputDirectory)
                ? this.fileHelper.GetFiles(this.outputDirectory, "*_crashdump*.dmp", SearchOption.AllDirectories)
                : Array.Empty<string>();

            // We are always collecting dump on exit even when collectAlways option is false, to make sure we collect
            // dump for Environment.FailFast. So there always can be a dump if the process already exited. In most cases
            // this was just a normal process exit that was not caused by an exception and user is not interested in getting that
            // dump because it only pollutes their CI.
            // The hangdumps and crash dumps actually end up in the same folder, but we can distinguish them based on the _crashdump suffix.
            if (this.collectAlways)
            {
                return allDumps;
            }

            if (processCrashed)
            {
                return allDumps;
            }

            // There can be more dumps in the crash folder from the child processes that were .NET5 or newer and crashed
            // get only the ones that match the path we provide to procdump. And get the last one created.
            var allTargetProcessDumps = allDumps
                .Where(dump => Path.GetFileNameWithoutExtension(dump)
                .StartsWith(this.outputFilePrefix))
                .Select(dump => new FileInfo(dump))
                .OrderBy(dump => dump.LastWriteTime).ThenBy(dump => dump.Name)
                .ToList();

            var dumpToRemove = allTargetProcessDumps.LastOrDefault();

            if (dumpToRemove != null)
            {
                EqtTrace.Verbose($"ProcDumpDumper.GetDumpFiles: Found {allTargetProcessDumps.Count} dumps for the target process, removing {dumpToRemove.Name} because we always collect a dump, even if there is no crash. But the process did not crash and user did not specify CollectAlways=true.");
                try
                {
                    File.Delete(dumpToRemove.FullName);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"ProcDumpDumper.GetDumpFiles: Removing dump failed with: {ex}");
                    EqtTrace.Error(ex);
                }
            }

            return allTargetProcessDumps.Take(allTargetProcessDumps.Count - 1).Select(dump => dump.FullName).ToList();
        }

        // Hang dumps the process using procdump.
        public void Dump(int processId, string outputDirectory, DumpTypeOption dumpType)
        {
            var process = Process.GetProcessById(processId);
            var outputFile = Path.Combine(outputDirectory, $"{process.ProcessName}_{processId}_{DateTime.Now:yyyyMMddTHHmmss}_hangdump.dmp");
            EqtTrace.Info($"ProcDumpDumper.Dump: Hang dumping process '{processId}' to dump into '{outputFile}'.");

            // Procdump will append .dmp at the end of the dump file. We generate this internally so it is rather a safety check.
            if (!outputFile.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Procdump crash dump file must end with .dmp extension.");
            }

            if (!this.TryGetProcDumpExecutable(processId, out var procDumpPath))
            {
                var err = $"{procDumpPath} could not be found, please set PROCDUMP_PATH environment variable to a directory that contains {procDumpPath} executable, or make sure that the executable is available on PATH.";
                ConsoleOutput.Instance.Warning(false, err);
                EqtTrace.Error($"ProcDumpDumper.Dump: {err}");
                return;
            }

            var tempDirectory = Path.GetDirectoryName(outputFile);
            var dumpFileName = Path.GetFileNameWithoutExtension(outputFile);

            string procDumpArgs = new ProcDumpArgsBuilder().BuildHangBasedProcDumpArgs(
                processId,
                dumpFileName,
                isFullDump: dumpType == DumpTypeOption.Full);

            EqtTrace.Info($"ProcDumpDumper.Dump: Running ProcDump with arguments: '{procDumpArgs}'.");
            var procDumpProcess = this.processHelper.LaunchProcess(
                                            procDumpPath,
                                            procDumpArgs,
                                            tempDirectory,
                                            null,
                                            null,
                                            null,
                                            this.OutputReceivedCallback) as Process;

            EqtTrace.Info($"ProcDumpDumper.Dump: ProcDump started as process with id '{procDumpProcess.Id}'.");

            this.processHelper?.WaitForProcessExit(procDumpProcess);

            EqtTrace.Info($"ProcDumpDumper.Dump: ProcDump finished hang dumping process with id '{processId}'.");
        }

        /// <summary>
        /// Try get proc dump executable path from env variable or PATH, if it does not success the result is false, and the name of the exe we tried to find.
        /// </summary>
        /// <param name="processId">
        /// Process Id to determine the bittness
        /// </param>
        /// <param name="path">
        /// Path to procdump or the name of the executable we tried to resolve when we don't find it
        /// </param>
        /// <returns>proc dump executable path</returns>
        private bool TryGetProcDumpExecutable(int processId, out string path)
        {
            var procdumpDirectory = Environment.GetEnvironmentVariable("PROCDUMP_PATH");
            var searchPath = false;
            if (string.IsNullOrWhiteSpace(procdumpDirectory))
            {
                EqtTrace.Verbose("ProcDumpDumper.GetProcDumpExecutable: PROCDUMP_PATH env variable is empty will try to run ProcDump from PATH.");
                searchPath = true;
            }
            else if (!Directory.Exists(procdumpDirectory))
            {
                EqtTrace.Verbose($"ProcDumpDumper.GetProcDumpExecutable: PROCDUMP_PATH env variable '{procdumpDirectory}' is not a directory, or the directory does not exist. Will try to run ProcDump from PATH.");
                searchPath = true;
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
            else if (this.environment.OperatingSystem == PlatformOperatingSystem.Unix)
            {
                filename = Constants.ProcdumpUnixProcess;
            }
            else
            {
                throw new NotSupportedException($"Not supported platform {this.environment.OperatingSystem}");
            }

            if (!searchPath)
            {
                var candidatePath = Path.Combine(procdumpDirectory, filename);
                if (File.Exists(candidatePath))
                {
                    EqtTrace.Verbose($"ProcDumpDumper.GetProcDumpExecutable: Path to ProcDump '{candidatePath}' exists, using that.");
                    path = candidatePath;
                    return true;
                }

                EqtTrace.Verbose($"ProcDumpDumper.GetProcDumpExecutable: Path '{candidatePath}' does not exist will try to run {filename} from PATH.");
            }

            if (this.TryGetExecutablePath(filename, out var p))
            {
                EqtTrace.Verbose($"ProcDumpDumper.GetProcDumpExecutable: Resolved {filename} to {p} from PATH.");
                path = p;
                return true;
            }

            EqtTrace.Verbose($"ProcDumpDumper.GetProcDumpExecutable: Could not find {filename} on PATH.");
            path = filename;
            return false;
        }

        private bool TryGetExecutablePath(string executable, out string executablePath)
        {
            executablePath = string.Empty;
            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(Path.PathSeparator))
            {
                string exeFullPath = Path.Combine(path.Trim(), executable);
                if (this.fileHelper.Exists(exeFullPath))
                {
                    executablePath = exeFullPath;
                    return true;
                }
            }

            return false;
        }
    }
}