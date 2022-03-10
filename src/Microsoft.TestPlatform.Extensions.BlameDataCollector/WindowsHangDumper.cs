// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector;

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using Win32.SafeHandles;

internal class WindowsHangDumper : IHangDumper
{
    private readonly Action<string> _logWarning;

    public WindowsHangDumper(Action<string> logWarning)
    {
        _logWarning = logWarning ?? (_ => { });
    }

    private static Action<object, string> OutputReceivedCallback => (process, data) =>
        // useful for visibility when debugging this tool
        // Console.ForegroundColor = ConsoleColor.Cyan;
        // Console.WriteLine(data);
        // Console.ForegroundColor = ConsoleColor.White;
        // Log all standard output message of procdump in diag files.
        // Otherwise they end up coming on console in pipleine.
        EqtTrace.Info("ProcDumpDumper.OutputReceivedCallback: Output received from procdump process: " + data);

    public void Dump(int processId, string outputDirectory, DumpTypeOption type)
    {
        var process = Process.GetProcessById(processId);
        var processTree = process.GetProcessTree().Where(p => p.Process.ProcessName is not "conhost" and not "WerFault").ToList();

        if (processTree.Count > 1)
        {
            var tree = processTree.OrderBy(t => t.Level);
            EqtTrace.Verbose("WindowsHangDumper.Dump: Dumping this process tree (from bottom):");
            foreach (var p in tree)
            {
                EqtTrace.Verbose($"WindowsHangDumper.Dump: {new string(' ', p.Level)}{(p.Level != 0 ? " +" : " >-")} {p.Process.Id} - {p.Process.ProcessName}");
            }

            // logging warning separately to avoid interleving the messages in the log which make this tree unreadable
            _logWarning(Resources.Resources.DumpingTree);
            foreach (var p in tree)
            {
                _logWarning($"{new string(' ', p.Level)}{(p.Level != 0 ? "+-" : ">")} {p.Process.Id} - {p.Process.ProcessName}");
            }
        }
        else
        {
            EqtTrace.Verbose($"NetClientHangDumper.Dump: Dumping {process.Id} - {process.ProcessName}.");
            var message = string.Format(CultureInfo.CurrentUICulture, Resources.Resources.Dumping, process.Id, process.ProcessName);
            _logWarning(message);
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

        if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X86)
        {
            // This is x86 OS, the current process and the target process must be x86. (Or maybe arm64, but let's not worry about that now).
            // Just dump it using PInvoke.
            EqtTrace.Verbose($"WindowsHangDumper.CollectDump: We are on x86 Windows, both processes are x86, using PInvoke dumper.");
            CollectDumpUsingMiniDumpWriteDump(process, outputFile, type);
        }
        else if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64)
        {
            var targetProcessIs64Bit = new NativeMethodsHelper().Is64Bit(process.Handle);

            var currentProcessIs64Bit = RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64;

            if (targetProcessIs64Bit && currentProcessIs64Bit)
            {
                // Both processes are x64 architecture, dump it using the PInvoke call.
                EqtTrace.Verbose($"WindowsHangDumper.CollectDump: We are on x64 Windows, and both processes are x64, using PInvoke dumper.");
                CollectDumpUsingMiniDumpWriteDump(process, outputFile, type);
            }
            else if (!targetProcessIs64Bit && !currentProcessIs64Bit)
            {
                // Both processes are x86 architecture, dump it using the PInvoke call.
                EqtTrace.Verbose($"WindowsHangDumper.CollectDump: We are on x64 Windows, and both processes are x86, using PInvoke dumper.");
                CollectDumpUsingMiniDumpWriteDump(process, outputFile, type);
            }
            else
            {
                string dumpMinitoolName;
                if (!currentProcessIs64Bit && targetProcessIs64Bit)
                {
                    EqtTrace.Verbose($"WindowsHangDumper.CollectDump: We are on x64 Windows, datacollector is x86, and target process is x64, using 64-bit MiniDumptool.");
                    dumpMinitoolName = "DumpMinitool.exe";
                }
                else
                {
                    EqtTrace.Verbose($"WindowsHangDumper.CollectDump: We are on x64 Windows, datacollector is x64, and target process is x86, using 32-bit MiniDumptool.");
                    dumpMinitoolName = "DumpMinitool.x86.exe";
                }

                var args = $"--file \"{outputFile}\" --processId {process.Id} --dumpType {type}";
                var dumpMinitoolPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), dumpMinitoolName);
                if (!File.Exists(dumpMinitoolPath))
                {
                    throw new FileNotFoundException("Could not find DumpMinitool", dumpMinitoolPath);
                }

                EqtTrace.Info($"ProcDumpDumper.CollectDump: Running DumpMinitool: '{dumpMinitoolPath} {args}'.");
                var dumpMiniTool = new ProcessHelper().LaunchProcess(
                    dumpMinitoolPath,
                    args,
                    Path.GetDirectoryName(outputFile),
                    null,
                    null,
                    null,
                    OutputReceivedCallback) as Process;
                dumpMiniTool.WaitForExit();
                EqtTrace.Info($"ProcDumpDumper.CollectDump: {dumpMinitoolName} exited with exitcode: '{dumpMiniTool.ExitCode}'.");
            }
        }

        EqtTrace.Verbose($"WindowsHangDumper.CollectDump: Finished dumping {process.Id} - {process.ProcessName} in {outputFile}. ");
    }

    private static void CollectDumpUsingMiniDumpWriteDump(Process process, string outputFile, DumpTypeOption type)
    {
        // Open the file for writing
        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        NativeMethods.MinidumpExceptionInformation exceptionInfo = default;

        NativeMethods.MinidumpType dumpType = NativeMethods.MinidumpType.MiniDumpNormal;
        switch (type)
        {
            case DumpTypeOption.Full:
                dumpType = NativeMethods.MinidumpType.MiniDumpWithFullMemory |
                           NativeMethods.MinidumpType.MiniDumpWithDataSegs |
                           NativeMethods.MinidumpType.MiniDumpWithHandleData |
                           NativeMethods.MinidumpType.MiniDumpWithUnloadedModules |
                           NativeMethods.MinidumpType.MiniDumpWithFullMemoryInfo |
                           NativeMethods.MinidumpType.MiniDumpWithThreadInfo |
                           NativeMethods.MinidumpType.MiniDumpWithTokenInformation;
                break;
            case DumpTypeOption.WithHeap:
                dumpType = NativeMethods.MinidumpType.MiniDumpWithPrivateReadWriteMemory |
                           NativeMethods.MinidumpType.MiniDumpWithDataSegs |
                           NativeMethods.MinidumpType.MiniDumpWithHandleData |
                           NativeMethods.MinidumpType.MiniDumpWithUnloadedModules |
                           NativeMethods.MinidumpType.MiniDumpWithFullMemoryInfo |
                           NativeMethods.MinidumpType.MiniDumpWithThreadInfo |
                           NativeMethods.MinidumpType.MiniDumpWithTokenInformation;
                break;
            case DumpTypeOption.Mini:
                dumpType = NativeMethods.MinidumpType.MiniDumpWithThreadInfo;
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
                if (err != NativeMethods.ErrorPartialCopy)
                {
                    Marshal.ThrowExceptionForHR(err);
                }
            }
        }
    }

    private static class NativeMethods
    {
        public const int ErrorPartialCopy = unchecked((int)0x8007012b);

        [DllImport("Dbghelp.dll", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeFileHandle hFile, MinidumpType dumpType, ref MinidumpExceptionInformation exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MinidumpExceptionInformation
        {
            public readonly uint ThreadId;
            public readonly IntPtr ExceptionPointers;
            public readonly int ClientPointers;
        }

        [Flags]
        public enum MinidumpType : uint
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
