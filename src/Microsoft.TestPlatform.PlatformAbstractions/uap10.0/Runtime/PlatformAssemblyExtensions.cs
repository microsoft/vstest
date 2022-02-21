﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

#if WINDOWS_UWP

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
    public static string GetAssemblyLocation(this Assembly assembly)
    {
        // In UWP all assemblies are packages inside Appx folder, so we return location of current directory
        return Directory.GetCurrentDirectory();
    }
}

#endif
