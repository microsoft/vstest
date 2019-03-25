// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    /// <summary>
    /// Handles test session events received from vstest console process.
    /// </summary>
    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private static readonly object SyncObject = new object();

        private readonly ICommunicationManager communicationManager;

        private IMessageSink messageSink;

        private IDataCollectionManager dataCollectionManager;

        private IDataCollectionTestCaseEventHandler dataCollectionTestCaseEventHandler;

        private Task testCaseEventMonitorTask;

        private IDataSerializer dataSerializer;

        /// <summary>
        /// Use to cancel data collection test case events monitoring if test run is cancelled.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestHandler"/> class.
        /// </summary>
        /// <param name="messageSink">
        /// The message sink.
        /// </param>
        protected DataCollectionRequestHandler(IMessageSink messageSink)
            : this(
                new SocketCommunicationManager(),
                messageSink,
                DataCollectionManager.Create(messageSink),
                new DataCollectionTestCaseEventHandler(),
                JsonDataSerializer.Instance)
        {
            this.messageSink = messageSink;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestHandler"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        /// <param name="messageSink">
        /// The message sink.
        /// </param>
        /// <param name="dataCollectionManager">
        /// The data collection manager.
        /// </param>
        /// <param name="dataCollectionTestCaseEventHandler">
        /// The data collection test case event handler.
        /// </param>
        /// <param name="dataSerializer">
        /// Serializer for serialization and deserialization of the messages.
        /// </param>
        protected DataCollectionRequestHandler(
            ICommunicationManager communicationManager,
            IMessageSink messageSink,
            IDataCollectionManager dataCollectionManager,
            IDataCollectionTestCaseEventHandler dataCollectionTestCaseEventHandler,
            IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.messageSink = messageSink;
            this.dataCollectionManager = dataCollectionManager;
            this.dataSerializer = dataSerializer;
            this.dataCollectionTestCaseEventHandler = dataCollectionTestCaseEventHandler;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the singleton instance of DataCollectionRequestHandler.
        /// </summary>
        public static DataCollectionRequestHandler Instance { get; private set; }

        /// <summary>
        /// Creates singleton instance of DataCollectionRequestHandler.
        /// </summary>
        /// <param name="communicationManager">
        /// Handles socket communication.
        /// </param>
        /// <param name="messageSink">
        /// Message sink for sending messages to execution process.
        /// </param>
        /// <returns>
        /// The instance of <see cref="DataCollectionRequestHandler"/>.
        /// </returns>
        public static DataCollectionRequestHandler Create(
            ICommunicationManager communicationManager,
            IMessageSink messageSink)
        {
            ValidateArg.NotNull(communicationManager, nameof(communicationManager));
            ValidateArg.NotNull(messageSink, nameof(messageSink));
            if (Instance == null)
            {
                lock (SyncObject)
                {
                    if (Instance == null)
                    {
                        Instance = new DataCollectionRequestHandler(
                            communicationManager,
                            messageSink,
                            DataCollectionManager.Create(messageSink),
                            new DataCollectionTestCaseEventHandler(),
                            JsonDataSerializer.Instance);
                    }
                }
            }

            return Instance;
        }

        /// <inheritdoc />
        public void InitializeCommunication(int port)
        {
            this.communicationManager.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, port));
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
            var isSessionEnded = false;

            do
            {
                var message = this.communicationManager.ReceiveMessage();

                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("DataCollectionRequestHandler.ProcessRequests : Datacollector received message: {0}", message);
                }

                switch (message.MessageType)
                {
                    case MessageType.BeforeTestRunStart:
                        this.HandleBeforeTestRunStart(message);
                        break;

                    case MessageType.AfterTestRunEnd:
                        this.HandleAfterTestRunEnd(message);
                        isSessionEnded = true;
                        break;

                    case MessageType.TestHostLaunched:
                        var testHostLaunchedPayload = this.dataSerializer.DeserializePayload<TestHostLaunchedPayload>(message);
                        this.dataCollectionManager.TestHostLaunched(testHostLaunchedPayload.ProcessId);
                        break;

                    default:
                        EqtTrace.Error("DataCollectionRequestHandler.ProcessRequests : Invalid Message types: {0}", message.MessageType);
                        break;
                }
            }
            while (!isSessionEnded);
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
        /// Update the test adapter paths provided through run settings to be used by the test plugin cache.
        /// </summary>
        /// <param name="runSettings">
        /// The run Settings.
        /// </param>
        private void AddExtensionAssemblies(string runSettings)
        {
            try
            {
                IEnumerable<string> customTestAdaptersPaths = RunSettingsUtilities.GetTestAdaptersPaths(runSettings);
                if (customTestAdaptersPaths != null)
                {
                    var fileHelper = new FileHelper();

                    List<string> extensionAssemblies = new List<string>();
                    foreach (var customTestAdaptersPath in customTestAdaptersPaths)
                    {
                        var adapterPath =
                            Path.GetFullPath(Environment.ExpandEnvironmentVariables(customTestAdaptersPath));
                        if (!fileHelper.DirectoryExists(adapterPath))
                        {
                            EqtTrace.Warning(string.Format("AdapterPath Not Found:", adapterPath));
                            continue;
                        }

                        extensionAssemblies.AddRange(
                            fileHelper.EnumerateFiles(
                                adapterPath,
                                SearchOption.AllDirectories,
                                TestPlatformConstants.DataCollectorEndsWithPattern));
                    }

                    if (extensionAssemblies.Count > 0)
                    {
                        TestPluginCache.Instance.UpdateExtensions(extensionAssemblies, skipExtensionFilters: false);
                    }
                }
            }
            catch (Exception e)
            {
                // If any exception is throuwn while updating additional assemblies, log the exception in eqt trace.
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionRequestHandler.AddExtensionAssemblies: Exception occured: {0}", e);
                }
            }
        }

        private void HandleBeforeTestRunStart(Message message)
        {
            // Initialize datacollectors and get enviornment variables.
            var payload = this.dataSerializer.DeserializePayload<BeforeTestRunStartPayload>(message);
            this.AddExtensionAssemblies(payload.SettingsXml);

            var envVariables = this.dataCollectionManager.InitializeDataCollectors(payload.SettingsXml);

            var properties = new Dictionary<string, object>();
            properties.Add(CoreUtilitiesConstants.TestSourcesKeyName, payload.Sources);
            var eventArgs = new SessionStartEventArgs(properties);

            var areTestCaseLevelEventsRequired = this.dataCollectionManager.SessionStarted(eventArgs);

            // Open a socket communication port for test level events.
            var testCaseEventsPort = 0;
            if (areTestCaseLevelEventsRequired)
            {
                testCaseEventsPort = this.dataCollectionTestCaseEventHandler.InitializeCommunication();

                this.testCaseEventMonitorTask = Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            var timeout = EnvironmentHelper.GetConnectionTimeout();
                            if (this.dataCollectionTestCaseEventHandler.WaitForRequestHandlerConnection(
                                timeout * 1000))
                            {
                                this.dataCollectionTestCaseEventHandler.ProcessRequests();
                            }
                            else
                            {
                                EqtTrace.Error(
                                    "DataCollectionRequestHandler.HandleBeforeTestRunStart: TestCaseEventHandler timed out while connecting to the Sender.");
                                this.dataCollectionTestCaseEventHandler.Close();
                                throw new TestPlatformException(
                                    string.Format(
                                        CultureInfo.CurrentUICulture,
                                        CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                                        CoreUtilitiesConstants.DatacollectorProcessName,
                                        CoreUtilitiesConstants.TesthostProcessName,
                                        timeout,
                                        EnvironmentHelper.VstestConnectionTimeout));
                            }
                        }
                        catch (Exception e)
                        {
                            EqtTrace.Error("DataCollectionRequestHandler.HandleBeforeTestRunStart : Error occured during initialization of TestHost : {0}", e);
                        }
                    },
                    this.cancellationTokenSource.Token);
            }

            this.communicationManager.SendMessage(
                MessageType.BeforeTestRunStartResult,
                new BeforeTestRunStartResult(envVariables, testCaseEventsPort));

            EqtTrace.Info("DataCollectionRequestHandler.ProcessRequests : DataCollection started.");
        }

        private void HandleAfterTestRunEnd(Message message)
        {
            var isCancelled = this.dataSerializer.DeserializePayload<bool>(message);

            if (isCancelled)
            {
                this.cancellationTokenSource.Cancel();
            }

            try
            {
                this.testCaseEventMonitorTask?.Wait(this.cancellationTokenSource.Token);
                this.dataCollectionTestCaseEventHandler.Close();
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DataCollectionRequestHandler.HandleAfterTestRunEnd : Error while processing event from testhost: {0}", ex.ToString());
            }

            var attachmentsets = this.dataCollectionManager.SessionEnded(isCancelled);

            // Dispose all datacollectors before sending attachements to vstest.console process.
            // As datacollector process exits itself on parent process(vstest.console) exits.
            this.dataCollectionManager?.Dispose();

            this.communicationManager.SendMessage(MessageType.AfterTestRunEndResult, attachmentsets);
            EqtTrace.Info("DataCollectionRequestHandler.ProcessRequests : Session End message received from server. Closing the connection.");

            this.Close();

            EqtTrace.Info("DataCollectionRequestHandler.ProcessRequests : DataCollection completed");
        }
    }
}