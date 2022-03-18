// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

using System;
using System.Reflection;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

public static class PlatformAssemblyExtensions
{
    public static string GetAssemblyLocation(this Assembly assembly)
    {
        throw new NotImplementedException();
    }
}

#endif
