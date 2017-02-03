// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The data collection request handler interface.
    /// </summary>
    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private readonly ICommunicationManager communicationManager;
        private IMessageSink messageSink;
        private IDataCollectionManager dataCollectionManager;
        private static readonly object obj = new object();

        internal static DataCollectionRequestHandler RequestHandler;

        internal DataCollectionRequestHandler(IMessageSink messageSink)
            : this(new SocketCommunicationManager())
        {
            this.messageSink = messageSink;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestHandler"/> class. 
        /// </summary>
        /// <param name="communicationManager">
        /// </param>
        internal DataCollectionRequestHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <summary>
        /// Gets singleton instance of DataCollectionRequestHandler.
        /// </summary>
        public static DataCollectionRequestHandler Instance
        {
            get
            {
                lock (obj)
                {
                    if (RequestHandler == null)
                    {
                        RequestHandler = new DataCollectionRequestHandler(default(IMessageSink));
                    }

                    return RequestHandler;
                }
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopClient();
            this.dataCollectionManager?.Dispose();
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection !");
        }

        /// <inheritdoc />
        public void InitializeCommunication(int port)
        {
            this.communicationManager.SetupClientAsync(port);
        }

        /// <inheritdoc />
        public bool WaitForRequestSenderConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForServerConnection(connectionTimeout);
        }

        /// <summary>
        /// Process requests.
        /// </summary>
        public void ProcessRequests()
        {
            bool isSessionEnd = false;

            do
            {
                var message = this.communicationManager.ReceiveMessage();
                switch (message.MessageType)
                {
                    case MessageType.BeforeTestRunStart:
                        EqtTrace.Info("DataCollection starting.");

                        var settingXml = message.Payload.ToObject<string>();
                        this.dataCollectionManager = new DataCollectionManager(this.messageSink);
                        var envVariables = this.dataCollectionManager.InitializeDataCollectors(settingXml);
                        this.dataCollectionManager.SessionStarted();

                        this.communicationManager.SendMessage(MessageType.BeforeTestRunStartResult, new BeforeTestRunStartResult(envVariables, true, 0));

                        EqtTrace.Info("DataCollection started.");
                        break;

                    case MessageType.AfterTestRunEnd:
                        EqtTrace.Info("DataCollection completing.");
                        var isCancelled = message.Payload.ToObject<bool>();

                        var attachments = this.dataCollectionManager.SessionEnded(isCancelled);

                        this.communicationManager.SendMessage(MessageType.AfterTestRunEndResult, attachments);
                        EqtTrace.Info("Session End message received from server. Closing the connection.");

                        isSessionEnd = true;
                        this.Close();

                        EqtTrace.Info("DataCollection completed");
                        break;
                    default:
                        EqtTrace.Info("DataCollection : Invalid Message types");
                        break;
                }
            }
            while (!isSessionEnd);
        }

        /// <summary>
        /// Sends datacollection message.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void SendDataCollectionMessage(DataCollectionMessageEventArgs args)
        {
            this.communicationManager.SendMessage(MessageType.DataCollectionMessage, args);
        }
    }
}