using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
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
    }
}
