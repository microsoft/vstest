// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System.Runtime.InteropServices;
    using Interfaces;

    /// <inheritdoc />
    internal class PlatformEnvironment : IEnvironment
    {/// <inheritdoc />
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
    }
}

