﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using System.IO;
using System.Reflection;

using Interfaces;

/// <inheritdoc/>
public class PlatformAssemblyLoadContext : IAssemblyLoadContext
{
    /// <inheritdoc/>
    public AssemblyName GetAssemblyNameFromPath(string assemblyPath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);
        return new AssemblyName(fileNameWithoutExtension);
    }

    /// <inheritdoc/>
    public Assembly LoadAssemblyFromPath(string assemblyPath)
    {
        return Assembly.Load(GetAssemblyNameFromPath(assemblyPath));
    }
}

#endif
