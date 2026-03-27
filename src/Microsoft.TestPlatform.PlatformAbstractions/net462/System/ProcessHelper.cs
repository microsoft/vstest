// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

public partial class ProcessHelper : IProcessHelper
{
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
    /// <inheritdoc/>
    public string GetCurrentProcessLocation()
        => Path.GetDirectoryName(GetCurrentProcessFileName());

    /// <inheritdoc/>
    public nint GetProcessHandle(int processId) =>
        processId == _currentProcess.Id
            ? _currentProcess.Handle
            : Process.GetProcessById(processId).Handle;

    /// <inheritdoc/>
    public PlatformArchitecture GetCurrentProcessArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => PlatformArchitecture.X86,
            Architecture.X64 => PlatformArchitecture.X64,
            Architecture.Arm => PlatformArchitecture.ARM,
            Architecture.Arm64 => PlatformArchitecture.ARM64,
            _ => throw new NotSupportedException(),
        };
    }
#endif

    public PlatformArchitecture GetProcessArchitecture(int processId)
    {
#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // No implementation for this for cross platform, and we cannot move this to platform specific file.
            // Usages are only from hang dumper.
            throw new NotImplementedException();
        }
#endif

        // For the current process, use RuntimeInformation directly.
        if (processId == _currentProcess.Id)
        {
            return GetCurrentProcessArchitecture();
        }

        // For remote processes, we need Windows APIs.
        var process = Process.GetProcessById(processId);
        try
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                return PlatformArchitecture.X86;
            }

            var isWow64Succeeded = NativeMethods.IsWow64Process(process.Handle, out var isWow64);
            if (isWow64Succeeded && isWow64)
            {
                // The process is running using WOW64, which means it is 32-bit.
                return PlatformArchitecture.X86;
            }

            // Not WOW64 — this is a native 64-bit process.
            // On ARM64 OS, distinguish ARM64 native from x64 emulated via the PE header.
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                return IsArm64Executable(process.MainModule!.FileName)
                    ? PlatformArchitecture.ARM64
                    : PlatformArchitecture.X64;
            }

            return PlatformArchitecture.X64;
        }
        catch
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                return PlatformArchitecture.X86;
            }

            // We are on 64-bit system, let's assume x64 when we fail to determine the value.
            return PlatformArchitecture.X64;
        }
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
