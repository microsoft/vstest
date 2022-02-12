// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DumpMinitool;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

internal class Program
{
    static int Main(string[] args)
    {
        DebuggerBreakpoint.WaitForDebugger("VSTEST_DUMPTOOL_DEBUG");
        Console.WriteLine($"Dump minitool: Started with arguments {string.Join(" ", args)}");
        if (args?.Length != 6)
        {
            Console.WriteLine($"There were { args?.Length ?? 0 } parameters. Provide exactly 6 parameters: --file <fullyResolvedPath> --processId <processId> --dumpType <dumpType>");
            return 2;
        }

        var outputFile = args[1];
        var processId = int.Parse(args[3]);
        var type = Enum.Parse(typeof(DumpTypeOption), args[5]);

        Console.WriteLine($"Output file: '{outputFile}'");
        Console.WriteLine($"Process id: {processId}");
        Console.WriteLine($"Dump type: {type}");

        var process = Process.GetProcessById(processId);

        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        NativeMethods.MINIDUMP_EXCEPTION_INFORMATION exceptionInfo = default;

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
                Console.WriteLine("Dumped process.");
                return 0;
            }
            else
            {
                int err = Marshal.GetHRForLastWin32Error();
                if (err != NativeMethods.ERROR_PARTIAL_COPY)
                {
                    Console.WriteLine($"Error dumping process {err}");
                    Marshal.ThrowExceptionForHR(err);
                }
                else
                {
                    Console.WriteLine($"Error dumping process, was ERROR_PARTIAL_COPY, retrying.");
                }
            }
        }

        Console.WriteLine($"Error dumping process after 5 retries.");
        return 1;
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Do not report for native methods")]
    private static class NativeMethods
    {
        public const int ERROR_PARTIAL_COPY = unchecked((int)0x8007012b);

        [DllImport("Dbghelp.dll", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeFileHandle hFile, MINIDUMP_TYPE dumpType, ref MINIDUMP_EXCEPTION_INFORMATION exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MINIDUMP_EXCEPTION_INFORMATION
        {
            public readonly uint ThreadId;
            public readonly IntPtr ExceptionPointers;
            public readonly int ClientPointers;
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

internal enum DumpTypeOption
{
    Full,
    WithHeap,
    Mini,
}

internal static class DebuggerBreakpoint
{
    internal static void WaitForDebugger(string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(environmentVariable))
        {
            throw new ArgumentException($"'{nameof(environmentVariable)}' cannot be null or whitespace.", nameof(environmentVariable));
        }

        var debugEnabled = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
        {
            Console.WriteLine("Waiting for debugger attach...");

            var currentProcess = Process.GetCurrentProcess();
            Console.WriteLine("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName);

            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }

            Debugger.Break();
        }
    }
}
