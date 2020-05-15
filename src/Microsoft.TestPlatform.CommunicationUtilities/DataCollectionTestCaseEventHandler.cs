// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System.Net;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The test case data collection request handler.
    /// </summary>
    internal class DataCollectionTestCaseEventHandler : IDataCollectionTestCaseEventHandler
    {
        private ICommunicationManager communicationManager;
        private IDataCollectionManager dataCollectionManager;
        private IDataSerializer dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        internal DataCollectionTestCaseEventHandler()
            : this(new SocketCommunicationManager(), DataCollectionManager.Instance, JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        /// <param name="communicationManager">Communication manager implementation.</param>
        /// <param name="dataCollectionManager">Data collection manager implementation.</param>
        /// <param name="dataSerializer">Serializer for serialization and deserialization of the messages.</param>
        internal DataCollectionTestCaseEventHandler(ICommunicationManager communicationManager, IDataCollectionManager dataCollectionManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataCollectionManager = dataCollectionManager;
            this.dataSerializer = dataSerializer;
        }

        /// <inheritdoc />
        public int InitializeCommunication()
        {
            var endpoint = this.communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0));
            this.communicationManager.AcceptClientAsync();
            return endpoint.Port;
        }

        /// <inheritdoc />
        public bool WaitForRequestHandlerConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForClientConnection(connectionTimeout);
        }

        /// <inheritdoc />
        public void Close()
        {
            this.communicationManager?.StopServer();
        }

        /// <inheritdoc />
        public void ProcessRequests()
        {
            var isSessionEnd = false;

            do
            {
                var message = this.communicationManager.ReceiveMessage();
                switch (message.MessageType)
                {
                    case MessageType.DataCollectionTestStart:
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test case starting.");
                        }

                        var testCaseStartEventArgs = this.dataSerializer.DeserializePayload<TestCaseStartEventArgs>(message);
                        this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);
                        this.communicationManager.SendMessage(MessageType.DataCollectionTestStartAck);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test case '{0} - {1}' started.", testCaseStartEventArgs.TestCaseName, testCaseStartEventArgs.TestCaseId);
                        }

                        break;

                    case MessageType.DataCollectionTestEnd:
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler : Test case completing.");
                        }

                        var testCaseEndEventArgs = this.dataSerializer.DeserializePayload<TestCaseEndEventArgs>(message);
                        var attachmentSets = this.dataCollectionManager.TestCaseEnded(testCaseEndEventArgs);
                        this.communicationManager.SendMessage(MessageType.DataCollectionTestEndResult, attachmentSets);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test case '{0} - {1}' completed", testCaseEndEventArgs.TestCaseName, testCaseEndEventArgs.TestCaseId);
                        }

                        break;

                    case MessageType.SessionEnd:
                        isSessionEnd = true;

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test session ended");
                        }

                        this.Close();

                        break;

                    default:
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Invalid Message type '{0}'", message.MessageType);
                        }

                        break;
                }
            }
            while (!isSessionEnd);
        }
    }
}
