// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;

    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private readonly ICommunicationManager communicationManager;
        private IDataSerializer dataSerializer;

        public DataCollectionRequestHandler()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
        {
        }

        internal DataCollectionRequestHandler(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopClient();
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection !");
        }

        /// <summary>
        /// Setups client based on port
        /// </summary>
        /// <param name="port">port number to connect</param>
        public void InitializeCommunication(int port)
        {
            this.communicationManager.SetupClientAsync(port);
        }

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
                        // TODO: Send actual BeforeTestRunStartResult
                        // string settingXml = message.Payload.ToObject<string>();
                        this.communicationManager.SendMessage(MessageType.BeforeTestRunStartResult, new BeforeTestRunStartResult(null, true, 0));
                        break;

                    case MessageType.AfterTestRunEnd:
                        // TODO: Send actual collection of AttachmentSet
                        this.communicationManager.SendMessage(MessageType.AfterTestRunEndResult, new Collection<AttachmentSet>());
                        EqtTrace.Info("Session End message received from server. Closing the connection.");

                        // TODO: Check if we need a separate message for closing the session.
                        isSessionEnd = true;
                        this.Close();
                        break;
                    default:
                        EqtTrace.Info("Invalid Message types");
                        break;
                }
            }
            while (!isSessionEnd);
        }
    }
}