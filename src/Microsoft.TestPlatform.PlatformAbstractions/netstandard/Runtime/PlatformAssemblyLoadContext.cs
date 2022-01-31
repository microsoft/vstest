﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using System.Reflection;

using Interfaces;

/// <inheritdoc/>
public class PlatformAssemblyLoadContext : IAssemblyLoadContext
{
    /// <inheritdoc/>
    public AssemblyName GetAssemblyNameFromPath(string assemblyPath)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Assembly LoadAssemblyFromPath(string assemblyPath)
    {
        throw new System.NotImplementedException();
    }
}

#endif
