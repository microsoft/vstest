﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System.Collections.Generic;

    public interface ICrashDumper
    {
        void AttachToTargetProcess(int processId, string outputDirectory, DumpTypeOption dumpType, bool collectAlways);

        void WaitForDumpToFinish();

        void DetachFromTargetProcess(int processId);

        IEnumerable<string> GetDumpFiles(bool processCrashed);
    }
}
