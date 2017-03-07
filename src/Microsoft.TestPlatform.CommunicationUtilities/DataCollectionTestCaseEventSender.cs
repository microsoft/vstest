// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    public class DataCollectionTestCaseEventSender : IDataCollectionTestCaseEventSender
    {
        private static readonly object obj = new object();

        private readonly ICommunicationManager communicationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class. 
        /// </summary>
        protected DataCollectionTestCaseEventSender() : this(new SocketCommunicationManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class. 
        /// </summary>
        /// <param name="communicationManager">Communication manager.</param>
        protected DataCollectionTestCaseEventSender(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <summary>
        /// Gets the singleton instance of DataCollectionTestCaseEventSender.
        /// </summary>
        // todo : Refactor to pass the instance as singleton.
        public static DataCollectionTestCaseEventSender Instance { get; private set; }

        /// <summary>
        /// Gets singleton instance of DataCollectionRequestHandler.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication Manager.
        /// </param>
        public static DataCollectionTestCaseEventSender Create()
        {
            if (Instance == null)
            {
                lock (obj)
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
            this.communicationManager.SetupClientAsync(port);
        }

        /// <inheritdoc />
        public bool WaitForRequestSenderConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForServerConnection(connectionTimeout);
        }

        /// <inheritdoc />
        public void Close()
        {
            this.communicationManager?.StopClient();
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Closing the connection !");
            }
        }

        /// <inheritdoc />
        public void SendTestCaseStart(TestCaseStartEventArgs e)
        {
            this.communicationManager.SendMessage(MessageType.DataCollectionTestStart, e);
        }

        /// <inheritdoc />
        public void SendTestCaseComplete(TestResultEventArgs e)
        {
            var attachmentSets = new Collection<AttachmentSet>();
            this.communicationManager.SendMessage(MessageType.DataCollectionTestEnd, e);

            var message = this.communicationManager.ReceiveMessage();

            if (message.MessageType == MessageType.DataCollectionTestEndResult)
            {
                attachmentSets = message.Payload.ToObject<Collection<AttachmentSet>>();
            }

            foreach (var attachmentSet in attachmentSets)
            {
                e.TestResult.Attachments.Add(attachmentSet);
            }
        }

        /// <inheritdoc />
        public void SendTestSessionEnd(SessionEndEventArgs e)
        {
            this.communicationManager.SendMessage(MessageType.SessionEnd, e);
        }
    }
}
