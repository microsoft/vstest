﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

#nullable disable

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector;

public interface ICrashDumper
{
    void AttachToTargetProcess(int processId, string outputDirectory, DumpTypeOption dumpType, bool collectAlways);

    void WaitForDumpToFinish();

    void DetachFromTargetProcess(int processId);

    IEnumerable<string> GetDumpFiles(bool processCrashed);
}
