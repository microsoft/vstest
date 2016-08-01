// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Metadata that is available from Test Loggers.
    /// </summary>
    public interface ITestLoggerCapabilities : ITestExtensionCapabilities
    {
        /// specifies the friendly name corresponding to the logger.
        string FriendlyName { get; }
    }
}
