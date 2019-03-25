// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Net;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

    /// <summary>
    /// Utility class that facilitates the IPC communication. Acts as server.
    /// </summary>
    public sealed class DataCollectionRequestSender : IDataCollectionRequestSender
    {
        private ICommunicationManager communicationManager;
        private IDataSerializer dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestSender"/> class.
        /// </summary>
        public DataCollectionRequestSender()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
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
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionRequestSender.InitializeCommunication : Initialize communication. ");
            }

            var endpoint = this.communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0));
            this.communicationManager.AcceptClientAsync();
            return endpoint.Port;
        }

        /// <summary>
        /// Waits for Request Handler to be connected
        /// </summary>
        /// <param name="clientConnectionTimeout">Time to wait for connection</param>
        /// <returns>True, if Handler is connected</returns>
        public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionRequestSender.WaitForRequestHandlerConnection : Waiting for connection with timeout: {0}", clientConnectionTimeout);
            }

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
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Closing the connection");
            }

            this.communicationManager?.StopServer();
        }

        /// <inheritdoc/>
        public void SendTestHostLaunched(TestHostLaunchedPayload testHostLaunchedPayload)
        {
            this.communicationManager.SendMessage(MessageType.TestHostLaunched, testHostLaunchedPayload);
        }

        /// <inheritdoc/>
        public BeforeTestRunStartResult SendBeforeTestRunStartAndGetResult(string settingsXml, IEnumerable<string> sources, ITestMessageEventHandler runEventsHandler)
        {
            var isDataCollectionStarted = false;
            BeforeTestRunStartResult result = null;

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionRequestSender.SendBeforeTestRunStartAndGetResult : Send BeforeTestRunStart message with settingsXml {0} and sources {1}: ", settingsXml, sources.ToString());
            }

            var payload = new BeforeTestRunStartPayload
            {
                SettingsXml = settingsXml,
                Sources = sources
            };

            this.communicationManager.SendMessage(MessageType.BeforeTestRunStart, payload);

            while (!isDataCollectionStarted)
            {
                var message = this.communicationManager.ReceiveMessage();

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionRequestSender.SendBeforeTestRunStartAndGetResult : Received message: {0}", message);
                }

                if (message.MessageType == MessageType.DataCollectionMessage)
                {
                    var dataCollectionMessageEventArgs = this.dataSerializer.DeserializePayload<DataCollectionMessageEventArgs>(message);
                    this.LogDataCollectorMessage(dataCollectionMessageEventArgs, runEventsHandler);
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

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionRequestSender.SendAfterTestRunStartAndGetResult : Send AfterTestRunEnd message with isCancelled: {0}", isCancelled);
            }

            this.communicationManager.SendMessage(MessageType.AfterTestRunEnd, isCancelled);

            // Cycle through the messages that the datacollector sends.
            // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
            while (!isDataCollectionComplete && !isCancelled)
            {
                var message = this.communicationManager.ReceiveMessage();

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionRequestSender.SendAfterTestRunStartAndGetResult : Received message: {0}", message);
                }

                if (message.MessageType == MessageType.DataCollectionMessage)
                {
                    var dataCollectionMessageEventArgs = this.dataSerializer.DeserializePayload<DataCollectionMessageEventArgs>(message);
                    this.LogDataCollectorMessage(dataCollectionMessageEventArgs, runEventsHandler);
                }
                else if (message.MessageType == MessageType.AfterTestRunEndResult)
                {
                    attachmentSets = this.dataSerializer.DeserializePayload<Collection<AttachmentSet>>(message);
                    isDataCollectionComplete = true;
                }
            }

            return attachmentSets;
        }

        private void LogDataCollectorMessage(DataCollectionMessageEventArgs dataCollectionMessageEventArgs, ITestMessageEventHandler requestHandler)
        {
            string logMessage;
            if (string.IsNullOrWhiteSpace(dataCollectionMessageEventArgs.FriendlyName))
            {
                // Message from data collection framework.
                logMessage = string.Format(CultureInfo.CurrentCulture, CommonResources.DataCollectionMessageFormat, dataCollectionMessageEventArgs.Message);
            }
            else
            {
                // Message from individual data collector.
                logMessage = string.Format(CultureInfo.CurrentCulture, CommonResources.DataCollectorMessageFormat, dataCollectionMessageEventArgs.FriendlyName, dataCollectionMessageEventArgs.Message);
            }

            requestHandler.HandleLogMessage(dataCollectionMessageEventArgs.Level, logMessage);
        }
    }
}