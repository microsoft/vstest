﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System.Collections.ObjectModel;
    using System.Net;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    public class DataCollectionTestCaseEventSender : IDataCollectionTestCaseEventSender
    {
        private static readonly object SyncObject = new();

        private readonly ICommunicationManager communicationManager;

        private readonly IDataSerializer dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class.
        /// </summary>
        protected DataCollectionTestCaseEventSender()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class.
        /// </summary>
        /// <param name="communicationManager">Communication manager.</param>
        /// <param name="dataSerializer">Serializer for serialization and deserialization of the messages.</param>
        protected DataCollectionTestCaseEventSender(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// Gets the singleton instance of DataCollectionTestCaseEventSender.
        /// </summary>
        // TODO : Re-factor to pass the instance as singleton.
        public static DataCollectionTestCaseEventSender Instance { get; private set; }

        /// <summary>
        /// Gets singleton instance of DataCollectionRequestHandler.
        /// </summary>
        /// <returns>A singleton instance of <see cref="DataCollectionTestCaseEventSender"/></returns>
        public static DataCollectionTestCaseEventSender Create()
        {
            if (Instance == null)
            {
                lock (SyncObject)
                {
                    if (Instance == null)
                    {
                        Instance = new DataCollectionTestCaseEventSender();
                    }
                }
            }

            return Instance;
        }

        /// <inheritdoc />
        public void InitializeCommunication(int port)
        {
            communicationManager.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, port));
        }

        /// <inheritdoc />
        public bool WaitForRequestSenderConnection(int connectionTimeout)
        {
            return communicationManager.WaitForServerConnection(connectionTimeout);
        }

        /// <inheritdoc />
        public void Close()
        {
            communicationManager?.StopClient();
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Closing the connection !");
            }
        }

        /// <inheritdoc />
        public void SendTestCaseStart(TestCaseStartEventArgs e)
        {
            communicationManager.SendMessage(MessageType.DataCollectionTestStart, e);

            var message = communicationManager.ReceiveMessage();
            if (message != null && message.MessageType != MessageType.DataCollectionTestStartAck)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionTestCaseEventSender.SendTestCaseStart : MessageType.DataCollectionTestStartAck not received.");
                }
            }
        }

        /// <inheritdoc />
        public Collection<AttachmentSet> SendTestCaseEnd(TestCaseEndEventArgs e)
        {
            var attachmentSets = new Collection<AttachmentSet>();
            communicationManager.SendMessage(MessageType.DataCollectionTestEnd, e);

            var message = communicationManager.ReceiveMessage();
            if (message != null && message.MessageType == MessageType.DataCollectionTestEndResult)
            {
                attachmentSets = dataSerializer.DeserializePayload<Collection<AttachmentSet>>(message);
            }

            return attachmentSets;
        }

        /// <inheritdoc />
        public void SendTestSessionEnd(SessionEndEventArgs e)
        {
            communicationManager.SendMessage(MessageType.SessionEnd, e);
        }
    }
}
