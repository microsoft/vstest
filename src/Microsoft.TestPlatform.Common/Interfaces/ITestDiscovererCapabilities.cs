// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

    /// <summary>
    /// Metadata that is available from Test Discoverers.
    /// </summary>
    public interface ITestDiscovererCapabilities
    {
        /// <summary>
        /// List of file extensions that the test discoverer can process tests from.
        /// </summary>
        IEnumerable<string> FileExtension { get; }

        /// <summary>
        /// Default executor Uri for this discoverer
        /// </summary>
        Uri DefaultExecutorUri { get; }

        /// <summary>
        /// Assembly type that the test discoverer supports.
        /// </summary>
        AssemblyType AssemblyType { get; }
    }
}
