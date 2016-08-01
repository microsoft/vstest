// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The design mode client.
    /// </summary>
    public class DesignModeClient : IDesignModeClient
    {
        private readonly ICommunicationManager communicationManager;

        private readonly IDataSerializer dataSerializer;

        private Action<Message> onAckMessageReceived;

        private object ackLockObject = new object();

        /// <summary>
        /// The timeout for the client to connect to the server.
        /// </summary>
        private const int ClientListenTimeOut = 5 * 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignModeClient"/> class.
        /// </summary>
        public DesignModeClient()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignModeClient"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        internal DesignModeClient(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// Property exposing the Instance
        /// </summary>
        public static IDesignModeClient Instance { get; private set; }

        /// <summary>
        /// Initializes DesignMode
        /// </summary>
        public static void Initialize()
        {
            Instance = new DesignModeClient();
        }

        /// <summary>
        /// Creates a client and waits for server to accept connection asynchronously
        /// </summary>
        /// <param name="port">port number to connect</param>
        public void ConnectToClientAndProcessRequests(int port, ITestRequestManager testRequestManager)
        {
            EqtTrace.Info("Trying to connect to server on port : {0}", port);
            this.communicationManager.SetupClientAsync(port);
            this.communicationManager.SendMessage(MessageType.SessionConnected);

            // Wait for the connection to the server and listen for requests.
            if (this.communicationManager.WaitForServerConnection(ClientListenTimeOut))
            {
                this.ProcessRequests(testRequestManager);
            }
            else
            {
                EqtTrace.Info("Client timed out while connecting to the server.");
                this.Dispose();
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// End the connection
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopClient();
        }

        /// <summary>
        /// Process Requests from the IDE
        /// </summary>
        /// <param name="handler"></param>
        private void ProcessRequests(ITestRequestManager testRequestManager)
        {
            var isSessionEnd = false;
            // Todo : Add exception handling here. Do we want some way of notifying the caller if for some reason deserialization throws exception.
            do
            {
                var message = this.communicationManager.ReceiveMessage();
                switch (message.MessageType)
                {
                    case MessageType.VersionCheck:
                        {
                            // At this point, we cannot add stuff to object model like "ProtocolVersionMessage"
                            // as that cannot be acessed from testwindow which still uses TP-V1
                            // we are sending a version number as an integer for now
                            // TODO: Find a better way without breaking TW which using TP-V1
                            var payload = 1;
                            this.communicationManager.SendMessage(MessageType.VersionCheck, payload);
                            break;
                        }

                    case MessageType.ExtensionsInitialize:
                        {
                            var extensionPaths = this.communicationManager.DeserializePayload<IEnumerable<string>>(message);
                            testRequestManager.InitializeExtensions(extensionPaths);
                            break;
                        }

                    case MessageType.StartDiscovery:
                        {
                            var discoveryPayload = message.Payload.ToObject<DiscoveryRequestPayload>();
                            testRequestManager.DiscoverTests(discoveryPayload, new DesignModeTestEventsRegistrar(this));
                            break;
                        }

                    case MessageType.GetTestRunnerProcessStartInfoForRunAll:
                    case MessageType.GetTestRunnerProcessStartInfoForRunSelected:
                        {
                            var testRunPayload =
                                this.communicationManager.DeserializePayload<TestRunRequestPayload>(
                                    message);
                            this.StartTestRun(testRunPayload, testRequestManager, skipTestHostLaunch: true);
                            break;
                        }

                    case MessageType.TestRunAllSourcesWithDefaultHost:
                    case MessageType.TestRunSelectedTestCasesDefaultHost:
                        {
                            var testRunPayload =
                                this.communicationManager.DeserializePayload<TestRunRequestPayload>(
                                    message);
                            this.StartTestRun(testRunPayload, testRequestManager, skipTestHostLaunch: false);
                            break;
                        }

                    case MessageType.CancelTestRun:
                        {
                            testRequestManager.CancelTestRun();
                            break;
                        }

                    case MessageType.AbortTestRun:
                        {
                            testRequestManager.AbortTestRun();
                            break;
                        }
                    case MessageType.CustomTestHostLaunchCallback:
                        {
                            this.onAckMessageReceived?.Invoke(message);
                            break;
                        }
                    case MessageType.SessionEnd:
                        {
                            EqtTrace.Info("Session End message received from server. Closing the connection.");
                            isSessionEnd = true;
                            this.Dispose();
                            break;
                        }

                    default:
                        {
                            EqtTrace.Info("Invalid Message types");
                            break;
                        }
                }
            }
            while (!isSessionEnd);
        }

        /// <summary>
        /// Send a custom host launch message to IDE
        /// </summary>
        /// <param name="customTestHostLaunchPayload">Payload required to launch a custom host</param>
        public int LaunchCustomHost(TestProcessStartInfo testProcessStartInfo)
        {
            lock (ackLockObject)
            {
                var waitHandle = new AutoResetEvent(false);
                Message ackMessage = null;
                this.onAckMessageReceived = (ackRawMessage) =>
                {
                    ackMessage = ackRawMessage;
                    waitHandle.Set();
                };

                this.communicationManager.SendMessage(MessageType.CustomTestHostLaunch, testProcessStartInfo);
                waitHandle.WaitOne(ClientListenTimeOut);

                this.onAckMessageReceived = null;
                return this.dataSerializer.DeserializePayload<int>(ackMessage);
            }
        }

        /// <summary>
        /// Send the raw messages to IDE
        /// </summary>
        /// <param name="rawMessage"></param>
        public void SendRawMessage(string rawMessage)
        {
            this.communicationManager.SendRawMessage(rawMessage);
        }

        private void StartTestRun(TestRunRequestPayload testRunPayload, ITestRequestManager testRequestManager, bool skipTestHostLaunch)
        {
            Task.Run(
            delegate
            {
                testRequestManager.ResetOptions();

                var customLauncher = skipTestHostLaunch ?
                    DesignModeTestHostLauncherFactory.GetCustomHostLauncherForTestRun(this, testRunPayload) : null;

                testRequestManager.RunTests(testRunPayload, customLauncher, new DesignModeTestEventsRegistrar(this));
            });
        }
    }
}