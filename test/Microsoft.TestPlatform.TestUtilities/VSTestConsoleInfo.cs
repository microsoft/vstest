﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.TestUtilities;

[Serializable]
public class VSTestConsoleInfo
{
    public string? VersionType { get; set; }
    public string? Version { get; set; }
    public string? Path { get; set; }

    public override string ToString() => $" vstest.console = {Version} [{VersionType}]";
}

