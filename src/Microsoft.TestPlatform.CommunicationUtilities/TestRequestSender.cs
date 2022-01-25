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
    using CommonResources = Resources.Resources;
    using ObjectModelConstants = TestPlatform.ObjectModel.Constants;

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
        private readonly int highestSupportedVersion = 5;

        private TestHostConnectionInfo connectionInfo;

        private readonly ITestRuntimeProvider runtimeProvider;

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
            SetCommunicationEndPoint();
        }

        internal TestRequestSender(
            ITestRuntimeProvider runtimeProvider,
            ICommunicationEndPoint communicationEndPoint,
            TestHostConnectionInfo connectionInfo,
            IDataSerializer serializer,
            ProtocolConfig protocolConfig,
            int clientExitedWaitTime)
        {
            dataSerializer = serializer;
            connected = new ManualResetEventSlim(false);
            clientExited = new ManualResetEventSlim(false);
            this.clientExitedWaitTime = clientExitedWaitTime;
            operationCompleted = 0;

            highestSupportedVersion = protocolConfig.Version;

            // The connectionInfo here is that of RuntimeProvider, so reverse the role of runner.
            this.runtimeProvider = runtimeProvider;
            communicationEndpoint = communicationEndPoint;
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
            clientExitErrorMessage = string.Empty;
            communicationEndpoint.Connected += (sender, args) =>
            {
                channel = args.Channel;
                if (args.Connected && channel != null)
                {
                    connected.Set();
                }
            };

            communicationEndpoint.Disconnected += (sender, args) =>
                // If there's an disconnected event handler, call it
                onDisconnected?.Invoke(args);

            // Server start returns the listener port
            // return int.Parse(this.communicationServer.Start());
            var endpoint = communicationEndpoint.Start(connectionInfo.Endpoint);
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
            var waitIndex = WaitHandle.WaitAny(new WaitHandle[] { connected.WaitHandle, cancellationToken.WaitHandle, clientExited.WaitHandle }, connectionTimeout);

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
            onMessageReceived = (sender, args) =>
            {
                var message = dataSerializer.DeserializeMessage(args.Data);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.CheckVersionWithTestHost: onMessageReceived received message: {0}", message);
                }

                if (message.MessageType == MessageType.VersionCheck)
                {
                    protocolVersion = dataSerializer.DeserializePayload<int>(message);
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
            channel.MessageReceived += onMessageReceived;

            try
            {
                // Send the protocol negotiation request. Note that we always serialize this data
                // without any versioning in the message itself.
                var data = dataSerializer.SerializePayload(MessageType.VersionCheck, highestSupportedVersion);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.CheckVersionWithTestHost: Sending check version message: {0}", data);
                }

                channel.Send(data);

                // Wait for negotiation response
                var timeout = EnvironmentHelper.GetConnectionTimeout();
                if (!protocolNegotiated.WaitOne(timeout * 1000))
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CommonResources.VersionCheckTimedout, timeout, EnvironmentHelper.VstestConnectionTimeout));
                }
            }
            finally
            {
                channel.MessageReceived -= onMessageReceived;
                onMessageReceived = null;
            }
        }

        #region Discovery Protocol

        /// <inheritdoc />
        public void InitializeDiscovery(IEnumerable<string> pathToAdditionalExtensions)
        {
            var message = dataSerializer.SerializePayload(
                MessageType.DiscoveryInitialize,
                pathToAdditionalExtensions,
                protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.InitializeDiscovery: Sending initialize discovery with message: {0}", message);
            }

            channel.Send(message);
        }

        /// <inheritdoc/>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            messageEventHandler = discoveryEventsHandler;
            onDisconnected = (disconnectedEventArgs) => OnDiscoveryAbort(discoveryEventsHandler, disconnectedEventArgs.Error, true);
            onMessageReceived = (sender, args) => OnDiscoveryMessageReceived(discoveryEventsHandler, args);

            channel.MessageReceived += onMessageReceived;
            var message = dataSerializer.SerializePayload(
                MessageType.StartDiscovery,
                discoveryCriteria,
                protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.DiscoverTests: Sending discover tests with message: {0}", message);
            }

            channel.Send(message);
        }
        #endregion

        #region Execution Protocol

        /// <inheritdoc />
        public void InitializeExecution(IEnumerable<string> pathToAdditionalExtensions)
        {
            var message = dataSerializer.SerializePayload(
                MessageType.ExecutionInitialize,
                pathToAdditionalExtensions,
                protocolVersion);
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.InitializeExecution: Sending initialize execution with message: {0}", message);
            }

            channel.Send(message);
        }

        /// <inheritdoc />
        public void StartTestRun(TestRunCriteriaWithSources runCriteria, ITestRunEventsHandler eventHandler)
        {
            messageEventHandler = eventHandler;
            onDisconnected = (disconnectedEventArgs) => OnTestRunAbort(eventHandler, disconnectedEventArgs.Error, true);

            onMessageReceived = (sender, args) => OnExecutionMessageReceived(sender, args, eventHandler);
            channel.MessageReceived += onMessageReceived;

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
                && runtimeProvider is ITestRuntimeProvider2 convertedRuntimeProvider
                && protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
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

            var message = dataSerializer.SerializePayload(
                MessageType.StartTestExecutionWithSources,
                runCriteria,
                protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.StartTestRun: Sending test run with message: {0}", message);
            }

            channel.Send(message);
        }

        /// <inheritdoc />
        public void StartTestRun(TestRunCriteriaWithTests runCriteria, ITestRunEventsHandler eventHandler)
        {
            messageEventHandler = eventHandler;
            onDisconnected = (disconnectedEventArgs) => OnTestRunAbort(eventHandler, disconnectedEventArgs.Error, true);

            onMessageReceived = (sender, args) => OnExecutionMessageReceived(sender, args, eventHandler);
            channel.MessageReceived += onMessageReceived;

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
                && runtimeProvider is ITestRuntimeProvider2 convertedRuntimeProvider
                && protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
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

            var message = dataSerializer.SerializePayload(
                MessageType.StartTestExecutionWithTests,
                runCriteria,
                protocolVersion);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.StartTestRun: Sending test run with message: {0}", message);
            }

            channel.Send(message);
        }

        /// <inheritdoc />
        public void SendTestRunCancel()
        {
            if (IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: SendTestRunCancel: Operation is already complete. Skip error message.");
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SendTestRunCancel: Sending test run cancel.");
            }

            channel?.Send(dataSerializer.SerializeMessage(MessageType.CancelTestRun));
        }

        /// <inheritdoc />
        public void SendTestRunAbort()
        {
            if (IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: SendTestRunAbort: Operation is already complete. Skip error message.");
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SendTestRunAbort: Sending test run abort.");
            }

            channel?.Send(dataSerializer.SerializeMessage(MessageType.AbortTestRun));
        }

        #endregion

        /// <inheritdoc />
        public void EndSession()
        {
            if (!IsOperationComplete())
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender.EndSession: Sending end session.");
                }

                channel?.Send(dataSerializer.SerializeMessage(MessageType.SessionEnd));
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

            clientExitErrorMessage = stdError;
            clientExited.Set();

            // Break communication loop. In some cases (E.g: When tests creates child processes to testhost) communication channel won't break if testhost exits.
            communicationEndpoint.Stop();
        }

        /// <inheritdoc />
        public void Close()
        {
            Dispose();
            EqtTrace.Info("Closing the connection");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (channel != null)
            {
                channel.MessageReceived -= onMessageReceived;
            }

            communicationEndpoint.Stop();
            GC.SuppressFinalize(this);
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

                var message = dataSerializer.DeserializeMessage(rawMessage);
                switch (message.MessageType)
                {
                    case MessageType.TestRunStatsChange:
                        var testRunChangedArgs = dataSerializer.DeserializePayload<TestRunChangedEventArgs>(message);
                        testRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
                        break;
                    case MessageType.ExecutionComplete:
                        var testRunCompletePayload = dataSerializer.DeserializePayload<TestRunCompletePayload>(message);

                        testRunEventsHandler.HandleTestRunComplete(
                            testRunCompletePayload.TestRunCompleteArgs,
                            testRunCompletePayload.LastRunTests,
                            testRunCompletePayload.RunAttachments,
                            testRunCompletePayload.ExecutorUris);

                        SetOperationComplete();
                        break;
                    case MessageType.TestMessage:
                        var testMessagePayload = dataSerializer.DeserializePayload<TestMessagePayload>(message);
                        testRunEventsHandler.HandleLogMessage(testMessagePayload.MessageLevel, testMessagePayload.Message);
                        break;
                    case MessageType.LaunchAdapterProcessWithDebuggerAttached:
                        var testProcessStartInfo = dataSerializer.DeserializePayload<TestProcessStartInfo>(message);
                        int processId = testRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

                        var data =
                            dataSerializer.SerializePayload(
                                MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback,
                                processId,
                                protocolVersion);
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("TestRequestSender.OnExecutionMessageReceived: Sending LaunchAdapterProcessWithDebuggerAttachedCallback message: {0}", data);
                        }

                        channel.Send(data);
                        break;

                    case MessageType.AttachDebugger:
                        var testProcessPid = dataSerializer.DeserializePayload<TestProcessAttachDebuggerPayload>(message);
                        bool result = ((ITestRunEventsHandler2)testRunEventsHandler).AttachDebuggerToProcess(testProcessPid.ProcessID);

                        var resultMessage = dataSerializer.SerializePayload(
                            MessageType.AttachDebuggerCallback,
                            result,
                            protocolVersion);

                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("TestRequestSender.OnExecutionMessageReceived: Sending AttachDebugger with message: {0}", message);
                        }

                        channel.Send(resultMessage);

                        break;
                }
            }
            catch (Exception exception)
            {
                OnTestRunAbort(testRunEventsHandler, exception, false);
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

                var data = dataSerializer.DeserializeMessage(rawMessage);
                switch (data.MessageType)
                {
                    case MessageType.TestCasesFound:
                        var testCases = dataSerializer.DeserializePayload<IEnumerable<TestCase>>(data);
                        discoveryEventsHandler.HandleDiscoveredTests(testCases);
                        break;
                    case MessageType.DiscoveryComplete:
                        var discoveryCompletePayload =
                            dataSerializer.DeserializePayload<DiscoveryCompletePayload>(data);
                        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(discoveryCompletePayload.TotalTests, discoveryCompletePayload.IsAborted);
                        discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;
                        discoveryEventsHandler.HandleDiscoveryComplete(
                            discoveryCompleteEventArgs,
                            discoveryCompletePayload.LastDiscoveredTests);
                        SetOperationComplete();
                        break;
                    case MessageType.TestMessage:
                        var testMessagePayload = dataSerializer.DeserializePayload<TestMessagePayload>(
                            data);
                        discoveryEventsHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                OnDiscoveryAbort(discoveryEventsHandler, ex, false);
            }
        }

        private void OnTestRunAbort(ITestRunEventsHandler testRunEventsHandler, Exception exception, bool getClientError)
        {
            if (IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: OnTestRunAbort: Operation is already complete. Skip error message.");
                return;
            }

            EqtTrace.Verbose("TestRequestSender: OnTestRunAbort: Set operation complete.");
            SetOperationComplete();

            var reason = GetAbortErrorMessage(exception, getClientError);
            EqtTrace.Error("TestRequestSender: Aborting test run because {0}", reason);
            LogErrorMessage(string.Format(CommonResources.AbortedTestRun, reason));

            // notify test run abort to vstest console wrapper.
            var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, null, null, TimeSpan.Zero);
            var payload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
            var rawMessage = dataSerializer.SerializePayload(MessageType.ExecutionComplete, payload);
            testRunEventsHandler.HandleRawMessage(rawMessage);

            // notify of a test run complete and bail out.
            testRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);
        }

        private void OnDiscoveryAbort(ITestDiscoveryEventsHandler2 eventHandler, Exception exception, bool getClientError)
        {
            if (IsOperationComplete())
            {
                EqtTrace.Verbose("TestRequestSender: OnDiscoveryAbort: Operation is already complete. Skip error message.");
                return;
            }

            EqtTrace.Verbose("TestRequestSender: OnDiscoveryAbort: Set operation complete.");
            SetOperationComplete();

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
            var reason = GetAbortErrorMessage(exception, getClientError);
            EqtTrace.Error("TestRequestSender: Aborting test discovery because {0}", reason);
            LogErrorMessage(string.Format(CommonResources.AbortedTestDiscovery, reason));

            // Notify discovery abort to IDE test output
            var payload = new DiscoveryCompletePayload()
            {
                IsAborted = true,
                LastDiscoveredTests = null,
                TotalTests = -1
            };
            var rawMessage = dataSerializer.SerializePayload(MessageType.DiscoveryComplete, payload);
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
                if (clientExited.Wait(clientExitedWaitTime))
                {
                    // Set a default message of test host process exited and additionally specify the error if present
                    EqtTrace.Info("TestRequestSender: GetAbortErrorMessage: Received test host error message.");
                    reason = CommonResources.TestHostProcessCrashed;
                    if (!string.IsNullOrWhiteSpace(clientExitErrorMessage))
                    {
                        reason = $"{reason} : {clientExitErrorMessage}";
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
            if (messageEventHandler == null)
            {
                EqtTrace.Error("TestRequestSender.LogErrorMessage: Message event handler not set. Error: " + message);
                return;
            }

            // Log to vstest console
            messageEventHandler.HandleLogMessage(TestMessageLevel.Error, message);

            // Log to vs ide test output
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = message };
            var rawMessage = dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            messageEventHandler.HandleRawMessage(rawMessage);
        }

        private bool IsOperationComplete()
        {
            return operationCompleted == 1;
        }

        private void SetOperationComplete()
        {
            // When sharing the testhost between discovery and execution we must keep the
            // testhost alive after completing the operation it was spawned for. As such we
            // suppress the test request sender channel close taking place here. This channel
            // will be closed when the test session owner decides to dispose of the test session
            // object.
            if (!CloseConnectionOnOperationComplete)
            {
                return;
            }

            // Complete the currently ongoing operation (Discovery/Execution)
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRequestSender.SetOperationComplete: Setting operation complete.");
            }

            communicationEndpoint.Stop();
            Interlocked.CompareExchange(ref operationCompleted, 1, 0);
        }

        private void SetCommunicationEndPoint()
        {
            // TODO: Use factory to get the communication endpoint. It will abstract out the type of communication endpoint like socket, shared memory or named pipe etc.,
            if (connectionInfo.Role == ConnectionRole.Client)
            {
                communicationEndpoint = new SocketClient();
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender is acting as client");
                }
            }
            else
            {
                communicationEndpoint = new SocketServer();
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("TestRequestSender is acting as server");
                }
            }
        }
    }
}
