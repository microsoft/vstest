// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <summary>
/// Assembly Extensions
/// </summary>
public static class PlatformAssemblyExtensions
{
    /// <summary>
    /// Get current assembly location as per current platform
    /// </summary>
    /// <param name="assembly">Assembly</param>
    /// <returns>Returns Assembly location as per platform</returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Part of the public API")]
    public static string GetAssemblyLocation(this Assembly assembly)
    {
        // In UWP all assemblies are packages inside Appx folder, so we return location of current directory
        return Directory.GetCurrentDirectory();
    }
}

#endif
