// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
        private string hangDumpDirectory;
        private string crashDumpDirectory;
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
        public IEnumerable<string> GetDumpFiles(bool warnOnNoDumpFiles = true)
        {
            if (!this.wasHangDumped)
            {
                this.crashDumper.WaitForDumpToFinish();
            }

            IEnumerable<string> crashDumps = this.fileHelper.DirectoryExists(this.crashDumpDirectory)
                ? this.fileHelper.EnumerateFiles(this.crashDumpDirectory, SearchOption.AllDirectories, new[] { ".dmp" })
                : new List<string>();

            IEnumerable<string> hangDumps = this.fileHelper.DirectoryExists(this.hangDumpDirectory)
                ? this.fileHelper.EnumerateFiles(this.hangDumpDirectory, SearchOption.TopDirectoryOnly, new[] { ".dmp" })
                : new List<string>();

            var foundDumps = new List<string>();
            foreach (var dumpPath in crashDumps.Concat(hangDumps))
            {
                EqtTrace.Info($"ProcessDumpUtility.GetDumpFiles: Looking for dump file '{dumpPath}'.");
                var found = this.fileHelper.Exists(dumpPath);
                if (found)
                {
                    EqtTrace.Info($"ProcessDumpUtility.GetDumpFile: Found dump file '{dumpPath}'.");
                    foundDumps.Add(dumpPath);
                }
                else
                {
                    EqtTrace.Warning($"ProcessDumpUtility.GetDumpFile: Dump file '{dumpPath}' was not found.");
                }
            }

            if (warnOnNoDumpFiles && !foundDumps.Any())
            {
                EqtTrace.Error($"ProcessDumpUtility.GetDumpFile: Could not find any dump file in {this.hangDumpDirectory}.");
                throw new FileNotFoundException(Resources.Resources.DumpFileNotGeneratedErrorMessage);
            }

            return foundDumps;
        }

        /// <inheritdoc/>
        public void StartHangBasedProcessDump(int processId, string tempDirectory, bool isFullDump, string targetFramework, Action<string> logWarning = null)
        {
            this.HangDump(processId, tempDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework, logWarning);
        }

        /// <inheritdoc/>
        public void StartTriggerBasedProcessDump(int processId, string testResultsDirectory, bool isFullDump, string targetFramework)
        {
            this.CrashDump(processId, testResultsDirectory, isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini, targetFramework);
        }

        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            this.crashDumper?.DetachFromTargetProcess(targetProcessId);
        }

        private void CrashDump(int processId, string tempDirectory, DumpTypeOption dumpType, string targetFramework)
        {
            var processName = this.processHelper.GetProcessName(processId);
            EqtTrace.Info($"ProcessDumpUtility.CrashDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{tempDirectory}'.");
            this.crashDumpDirectory = tempDirectory;

            this.crashDumper = this.crashDumperFactory.Create(targetFramework);
            ConsoleOutput.Instance.Information(false, $"Blame: Attaching crash dump utility to process {processName} ({processId}).");
            this.crashDumper.AttachToTargetProcess(processId, tempDirectory, dumpType);
        }

        private void HangDump(int processId, string tempDirectory, DumpTypeOption dumpType, string targetFramework, Action<string> logWarning = null)
        {
            this.wasHangDumped = true;

            var processName = this.processHelper.GetProcessName(processId);
            EqtTrace.Info($"ProcessDumpUtility.HangDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into temporary path '{tempDirectory}'.");

            this.hangDumpDirectory = tempDirectory;

            // oh how ugly this is, but the whole infra above this starts with initializing the logger in Initialize
            // the logger needs to pass around 2 parameters, so I am just passing it around as callback instead
            this.hangDumperFactory.LogWarning = logWarning;
            var dumper = this.hangDumperFactory.Create(targetFramework);

            try
            {
                dumper.Dump(processId, tempDirectory, dumpType);
            }
            catch (Exception ex)
            {
                EqtTrace.Error($"Blame: Failed with error {ex}.");
                throw;
            }
        }
    }
}
