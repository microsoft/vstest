// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector.Interfaces
{
    /// <summary>
    /// Operating system environment abstractions.
    /// </summary>
    internal interface IEnvironment
    {
        /// <summary>
        /// Gets operating System.
        /// </summary>
        PlatformOperatingSystem OperatingSystem { get; }
    }
}
