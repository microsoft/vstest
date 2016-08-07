// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Provides settings for test run configuration.
    /// </summary>
    public interface IRunConfigurationSettingsProvider : ISettingsProvider
    {
        /// <summary>
        /// Gets run specific data collection settings.
        /// </summary>
        RunConfiguration Settings { get; }
    }
}
