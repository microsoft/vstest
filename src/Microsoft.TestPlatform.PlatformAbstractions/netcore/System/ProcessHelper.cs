// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Interfaces;

public partial class ProcessHelper : IProcessHelper
{
    /// <inheritdoc/>
    public string GetCurrentProcessLocation()
    {
        return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    }

    /// <inheritdoc/>
    public IntPtr GetProcessHandle(int processId)
    {
        // An IntPtr representing the value of the handle field.
        // If the handle has been marked invalid with SetHandleAsInvalid, this method still returns the original handle value, which can be a stale value.
        return Process.GetProcessById(processId).SafeHandle.DangerousGetHandle();
    }

    public PlatformArchitecture GetCurrentProcessArchitecture()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                return PlatformArchitecture.X86;
            case Architecture.X64:
                return PlatformArchitecture.X64;
            case Architecture.Arm:
                return PlatformArchitecture.ARM;
            case Architecture.Arm64:
                return PlatformArchitecture.ARM64;

            // The symbolic value is only available with .NET 6
            // preview 6 or later, so use the numerical value for now.
            // case System.Runtime.InteropServices.Architecture.S390x:
            case (Architecture)5:
                return PlatformArchitecture.S390x;
            default:
                throw new NotSupportedException();
        }
    }
}

#endif
