// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class NetClientCrashDumper : ICrashDumper
    {
        public void AttachToTargetProcess(int processId, string outputFile, DumpTypeOption dumpType)
        {
            // COMPlus_DbgMiniDumpName
            Environment.SetEnvironmentVariable("COMPlus_DbgEnableMiniDump", "1");
            Environment.SetEnvironmentVariable("COMPlus_CreateDumpDiagnostics", "1");
        }

        public void DetachFromTargetProcess(int processId)
        {
            Environment.SetEnvironmentVariable("COMPlus_DbgEnableMiniDump", "0");
            Environment.SetEnvironmentVariable("COMPlus_CreateDumpDiagnostics", "0");
        }

        public void WaitForDumpToFinish()
        {
        }
    }
}
