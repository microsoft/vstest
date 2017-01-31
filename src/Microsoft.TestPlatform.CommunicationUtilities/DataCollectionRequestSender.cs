// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Utility class that facilitates the IPC communication. Acts as server.
    /// </summary>
    public sealed class DataCollectionRequestSender : IDataCollectionRequestSender, IDisposable
    {
        private ICommunicationManager communicationManager;
        private IDataSerializer dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestSender"/> class. 
        /// </summary>
        public DataCollectionRequestSender() : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestSender"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        /// <param name="dataSerializer">
        /// The data serializer.
        /// </param>
        internal DataCollectionRequestSender(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// Creates an endpoint and listens for client connection asynchronously
        /// </summary>
        /// <returns>Port number</returns>
        public int InitializeCommunication()
        {
            var port = this.communicationManager.HostServer();
            this.communicationManager.AcceptClientAsync();
            return port;
        }

        /// <summary>
        /// Waits for Request Handler to be connected 
        /// </summary>
        /// <param name="clientConnectionTimeout">Time to wait for connection</param>
        /// <returns>True, if Handler is connected</returns>
        public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
        {
            return this.communicationManager.WaitForClientConnection(clientConnectionTimeout);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopServer();
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection");
        }

        /// <inheritdoc/>
        public BeforeTestRunStartResult SendBeforeTestRunStartAndGetResult(string settingsXml, ITestMessageEventHandler runEventsHandler)
        {
            var isDataCollectionStarted = false;
            BeforeTestRunStartResult result = null;

            this.communicationManager.SendMessage(MessageType.BeforeTestRunStart, settingsXml);

            while (!isDataCollectionStarted)
            {
                var message = this.communicationManager.ReceiveMessage();

                if (message.MessageType == MessageType.DataCollectionMessage)
                {
                    var msg = this.dataSerializer.DeserializePayload<DataCollectionMessageEventArgs>(message);
                    runEventsHandler.HandleLogMessage(msg.Level, msg.Message);
                }
                else if (message.MessageType == MessageType.BeforeTestRunStartResult)
                {
                    isDataCollectionStarted = true;
                    result = this.dataSerializer.DeserializePayload<BeforeTestRunStartResult>(message);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public Collection<AttachmentSet> SendAfterTestRunStartAndGetResult(ITestMessageEventHandler runEventsHandler, bool isCancelled)
        {
            var isDataCollectionComplete = false;
            Collection<AttachmentSet> attachmentSets = null;

            this.communicationManager.SendMessage(MessageType.AfterTestRunEnd, isCancelled);

            // Cycle through the messages that the datacollector sends. 
            // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
            while (!isDataCollectionComplete)
            {
                var message = this.communicationManager.ReceiveMessage();

                if (message.MessageType == MessageType.DataCollectionMessage)
                {
                    var msg = this.dataSerializer.DeserializePayload<DataCollectionMessageEventArgs>(message);
                    runEventsHandler.HandleLogMessage(msg.Level, msg.Message);
                }
                else if (message.MessageType == MessageType.AfterTestRunEndResult)
                {
                    attachmentSets = this.dataSerializer.DeserializePayload<Collection<AttachmentSet>>(message);
                    isDataCollectionComplete = true;
                }
            }

            return attachmentSets;
        }
    }
}