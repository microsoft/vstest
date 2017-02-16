// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Metadata that is available from Test Host.
    /// </summary>
    public interface ITestHostCapabilities : ITestExtensionCapabilities
    {
        /// specifies the friendly name corresponding to the TestHost.
        string FriendlyName { get; }
    }
}
