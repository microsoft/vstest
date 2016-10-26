// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.TestUtilities
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     The number of process launched utility.
    /// </summary>
    public class NumberOfProcessLaunchedUtility
    {
        /// <summary>
        /// The number of process created.
        /// </summary>
        /// <param name="cts">
        /// The cts.
        /// </param>
        /// <param name="testhostProcessName">
        /// The testhost process name.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task<int> NumberOfProcessCreated(CancellationTokenSource cts, string testhostProcessName)
        {
            var testhostProcessIDsBeforeRun = new List<int>();
            var testhostProcessesBeforeRun = Process.GetProcessesByName(testhostProcessName);

            foreach (var process in testhostProcessesBeforeRun)
            {
                testhostProcessIDsBeforeRun.Add(process.Id);
            }

            var numOfProcessTask =
                Task.Run(() => NumberOfProcessLaunchedDuringRun(cts.Token, testhostProcessIDsBeforeRun, testhostProcessName));
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
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int NumberOfProcessLaunchedDuringRun(
            CancellationToken token,
            List<int> executorProcessesBeforeRun,
            string processName)
        {
            var preCreatedProcessIDs = new List<int>(executorProcessesBeforeRun);
            var desireCount = 0;

            while (!token.IsCancellationRequested)
            {
                var executorProcessDuringRun = Process.GetProcessesByName(processName);

                foreach (var process in executorProcessDuringRun)
                {
                    if (preCreatedProcessIDs.Contains(process.Id))
                    {
                        continue;
                    }

                    desireCount++;
                    preCreatedProcessIDs.Add(process.Id);
                }
            }

            return desireCount;
        }
    }
}