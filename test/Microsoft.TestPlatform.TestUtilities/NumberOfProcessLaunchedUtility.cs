// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ProcessInvocation : IDisposable
    {
        private readonly TaskCompletionSource<Process> root = new TaskCompletionSource<Process>();
        private int observedCount;

        public int ObservedCount => Volatile.Read(ref this.observedCount);

        public bool HasExited
        {
            get
            {
                if (this.root.Task.Status != TaskStatus.RanToCompletion)
                {
                    throw new InvalidOperationException("The invocation root has not been assigned.");
                }
                return this.root.Task.Result.HasExited;
            }
        }

        public void SetRoot(Process process)
        {
            var owned = Process.GetProcessById(process.Id);
            // Keep the handle, not just the PID, so an exit followed by PID reuse cannot change identity.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) { var handle = owned.Handle; }
            if (owned.StartTime.ToUniversalTime() != process.StartTime.ToUniversalTime())
            {
                owned.Dispose();
                throw new InvalidOperationException("The invocation root exited before its identity was captured.");
            }

            if (!this.root.TrySetResult(owned))
            {
                owned.Dispose();
                throw new InvalidOperationException("The invocation root has already been assigned.");
            }
            Console.WriteLine("Invocation root: pid={0}, createdUtc={1:O}, executable={2}",
                owned.Id, owned.StartTime.ToUniversalTime(), NumberOfProcessLaunchedUtility.ExecutableIdentity(owned));
        }

        internal Process WaitForRoot(CancellationToken token)
        {
            try
            {
                this.root.Task.Wait(token);
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException("Process counting ended before an invocation root was assigned.");
            }
            return this.root.Task.Result;
        }

        internal void CountObserved() => Interlocked.Increment(ref this.observedCount);

        public void Dispose()
        {
            if (this.root.Task.Status == TaskStatus.RanToCompletion) { this.root.Task.Result.Dispose(); }
        }
    }

    public static class NumberOfProcessLaunchedUtility
    {
        public static Task<ICollection<string>> NumberOfProcessCreated(
            CancellationTokenSource cancellation,
            string processName,
            ProcessInvocation invocation,
            bool includeRoot = true)
            => NumberOfProcessCreated(cancellation, new[] { processName }, invocation, includeRoot);

        public static Task<ICollection<string>> NumberOfProcessCreated(
            CancellationTokenSource cancellation,
            IEnumerable<string> processNames,
            ProcessInvocation invocation,
            bool includeRoot = true)
        {
            if (invocation == null) { throw new ArgumentNullException(nameof(invocation)); }
            var names = new HashSet<string>(processNames, StringComparer.OrdinalIgnoreCase);
            var existing = new HashSet<string>();
            foreach (var name in names)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    using (process)
                    {
                        try { existing.Add(Identity(process)); }
                        catch (InvalidOperationException) when (HasExited(process)) { }
                    }
                }
            }
            return Task.Run<ICollection<string>>(() =>
            {
                var root = invocation.WaitForRoot(cancellation.Token);
                var rootStart = root.StartTime.ToUniversalTime();
                var owned = new Dictionary<int, Process> { [root.Id] = root };
                var counted = new HashSet<string>();
                var startedNames = new List<string>();
                try
                {
                    if (includeRoot && names.Contains(root.ProcessName) && !existing.Contains(Identity(root)))
                    {
                        Record(root, counted, startedNames, invocation, root.Id, GetParentId(root));
                    }

                    do
                    {
                        foreach (var name in names)
                        {
                            foreach (var process in Process.GetProcessesByName(name))
                            {
                                using (process)
                                {
                                    try
                                    {
                                        var start = process.StartTime.ToUniversalTime();
                                        if (start < rootStart || (process.Id == root.Id && start == rootStart)) { continue; }
                                        if (existing.Contains(Identity(process))) { continue; }
                                        if (IsOwned(process, start, root, owned, out var parentId))
                                        {
                                            Record(process, counted, startedNames, invocation, root.Id, parentId);
                                        }
                                    }
                                    catch (InvalidOperationException) when (HasExited(process))
                                    {
                                        // A candidate that already exited cannot be mistaken for a recycled PID.
                                    }
                                }
                            }
                        }
                    }
                    while (!cancellation.Token.WaitHandle.WaitOne(5));
                    return startedNames;
                }
                finally
                {
                    foreach (var process in owned.Values.Where(value => !ReferenceEquals(value, root))) { process.Dispose(); }
                }
            });
        }

        private static bool IsOwned(Process child, DateTime childStart, Process root, Dictionary<int, Process> owned, out int parentId)
        {
            parentId = GetParentId(child);
            if (parentId <= 0)
            {
                if (HasExited(child)) { return false; }
                throw new InvalidOperationException("Could not determine the parent of process " + child.Id + ".");
            }

            Process parent;
            var retained = owned.TryGetValue(parentId, out parent);
            if (retained && parentId != root.Id && parent.HasExited && childStart > parent.ExitTime.ToUniversalTime())
            {
                owned.Remove(parentId);
                parent.Dispose();
                retained = false;
            }
            if (!retained)
            {
                try { parent = Process.GetProcessById(parentId); }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException("Parent " + parentId + " exited before process ownership could be determined.");
                }
            }

            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT) { var handle = parent.Handle; }
                var parentStart = parent.StartTime.ToUniversalTime();
                if (!IsValidParentLifetime(parentStart, parent.HasExited ? (DateTime?)parent.ExitTime.ToUniversalTime() : null, childStart))
                {
                    return false;
                }
                if (parent.Id == root.Id && parentStart == root.StartTime.ToUniversalTime()) { return true; }
                if (retained) { return true; }
                if (parentStart < root.StartTime.ToUniversalTime()) { return false; }
                if (!IsOwned(parent, parentStart, root, owned, out _)) { return false; }
                owned.Add(parentId, parent);
                retained = true;
                return true;
            }
            finally
            {
                if (!retained) { parent.Dispose(); }
            }
        }

        public static bool IsValidParentLifetime(DateTime parentStart, DateTime? parentExit, DateTime childStart)
            => childStart >= parentStart && (!parentExit.HasValue || childStart <= parentExit.Value);

        private static bool HasExited(Process process)
        {
            try { return process.HasExited; }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 87) { return true; }
        }

        private static void Record(Process process, HashSet<string> counted, List<string> names, ProcessInvocation invocation, int rootId, int parentId)
        {
            var key = Identity(process);
            if (counted.Add(key))
            {
                names.Add(process.ProcessName);
                invocation.CountObserved();
                Console.WriteLine("Owned process: rootPid={0}, parentPid={1}, pid={2}, createdUtc={3:O}, observedUtc={4:O}, name={5}, executable={6}",
                    rootId, parentId, process.Id, process.StartTime.ToUniversalTime(), DateTime.UtcNow,
                    process.ProcessName, ExecutableIdentity(process));
            }
        }

        internal static string ExecutableIdentity(Process process)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var path = new StringBuilder(32768);
                    var length = path.Capacity;
                    return QueryFullProcessImageName(process.Handle, 0, path, ref length)
                        ? path.ToString()
                        : "<unavailable: Win32 error " + Marshal.GetLastWin32Error() + ">";
                }
                return process.MainModule.FileName;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return "<unavailable: " + ex.GetType().Name + ">";
            }
        }

        private static string Identity(Process process) => process.Id + ":" + process.StartTime.ToUniversalTime().Ticks;

        private static int GetParentId(Process process)
        {
            // Same platform queries as BlameDataCollector.ProcessCodeMethods, but failures must not
            // silently remove an owned process from an acceptance assertion. net451 has no RuntimeInformation.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                ProcessBasicInformation information;
                int length;
                var status = NtQueryInformationProcess(process.Handle, 0, out information,
                    Marshal.SizeOf(typeof(ProcessBasicInformation)), out length);
                if (status != 0) { throw new InvalidOperationException("Could not read process parent. NTSTATUS=" + status); }
                return information.ParentId.ToInt32();
            }
            if (Directory.Exists("/proc"))
            {
                var stat = File.ReadAllText("/proc/" + process.Id + "/stat");
                var fields = stat.Substring(stat.LastIndexOf(')') + 2).Split(' ');
                return int.Parse(fields[1], CultureInfo.InvariantCulture);
            }
            using (var query = Process.Start(new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = "-o ppid= " + process.Id,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                if (!query.WaitForExit(5000))
                {
                    query.Kill();
                    throw new TimeoutException("Reading process ownership timed out.");
                }
                if (query.ExitCode != 0) { throw new InvalidOperationException(query.StandardError.ReadToEnd()); }
                return int.Parse(query.StandardOutput.ReadToEnd().Trim(), CultureInfo.InvariantCulture);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessBasicInformation
        {
            public IntPtr ExitStatus;
            public IntPtr PebAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr ProcessId;
            public IntPtr ParentId;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle, int informationClass, out ProcessBasicInformation information,
            int informationLength, out int returnLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder path, ref int size);
    }
}
