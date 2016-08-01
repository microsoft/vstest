// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Basic metadata for extensions which are identified by a URI.
    /// </summary>
    public interface ITestExtensionCapabilities
    {
        /// <summary>
        /// Gets the URI of the test extension.
        /// </summary>
        string ExtensionUri { get; }
    }
}
