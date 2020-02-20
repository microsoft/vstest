// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The number of process launched utility.
    /// </summary>
    public class NumberOfProcessLaunchedUtility
    {
        /// <summary>
        /// Counts processes that are created until cancellation is requested.
        /// </summary>
        /// <param name="cts">
        /// To cancel the task and finish counting
        /// </param>
        /// <param name="runnerName">
        /// Name of the process, or a library that is launched by dotnet.exe
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task<int> NumberOfProcessCreated(CancellationTokenSource cts, string processName)
        {
            return await NumberOfProcessCreated(cts, new[] { processName });
        }
            
        public static async Task<int> NumberOfProcessCreated(CancellationTokenSource cts, IEnumerable<string> processNames)
        {
            var processesBeforeRun = GetProcesses(processNames);

            var numOfProcessTask = Task.Run(() => NumberOfProcessLaunchedDuringRun(cts.Token, processesBeforeRun, processNames));
            return await numOfProcessTask;
        }

        /// <summary>
        /// The number of process launched during run.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="processesBeforeRun">
        /// The processes that were already running.
        /// </param>
        /// <param name="runnerName">
        /// The process name.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int NumberOfProcessLaunchedDuringRun(
            CancellationToken token,
            IEnumerable<Process> processesBeforeRun,
            IEnumerable<string> processNames)
        {
            var existingProcessIDs = processesBeforeRun.Select(p => p.Id).ToList();
            var startedCount = 0;

            while (!token.IsCancellationRequested)
            {
                var startedDuringRun = GetProcesses(processNames);

                foreach (var process in startedDuringRun)
                {
                    if (existingProcessIDs.Contains(process.Id))
                    {
                        continue;
                    }

                    startedCount++;
                    existingProcessIDs.Add(process.Id);
                }
            }

            return startedCount;
        }

        private static IEnumerable<Process> GetProcesses(IEnumerable<string> processNames)
        {

            var processes = new List<Process>();
            foreach (var processName in processNames)
            {
                processes.AddRange(Process.GetProcessesByName(processName));
            }

            return processes;
        }
    }
}