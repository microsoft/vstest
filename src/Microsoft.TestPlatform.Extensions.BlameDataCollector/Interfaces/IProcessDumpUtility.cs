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
        /// Launch procdump process
        /// </summary>
        /// <param name="procDumpConfig">
        /// Configurations for proc dump
        /// </param>
        void StartProcessDump(ProcDumpConfig procDumpConfig);

        /// <summary>
        /// Terminate the proc dump process
        /// </summary>
        void TerminateProcess();
    }
}
