// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.ComponentModel;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc />
public class PlatformEnvironment : IEnvironment
{
    /// <inheritdoc />
    public PlatformArchitecture Architecture
    {
        get
        {
            return Environment.Is64BitOperatingSystem
                ? IsArm64()
                    ? PlatformArchitecture.ARM64
                    : PlatformArchitecture.X64
                : PlatformArchitecture.X86;
        }
    }

    private static bool IsArm64()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            if (!NativeMethods.IsWow64Process2(currentProcess.Handle, out ushort _, out ushort nativeMachine))
            {
                throw new Win32Exception();
            }

            // If nativeMachine is IMAGE_FILE_MACHINE_ARM64 mean that we're running on ARM64 architecture device.
            return nativeMachine == NativeMethods.IMAGE_FILE_MACHINE_ARM64;
        }
        catch (Exception ex)
        {
            PlatformEqtTrace.Verbose($"PlatformEnvironment.IsArm64: Exception during ARM64 machine evaluation, {ex}\n");
        }

        return false;
    }

    /// <inheritdoc />
    public PlatformOperatingSystem OperatingSystem
    {
        get
        {
            // Ensure the value is detected appropriately for Desktop CLR, Mono CLR 1.x and Mono
            // CLR 2.x. See below link for more information:
            // http://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
            int p = (int)Environment.OSVersion.Platform;
            return p is 4 or 6 or 128
                ? PlatformOperatingSystem.Unix
                : PlatformOperatingSystem.Windows;
        }
    }

    /// <inheritdoc />
    public string OperatingSystemVersion
    {
        get
        {
            return Environment.OSVersion.ToString();
        }
    }

    /// <inheritdoc />
    public void Exit(int exitcode)
    {
        Environment.Exit(exitcode);
    }

    /// <inheritdoc />
    public int GetCurrentManagedThreadId()
        => Environment.CurrentManagedThreadId;
}

#endif
