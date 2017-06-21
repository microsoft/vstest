// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    using System;

    /// <summary>
    /// Operating system environment abstractions.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Operating System architecture.
        /// </summary>
        PlatformArchitecture Architecture { get; }

        /// <summary>
        /// Operating System name.
        /// </summary>
        PlatformOperatingSystem OperatingSystem { get; }
    }
}
