// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc />
public class PlatformEnvironment : IEnvironment
{
    private PlatformArchitecture? _architecture;

    /// <inheritdoc />
    public PlatformArchitecture Architecture
    {
        get
        {
            return _architecture ??= RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.X86 => PlatformArchitecture.X86,
                System.Runtime.InteropServices.Architecture.X64 => PlatformArchitecture.X64,
                System.Runtime.InteropServices.Architecture.Arm => PlatformArchitecture.ARM,
                System.Runtime.InteropServices.Architecture.Arm64 => PlatformArchitecture.ARM64,
                _ => throw new NotSupportedException(),
            };
        }
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
    public string OperatingSystemVersion => Environment.OSVersion.ToString();

    /// <inheritdoc />
    public int ProcessorCount => Environment.ProcessorCount;

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
