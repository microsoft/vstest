// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    internal class DataCollectionTestCaseEventSender : IDataCollectionTestCaseEventSender
    {
        private readonly ICommunicationManager communicationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class. 
        /// </summary>
        public DataCollectionTestCaseEventSender() : this(new SocketCommunicationManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventSender"/> class. 
        /// </summary>
        /// <param name="communicationManager">Communication manager.</param>
        public DataCollectionTestCaseEventSender(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
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
        public void SendTestCaseStart(TestCase testCase)
        {
            this.communicationManager.SendMessage(MessageType.BeforeTestCaseStart, testCase);
        }

        /// <inheritdoc />
        public void SendTestCaseCompleted(TestCase testCase, TestOutcome outcome)
        {
            var message = JsonDataSerializer.Instance.Serialize<object[]>(new object[] { testCase, outcome });
            this.communicationManager.SendMessage(MessageType.AfterTestCaseCompleted, message);
        }
    }
}
