// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    public interface IProcessDumpUtility
    {
        /// <summary>
        /// Get generated dump files
        /// </summary>
        /// <returns>
        /// Path of dump file
        /// </returns>
        string GetDumpFile();

        /// <summary>
        /// Launch proc dump process
        /// </summary>
        /// <param name="processId">
        /// Process ID of test host
        /// </param>
        /// <param name="dumpFileGuid">
        /// Guid as postfix for dump file, testhost.exe_&lt;guid&gt;.dmp
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
        void StartTriggerBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump, string targetFramework);

        /// <summary>
        /// Launch proc dump process to capture dump in case of a testhost hang and wait for it to exit
        /// </summary>
        /// <param name="processId">
        /// Process ID of test host
        /// </param>
        /// <param name="dumpFileGuid">
        /// Guid as postfix for dump file, testhost.exe_&lt;guid&gt;.dmp
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
        void StartHangBasedProcessDump(int processId, string dumpFileGuid, string testResultsDirectory, bool isFullDump, string targetFramework);

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
