// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;

    public interface IProcessDumpUtility
    {
        /// <summary>
        /// Get generated dump files
        /// </summary>
        /// <param name="warnOnNoDumpFiles">Writes warning when no dump file is found.</param>
        /// <returns>
        /// Path of dump file
        /// </returns>
        IEnumerable<string> GetDumpFiles(bool warnOnNoDumpFiles = true);

        /// <summary>
        /// Launch proc dump process
        /// </summary>
        /// <param name="processId">
        /// Process ID of test host
        /// </param>
        /// <param name="testResultsDirectory">
        /// Path to TestResults directory
        /// </param>
        /// <param name="isFullDump">
        /// Is full dump enabled
        /// </param>
        /// <param name="targetFramework">
        /// The target framework of the process
        /// </param>
        void StartTriggerBasedProcessDump(int processId, string testResultsDirectory, bool isFullDump, string targetFramework);

        /// <summary>
        /// Launch proc dump process to capture dump in case of a testhost hang and wait for it to exit
        /// </summary>
        /// <param name="processId">
        /// Process ID of test host
        /// </param>
        /// <param name="testResultsDirectory">
        /// Path to TestResults directory
        /// </param>
        /// <param name="isFullDump">
        /// Is full dump enabled
        /// </param>
        /// <param name="targetFramework">
        /// The target framework of the process
        /// </param>
        /// <param name="logWarning">
        /// Callback to datacollector logger to log warning
        /// </param>
        void StartHangBasedProcessDump(int processId, string testResultsDirectory, bool isFullDump, string targetFramework, Action<string> logWarning = null);

        /// <summary>
        /// Detaches the proc dump process from the target process
        /// Ensure this is called before terminating the proc dump process
        /// as it might lead to the testhost process crashing otherwise.
        /// </summary>
        /// <param name="targetProcessId">
        /// Process Id of the process to detach from
        /// </param>
        void DetachFromTargetProcess(int targetProcessId);
    }
}
