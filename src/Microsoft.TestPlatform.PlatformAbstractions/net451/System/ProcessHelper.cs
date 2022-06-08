﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

public partial class ProcessHelper : IProcessHelper
{
    private PlatformArchitecture? _currentProcessArchitecture;

    /// <inheritdoc/>
    public string GetCurrentProcessLocation()
        => Path.GetDirectoryName(GetCurrentProcessFileName());

    /// <inheritdoc/>
    public IntPtr GetProcessHandle(int processId) =>
        processId == _currentProcess.Id
            ? _currentProcess.Handle
            : Process.GetProcessById(processId).Handle;

    /// <inheritdoc/>
    public PlatformArchitecture GetCurrentProcessArchitecture()
    {
        _currentProcessArchitecture ??= GetProcessArchitecture(_currentProcess.Id);
        return _currentProcessArchitecture.Value;
    }

    public PlatformArchitecture GetProcessArchitecture(int processId)
    {
        try
        {
            if (_currentProcess.Id == processId)
            {
                // If we already cached the current process architecture, no need to figure it out again.
                if (_currentProcessArchitecture != null)
                {
                    return _currentProcessArchitecture.Value;
                }

                // When this is current process, we can just check if IntPointer size to get if we are 64-bit or 32-bit.
                // When it is 32-bit we can just return, if it is 64-bit we need to clarify if x64 or arm64.
                if (IntPtr.Size == 4)
                {
                    return PlatformArchitecture.X86;
                }
            }



            // If the current process is 64-bit, or this is any remote process, we need to query it via native api.
            var process = processId == _currentProcess.Id ? _currentProcess : Process.GetProcessById(processId);
            if (!NativeMethods.IsWow64Process2(process.Handle, out ushort processMachine, out ushort nativeMachine))
            {
                throw new Win32Exception();
            }

            if (processMachine != NativeMethods.IMAGE_FILE_MACHINE_UNKNOWN)
            {
                // The process is running using WOW64, which suggests it is 32-bit (or any of the other machines, that we cannot
                // handle, so we just assume x86).
                return PlatformArchitecture.X86;
            }

            // If processMachine is IMAGE_FILE_MACHINE_UNKNOWN mean that we're not running using WOW64 emulation.
            // If nativeMachine is IMAGE_FILE_MACHINE_ARM64 mean that we're running on ARM64 architecture device.
            if (processMachine == NativeMethods.IMAGE_FILE_MACHINE_UNKNOWN && nativeMachine == NativeMethods.IMAGE_FILE_MACHINE_ARM64)
            {
                // To distinguish between ARM64 and x64 emulated on ARM64 we check the PE header of the current running executable.
                if (IsArm64Executable(process.MainModule.FileName))
                {
                    return PlatformArchitecture.ARM64;
                }
                else
                {
                    return PlatformArchitecture.X64;
                }
            }
            else
            {
                return PlatformArchitecture.X64;
            }
        }
        catch
        {
            // At the moment we cannot log messages inside the Microsoft.TestPlatform.PlatformAbstractions.
            // We did an attempt in https://github.com/microsoft/vstest/pull/3422 - 17.2.0-preview-20220301-01 - but we reverted after
            // because we broke a scenario where for .NET Framework application inside the test host
            // we loaded runner version of Microsoft.TestPlatform.PlatformAbstractions but newer version Microsoft.TestPlatform.ObjectModel(the one close
            // to the test container) and the old PlatformAbstractions doesn't contain the methods expected by the new ObjectModel throwing
            // a MissedMethodException.
        }

        // In case of error just return the current process architecture.
        return GetCurrentProcessArchitecture();
    }

    private static bool IsArm64Executable(string path)
    {
        // This document specifies the structure of executable (image) files
        // https://docs.microsoft.com/windows/win32/debug/pe-format#general-concepts
        using Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fs);

        // https://docs.microsoft.com/windows/win32/debug/pe-format#ms-dos-stub-image-only
        // At location 0x3c, the stub has the file offset to the PE signature.
        fs.Position = 0x3C;
        var peHeader = reader.ReadUInt32();

        // Check if the offset is invalid
        if (peHeader > fs.Length - 5)
        {
            return false;
        }

        // https://docs.microsoft.com/windows/win32/debug/pe-format#signature-image-only
        // Moving to the PE Header start location.
        fs.Position = peHeader;

        // After the MS-DOS stub, at the file offset specified at offset 0x3c, is a 4-byte signature that identifies the file as a PE format image file.
        // This signature is "PE\0\0" (the letters "P" and "E" followed by two null bytes).
        uint signature = reader.ReadUInt32();
        if (signature != 0x00004550)
        {
            return false;
        }

        // https://docs.microsoft.com/windows/win32/debug/pe-format#coff-file-header-object-and-image
        // At the beginning of an object file, or immediately after the signature of an image file, is a standard COFF file header.
        var machine = reader.ReadUInt16();
        reader.ReadUInt16(); //NumberOfSections
        reader.ReadUInt32(); //TimeDateStamp
        reader.ReadUInt32(); //PointerToSymbolTable
        reader.ReadUInt32(); //NumberOfSymbols
        reader.ReadUInt16(); //SizeOfOptionalHeader
        reader.ReadUInt16(); //Characteristics

        // https://docs.microsoft.com/windows/win32/debug/pe-format#optional-header-image-only
        ushort magic = reader.ReadUInt16();
        return magic is 0x010B or 0x020B && machine == NativeMethods.IMAGE_FILE_MACHINE_ARM64;
    }
}

#endif
