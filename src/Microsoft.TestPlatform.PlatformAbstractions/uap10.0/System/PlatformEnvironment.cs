// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <inheritdoc />
public class PlatformEnvironment : IEnvironment
{
    /// <inheritdoc />
    public PlatformArchitecture Architecture
        => RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X86 => PlatformArchitecture.X86,
            System.Runtime.InteropServices.Architecture.X64 => PlatformArchitecture.X64,
            System.Runtime.InteropServices.Architecture.Arm => PlatformArchitecture.ARM,
            System.Runtime.InteropServices.Architecture.Arm64 => PlatformArchitecture.ARM64,
            _ => throw new NotSupportedException(),
        };

    /// <inheritdoc />
    public PlatformOperatingSystem OperatingSystem
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PlatformOperatingSystem.Windows
            : PlatformOperatingSystem.Unix;

    /// <inheritdoc />
    public string OperatingSystemVersion => RuntimeInformation.OSDescription;

    /// <inheritdoc />
    public int ProcessorCount => Environment.ProcessorCount;

    /// <inheritdoc />
    public void Exit(int exitcode)
    {
        Environment.FailFast("Process terminating with exit code: " + exitcode);
    }

    /// <inheritdoc />
    public int GetCurrentManagedThreadId()
    {
        return Environment.CurrentManagedThreadId;
    }
}

#endif
