// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using CommonResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using ObjectModelConstants = Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants;

    /// <summary>
    /// Test request sender implementation.
    /// </summary>
    public class TestRequestSender : ITestRequestSender
    {
        // Time to wait for test host exit
        private const int ClientProcessExitWaitTimeout = 10 * 1000;

        private readonly IDataSerializer dataSerializer;

        private readonly ManualResetEventSlim connected;

        private readonly ManualResetEventSlim clientExited;

        private readonly int clientExitedWaitTime;

        private ICommunicationEndPoint communicationEndpoint;

        private ICommunicationChannel channel;

        private EventHandler<MessageReceivedEventArgs> onMessageReceived;

        private Action<DisconnectedEventArgs> onDisconnected;

        // Set to 1 if Discovery/Execution is complete, i.e. complete handlers have been invoked
        private int operationCompleted;

        private ITestMessageEventHandler messageEventHandler;

        private string clientExitErrorMessage;

        // Set default to 1, if protocol version check does not happen
        // that implies host is using version 1.
        private int protocolVersion = 1;

        // Must be in sync with the highest supported version in
        // src/Microsoft.TestPlatform.CrossPlatEngine/EventHandlers/TestRequestHandler.cs file.
        private int highestSupportedVersion = 5;

        private TestHostConnectionInfo connectionInfo;

        private ITestRuntimeProvider runtimeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// </summary>
        /// <param name="protocolConfig">Protocol configuration.</param>
        /// <param name="runtimeProvider">The runtime provider.</param>
        public TestRequestSender(ProtocolConfig protocolConfig, ITestRuntimeProvider runtimeProvider)
            : this(
                  runtimeProvider,
                  communicationEndPoint: null,
                  runtimeProvider.GetTestHostConnectionInfo(),
                  JsonDataSerializer.Instance,
                  protocolConfig,
                  ClientProcessExitWaitTimeout)
        {
            this.SetCommunicationEndPoint();
        }

        internal TestRequestSender(
            ITestRuntimeProvider runtimeProvider,
            ICommunicationEndPoint communicationEndPoint,
            TestHostConnectionInfo connectionInfo,
            IDataSerializer serializer,
            ProtocolConfig protocolConfig,
            int clientExitedWaitTime)
        {
            this.dataSerializer = serializer;
            this.connected = new ManualResetEventSlim(false);
            this.clientExited = new ManualResetEventSlim(false);
            this.clientExitedWaitTime = clientExitedWaitTime;
            this.operationCompleted = 0;

            this.highestSupportedVersion = protocolConfig.Version;

            // The connectionInfo here is that of RuntimeProvider, so reverse the role of runner.
            this.runtimeProvider = runtimeProvider;
            this.communicationEndpoint = communicationEndPoint;
            this.connectionInfo.Endpoint = connectionInfo.Endpoint;
            this.connectionInfo.Role = connectionInfo.Role == ConnectionRole.Host
                ? ConnectionRole.Client
                : ConnectionRole.Host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// Used only for testing to inject communication endpoint.
        /// </summary>
        /// <param name="communicationEndPoint">Communication server implementation.</param>
        /// <param name="connectionInfo">ConnectionInfo to set up transport layer</param>
        /// <param name="serializer">Serializer implementation.</param>
        /// <param name="protocolConfig">Protocol configuration.</param>
        /// <param name="clientExitedWaitTime">Time to wait for client process exit.</param>
        internal TestRequestSender(
            ICommunicationEndPoint communicationEndPoint,
            TestHostConnectionInfo connectionInfo,
            IDataSerializer serializer,
            ProtocolConfig protocolConfig,
            int clientExitedWaitTime)
            : this(
                  runtimeProvider: null,
                  communicationEndPoint,
                  connectionInfo,
                  serializer,
                  protocolConfig,
                  clientExitedWaitTime)
        {
        }

        public bool CloseConnectionOnOperationComplete { get; set; } = true;

        /// <inheritdoc />
        public int InitializeCommunication()
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.InitializeCommunication: initialize communication. ");
            }

            // this.clientExitCancellationSource = new CancellationTokenSource();
            this.clientExitErrorMessage = string.Empty;
            this.communicationEndpoint.Connected += (sender, args) =>
            {
                this.channel = args.Channel;
                if (args.Connected && this.channel != null)
                {
                    this.connected.Set();
                }
            };

            this.communicationEndpoint.Disconnected += (sender, args) =>
            {
                // If there's an disconnected event handler, call it
                this.onDisconnected?.Invoke(args);
            };

            // Server start returns the listener port
            // return int.Parse(this.communicationServer.Start());
            var endpoint = this.communicationEndpoint.Start(this.connectionInfo.Endpoint);
            return endpoint.GetIPEndPoint().Port;
        }

        /// <inheritdoc />
        public bool WaitForRequestHandlerConnection(int connectionTimeout, CancellationToken cancellationToken)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.WaitForRequestHandlerConnection: waiting for connection with timeout: {0}", connectionTimeout);
            }

            // Wait until either connection is successful, handled by connected.WaitHandle
            // or operation is canceled, handled by cancellationToken.WaitHandle
            // or testhost exits unexpectedly, handled by clientExited.WaitHandle
            var waitIndex = WaitHandle.WaitAny(new WaitHandle[] { this.connected.WaitHandle, cancellationToken.WaitHandle, this.clientExited.WaitHandle }, connectionTimeout);

            // Return true if connection was successful.
            return waitIndex == 0;
        }

        /// <inheritdoc />
        public void CheckVersionWithTestHost()
        {
            // Negotiation follows these steps:
            // Runner sends highest supported version to Test host
            // Test host sends the version it can support (must be less than highest) to runner
            // Error case: test host can send a protocol error if it cannot find a supported version
            var protocolNegotiated = new ManualResetEvent(false);
            this.onMessageReceived = (sender, args) =>
            {
                var message = this.dataSerializer.DeserializeMessage(args.Data);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.CheckVersionWithTestHost: onMessageReceived received message: {0}", message);
                }

                if (message.MessageType == MessageType.VersionCheck)
                {
                    this.protocolVersion = this.dataSerializer.DeserializePayload<int>(message);
                }

                // TRH can also send TestMessage if tracing is enabled, so log it at runner end
                else if (message.MessageType == MessageType.TestMessage)
                {
                    // Ignore test messages. Currently we don't have handler(which sends messages to client/console.) here.
                    // Above we are logging it to EqtTrace.
                }
                else if (message.MessageType == MessageType.ProtocolError)
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CommonResources.VersionCheckFailed));
                }
                else
                {
                    throw new TestPlatformException(string.Format(
                        CultureInfo.CurrentUICulture,
                        CommonResources.UnexpectedMessage,
                        MessageType.VersionCheck,
                        message.MessageType));
                }

                protocolNegotiated.Set();
            };
            this.channel.MessageReceived += this.onMessageReceived;

            try
            {
                // Send the protocol negotiation request. Note that we always serialize this data
                // without any versioning in the message itself.
                var data = this.dataSerializer.SerializePayload(MessageType.VersionCheck, this.highestSupportedVersion);
                this.channel.Send(data);

                // Wait for negotiation response
                var timeout = EnvironmentHelper.GetConnectionTimeout();
                if (!protocolNegotiated.WaitOne(timeout * 1000))
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CommonResources.VersionCheckTimedout, timeout, EnvironmentHelper.VstestConnectionTimeout));
                }
            }
            finally
            {
                this.channel.MessageReceived -= this.onMessageReceived;
                this.onMessageReceived = null;
            }
        }

        #region Discovery Protocol

        /// <inheritdoc />
        public void InitializeDiscovery(IEnumerable<string> pathToAdditionalExtensions)
        {
            var message = this.dataSerializer.SerializePayload(
                MessageType.DiscoveryInitialize,
                pathToAdditionalExtensions,
                this.protocolVersion);
            this.channel.Send(message);
        }

        /// <inheritdoc/>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.messageEventHandler = discoveryEventsHandler;
            this.onDisconnected = (disconnectedEventArgs) =>
                {
                    this.OnDiscoveryAbort(discoveryEventsHandler, disconnectedEventArgs.Error, true);
                };
            this.onMessageReceived = (sender, args) => this.OnDiscoveryMessageReceived(discoveryEventsHandler, args);

            this.channel.MessageReceived += this.onMessageReceived;
            var message = this.dataSerializer.SerializePayload(
                MessageType.StartDiscovery,
                discoveryCriteria,
                this.protocolVersion);
            this.channel.Send(message);
        }
        #endregion

        #region Execution Protocol

        /// <inheritdoc />
        public void InitializeExecution(IEnumerable<string> pathToAdditionalExtensions)
        {
            var message = this.dataSerializer.SerializePayload(
                MessageType.ExecutionInitialize,
                pathToAdditionalExtensions,
                this.protocolVersion);
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.InitializeExecution: Sending initializing execution with message: {0}", message);
            }

            this.channel.Send(message);
        }

        /// <inheritdoc />
        public void StartTestRun(TestRunCriteriaWithSources runCriteria, ITestRunEventsHandler eventHandler)
        {
            this.messageEventHandler = eventHandler;
            this.onDisconnected = (disconnectedEventArgs) =>
            {
                this.OnTestRunAbort(eventHandler, disconnectedEventArgs.Error, true);
            };

            this.onMessageReceived = (sender, args) => this.OnExecutionMessageReceived(sender, args, eventHandler);
            this.channel.MessageReceived += this.onMessageReceived;

            // This code section is needed because we altered the old testhost launch process for
            // the debugging workflow. Now we don't ask VS to launch and attach to the testhost
            // process for us as we previously did, instead we launch it as a standalone process
            // and rely on the testhost to ask VS to attach the debugger to itself.
            //
            // In order to avoid breaking compatibility with previous testhost versions because of
            // those changes (older testhosts won't know to request VS to attach to themselves
            // thinking instead VS launched and attached to them already), we request VS to attach
            // to the testhost here before starting the test run.
            if (runCriteria.TestExecutionContext != null
                && runCriteria.TestExecutionContext.IsDebug
                && this.runtimeProvider is ITestRuntimeProvider2 convertedRuntimeProvider
                && this.protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
            {
                var handler = (ITestRunEventsHandler2)eventHandler;
                if (!convertedRuntimeProvider.AttachDebuggerToTestHost())
                {
                    EqtTrace.Warning(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CommonResources.AttachDebuggerToDefaultTestHostFailure));
                }
            }

            var message = this.dataSerializer.SerializePayload(
                MessageType.StartTestExecutionWithSources,
                runCriteria,
                this.protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.StartTestRun: Sending test run with message: {0}", message);
            }

            this.channel.Send(message);
        }

        /// <inheritdoc />
        public void StartTestRun(TestRunCriteriaWithTests runCriteria, ITestRunEventsHandler eventHandler)
        {
            this.messageEventHandler = eventHandler;
            this.onDisconnected = (disconnectedEventArgs) =>
            {
                this.OnTestRunAbort(eventHandler, disconnectedEventArgs.Error, true);
            };

            this.onMessageReceived = (sender, args) => this.OnExecutionMessageReceived(sender, args, eventHandler);
            this.channel.MessageReceived += this.onMessageReceived;

            // This code section is needed because we altered the old testhost launch process for
            // the debugging workflow. Now we don't ask VS to launch and attach to the testhost
            // process for us as we previously did, instead we launch it as a standalone process
            // and rely on the testhost to ask VS to attach the debugger to itself.
            //
            // In order to avoid breaking compatibility with previous testhost versions because of
            // those changes (older testhosts won't know to request VS to attach to themselves
            // thinking instead VS launched and attached to them already), we request VS to attach
            // to the testhost here before starting the test run.
            if (runCriteria.TestExecutionContext != null
                && runCriteria.TestExecutionContext.IsDebug
                && this.runtimeProvider is ITestRuntimeProvider2 convertedRuntimeProvider
                && this.protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
            {
                var handler = (ITestRunEventsHandler2)eventHandler;
                if (!convertedRuntimeProvider.AttachDebuggerToTestHost())
                {
                    EqtTrace.Warning(
                        string.Format(
                            CultureInfo.CurrentUICulture,
                            CommonResources.AttachDebuggerToDefaultTestHostFailure));
                }
            }

            var message = this.dataSerializer.SerializePayload(
                MessageType.StartTestExecutionWithTests,
                runCriteria,
                this.protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.StartTestRun: Sending test run with message: {0}", message);
            }

            this.channel.Send(message);
        }

        /// <inheritdoc />
        public void SendTestRunCancel()
        {
            if (this.IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: SendTestRunCancel: Operation is already complete. Skip error message.");
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SendTestRunCancel: Sending test run cancel.");
            }

            this.channel?.Send(this.dataSerializer.SerializeMessage(MessageType.CancelTestRun));
        }

        /// <inheritdoc />
        public void SendTestRunAbort()
        {
            if (this.IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: SendTestRunAbort: Operation is already complete. Skip error message.");
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SendTestRunAbort: Sending test run abort.");
            }

            this.channel?.Send(this.dataSerializer.SerializeMessage(MessageType.AbortTestRun));
        }

        #endregion

        /// <inheritdoc />
        public void EndSession()
        {
            if (!this.IsOperationComplete())
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.EndSession: Sending end session.");
                }

                this.channel?.Send(this.dataSerializer.SerializeMessage(MessageType.SessionEnd));
            }
        }

        /// <inheritdoc />
        public void OnClientProcessExit(string stdError)
        {
            // This method is called on test host exit. If test host has any errors, stdError
            // provides the crash call stack.
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"TestRequestSender.OnClientProcessExit: Test host process exited. Standard error: {stdError}");
            }

            this.clientExitErrorMessage = stdError;
            this.clientExited.Set();

            // Break communication loop. In some cases (E.g: When tests creates child processes to testhost) communication channel won't break if testhost exits.
            this.communicationEndpoint.Stop();
        }

        /// <inheritdoc />
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.channel != null)
            {
                this.channel.MessageReceived -= this.onMessageReceived;
            }

            this.communicationEndpoint.Stop();
        }

        private void OnExecutionMessageReceived(object sender, MessageReceivedEventArgs messageReceived, ITestRunEventsHandler testRunEventsHandler)
        {
            try
            {
                var rawMessage = messageReceived.Data;
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.OnExecutionMessageReceived: Received message: {0}", rawMessage);
                }

                // Send raw message first to unblock handlers waiting to send message to IDEs
                testRunEventsHandler.HandleRawMessage(rawMessage);

                var message = this.dataSerializer.DeserializeMessage(rawMessage);
                switch (message.MessageType)
                {
                    case MessageType.TestRunStatsChange:
                        var testRunChangedArgs = this.dataSerializer.DeserializePayload<TestRunChangedEventArgs>(message);
                        testRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
                        break;
                    case MessageType.ExecutionComplete:
                        var testRunCompletePayload = this.dataSerializer.DeserializePayload<TestRunCompletePayload>(message);

                        testRunEventsHandler.HandleTestRunComplete(
                            testRunCompletePayload.TestRunCompleteArgs,
                            testRunCompletePayload.LastRunTests,
                            testRunCompletePayload.RunAttachments,
                            testRunCompletePayload.ExecutorUris);

                        this.SetOperationComplete();
                        break;
                    case MessageType.TestMessage:
                        var testMessagePayload = this.dataSerializer.DeserializePayload<TestMessagePayload>(message);
                        testRunEventsHandler.HandleLogMessage(testMessagePayload.MessageLevel, testMessagePayload.Message);
                        break;
                    case MessageType.LaunchAdapterProcessWithDebuggerAttached:
                        var testProcessStartInfo = this.dataSerializer.DeserializePayload<TestProcessStartInfo>(message);
                        int processId = testRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

                        var data =
                            this.dataSerializer.SerializePayload(
                                MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback,
                                processId,
                                this.protocolVersion);
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("TestRequestSender.OnExecutionMessageReceived: Sending LaunchAdapterProcessWithDebuggerAttachedCallback message: {0}", data);
                        }

                        this.channel.Send(data);
                        break;

                    case MessageType.AttachDebugger:
                        var testProcessPid = this.dataSerializer.DeserializePayload<TestProcessAttachDebuggerPayload>(message);
                        bool result = ((ITestRunEventsHandler2)testRunEventsHandler).AttachDebuggerToProcess(testProcessPid.ProcessID);

                        var resultMessage = this.dataSerializer.SerializePayload(
                            MessageType.AttachDebuggerCallback,
                            result,
                            this.protocolVersion);

                        this.channel.Send(resultMessage);

                        break;
                }
            }
            catch (Exception exception)
            {
                this.OnTestRunAbort(testRunEventsHandler, exception, false);
            }
        }

        private void OnDiscoveryMessageReceived(ITestDiscoveryEventsHandler2 discoveryEventsHandler, MessageReceivedEventArgs args)
        {
            try
            {
                var rawMessage = args.Data;

                // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.OnDiscoveryMessageReceived: Received message: {0}", rawMessage);
                }

                // Send raw message first to unblock handlers waiting to send message to IDEs
                discoveryEventsHandler.HandleRawMessage(rawMessage);

                var data = this.dataSerializer.DeserializeMessage(rawMessage);
                switch (data.MessageType)
                {
                    case MessageType.TestCasesFound:
                        var testCases = this.dataSerializer.DeserializePayload<IEnumerable<TestCase>>(data);
                        discoveryEventsHandler.HandleDiscoveredTests(testCases);
                        break;
                    case MessageType.DiscoveryComplete:
                        var discoveryCompletePayload =
                            this.dataSerializer.DeserializePayload<DiscoveryCompletePayload>(data);
                        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(discoveryCompletePayload.TotalTests, discoveryCompletePayload.IsAborted);
                        discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;
                        discoveryEventsHandler.HandleDiscoveryComplete(
                            discoveryCompleteEventArgs,
                            discoveryCompletePayload.LastDiscoveredTests);
                        this.SetOperationComplete();
                        break;
                    case MessageType.TestMessage:
                        var testMessagePayload = this.dataSerializer.DeserializePayload<TestMessagePayload>(
                            data);
                        discoveryEventsHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.OnDiscoveryAbort(discoveryEventsHandler, ex, false);
            }
        }

        private void OnTestRunAbort(ITestRunEventsHandler testRunEventsHandler, Exception exception, bool getClientError)
        {
            if (this.IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: OnTestRunAbort: Operation is already complete. Skip error message.");
                return;
            }

            EqtTrace.Verbose("TestRequestSender: OnTestRunAbort: Set operation complete.");
            this.SetOperationComplete();

            var reason = this.GetAbortErrorMessage(exception, getClientError);
            EqtTrace.Error("TestRequestSender: Aborting test run because {0}", reason);
            this.LogErrorMessage(string.Format(CommonResources.AbortedTestRun, reason));

            // notify test run abort to vstest console wrapper.
            var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, null, TimeSpan.Zero);
            var payload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.ExecutionComplete, payload);
            testRunEventsHandler.HandleRawMessage(rawMessage);

            // notify of a test run complete and bail out.
            testRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);
        }

        private void OnDiscoveryAbort(ITestDiscoveryEventsHandler2 eventHandler, Exception exception, bool getClientError)
        {
            if (this.IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: OnDiscoveryAbort: Operation is already complete. Skip error message.");
                return;
            }

            EqtTrace.Verbose("TestRequestSender: OnDiscoveryAbort: Set operation complete.");
            this.SetOperationComplete();

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
            var reason = this.GetAbortErrorMessage(exception, getClientError);
            EqtTrace.Error("TestRequestSender: Aborting test discovery because {0}", reason);
            this.LogErrorMessage(string.Format(CommonResources.AbortedTestDiscovery, reason));

            // Notify discovery abort to IDE test output
            var payload = new DiscoveryCompletePayload()
            {
                IsAborted = true,
                LastDiscoveredTests = null,
                TotalTests = -1
            };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.DiscoveryComplete, payload);
            eventHandler.HandleRawMessage(rawMessage);

            // Complete discovery
            eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);
        }

        private string GetAbortErrorMessage(Exception exception, bool getClientError)
        {
            EqtTrace.Verbose("TestRequestSender: GetAbortErrorMessage: Exception: " + exception);

            // It is also possible for an operation to abort even if client has not
            // disconnected, e.g. if there's an error parsing the response from test host. We
            // want the exception to be available in those scenarios.
            var reason = exception?.Message;
            if (getClientError)
            {
                EqtTrace.Verbose("TestRequestSender: GetAbortErrorMessage: Client has disconnected. Wait for standard error.");

                // Wait for test host to exit for a moment
                if (this.clientExited.Wait(this.clientExitedWaitTime))
                {
                    // Set a default message of test host process exited and additionally specify the error if present
                    EqtTrace.Info("TestRequestSender: GetAbortErrorMessage: Received test host error message.");
                    reason = CommonResources.TestHostProcessCrashed;
                    if (!string.IsNullOrWhiteSpace(this.clientExitErrorMessage))
                    {
                        reason = $"{reason} : {this.clientExitErrorMessage}";
                    }
                }
                else
                {
                    reason = CommonResources.UnableToCommunicateToTestHost;
                    EqtTrace.Info("TestRequestSender: GetAbortErrorMessage: Timed out waiting for test host error message.");
                }
            }

            return reason;
        }

        private void LogErrorMessage(string message)
        {
            if (this.messageEventHandler == null)
            {
                EqtTrace.Error("TestRequestSender.LogErrorMessage: Message event handler not set. Error: " + message);
                return;
            }

            // Log to vstest console
            this.messageEventHandler.HandleLogMessage(TestMessageLevel.Error, message);

            // Log to vs ide test output
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = message };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            this.messageEventHandler.HandleRawMessage(rawMessage);
        }

        private bool IsOperationComplete()
        {
            return this.operationCompleted == 1;
        }

        private void SetOperationComplete()
        {
            // When sharing the testhost between discovery and execution we must keep the
            // testhost alive after completing the operation it was spawned for. As such we
            // suppress the test request sender channel close taking place here. This channel
            // will be closed when the test session owner decides to dispose of the test session
            // object.
            if (!this.CloseConnectionOnOperationComplete)
            {
                return;
            }

            // Complete the currently ongoing operation (Discovery/Execution)
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SetOperationComplete: Setting operation complete.");
            }

            this.communicationEndpoint.Stop();
            Interlocked.CompareExchange(ref this.operationCompleted, 1, 0);
        }

        private void SetCommunicationEndPoint()
        {
            // TODO: Use factory to get the communication endpoint. It will abstract out the type of communication endpoint like socket, shared memory or named pipe etc.,
            if (this.connectionInfo.Role == ConnectionRole.Client)
            {
                this.communicationEndpoint = new SocketClient();
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender is acting as client");
                }
            }
            else
            {
                this.communicationEndpoint = new SocketServer();
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender is acting as server");
                }
            }
        }
    }
}
