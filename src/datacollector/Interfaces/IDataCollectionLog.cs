// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
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
