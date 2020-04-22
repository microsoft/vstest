// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.IO;
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
        private readonly IDumperFactory dumperFactory;
        private string dumpPath;

        public ProcessDumpUtility() : this(new ProcessHelper(), new FileHelper(), new DumperFactory())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IDumperFactory dumperFactory)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.dumperFactory = dumperFactory;
        }

        protected Action<object, string> OutputReceivedCallback => (process, data) =>
        {
            // Log all standard output message of procdump in diag files.
            // Otherwise they end up coming on console in pipleine.
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DotnetProcessDumpUtility.OutputReceivedCallback: Output received from procdump process: " + data);
            }
        };

        /// <inheritdoc/>
        public string GetDumpFile()
        {  
            if (string.IsNullOrWhiteSpace(this.dumpPath))
            {
                return string.Empty;
            }

            var found = this.fileHelper.Exists(this.dumpPath);
            if (found)
            {
                EqtTrace.Info($"DotnetProcessDumpUtility.GetDumpFile: Found dump file '{this.dumpPath}'.");
                return this.dumpPath;
            }

            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Info($"DotnetProcessDumpUtility.GetDumpFile: Dump file '{this.dumpPath}' was not found.");
            }

            throw new FileNotFoundException(Resources.Resources.DumpFileNotGeneratedErrorMessage);
        }

        /// <inheritdoc/>
        public void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false, string frameworkVersion = null)
        {
            Dump(processId, dumpFileGuid, testResultsDirectory, isFullDump, isHangDump: false, nameof(StartTriggerBasedProcessDump), frameworkVersion);
        }

        /// <inheritdoc/>
        public void StartHangBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false, string frameworkVersion = null)
        {
            Dump(processId, dumpFileGuid, testResultsDirectory, isFullDump, isHangDump: true, nameof(StartHangBasedProcessDump), frameworkVersion);
        }


        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            // noop, it does not use procdump.exe anymore
        }

        /// <inheritdoc/>
        public void TerminateProcess()
        {
            // noop, it does not use procdump.exe anymore
        }

        private void Dump(int processId, string dumpFileGuid, string tempDirectory, bool isFullDump , bool isHangDump, string caller, string frameworkVersion = "4.6")
        {
            var processName = this.processHelper.GetProcessName(processId);

            var dumpType = isFullDump ? DumpTypeOption.Full : DumpTypeOption.Mini;
            // the below format is extremely ugly maybe we can use: 
            // https://github.com/microsoft/testfx/issues/678
            // which will order the files correctly gives more info when transported out of 
            // the context of the run, and keeps the file name unique-enough for our purposes"
            // $"{processName}_{processId}_{dumpFileGuid}_hangdump.dmp"
            // var dumpFileName = $"crash_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";
            //var dumpFileName = $"{prefix}_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";

            var suffix = isHangDump ? "hang" : "crash";
            var dumpFileName = $"{processName}_{processId}_{dumpFileGuid}_{suffix}dump.dmp";

            var path = Path.GetFullPath(tempDirectory);
            var dumpPath = Path.Combine(path, dumpFileName);

            EqtTrace.Info($"DotnetProcessDumpUtility.{caller}: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into '{dumpPath}'.");
            this.dumpPath = dumpPath;

            var dumper = this.dumperFactory.Create(Version.TryParse(frameworkVersion, out var v) ? v : default);
            ConsoleOutput.Instance.Warning(true, $"Creating { dumpType.ToString().ToLowerInvariant()} dump of process { processName} (id { processId}).");
            dumper.Dump(processId, dumpPath, dumpType);

            EqtTrace.Info($"DotnetProcessDumpUtility.{caller}: Process {processName} ({processId}) was dumped into '{dumpPath}'.");
        }
    }
}
