// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc />
    public class PlatformEnvironment : IEnvironment
    {
        /// <inheritdoc />
        public PlatformArchitecture Architecture
        {
            get
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case System.Runtime.InteropServices.Architecture.X86:
                        return PlatformArchitecture.X86;
                    case System.Runtime.InteropServices.Architecture.X64:
                        return PlatformArchitecture.X64;
                    case System.Runtime.InteropServices.Architecture.Arm:
                        return PlatformArchitecture.ARM;
                    case System.Runtime.InteropServices.Architecture.Arm64:
                        return PlatformArchitecture.ARM64;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        /// <inheritdoc />
        public PlatformOperatingSystem OperatingSystem
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return PlatformOperatingSystem.Windows;
                }

                return PlatformOperatingSystem.Unix;
            }
        }

        /// <inheritdoc />
        public string OperatingSystemVersion
        {
            get
            {
                return RuntimeInformation.OSDescription;
            }
        }

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
}

#endif
