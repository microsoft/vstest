// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    class WindowsHangDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {
            var process = Process.GetProcessById(processId);
            CollectDump(process, outputFile, type);
        }

        internal static void CollectDump(Process process, string outputFile, DumpTypeOption type)
        {
            // Open the file for writing
            using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                var exceptionInfo = new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION();

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

    class SigtrapDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {
            Process.Start("kill", $"-s SIGTRAP { processId }");
        }
    }

    class NetClientDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {

            var client = new DiagnosticsClient(processId);
            client.WriteDump(type == DumpTypeOption.Full ? DumpType.Full : DumpType.Normal, outputFile);
        }
    }

    class HangDumperFactory : IHangDumperFactory
    {
        public IHangDumper Create(string targetFramework)
        {
            EqtTrace.Info($"HangDumperFactory: Creating dumper for {RuntimeInformation.OSDescription} with target framework {targetFramework}.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EqtTrace.Info($"HangDumperFactory: This is Windows, returning the default WindowsHangDumper that P/Invokes MiniDumpWriteDump.");
                return new WindowsHangDumper();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!string.IsNullOrWhiteSpace(targetFramework) && targetFramework.Contains("v2.1"))
                {
                    EqtTrace.Info($"HangDumperFactory: This is Linux on netcoreapp2.1, returning SigtrapDumper.");

                    return new SigtrapDumper();
                }

                EqtTrace.Info($"HangDumperFactory: This is Linux netcoreapp3.1 or newer, returning the standard NETClient library dumper.");
                return new NetClientDumper();
            }

            // this is not supported yet
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            //{
                
                //if (frameworkVersion != default && frameworkVersion <= new Version("5.0"))
                //{
                //    return new SigtrapDumper();
                //}

                //EqtTrace.Info($"HangDumperFactory: This is OSX on netcoreapp3.1 or newer, returning the standard NETClient library dumper.");
                //return new NetClientDumper();
            //}

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }

    public interface IHangDumperFactory
    {
        IHangDumper Create(string targetFramework);
    }

    public interface ICrashDumperFactory
    {
        ICrashDumper Create(string targetFramework);
    }

    public interface IHangDumper
    {
        void Dump(int processId, string outputFile, DumpTypeOption dumpType);
    }

    public interface ICrashDumper
    {
        void AttachToTargetProcess(int processId, string outputFile, DumpTypeOption dumpType);
        
        void WaitForDumpToFinish();

        void DetachFromTargetProcess(int processId);
    }

    public enum DumpTypeOption
    {
        Full,
        WithHeap,
        Mini,
    }
}
