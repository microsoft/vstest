﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector;
using System.Diagnostics;

internal class ProcessTreeNode
{
    public Process Process { get; set; }

    public int Level { get; set; }

    public int ParentId { get; set; }

    public Process ParentProcess { get; set; }
}
