// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    /// <summary>
    /// Wrapper class for configs related to proc dump
    /// </summary>
    public class ProcDumpConfig
    {
        public ProcDumpConfig(int processId, string dumpFileGuid, string dumpFileSaveLocation, bool includeFirstChanceExceptions = true, bool isFullDump = false)
        {
            this.ProcessId = processId;
            this.DumpFileGuid = dumpFileGuid;
            this.DumpFileSaveLocation = dumpFileSaveLocation;
            this.IncludeFirstChanceExceptions = includeFirstChanceExceptions;
            this.IsFullDump = isFullDump;
        }

        /// <summary>
        /// Gets a value indicating process ID of test host
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets a value indicating postfix for dump file, testhost.exe_&lt;guid&gt;.dmp
        /// </summary>
        public string DumpFileGuid { get; }

        /// <summary>
        /// Gets a value indicating path to where the dump should be written
        /// </summary>
        public string DumpFileSaveLocation { get; }

        /// <summary>
        /// Gets a value indicating whether proc dump should be configured to capture dumps on first chance exceptions.
        /// </summary>
        public bool IncludeFirstChanceExceptions { get; }

        /// <summary>
        /// Gets a value indicating whether full dump capture enabled
        /// </summary>
        public bool IsFullDump { get; }
    }
}
