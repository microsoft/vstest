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

    /// <summary>
    /// Helper functions for process info.
    /// </summary>
    internal static class ProcessCodeMethods
    {
        private const int InvalidProcessId = -1;

        public static void Suspend(this Process process)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EqtTrace.Verbose($"ProcessCodeMethods.Suspend: Suspending processes is not supported on non-windows returning.");
                return;
            }

            if (process.HasExited)
            {
                EqtTrace.Verbose($"ProcessCodeMethods.Suspend: Process {process.Id} - {process.ProcessName} already exited, skipping.");
                return;
            }

            EqtTrace.Verbose($"ProcessCodeMethods.Suspend: Suspending process {process.Id} - {process.ProcessName}.");
            NtSuspendProcess(process.Handle);
        }

        public static List<ProcessTreeNode> GetProcessTree(this Process process)
        {
            var sw = Stopwatch.StartNew();
            var childProcesses = Process.GetProcesses()
            .Where(p => IsChildCandidate(p, process))
            .ToList();

            Console.WriteLine(sw.ElapsedMilliseconds.ToString());
            var acc = new List<ProcessTreeNode>();
            foreach (var c in childProcesses)
            {
                try
                {
                    var parentId = GetParentPid(c);

                    // c.ParentId = parentId;
                    acc.Add(new ProcessTreeNode { ParentId = parentId, Process = c });
                }
                catch
                {
                    // many things can go wrong with this
                    // just ignore errors
                }
            }

            var level = 1;
            var limit = 10;
            ResolveChildren(process, acc, level, limit);

            return new List<ProcessTreeNode> { new ProcessTreeNode { Process = process, Level = 0 } }.Concat(acc.Where(a => a.Level > 0)).ToList();
        }

        /// <summary>
        /// Returns the parent id of a process or -1 if it fails.
        /// </summary>
        /// <param name="process">The process to find parent of.</param>
        /// <returns>The pid of the parent process.</returns>
        internal static int GetParentPid(Process process)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetParentPidWindows(process.Handle)
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? GetParentPidLinux(process)
                    : throw new NotSupportedException();
        }

        internal static int GetParentPidLinux(Process process)
        {
            return NonWindowsGetProcessParentPid(process.Id);
        }

        internal static int NonWindowsGetProcessParentPid(int pid)
        {
            return /*RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Unix.NativeMethods.GetPPid(pid) : */ Unix.GetProcFSParentPid(pid);
        }

        internal static int GetParentPidWindows(IntPtr processHandle)
        {
            PROCESS_BASIC_INFORMATION pbi;
            int size;
            var res = NtQueryInformationProcess(processHandle, 0, out pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out size);

            var p = res != 0 ? InvalidProcessId : pbi.InheritedFromUniqueProcessId.ToInt32();

            return p;
        }

        private static void ResolveChildren(Process parent, List<ProcessTreeNode> acc, int level, int limit)
        {
            if (limit < 0)
            {
                // hit recursion limit, just returning
                return;
            }

            // only take children that are newer than the parent, because process ids (PIDs) get recycled
            var children = acc.Where(p => p.ParentId == parent.Id && p.Process.StartTime > parent.StartTime).ToList();

            foreach (var child in children)
            {
                child.Level = level;
                ResolveChildren(child.Process, acc, level + 1, limit);
            }
        }

        private static bool IsChildCandidate(Process child, Process parent)
        {
            // this is extremely slow under debugger, but fast without it
            try
            {
                return child.StartTime > parent.StartTime && child.Id != parent.Id;
            }
            catch
            {
                /* access denied or process has exits most likely */
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
#pragma warning disable SA1201 // Elements must appear in the correct order
        private static extern int NtQueryInformationProcess(
                IntPtr processHandle,
                int processInformationClass,
                out PROCESS_BASIC_INFORMATION processInformation,
                int processInformationLength,
                out int returnLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern IntPtr NtSuspendProcess(IntPtr processHandle);
#pragma warning restore SA1201 // Elements must appear in the correct order

        /// <summary>Unix specific implementations of required functionality.</summary>
        internal static class Unix
        {
            ///// <summary>The native methods class.</summary>
            // internal static class NativeMethods
            // {
            //    [DllImport(psLib)]
            //    internal static extern int GetPPid(int pid);
            // }

            /// <summary>Read the /proc file system for information about the parent.</summary>
            /// <param name="pid">The process id used to get the parent process.</param>
            /// <returns>The process id.</returns>
            public static int GetProcFSParentPid(int pid)
            {
                const int invalidPid = -1;

                // read /proc/<pid>/stat
                // 4th column will contain the ppid, 92 in the example below
                // ex: 93 (bash) S 92 93 2 4294967295 ...
                var path = $"/proc/{pid}/stat";
                try
                {
                    var stat = File.ReadAllText(path);
                    var parts = stat.Split(' ');

                    if (parts.Length < 5)
                    {
                        return invalidPid;
                    }

                    return int.Parse(parts[3]);
                }
                catch (Exception)
                {
                    return invalidPid;
                }
            }
        }
    }
}
