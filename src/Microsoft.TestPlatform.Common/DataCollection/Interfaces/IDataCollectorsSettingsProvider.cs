// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Provides settings for data collectors.
    /// </summary>
    public interface IDataCollectorsSettingsProvider : ISettingsProvider
    {
        /// <summary>
        /// Gets data collectors settings.
        /// </summary>
        DataCollectionRunSettings Settings { get; }
    }
}