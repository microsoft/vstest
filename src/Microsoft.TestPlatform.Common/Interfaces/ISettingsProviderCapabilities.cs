// Copyright (c) Microsoft. All rights reserved.

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
