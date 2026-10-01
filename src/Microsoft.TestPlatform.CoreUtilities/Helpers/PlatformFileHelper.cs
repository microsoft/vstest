// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

/// <summary>
/// Shared helpers for hardening files/directories created under the shared OS temp path.
/// </summary>
public static class PlatformFileHelper
{
#if !NETFRAMEWORK
    [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
    private static extern int NativeChmod(string pathname, int mode);
#endif

    /// <summary>
    /// Restricts access to the given directory to the owning user only (chmod 0700) on non-Windows
    /// platforms. No-op on Windows, or when the directory does not exist.
    /// </summary>
    /// <param name="path">The directory to harden.</param>
    public static void SetOwnerOnlyUnixDirectoryPermissions(string path)
    {
#if !NETFRAMEWORK
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !Directory.Exists(path))
        {
            return;
        }

        // 0700 octal = owner read/write/execute only
        const int ownerFullAccess = 0x1C0;

        int result = NativeChmod(path, ownerFullAccess);
        if (result != 0)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to set permissions on '{path}', errno: {error}");
        }
#endif
    }
}
