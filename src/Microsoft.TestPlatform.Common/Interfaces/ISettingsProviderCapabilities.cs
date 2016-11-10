// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Metadata that is available from Settings Providers.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "This interface is only public due to limitations in MEF which require metadata interfaces to be public.")]
    public interface ISettingsProviderCapabilities
    {
        /// <summary>
        /// Gets the name of the settings section.
        /// </summary>
        string SettingsName { get; }
    }
}
