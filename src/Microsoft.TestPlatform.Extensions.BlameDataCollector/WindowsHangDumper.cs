// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.Win32.SafeHandles;

    internal class WindowsHangDumper : IHangDumper
    {
        private Action<string> logWarning;

        public WindowsHangDumper(Action<string> logWarning)
        {
            this.logWarning = logWarning ?? (_ => { });
        }

        public void Dump(int processId, string outputDirectory, DumpTypeOption type)
        {
            var process = Process.GetProcessById(processId);
            var processTree = process.GetProcessTree().Where(p => p.Process.ProcessName != "conhost" && p.Process.ProcessName != "WerFault").ToList();

            if (processTree.Count > 1)
            {
                var tree = processTree.OrderBy(t => t.Level);
                EqtTrace.Verbose("WindowsHangDumper.Dump: Dumping this process tree (from bottom):");
                foreach (var p in tree)
                {
                    EqtTrace.Verbose($"WindowsHangDumper.Dump: {new string(' ', p.Level)}{(p.Level != 0 ? " +" : " >-")} {p.Process.Id} - {p.Process.ProcessName}");
                }

                // logging warning separately to avoid interleving the messages in the log which make this tree unreadable
                this.logWarning(Resources.Resources.DumpingTree);
                foreach (var p in tree)
                {
                    this.logWarning($"{new string(' ', p.Level)}{(p.Level != 0 ? "+-" : ">")} {p.Process.Id} - {p.Process.ProcessName}");
                }
            }
            else
            {
                EqtTrace.Verbose($"NetClientHangDumper.Dump: Dumping {process.Id} - {process.ProcessName}.");
                var message = string.Format(CultureInfo.CurrentUICulture, Resources.Resources.Dumping, process.Id, process.ProcessName);
                this.logWarning(message);
            }

            var bottomUpTree = processTree.OrderByDescending(t => t.Level).Select(t => t.Process);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var p in bottomUpTree)
                {
                    try
                    {
                        p.Suspend();
                    }
                    catch (Exception ex)
                    {
                        EqtTrace.Error($"WindowsHangDumper.Dump: Error suspending process {p.Id} - {p.ProcessName}: {ex}.");
                    }
                }
            }

            foreach (var p in bottomUpTree)
            {
                try
                {
                    var outputFile = Path.Combine(outputDirectory, $"{p.ProcessName}_{p.Id}_{DateTime.Now:yyyyMMddTHHmmss}_hangdump.dmp");
                    CollectDump(p, outputFile, type);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"WindowsHangDumper.Dump: Error dumping process {p.Id} - {p.ProcessName}: {ex}.");
                }

                try
                {
                    EqtTrace.Verbose($"WindowsHangDumper.Dump: Killing process {p.Id} - {p.ProcessName}.");
                    p.Kill();
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"WindowsHangDumper.Dump: Error killing process {p.Id} - {p.ProcessName}: {ex}.");
                }
            }
        }

        internal static void CollectDump(Process process, string outputFile, DumpTypeOption type)
        {
            if (process.HasExited)
            {
                EqtTrace.Verbose($"WindowsHangDumper.CollectDump: {process.Id} - {process.ProcessName} already exited, skipping.");
                return;
            }

            EqtTrace.Verbose($"WindowsHangDumper.CollectDump: Selected dump type {type}. Dumping {process.Id} - {process.ProcessName} in {outputFile}. ");

            // Open the file for writing
            using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                NativeMethods.MINIDUMP_EXCEPTION_INFORMATION exceptionInfo = default(NativeMethods.MINIDUMP_EXCEPTION_INFORMATION);

                NativeMethods.MINIDUMP_TYPE dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpNormal;
                switch (type)
                {
                    case DumpTypeOption.Full:
                        dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                        break;
                    case DumpTypeOption.WithHeap:
                        dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithPrivateReadWriteMemory |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                        break;
                    case DumpTypeOption.Mini:
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

            EqtTrace.Verbose($"WindowsHangDumper.CollectDump: Finished dumping {process.Id} - {process.ProcessName} in {outputFile}. ");
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
#pragma warning disable SA1201 // Elements must appear in the correct order
            public enum MINIDUMP_TYPE : uint
#pragma warning restore SA1201 // Elements must appear in the correct order
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
