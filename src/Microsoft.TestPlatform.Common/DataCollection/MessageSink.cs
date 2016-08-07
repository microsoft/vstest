// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using Resources = Microsoft.VisualStudio.TestPlatform.Common.Resources;

    /// <summary>
    /// The message sink.
    /// </summary>
    internal class MessageSink : IMessageSink
    {
        /// <summary>
        /// The file manager.
        /// </summary>
        private IDataCollectionFileManager fileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageSink"/> class.
        /// </summary>
        public MessageSink() : this(new DataCollectionFileManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageSink"/> class.
        /// </summary>
        /// <param name="fileManager">
        /// The file manager.
        /// </param>
        internal MessageSink(IDataCollectionFileManager fileManager)
        {
            ValidateArg.NotNull(fileManager, nameof(fileManager));
            this.fileManager = fileManager;
        }

        /// <summary>
        /// Gets or sets the on data collection message.
        /// </summary>
        public EventHandler<DataCollectionMessageEventArgs> OnDataCollectionMessage { get; set; }

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void SendMessage(DataCollectionMessageEventArgs args)
        {
            this.OnDataCollectionMessage.SafeInvoke(this, args, "DataCollectionManager.SendMessage");
        }

        /// <summary>
        /// Data collection message as sent by DataCollectionSink.
        /// </summary>
        /// <param name="collectorDataMessage">DataCollection data message</param>
        public void SendMessage(DataCollectorDataMessage collectorDataMessage)
        {
            ValidateArg.NotNull(collectorDataMessage, nameof(collectorDataMessage));

            if (collectorDataMessage is FileDataHeaderMessage)
            {
                // Dispatch message to file manager.
                this.fileManager.DispatchMessage(collectorDataMessage);
                return;
            }

            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.DataCollectorUnsupportedMessageType, collectorDataMessage.GetType()));
        }
    }
}
