// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;
    using System.Threading.Tasks;

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
    /// Handles test session events received from vstest console process.
    /// </summary>
    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private readonly ICommunicationManager communicationManager;
        private IMessageSink messageSink;
        private IDataCollectionManager dataCollectionManager;
        private IDataCollectionTestCaseEventHandler testCaseDataCollectionRequestHandler;
        private IDataCollectionManagerFactory dataCollectionManagerFactory;
        private IDataCollectionTestCaseEventManagerFactory testCaseDataCollectionCommunicationFactory;

        private static readonly object obj = new object();

        internal static DataCollectionRequestHandler Instance;

        internal DataCollectionRequestHandler(IMessageSink messageSink)
            : this(new SocketCommunicationManager(), messageSink, new DataCollectionManagerFactory(), new DataCollectionTestCaseEventManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventHandler"/> class. 
        /// </summary>
        /// <param name="communicationManager">
        /// </param>
        internal DataCollectionRequestHandler(ICommunicationManager communicationManager, IMessageSink messageSink, IDataCollectionManagerFactory dataCollectionManagerFactory, IDataCollectionTestCaseEventManagerFactory testCaseDataCollectionCommunicationFactory)
        {
            this.communicationManager = communicationManager;
            this.messageSink = messageSink;
            this.dataCollectionManagerFactory = dataCollectionManagerFactory;
            this.testCaseDataCollectionCommunicationFactory = testCaseDataCollectionCommunicationFactory;
        }

        /// <summary>
        /// Gets singleton instance of DataCollectionRequestHandler.
        /// </summary>
        public static DataCollectionRequestHandler CreateInstance(ICommunicationManager communicationManager, IMessageSink messageSink)
        {
            if (Instance == null)
            {
                ValidateArg.NotNull(communicationManager, nameof(communicationManager));
                ValidateArg.NotNull(messageSink, nameof(messageSink));

                lock (obj)
                {
                    if (Instance == null)
                    {
                        Instance = new DataCollectionRequestHandler(messageSink);
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

        /// <summary>
        /// Process requests.
        /// </summary>
        public void ProcessRequests()
        {
            var isSessionEnd = false;

            do
            {
                var message = this.communicationManager.ReceiveMessage();
                switch (message.MessageType)
                {
                    case MessageType.BeforeTestRunStart:
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info("DataCollection starting.");
                        }

                        // Initialize datacollectors and get enviornment variables.
                        var settingXml = message.Payload.ToObject<string>();
                        this.dataCollectionManager = this.dataCollectionManagerFactory.Create(this.messageSink);
                        var envVariables = this.dataCollectionManager.InitializeDataCollectors(settingXml);
                        var areTestCaseLevelEventsRequired = this.dataCollectionManager.SessionStarted();

                        // Open a socket communication port for test level events.
                        int testCaseEventsPort = -1;
                        if (areTestCaseLevelEventsRequired)
                        {
                            this.testCaseDataCollectionRequestHandler = this.testCaseDataCollectionCommunicationFactory.GetTestCaseDataCollectionRequestHandler();
                            testCaseEventsPort = this.testCaseDataCollectionRequestHandler.InitializeCommunication();

                            Task.Factory.StartNew(() =>
                            {
                                this.testCaseDataCollectionRequestHandler.WaitForRequestHandlerConnection(0);
                                this.testCaseDataCollectionRequestHandler.ProcessRequests();
                            });
                        }

                        this.communicationManager.SendMessage(MessageType.BeforeTestRunStartResult, new BeforeTestRunStartResult(envVariables, areTestCaseLevelEventsRequired, testCaseEventsPort));

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
    }
}