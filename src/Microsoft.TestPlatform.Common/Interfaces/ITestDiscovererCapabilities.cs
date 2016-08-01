// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System;
    using System.Collections.Generic;

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
    }
}
