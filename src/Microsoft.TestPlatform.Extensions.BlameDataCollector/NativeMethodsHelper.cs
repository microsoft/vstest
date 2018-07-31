// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.TestPlatform.Extensions.BlameDataCollector.Interfaces;

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
            bool isWow64 = false;
            if (!IsWow64Process(processHandle, out isWow64))
            {
                throw new Win32Exception();
            }

            return !isWow64;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }
}
