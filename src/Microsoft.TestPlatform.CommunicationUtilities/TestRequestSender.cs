// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CommonResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;

    /// <summary>
    /// Utility class that facilitates the IPC comunication. Acts as server.
    /// </summary>
    public sealed class TestRequestSender : ITestRequestSender
    {
        private ICommunicationManager communicationManager;

        private ITransport transport;

        private bool sendMessagesToRemoteHost = true;

        private IDataSerializer dataSerializer;

        // Set default to 1, if protocol version check does not happen
        // that implies host is using version 1
        private int protocolVersion = 1;

        private int highestSupportedVersion = 2;

        /// <summary>
        /// Use to cancel blocking tasks associated with testhost process
        /// </summary>
        private CancellationTokenSource clientExitCancellationSource;

        private string clientExitErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// </summary>
        /// <param name="protocolConfig">Protocol related information</param>
        /// <param name="connectionInfo">Transport layer to set up connection</param>
        public TestRequestSender(ProtocolConfig protocolConfig, TestHostConnectionInfo connectionInfo)
            : this(new SocketCommunicationManager(), connectionInfo, JsonDataSerializer.Instance, protocolConfig)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// </summary>
        /// <param name="communicationManager">Communication Manager for sending and receiving messages.</param>
        /// <param name="connectionInfo">ConnectionInfo to set up transport layer</param>
        /// <param name="dataSerializer">Serializer for serialization and deserialization of the messages.</param>
        /// <param name="protocolConfig">Protocol related information</param>
        internal TestRequestSender(ICommunicationManager communicationManager, TestHostConnectionInfo connectionInfo, IDataSerializer dataSerializer, ProtocolConfig protocolConfig)
        {
            this.highestSupportedVersion = protocolConfig.Version;
            this.communicationManager = communicationManager;

            // The connectionInfo here is that of RuntimeProvider, so reverse the role of runner.
            connectionInfo.Role = connectionInfo.Role == ConnectionRole.Host
                                                ? ConnectionRole.Client
                                                : ConnectionRole.Host;

            this.transport = new SocketTransport(communicationManager, connectionInfo);
            this.dataSerializer = dataSerializer;
        }

        /// <inheritdoc/>
        public int InitializeCommunication()
        {
            this.clientExitCancellationSource = new CancellationTokenSource();
            this.clientExitErrorMessage = string.Empty;
            return this.transport.Initialize().Port;
        }

        /// <inheritdoc/>
        public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
        {
            return this.transport.WaitForConnection(clientConnectionTimeout);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.transport.Dispose();
        }

        /// <inheritdoc/>
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection");
        }

        /// <inheritdoc/>
        public void CheckVersionWithTestHost()
        {
            this.communicationManager.SendMessage(MessageType.VersionCheck, payload: this.highestSupportedVersion);

            var message = this.communicationManager.ReceiveMessage();

            if (message.MessageType == MessageType.VersionCheck)
            {
                this.protocolVersion = this.dataSerializer.DeserializePayload<int>(message);

                EqtTrace.Info("TestRequestSender: VersionCheck Succeeded, NegotiatedVersion = {0}", this.protocolVersion);
            }
            else if (message.MessageType == MessageType.ProtocolError)
            {
                // TODO : Payload for ProtocolError needs to finalized.
                throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CommonResources.VersionCheckFailed));
            }
            else
            {
                throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CommonResources.UnexpectedMessage, MessageType.VersionCheck, message.MessageType));
            }
        }

        /// <inheritdoc/>
        public void InitializeDiscovery(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions)
        {
            this.communicationManager.SendMessage(MessageType.DiscoveryInitialize, pathToAdditionalExtensions, version: this.protocolVersion);
        }

        /// <inheritdoc/>
        public void InitializeExecution(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions)
        {
            this.communicationManager.SendMessage(MessageType.ExecutionInitialize, pathToAdditionalExtensions, version: this.protocolVersion);
        }

        /// <inheritdoc/>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            try
            {
                this.communicationManager.SendMessage(MessageType.StartDiscovery, discoveryCriteria, version: this.protocolVersion);

                var isDiscoveryComplete = false;

                // Cycle through the messages that the testhost sends.
                // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
                while (!isDiscoveryComplete)
                {
                    var rawMessage = this.TryReceiveRawMessage();
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("Received message: {0}", rawMessage);
                    }

                    // Send raw message first to unblock handlers waiting to send message to IDEs
                    discoveryEventsHandler.HandleRawMessage(rawMessage);

                    var message = this.dataSerializer.DeserializeMessage(rawMessage);
                    if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                    {
                        var testCases = this.dataSerializer.DeserializePayload<IEnumerable<TestCase>>(message);
                        discoveryEventsHandler.HandleDiscoveredTests(testCases);
                    }
                    else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                    {
                        var discoveryCompletePayload = this.dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);
                        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(discoveryCompletePayload.TotalTests, discoveryCompletePayload.IsAborted);

                        discoveryEventsHandler.HandleDiscoveryComplete(
                            discoveryCompleteEventArgs,
                            discoveryCompletePayload.LastDiscoveredTests);
                        isDiscoveryComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer.DeserializePayload<TestMessagePayload>(message);
                        discoveryEventsHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnDiscoveryAbort(discoveryEventsHandler, ex);
            }
        }

        /// <summary>
        /// Ends the session with the test host.
        /// </summary>
        public void EndSession()
        {
            // don't try to communicate if connection is broken
            if (!this.sendMessagesToRemoteHost)
            {
                EqtTrace.Error("Connection has been broken: not sending SessionEnd message");
                return;
            }

            this.communicationManager.SendMessage(MessageType.SessionEnd);
        }

        /// <summary>
        /// Executes tests on the sources specified with the criteria mentioned.
        /// </summary>
        /// <param name="runCriteria">The test run criteria.</param>
        /// <param name="eventHandler">The handler for execution events from the test host.</param>
        public void StartTestRun(TestRunCriteriaWithSources runCriteria, ITestRunEventsHandler eventHandler)
        {
            this.StartTestRunAndListenAndReportTestResults(MessageType.StartTestExecutionWithSources, runCriteria, eventHandler);
        }

        /// <summary>
        /// Executes the specified tests with the criteria mentioned.
        /// </summary>
        /// <param name="runCriteria">The test run criteria.</param>
        /// <param name="eventHandler">The handler for execution events from the test host.</param>
        public void StartTestRun(TestRunCriteriaWithTests runCriteria, ITestRunEventsHandler eventHandler)
        {
            this.StartTestRunAndListenAndReportTestResults(MessageType.StartTestExecutionWithTests, runCriteria, eventHandler);
        }

        /// <summary>
        /// Send the cancel message to test host
        /// </summary>
        public void SendTestRunCancel()
        {
            this.communicationManager.SendMessage(MessageType.CancelTestRun);
        }

        /// <summary>
        /// Send the Abort test run message
        /// </summary>
        public void SendTestRunAbort()
        {
            this.communicationManager.SendMessage(MessageType.AbortTestRun);
        }

        /// <summary>
        /// Handles exit of the client process.
        /// </summary>
        /// <param name="stdError">Standard Error.</param>
        public void OnClientProcessExit(string stdError)
        {
            this.clientExitErrorMessage = stdError;
            this.clientExitCancellationSource.Cancel();
        }

        private void StartTestRunAndListenAndReportTestResults(
            string messageType,
            object payload,
            ITestRunEventsHandler eventHandler)
        {
            try
            {
                this.communicationManager.SendMessage(messageType, payload, version: this.protocolVersion);

                // This needs to happen asynchronously.
                Task.Run(() => this.ListenAndReportTestResults(eventHandler));
            }
            catch (Exception exception)
            {
                this.OnTestRunAbort(eventHandler, exception);
            }
        }

        private void ListenAndReportTestResults(ITestRunEventsHandler testRunEventsHandler)
        {
            var isTestRunComplete = false;

            // Cycle through the messages that the testhost sends.
            // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
            while (!isTestRunComplete)
            {
                try
                {
                    var rawMessage = this.TryReceiveRawMessage();

                    // Send raw message first to unblock handlers waiting to send message to IDEs
                    testRunEventsHandler.HandleRawMessage(rawMessage);

                    var message = this.dataSerializer.DeserializeMessage(rawMessage);
                    if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                    {
                        var testRunChangedArgs = this.dataSerializer.DeserializePayload<TestRunChangedEventArgs>(
                            message);
                        testRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
                    }
                    else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                    {
                        var testRunCompletePayload =
                            this.dataSerializer.DeserializePayload<TestRunCompletePayload>(message);

                        testRunEventsHandler.HandleTestRunComplete(
                            testRunCompletePayload.TestRunCompleteArgs,
                            testRunCompletePayload.LastRunTests,
                            testRunCompletePayload.RunAttachments,
                            testRunCompletePayload.ExecutorUris);
                        isTestRunComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer.DeserializePayload<TestMessagePayload>(message);
                        testRunEventsHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                    else if (string.Equals(MessageType.LaunchAdapterProcessWithDebuggerAttached, message.MessageType))
                    {
                        var testProcessStartInfo = this.dataSerializer.DeserializePayload<TestProcessStartInfo>(message);
                        int processId = testRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);

                        this.communicationManager.SendMessage(
                            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback,
                            processId,
                            version: this.protocolVersion);
                    }
                }
                catch (IOException exception)
                {
                    // To avoid further communication with remote host
                    this.sendMessagesToRemoteHost = false;

                    this.OnTestRunAbort(testRunEventsHandler, exception);
                    isTestRunComplete = true;
                }
                catch (Exception exception)
                {
                    this.OnTestRunAbort(testRunEventsHandler, exception);
                    isTestRunComplete = true;
                }
            }
        }

        private void CleanupCommunicationIfProcessExit()
        {
            if (this.clientExitCancellationSource != null && this.clientExitCancellationSource.IsCancellationRequested)
            {
                this.communicationManager.StopServer();
            }
        }

        private void OnTestRunAbort(ITestRunEventsHandler testRunEventsHandler, Exception exception)
        {
            try
            {
                EqtTrace.Error("Server: TestExecution: Aborting test run because {0}", exception);

                var reason = string.Format(CommonResources.AbortedTestRun, exception?.Message);

                // log console message to vstest console
                testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, reason);

                // log console message to vstest console wrapper
                var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = reason };
                var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
                testRunEventsHandler.HandleRawMessage(rawMessage);

                // notify test run abort to vstest console wrapper.
                var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, null, TimeSpan.Zero);
                var payload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
                rawMessage = this.dataSerializer.SerializePayload(MessageType.ExecutionComplete, payload);
                testRunEventsHandler.HandleRawMessage(rawMessage);

                // notify of a test run complete and bail out.
                testRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);

                this.CleanupCommunicationIfProcessExit();
            }
            catch (Exception ex)
            {
                EqtTrace.Error(ex);
                throw ex;
            }
        }

        private void OnDiscoveryAbort(ITestDiscoveryEventsHandler2 eventHandler, Exception exception)
        {
            try
            {
                EqtTrace.Error("Server: TestExecution: Aborting test discovery because {0}", exception);

                var reason = string.Format(CommonResources.AbortedTestDiscovery, exception?.Message);

                // Log to vstest console
                eventHandler.HandleLogMessage(TestMessageLevel.Error, reason);

                // Log to vs ide test output
                var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = reason };
                var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
                eventHandler.HandleRawMessage(rawMessage);

                // Notify discovery abort to IDE test output
                var payload = new DiscoveryCompletePayload()
                {
                    IsAborted = true,
                    LastDiscoveredTests = null,
                    TotalTests = -1
                };
                rawMessage = this.dataSerializer.SerializePayload(MessageType.DiscoveryComplete, payload);
                eventHandler.HandleRawMessage(rawMessage);

                // Complete discovery
                var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
                eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);

                this.CleanupCommunicationIfProcessExit();
            }
            catch (Exception ex)
            {
                EqtTrace.Error(ex);
                throw ex;
            }
        }

        private string TryReceiveRawMessage()
        {
            string message = null;
            var receiverMessageTask = this.communicationManager.ReceiveRawMessageAsync(this.clientExitCancellationSource.Token);
            receiverMessageTask.Wait();
            message = receiverMessageTask.Result;

            if (message == null)
            {
                EqtTrace.Warning("TestRequestSender: Communication channel with test host is broken.");
                var reason = CommonResources.UnableToCommunicateToTestHost;
                if (!string.IsNullOrWhiteSpace(this.clientExitErrorMessage))
                {
                    reason = this.clientExitErrorMessage;
                }
                else
                {
                    // Test host process has not exited yet. We will wait for exit to allow us gather
                    // standard error
                    var processWaitEvent = new ManualResetEventSlim();
                    try
                    {
                        EqtTrace.Info("TestRequestSender: Waiting for test host to exit.");
                        processWaitEvent.Wait(TimeSpan.FromSeconds(10), this.clientExitCancellationSource.Token);

                        // Use a default error message
                        EqtTrace.Info("TestRequestSender: Timed out waiting for test host to exit.");
                        reason = CommonResources.UnableToCommunicateToTestHost;
                    }
                    catch (OperationCanceledException)
                    {
                        EqtTrace.Info("TestRequestSender: Got error message from test host.");
                        reason = this.clientExitErrorMessage;
                    }
                }

                EqtTrace.Error("TestRequestSender: Unable to receive message from testhost: {0}", reason);
                throw new IOException(reason);
            }

            return message;
        }
    }
}
