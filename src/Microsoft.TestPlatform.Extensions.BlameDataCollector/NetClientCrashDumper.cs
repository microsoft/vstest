// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    internal class NetClientCrashDumper : ICrashDumper
    {
        public void AttachToTargetProcess(int processId, string outputDirectory, DumpTypeOption dumpType)
        {
            // we don't need to do anything directly here, we setup the env variables
            // in the dumper configuration, including the path
        }

        public void DetachFromTargetProcess(int processId)
        {
            // here we might consider renaming the files to have timestamp
        }

        public void WaitForDumpToFinish()
        {
        }
    }
}
