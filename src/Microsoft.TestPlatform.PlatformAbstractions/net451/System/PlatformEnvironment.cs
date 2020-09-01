// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc />
    public class PlatformEnvironment : IEnvironment
    {
        /// <inheritdoc />
        public PlatformArchitecture Architecture
        {
            get
            {
                // On Mono System.Runtime.InteropServices.RuntimeInformation breaks
                // See https://github.com/dotnet/corefx/issues/15112
                // Support just x86 and x64 for now, likely our solution for ARM is going to be
                // netcore based.
                return Environment.Is64BitOperatingSystem ? PlatformArchitecture.X64 : PlatformArchitecture.X86;
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
                int p = (int)System.Environment.OSVersion.Platform;
                if ((p == 4) || (p == 6) || (p == 128))
                {
                    return PlatformOperatingSystem.Unix;
                }

                return PlatformOperatingSystem.Windows;
            }
        }

        /// <inheritdoc />
        public string OperatingSystemVersion
        {
            get
            {
                return System.Environment.OSVersion.ToString();
            }
        }

        /// <inheritdoc />
        public void Exit(int exitcode)
        {
            Environment.Exit(exitcode);
        }

        /// <inheritdoc />
        public int GetCurrentManagedThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}

#endif
