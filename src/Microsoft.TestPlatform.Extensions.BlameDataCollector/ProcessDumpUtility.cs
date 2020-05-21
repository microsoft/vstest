// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    internal class ProcessDumpUtility : IProcessDumpUtility
    {
        private readonly IProcessHelper processHelper;
        private readonly IFileHelper fileHelper;
        private readonly IHangDumperFactory hangDumperFactory;
        private readonly ICrashDumperFactory crashDumperFactory;
        private ICrashDumper crashDumper;
        private string hangDumpPath;
        private string crashDumpPath;
        private bool wasHangDumped;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper(), new HangDumperFactory(), new CrashDumperFactory())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IHangDumperFactory hangDumperFactory, ICrashDumperFactory crashDumperFactory)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.hangDumperFactory = hangDumperFactory;
            this.crashDumperFactory = crashDumperFactory;
        }

        protected Action<object, string> OutputReceivedCallback => (process, data) =>
        {
            // Log all standard output message of procdump in diag files.
            // Otherwise they end up coming on console in pipleine.
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("ProcessDumpUtility.OutputReceivedCallback: Output received from procdump process: " + data);
            }
        };

        /// <inheritdoc/>
        public string GetDumpFile()
        {
            string dumpPath;
            if (!this.wasHangDumped)
            {
                this.crashDumper.WaitForDumpToFinish();
                dumpPath = this.crashDumpPath;
            }
            else
            {
                dumpPath = this.hangDumpPath;
            }

            EqtTrace.Info($"ProcessDumpUtility.GetDumpFile: Looking for dump file '{dumpPath}'.");
            var found = this.fileHelper.Exists(dumpPath);
            if (found)
            {
                EqtTrace.Info($"ProcessDumpUtility.GetDumpFile: Found dump file '{dumpPath}'.");
                return dumpPath;
            }

            EqtTrace.Error($"ProcessDumpUtility.GetDumpFile: Dump file '{dumpPath}' was not found.");
            throw new FileNotFoundException(Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }

        /// <inheritdoc/>
        public void StartHangBasedProcessDump(int processId, string dumpFileGuid, string tempDirectory, bool isFullDump, string targetFramework)
        {
            this.HangDump(processId, dumpFileGuid, tempDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework);
        }

        /// <inheritdoc/>
        public void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump, string targetFramework)
        {
            this.CrashDump(processId, dumpFileGuid, testResultsDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework);
        }

        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            this.crashDumper?.DetachFromTargetProcess(targetProcessId);
        }

        private void CrashDump(int processId, string dumpFileGuid, string tempDirectory, DumpTypeOption dumpType, string targetFramework)
        {
            var dumpPath = this.GetDumpPath(processId, dumpFileGuid, tempDirectory, isHangDump: false, out var processName);

            EqtTrace.Info($"ProcessDumpUtility.CrashDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{dumpPath}'.");
            this.crashDumpPath = dumpPath;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new NotSupportedException($"Operating system {RuntimeInformation.OSDescription} is not supported for crash dumps.");
            }

            this.crashDumper = this.crashDumperFactory.Create(targetFramework);
            ConsoleOutput.Instance.Information(false, $"Blame: Attaching crash dump utility to process {processName} ({processId}).");
            this.crashDumper.AttachToTargetProcess(processId, dumpPath, dumpType);
        }

        private void HangDump(int processId, string dumpFileGuid, string tempDirectory, DumpTypeOption dumpType, string targetFramework)
        {
            this.wasHangDumped = true;

            // the below format is extremely ugly maybe we can use:

            // https://github.com/microsoft/testfx/issues/678
            // which will order the files correctly gives more info when transported out of
            // the context of the run, and keeps the file name unique-enough for our purposes"
            // $"{processName}_{processId}_{dumpFileGuid}_hangdump.dmp"
            // var dumpFileName = $"crash_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";
            // var dumpFileName = $"{prefix}_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";
            var dumpPath = this.GetDumpPath(processId, dumpFileGuid, tempDirectory, isHangDump: true, out var processName);

            EqtTrace.Info($"ProcessDumpUtility.HangDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{dumpPath}'.");
            this.hangDumpPath = dumpPath;

            var dumper = this.hangDumperFactory.Create(targetFramework);

            try
            {
                ConsoleOutput.Instance.Information(false, $"Blame: Creating hang dump of process {processName} ({processId}).");
                dumper.Dump(processId, dumpPath, dumpType);
                EqtTrace.Info($"ProcessDumpUtility.HangDump: Process {processName} ({processId}) was dumped into temporary path '{dumpPath}'.");
            }
            catch (Exception ex)
            {
                EqtTrace.Error($"Blame: Failed with error {ex}.");
                throw;
            }
        }

        private string GetDumpPath(int processId, string dumpFileGuid, string tempDirectory, bool isHangDump, out string processName)
        {
            processName = this.processHelper.GetProcessName(processId);
            var suffix = isHangDump ? "hang" : "crash";
            var dumpFileName = $"{processName}_{processId}_{dumpFileGuid}_{suffix}dump.dmp";

            var path = Path.GetFullPath(tempDirectory);
            return Path.Combine(path, dumpFileName);
        }
    }
}
