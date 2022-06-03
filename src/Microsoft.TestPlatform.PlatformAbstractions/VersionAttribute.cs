// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.TestPlatform.PlatformAbstractions;

[Conditional("DEBUG")]
internal class VersionAttribute : Attribute
{
    public string? Description { get; set; }

    public VersionAttribute(int version)
    {

    }
}
