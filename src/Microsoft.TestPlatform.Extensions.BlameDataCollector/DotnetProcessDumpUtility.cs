// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
# if NETSTANDARD

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Diagnostics.NETCore.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.Win32.SafeHandles;

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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Get the process
                    Process process = Process.GetProcessById(processId);
                    DumperWindows.CollectDumpAsync(process, dumpPath, dumpType);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {

                    client.WriteDump(DumpType.Full, dumpPath);
                }
                else
                {
                    throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
                }
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

        private static class DumperWindows
        {
            internal static void CollectDumpAsync(Process process, string outputFile, DumpType type)
            {
                // We can't do this "asynchronously" so just Task.Run it. It shouldn't be "long-running" so this is fairly safe.

                // Open the file for writing
                using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    var exceptionInfo = new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION();

                    NativeMethods.MINIDUMP_TYPE dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpNormal;
                    switch (type)
                    {
                        case DumpType.Full:
                            dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                            break;
                        case DumpType.WithHeap:
                            dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithPrivateReadWriteMemory |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                       NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                            break;
                        case DumpType.Normal:
                            dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo;
                            break;
                    }

                    // Retry the write dump on ERROR_PARTIAL_COPY
                    for (int i = 0; i < 5; i++)
                    {
                        // Dump the process!
                        if (NativeMethods.MiniDumpWriteDump(process.Handle, (uint)process.Id, stream.SafeFileHandle, dumpType, ref exceptionInfo, IntPtr.Zero, IntPtr.Zero))
                        {
                            break;
                        }
                        else
                        {
                            int err = Marshal.GetHRForLastWin32Error();
                            if (err != NativeMethods.ERROR_PARTIAL_COPY)
                            {
                                Marshal.ThrowExceptionForHR(err);
                            }
                        }
                    }
                }
            }

            private static class NativeMethods
            {
                public const int ERROR_PARTIAL_COPY = unchecked((int)0x8007012b);

                [DllImport("Dbghelp.dll", SetLastError = true)]
                public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeFileHandle hFile, MINIDUMP_TYPE DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

                [StructLayout(LayoutKind.Sequential, Pack = 4)]
                public struct MINIDUMP_EXCEPTION_INFORMATION
                {
                    public uint ThreadId;
                    public IntPtr ExceptionPointers;
                    public int ClientPointers;
                }

                [Flags]
                public enum MINIDUMP_TYPE : uint
                {
                    MiniDumpNormal = 0,
                    MiniDumpWithDataSegs = 1 << 0,
                    MiniDumpWithFullMemory = 1 << 1,
                    MiniDumpWithHandleData = 1 << 2,
                    MiniDumpFilterMemory = 1 << 3,
                    MiniDumpScanMemory = 1 << 4,
                    MiniDumpWithUnloadedModules = 1 << 5,
                    MiniDumpWithIndirectlyReferencedMemory = 1 << 6,
                    MiniDumpFilterModulePaths = 1 << 7,
                    MiniDumpWithProcessThreadData = 1 << 8,
                    MiniDumpWithPrivateReadWriteMemory = 1 << 9,
                    MiniDumpWithoutOptionalData = 1 << 10,
                    MiniDumpWithFullMemoryInfo = 1 << 11,
                    MiniDumpWithThreadInfo = 1 << 12,
                    MiniDumpWithCodeSegs = 1 << 13,
                    MiniDumpWithoutAuxiliaryState = 1 << 14,
                    MiniDumpWithFullAuxiliaryState = 1 << 15,
                    MiniDumpWithPrivateWriteCopyMemory = 1 << 16,
                    MiniDumpIgnoreInaccessibleMemory = 1 << 17,
                    MiniDumpWithTokenInformation = 1 << 18,
                    MiniDumpWithModuleHeaders = 1 << 19,
                    MiniDumpFilterTriage = 1 << 20,
                    MiniDumpWithAvxXStateContext = 1 << 21,
                    MiniDumpWithIptTrace = 1 << 22,
                    MiniDumpValidTypeFlags = (-1) ^ ((~1) << 22)
                }
            }
        }
    }
}

#endif