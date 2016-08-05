// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1.Interfaces
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Expose methods to be used by data collection process to send messages to Test Platform Client.
    /// </summary>
    internal interface IMessageSink
    {
        /// <summary>
        /// Gets or sets event handler to be invoked on data collection message
        /// </summary>
        EventHandler<DataCollectionMessageEventArgs> OnDataCollectionMessage { get; set; }

        /// <summary>
        /// Data collection message as sent by DataCollectionLogger.
        /// </summary>
        /// <param name="args">Data collection message event args.</param>
        void SendMessage(DataCollectionMessageEventArgs args);

        /// <summary>
        /// Data collection data message as sent by DataCollectionSink.
        /// </summary>
        /// <param name="collectorDataMessage">Data collector data message.</param>
        void SendMessage(DataCollectorDataMessage collectorDataMessage);
    }
}
