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

        private bool sendMessagesToRemoteHost = true;

        private IDataSerializer dataSerializer;

        // TODO:sasin Change the version to 2
        private int highestNegotiatedVersion = 1;

        /// <summary>
        /// Use to cancel blocking tasks associated with testhost process
        /// </summary>
        private CancellationTokenSource clientExitCancellationSource;

        private string clientExitErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// </summary>
        /// <param name="protocolConfig">Protocol related information</param>
        public TestRequestSender(ProtocolConfig protocolConfig)
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance, protocolConfig)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestSender"/> class.
        /// </summary>
        /// <param name="communicationManager">Communication Manager for sending and receiving messages.</param>
        /// <param name="dataSerializer">Serializer for serialization and deserialization of the messages.</param>
        /// <param name="protocolConfig">Protocol related information</param>
        internal TestRequestSender(ICommunicationManager communicationManager, IDataSerializer dataSerializer, ProtocolConfig protocolConfig)
        {
            this.highestNegotiatedVersion = protocolConfig.Version;
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <inheritdoc/>
        public int InitializeCommunication()
        {
            this.clientExitCancellationSource = new CancellationTokenSource();
            this.clientExitErrorMessage = string.Empty;
            var port = this.communicationManager.HostServer();
            this.communicationManager.AcceptClientAsync();
            return port;
        }

        /// <inheritdoc/>
        public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
        {
            return this.communicationManager.WaitForClientConnection(clientConnectionTimeout);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.communicationManager?.StopServer();
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
            this.communicationManager.SendMessage(MessageType.VersionCheck, payload: this.highestNegotiatedVersion);

            var message = this.communicationManager.ReceiveMessage();

            if (message.MessageType == MessageType.VersionCheck)
            {
                var protocolVersion = this.dataSerializer.DeserializePayload<int>(message);
                this.highestNegotiatedVersion = protocolVersion;

                EqtTrace.Info("TestRequestSender: VersionCheck Succeeded, NegotiatedVersion = {0}", this.highestNegotiatedVersion);
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
            this.communicationManager.SendMessage(MessageType.DiscoveryInitialize, pathToAdditionalExtensions, version: this.highestNegotiatedVersion);
        }

        /// <inheritdoc/>
        public void InitializeExecution(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions)
        {
            this.communicationManager.SendMessage(MessageType.ExecutionInitialize, pathToAdditionalExtensions, version: this.highestNegotiatedVersion);
        }

        /// <inheritdoc/>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            try
            {
                this.communicationManager.SendMessage(MessageType.StartDiscovery, discoveryCriteria, version: this.highestNegotiatedVersion);

                var isDiscoveryComplete = false;

                // Cycle through the messages that the testhost sends.
                // Currently each of the operations are not separate tasks since they should not each take much time. This is just a notification.
                while (!isDiscoveryComplete)
                {
                    var rawMessage = this.TryReceiveRawMessage();
                    EqtTrace.Info("received message: {0}", rawMessage);

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
                        discoveryEventsHandler.HandleDiscoveryComplete(
                            discoveryCompletePayload.TotalTests,
                            discoveryCompletePayload.LastDiscoveredTests,
                            discoveryCompletePayload.IsAborted);
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
                this.communicationManager.SendMessage(messageType, payload, version: this.highestNegotiatedVersion);

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
                            version: this.highestNegotiatedVersion);
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

        private void OnDiscoveryAbort(ITestDiscoveryEventsHandler eventHandler, Exception exception)
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
            eventHandler.HandleDiscoveryComplete(-1, null, true);

            this.CleanupCommunicationIfProcessExit();
        }

        private string TryReceiveRawMessage()
        {
            string message = null;
            var receiverMessageTask = this.communicationManager.ReceiveRawMessageAsync(this.clientExitCancellationSource.Token);
            receiverMessageTask.Wait();
            message = receiverMessageTask.Result;

            if (message == null)
            {
                var reason = string.IsNullOrWhiteSpace(this.clientExitErrorMessage)
                    ? CommonResources.UnableToCommunicateToTestHost
                    : this.clientExitErrorMessage;
                EqtTrace.Error("Unable to receive message from testhost: {0}", reason);
                throw new IOException(reason);
            }

            return message;
        }
    }
}
