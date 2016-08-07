// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Interface for acting upon data collection messages.
    /// </summary>
    public interface IDataCollectionLog
    {
        /// <summary>
        /// Log data collection messages
        /// </summary>
        /// <param name="args">DataCollectionMessage details</param>
        void SendDataCollectionMessage(DataCollectionMessageEventArgs args);
    }
}
