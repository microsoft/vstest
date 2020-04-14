// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
# if NETSTANDARD

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Diagnostics.NETCore.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    internal class DotnetProcessDumpUtility : IProcessDumpUtility
    {
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private string dumpPath;

        public DotnetProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper())
        {
        }

        public DotnetProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
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
        public void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false)
        {
            throw new NotImplementedException();
            //this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}";
            //this.testResultsDirectory = testResultsDirectory;

            //string procDumpArgs = new ProcDumpArgsBuilder().BuildTriggerBasedProcDumpArgs(
            //    processId,
            //    this.dumpFileName,
            //    ProcDumpExceptionsList,
            //    isFullDump);

            //if (EqtTrace.IsInfoEnabled)
            //{
            //    EqtTrace.Info($"DotnetProcessDumpUtility : The proc dump argument is {procDumpArgs}");
            //}

            //this.procDumpProcess = this.processHelper.LaunchProcess(
            //                                this.GetProcDumpExecutable(processId),
            //                                procDumpArgs,
            //                                testResultsDirectory,
            //                                null,
            //                                null,
            //                                null,
            //                                this.OutputReceivedCallback) as Process;
        }

        /// <inheritdoc/>
        public void StartHangBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump = false)
        {
            // this is just nice to have info, should we continue if this fails? --jajares
            var processName = this.processHelper.GetProcessName(processId);
            
            var dumpType = isFullDump ? DumpType.Full : DumpType.Normal;
            // the below format is extremely ugly maybe we can use: 
            // https://github.com/microsoft/testfx/issues/678
            // which will order the files correctly gives more info when transported out of 
            // the context of the run, and keeps the file name unique-enough for our purposes"
            // $"{processName}_{processId}_{dumpFileGuid}_hangdump.dmp"
            // var dumpFileName = $"crash_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";
            var dumpFileName = $"hang_{processName}_{DateTime.Now:yyyyMMddTHHmmss}_{processId}.dmp";

            var path = Path.IsPathRooted(testResultsDirectory) ? testResultsDirectory : Path.Combine(Directory.GetCurrentDirectory(), testResultsDirectory);

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                var tempPath = Path.GetTempPath();
                path = tempPath;

                EqtTrace.Error($"DotnetProcessDumpUtility.StartHangBasedProcessDump: Creating directory '{path}' failed, using temp path '{tempPath}'.{Environment.NewLine}Exception: {ex}");
            }

            var dumpPath = Path.Combine(path, dumpFileName);
            var client = new DiagnosticsClient(processId);
            EqtTrace.Info($"DotnetProcessDumpUtility.StartHangBasedProcessDump: Creating {dumpType.ToString().ToLowerInvariant()} dump of process {processName} ({processId}) into '{dumpPath}'.");
            this.dumpPath = dumpPath;
            try
            {
                client.WriteDump(DumpType.Full, dumpPath);
            }
            catch (Exception ex)
            {
                EqtTrace.Error(ex);
                throw;
            }

            EqtTrace.Info($"DotnetProcessDumpUtility.StartHangBasedProcessDump: Process {processName} ({processId}) was dumped into '{dumpPath}'.");
        }

        /// <inheritdoc/>
        public void DetachFromTargetProcess(int targetProcessId)
        {
            // noop
        }

        /// <inheritdoc/>
        public void TerminateProcess()
        {
            // noop
        }
    }
}

#endif