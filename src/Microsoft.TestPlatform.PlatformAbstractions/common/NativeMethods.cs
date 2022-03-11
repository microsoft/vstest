// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

internal class NativeMethods
{
    public const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;
    public const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);
}

#endif
