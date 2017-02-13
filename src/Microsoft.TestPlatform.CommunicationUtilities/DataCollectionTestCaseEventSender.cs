// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    internal class DataCollectionTestCaseEventSender : IDataCollectionTestCaseEventSender, IDisposable
    {
        private readonly ICommunicationManager communicationManager;

        public DataCollectionTestCaseEventSender() : this(new SocketCommunicationManager())
        {
        }

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
            this.Dispose();
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Closing the connection !");
            }
        }

        /// <summary>
        /// Disposes communication manager.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopClient();
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
