// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        internal DataCollectionTestCaseEventHandler() : this(new SocketCommunicationManager(), DataCollectionManager.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        internal DataCollectionTestCaseEventHandler(ICommunicationManager communicationManager, IDataCollectionManager dataCollectionManager)
        {
            this.communicationManager = communicationManager;
            this.dataCollectionManager = dataCollectionManager;
        }

        /// <inheritDoc />
        public int InitializeCommunication()
        {
            var port = this.communicationManager.HostServer();
            this.communicationManager.AcceptClientAsync();
            return port;
        }

        /// <inheritDoc />
        public bool WaitForRequestHandlerConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForClientConnection(connectionTimeout);
        }

        /// <inheritDoc />
        public void Close()
        {
            this.communicationManager?.StopServer();
        }

        /// <inheritDoc />
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

                        var testCaseStartEventArgs = message.Payload.ToObject<TestCaseStartEventArgs>();
                        this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test case started.");
                        }

                        break;

                    case MessageType.DataCollectionTestEnd:
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler : Test case completing.");
                        }

                        var testCaseEndEventArgs = message.Payload.ToObject<TestCaseEndEventArgs>();
                        var attachmentSets = this.dataCollectionManager.TestCaseEnded(testCaseEndEventArgs);
                        this.communicationManager.SendMessage(MessageType.AfterTestCaseEndResult, attachmentSets);

                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Test case completed");
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
                            EqtTrace.Info("DataCollectionTestCaseEventHandler: Invalid Message types");
                        }

                        break;
                }
            }
            while (!isSessionEnd);
        }
    }
}
