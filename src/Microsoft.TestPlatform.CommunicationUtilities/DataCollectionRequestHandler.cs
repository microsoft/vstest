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
    using System.Threading;

    /// <summary>
    /// Handles test session events received from vstest console process.
    /// </summary>
    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private readonly ICommunicationManager communicationManager;
        private IMessageSink messageSink;
        private IDataCollectionManager dataCollectionManager;
        private IDataCollectionTestCaseEventHandler dataCollectionTestCaseEventHandler;
        private Task testCaseEventMonitorTask;

        /// <summary>
        /// Use to cancel data collection test case evets monitoring if test run is cancelled.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        private static readonly object obj = new object();

        internal static DataCollectionRequestHandler Instance;

        internal DataCollectionRequestHandler(IMessageSink messageSink)
            : this(new SocketCommunicationManager(), messageSink, new DataCollectionManager(messageSink), new DataCollectionTestCaseEventHandler())
        {
            this.messageSink = messageSink;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventHandler"/> class. 
        /// </summary>
        /// <param name="communicationManager">
        /// </param>
        internal DataCollectionRequestHandler(ICommunicationManager communicationManager, IMessageSink messageSink, IDataCollectionManager dataCollectionManager, IDataCollectionTestCaseEventHandler dataCollectionTestCaseEventHandler)
        {
            this.communicationManager = communicationManager;
            this.messageSink = messageSink;
            this.dataCollectionManager = dataCollectionManager;
            this.dataCollectionTestCaseEventHandler = dataCollectionTestCaseEventHandler;
            this.cancellationTokenSource = new CancellationTokenSource();
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
                        Instance = new DataCollectionRequestHandler(communicationManager, messageSink, new DataCollectionManager(messageSink), new DataCollectionTestCaseEventHandler());
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
                        var envVariables = this.dataCollectionManager.InitializeDataCollectors(settingXml);
                        var areTestCaseLevelEventsRequired = this.dataCollectionManager.SessionStarted();

                        // Open a socket communication port for test level events.
                        int testCaseEventsPort = -1;
                        if (areTestCaseLevelEventsRequired)
                        {
                            testCaseEventsPort = this.dataCollectionTestCaseEventHandler.InitializeCommunication();

                            this.testCaseEventMonitorTask = Task.Factory.StartNew(() =>
                             {
                                 // todo : decide the time out for this wait as it has to wait till test execution process get created.
                                 this.dataCollectionTestCaseEventHandler.WaitForRequestHandlerConnection(0);
                                 this.dataCollectionTestCaseEventHandler.ProcessRequests();
                             }, this.cancellationTokenSource.Token);
                        }

                        this.communicationManager.SendMessage(MessageType.BeforeTestRunStartResult, new BeforeTestRunStartResult(envVariables, areTestCaseLevelEventsRequired, testCaseEventsPort));

                        EqtTrace.Info("DataCollection started.");
                        break;

                    case MessageType.AfterTestRunEnd:
                        EqtTrace.Info("DataCollection completing.");
                        var isCancelled = message.Payload.ToObject<bool>();

                        if (isCancelled)
                        {
                            this.cancellationTokenSource.Cancel();
                        }

                        try
                        {
                            this.testCaseEventMonitorTask.Wait();
                        }
                        catch (Exception ex)
                        {
                            EqtTrace.Error("DataCollectionRequestHandler.ProcessRequests : {0}", ex.Message);
                        }

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