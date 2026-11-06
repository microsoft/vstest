// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The number of process launched utility.
    /// </summary>
    public class NumberOfProcessLaunchedUtility
    {
        public static int CaptureStartedChildProcessId(string processName, Action startProcess)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Capturing the owned console process requires a Windows process snapshot.");
            }

            using (var current = Process.GetCurrentProcess())
            {
                var existing = new HashSet<int>(ReadProcessSnapshot().Keys);
                startProcess();
                var processId = FindStartedChildProcessId(current.Id, processName, existing, ReadProcessSnapshot());
                Console.WriteLine($"Observing {processName} process {processId}, launched by test process {current.Id}.");
                return processId;
            }
        }

        public static int FindStartedChildProcessId(
            int parentProcessId,
            string processName,
            ISet<int> existingProcessIds,
            IDictionary<int, Tuple<int, string>> processes)
        {
            var matches = new List<int>();
            foreach (var process in processes)
            {
                if (process.Value.Item1 == parentProcessId &&
                    string.Equals(process.Value.Item2, processName, StringComparison.OrdinalIgnoreCase) &&
                    !existingProcessIds.Contains(process.Key))
                {
                    matches.Add(process.Key);
                }
            }

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one new {processName} child of process {parentProcessId}, found {matches.Count}: {string.Join(", ", matches)}.");
            }

            return matches[0];
        }

        /// <summary>
        /// To Find the process created by name during this task run
        /// </summary>
        /// <param name="cts">
        /// To cancel task
        /// </param>
        /// <param name="processName">
        /// Name of the process
        /// </param>
        /// <param name="parentProcessId">Count this console's direct workers, or the current test process's descendants when omitted.</param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task<int> NumberOfProcessCreated(CancellationTokenSource cts, string processName, int? parentProcessId = null)
        {
            var testhostProcessIDsBeforeRun = new List<int>();
            var testhostProcessesBeforeRun = Process.GetProcessesByName(processName);

            foreach (var process in testhostProcessesBeforeRun)
            {
                using (process)
                {
                    testhostProcessIDsBeforeRun.Add(process.Id);
                }
            }

            var numOfProcessTask =
                Task.Run(() => NumberOfProcessLaunchedDuringRun(cts.Token, testhostProcessIDsBeforeRun, processName, parentProcessId));
            return await numOfProcessTask;
        }

        /// <summary>
        /// The number of process launched during run.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="executorProcessesBeforeRun">
        /// The executor processes before run.
        /// </param>
        /// <param name="processName">
        /// The process name.
        /// </param>
        /// <param name="parentProcessId">Count this console's direct workers, or the current test process's descendants when omitted.</param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int NumberOfProcessLaunchedDuringRun(
            CancellationToken token,
            List<int> executorProcessesBeforeRun,
            string processName,
            int? parentProcessId = null)
        {
            var preCreatedProcessIDs = new HashSet<int>(executorProcessesBeforeRun);
            var desireCount = 0;
            ProcessTreeCounter counter = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var current = Process.GetCurrentProcess())
                {
                    counter = new ProcessTreeCounter(
                        parentProcessId ?? current.Id,
                        processName,
                        preCreatedProcessIDs,
                        includeDescendants: !parentProcessId.HasValue);
                }
            }
            else if (parentProcessId.HasValue)
            {
                throw new PlatformNotSupportedException("Counting an owned process tree requires a Windows process snapshot.");
            }

            while (!token.IsCancellationRequested)
            {
                if (counter != null)
                {
                    counter.ObserveSnapshot(ReadProcessSnapshot());
                    Thread.Sleep(10);
                    continue;
                }

                var executorProcessDuringRun = Process.GetProcessesByName(processName);

                foreach (var process in executorProcessDuringRun)
                {
                    using (process)
                    {
                        if (preCreatedProcessIDs.Add(process.Id))
                        {
                            desireCount++;
                        }
                    }
                }
                Thread.Sleep(10);
            }

            return counter == null ? desireCount : counter.Count;
        }

        private static IDictionary<int, Tuple<int, string>> ReadProcessSnapshot()
        {
            var snapshot = CreateToolhelp32Snapshot(2, 0);
            if (snapshot == new IntPtr(-1))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            try
            {
                var processes = new Dictionary<int, Tuple<int, string>>();
                var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
                if (!Process32First(snapshot, ref entry))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                do
                {
                    processes[(int)entry.ProcessId] = Tuple.Create((int)entry.ParentProcessId, Path.GetFileNameWithoutExtension(entry.ExeFile));
                }
                while (Process32Next(snapshot, ref entry));
                var error = Marshal.GetLastWin32Error();
                if (error != 18)
                {
                    throw new Win32Exception(error);
                }
                return processes;
            }
            finally
            {
                CloseHandle(snapshot);
            }
        }

        // Host providers launch workers directly. Transitive PID ancestry also includes grandchildren
        // that are not console-launched workers, and parent IDs can outlive their original process.
        public sealed class ProcessTreeCounter
        {
            private readonly HashSet<int> descendants;
            private readonly HashSet<int> observed;
            private readonly string processName;
            private readonly int parentProcessId;
            private readonly bool includeDescendants;

            public ProcessTreeCounter(int parentProcessId, string processName, IEnumerable<int> existingProcessIds, bool includeDescendants = true)
            {
                this.descendants = new HashSet<int> { parentProcessId };
                this.observed = new HashSet<int>(existingProcessIds) { parentProcessId };
                this.processName = processName;
                this.parentProcessId = parentProcessId;
                this.includeDescendants = includeDescendants;
            }

            public int Count { get; private set; }

            public void ObserveSnapshot(IDictionary<int, Tuple<int, string>> processes)
            {
                if (this.includeDescendants)
                {
                    bool added;
                    do
                    {
                        added = false;
                        foreach (var process in processes)
                        {
                            if (this.descendants.Contains(process.Value.Item1))
                            {
                                added |= this.descendants.Add(process.Key);
                            }
                        }
                    }
                    while (added);
                }

                foreach (var process in processes)
                {
                    var owned = this.includeDescendants
                        ? this.descendants.Contains(process.Key)
                        : process.Value.Item1 == this.parentProcessId;
                    if (owned &&
                        string.Equals(process.Value.Item2, this.processName, StringComparison.OrdinalIgnoreCase) &&
                        this.observed.Add(process.Key))
                    {
                        this.Count++;
                        Console.WriteLine(
                            $"Counted {process.Value.Item2} process {process.Key}, parent {process.Value.Item1}, owner {this.parentProcessId}.");
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int Priority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}