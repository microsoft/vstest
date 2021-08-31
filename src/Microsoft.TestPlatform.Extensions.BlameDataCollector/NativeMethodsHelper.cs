﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    public class NativeMethodsHelper : INativeMethodsHelper
    {
        /// <summary>
        /// Returns if a process is 64 bit process
        /// </summary>
        /// <param name="processHandle">Process Handle</param>
        /// <returns>Bool for Is64Bit</returns>
        public bool Is64Bit(IntPtr processHandle)
        {
            // WOW64 is the x86 emulator that allows 32 bit Windows - based applications to run seamlessly on 64 bit Windows.

            // If the function succeeds, the return value is a nonzero value.
            if (!IsWow64Process(processHandle, out var isWow64))
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("NativeMethodsHelper: The call to IsWow64Process failed.");
                }
            }

            return !isWow64;
        }

        // A pointer to a value that is set to TRUE if the process is running under WOW64.
        // If the process is running under 32-bit Windows, the value is set to FALSE.
        // If the process is a 64-bit application running under 64-bit Windows, the value is also set to FALSE.
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }
}
