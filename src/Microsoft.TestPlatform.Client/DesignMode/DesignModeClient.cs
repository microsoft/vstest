// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.Client.TestRunAttachmentsProcessing;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;
    using ObjectModelConstants = Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants;

    /// <summary>
    /// The design mode client.
    /// </summary>
    public class DesignModeClient : IDesignModeClient
    {
        private readonly ICommunicationManager communicationManager;
        private readonly IDataSerializer dataSerializer;

        private ProtocolConfig protocolConfig = Constants.DefaultProtocolConfig;
        private IEnvironment platformEnvironment;
        private TestSessionMessageLogger testSessionMessageLogger;
        private object lockObject = new object();

        protected Action<Message> onCustomTestHostLaunchAckReceived;
        protected Action<Message> onAttachDebuggerAckRecieved;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignModeClient"/> class.
        /// </summary>
        public DesignModeClient()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance, new PlatformEnvironment())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignModeClient"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        /// <param name="dataSerializer">
        /// The data Serializer.
        /// </param>
        /// <param name="platformEnvironment">
        /// The platform Environment
        /// </param>
        internal DesignModeClient(ICommunicationManager communicationManager, IDataSerializer dataSerializer, IEnvironment platformEnvironment)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
            this.platformEnvironment = platformEnvironment;
            this.testSessionMessageLogger = TestSessionMessageLogger.Instance;
            this.testSessionMessageLogger.TestRunMessage += this.TestRunMessageHandler;
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
        /// <param name="port">
        /// Port number to connect
        /// </param>
        /// <param name="testRequestManager">
        /// The test Request Manager.
        /// </param>
        public void ConnectToClientAndProcessRequests(int port, ITestRequestManager testRequestManager)
        {
            EqtTrace.Info("Trying to connect to server on port : {0}", port);
            this.communicationManager.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, port));

            var connectionTimeoutInSecs = EnvironmentHelper.GetConnectionTimeout();

            // Wait for the connection to the server and listen for requests.
            if (this.communicationManager.WaitForServerConnection(connectionTimeoutInSecs * 1000))
            {
                this.communicationManager.SendMessage(MessageType.SessionConnected);
                this.ProcessRequests(testRequestManager);
            }
            else
            {
                EqtTrace.Error("DesignModeClient : ConnectToClientAndProcessRequests : Client timed out while connecting to the server.");
                this.Dispose();
                throw new TimeoutException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                        CoreUtilitiesConstants.VstestConsoleProcessName,
                        "translation layer",
                        connectionTimeoutInSecs,
                        EnvironmentHelper.VstestConnectionTimeout)
                    );
            }
        }

        public void HandleParentProcessExit()
        {
            // Dispose off the communications to end the session
            // this should end the "ProcessRequests" loop with an exception
            this.Dispose();

            EqtTrace.Info("DesignModeClient: Parent process exited, Exiting myself..");

            this.platformEnvironment.Exit(1);
        }

        /// <summary>
        /// Process Requests from the IDE
        /// </summary>
        /// <param name="testRequestManager">
        /// The test Request Manager.
        /// </param>
        private void ProcessRequests(ITestRequestManager testRequestManager)
        {
            var isSessionEnd = false;

            do
            {
                try
                {
                    var message = this.communicationManager.ReceiveMessage();

                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("DesignModeClient.ProcessRequests: Processing Message: {0}", message);
                    }

                    switch (message.MessageType)
                    {
                        case MessageType.VersionCheck:
                            {
                                var version = this.dataSerializer.DeserializePayload<int>(message);
                                this.protocolConfig.Version = Math.Min(version, this.protocolConfig.Version);
                                this.communicationManager.SendMessage(MessageType.VersionCheck, this.protocolConfig.Version);
                                break;
                            }

                        case MessageType.ExtensionsInitialize:
                            {
                                // Do not filter the Editor/IDE provided extensions by name
                                var extensionPaths = this.communicationManager.DeserializePayload<IEnumerable<string>>(message);
                                testRequestManager.InitializeExtensions(extensionPaths, skipExtensionFilters: true);
                                break;
                            }

                        case MessageType.StartDiscovery:
                            {
                                var discoveryPayload = this.dataSerializer.DeserializePayload<DiscoveryRequestPayload>(message);
                                this.StartDiscovery(discoveryPayload, testRequestManager);
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

                        case MessageType.TestRunAttachmentsProcessingStart:
                            {
                                var testRunAttachmentsProcessingPayload =
                                    this.communicationManager.DeserializePayload<TestRunAttachmentsProcessingPayload>(message);
                                this.StartTestRunAttachmentsProcessing(testRunAttachmentsProcessingPayload, testRequestManager);
                                break;
                            }

                        case MessageType.CancelDiscovery:
                            {
                                testRequestManager.CancelDiscovery();
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

                        case MessageType.TestRunAttachmentsProcessingCancel:
                            {
                                testRequestManager.CancelTestRunAttachmentsProcessing();
                                break;
                            }

                        case MessageType.CustomTestHostLaunchCallback:
                            {
                                this.onCustomTestHostLaunchAckReceived?.Invoke(message);
                                break;
                            }

                        case MessageType.EditorAttachDebuggerCallback:
                            {
                                this.onAttachDebuggerAckRecieved?.Invoke(message);
                                break;
                            }

                        case MessageType.SessionEnd:
                            {
                                EqtTrace.Info("DesignModeClient: Session End message received from server. Closing the connection.");
                                isSessionEnd = true;
                                this.Dispose();
                                break;
                            }

                        default:
                            {
                                EqtTrace.Info("DesignModeClient: Invalid Message received: {0}", message);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("DesignModeClient: Error processing request: {0}", ex);
                    isSessionEnd = true;
                    this.Dispose();
                }
            }
            while (!isSessionEnd);
        }

        /// <summary>
        /// Send a custom host launch message to IDE
        /// </summary>
        /// <param name="testProcessStartInfo">
        /// The test Process Start Info.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int LaunchCustomHost(TestProcessStartInfo testProcessStartInfo, CancellationToken cancellationToken)
        {
            lock (this.lockObject)
            {
                var waitHandle = new AutoResetEvent(false);
                Message ackMessage = null;
                this.onCustomTestHostLaunchAckReceived = (ackRawMessage) =>
                {
                    ackMessage = ackRawMessage;
                    waitHandle.Set();
                };

                this.communicationManager.SendMessage(MessageType.CustomTestHostLaunch, testProcessStartInfo);

                // LifeCycle of the TP through DesignModeClient is maintained by the IDEs or user-facing-clients like LUTs, who call TestPlatform
                // TP is handing over the control of launch to these IDEs and so, TP has to wait indefinite
                // Even if TP has a timeout here, there is no way TP can abort or stop the thread/task that is hung in IDE or LUT
                // Even if TP can abort the API somehow, TP is essentially putting IDEs or Clients in inconsistent state without having info on
                // Since the IDEs own user-UI-experience here, TP will let the custom host launch as much time as IDEs define it for their users
                WaitHandle.WaitAny(new WaitHandle[] { waitHandle, cancellationToken.WaitHandle });

                cancellationToken.ThrowTestPlatformExceptionIfCancellationRequested();

                this.onCustomTestHostLaunchAckReceived = null;

                var ackPayload = this.dataSerializer.DeserializePayload<CustomHostLaunchAckPayload>(ackMessage);

                if (ackPayload.HostProcessId > 0)
                {
                    return ackPayload.HostProcessId;
                }
                else
                {
                    throw new TestPlatformException(ackPayload.ErrorMessage);
                }
            }
        }

        /// <inheritdoc/>
        public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
        {
            // If an attach request is issued but there is no support for attaching on the other
            // side of the communication channel, we simply return and let the caller know the
            // request failed.
            if (this.protocolConfig.Version < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
            {
                return false;
            }

            lock (this.lockObject)
            {
                var waitHandle = new AutoResetEvent(false);
                Message ackMessage = null;
                this.onAttachDebuggerAckRecieved = (ackRawMessage) =>
                {
                    ackMessage = ackRawMessage;
                    waitHandle.Set();
                };

                this.communicationManager.SendMessage(MessageType.EditorAttachDebugger, pid);

                WaitHandle.WaitAny(new WaitHandle[] { waitHandle, cancellationToken.WaitHandle });

                cancellationToken.ThrowTestPlatformExceptionIfCancellationRequested();
                this.onAttachDebuggerAckRecieved = null;

                var ackPayload = this.dataSerializer.DeserializePayload<EditorAttachDebuggerAckPayload>(ackMessage);
                if (!ackPayload.Attached)
                {
                    EqtTrace.Warning(ackPayload.ErrorMessage);
                }

                return ackPayload.Attached;
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

        /// <inheritdoc />
        public void SendTestMessage(TestMessageLevel level, string message)
        {
            var payload = new TestMessagePayload { MessageLevel = level, Message = message };
            this.communicationManager.SendMessage(MessageType.TestMessage, payload);
        }

        /// <summary>
        /// Sends the test session logger warning and error messages to IDE; 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TestRunMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            // save into trace log and send the message to the IDE
            //
            // there is a mismatch between log levels that VS uses and that TP
            // uses. In VS you can choose Trace level which will enable Test platform
            // logs on Verbose level. Below we report Errors and warnings always to the 
            // IDE no matter what the level of VS logging is, but Info only when the Eqt trace 
            // info level is enabled (so only when VS enables Trace logging)
            switch (e.Level)
            {
                case TestMessageLevel.Error:
                    EqtTrace.Error(e.Message);
                    SendTestMessage(e.Level, e.Message);
                    break;
                case TestMessageLevel.Warning:
                    EqtTrace.Warning(e.Message);
                    SendTestMessage(e.Level, e.Message);
                    break;

                case TestMessageLevel.Informational:
                    EqtTrace.Info(e.Message);

                    if (EqtTrace.IsInfoEnabled)
                        SendTestMessage(e.Level, e.Message);
                    break;

               
                default:
                    throw new NotSupportedException($"Test message level '{e.Level}' is not supported.");
            }
        }

        private void StartTestRun(TestRunRequestPayload testRunPayload, ITestRequestManager testRequestManager, bool skipTestHostLaunch)
        {
            Task.Run(
            delegate
            {
                try
                {
                    testRequestManager.ResetOptions();

                    var customLauncher = skipTestHostLaunch ?
                        DesignModeTestHostLauncherFactory.GetCustomHostLauncherForTestRun(this, testRunPayload) : null;

                    testRequestManager.RunTests(testRunPayload, customLauncher, new DesignModeTestEventsRegistrar(this), this.protocolConfig);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("DesignModeClient: Exception in StartTestRun: " + ex);

                    var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = ex.ToString() };
                    this.communicationManager.SendMessage(MessageType.TestMessage, testMessagePayload);
                    var runCompletePayload = new TestRunCompletePayload()
                    {
                        TestRunCompleteArgs = new TestRunCompleteEventArgs(null, false, true, ex, null, TimeSpan.MinValue),
                        LastRunTests = null
                    };

                    // Send run complete to translation layer
                    this.communicationManager.SendMessage(MessageType.ExecutionComplete, runCompletePayload);
                }
            });
        }

        private void StartDiscovery(DiscoveryRequestPayload discoveryRequestPayload, ITestRequestManager testRequestManager)
        {
            Task.Run(
                delegate
                {
                    try
                    {
                        testRequestManager.ResetOptions();
                        testRequestManager.DiscoverTests(discoveryRequestPayload, new DesignModeTestEventsRegistrar(this), this.protocolConfig);
                    }
                    catch (Exception ex)
                    {
                        EqtTrace.Error("DesignModeClient: Exception in StartDiscovery: " + ex);

                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = ex.ToString() };
                        this.communicationManager.SendMessage(MessageType.TestMessage, testMessagePayload);

                        var payload = new DiscoveryCompletePayload()
                        {
                            IsAborted = true,
                            LastDiscoveredTests = null,
                            TotalTests = -1
                        };

                        // Send run complete to translation layer
                        this.communicationManager.SendMessage(MessageType.DiscoveryComplete, payload);
                    }
                });
        }

        private void StartTestRunAttachmentsProcessing(TestRunAttachmentsProcessingPayload attachmentsProcessingPayload, ITestRequestManager testRequestManager)
        {
            Task.Run(
                delegate
                {
                    try
                    {
                        testRequestManager.ProcessTestRunAttachments(attachmentsProcessingPayload, new TestRunAttachmentsProcessingEventsHandler(this.communicationManager), this.protocolConfig);
                    }
                    catch (Exception ex)
                    {
                        EqtTrace.Error("DesignModeClient: Exception in StartTestRunAttachmentsProcessing: " + ex);

                        var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = ex.ToString() };
                        this.communicationManager.SendMessage(MessageType.TestMessage, testMessagePayload);

                        var payload = new TestRunAttachmentsProcessingCompletePayload()
                        {
                            Attachments = null
                        };

                        // Send run complete to translation layer
                        this.communicationManager.SendMessage(MessageType.TestRunAttachmentsProcessingComplete, payload);
                    }
                });
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.communicationManager?.StopClient();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
