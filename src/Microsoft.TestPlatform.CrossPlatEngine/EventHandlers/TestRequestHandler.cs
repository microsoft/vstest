// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CrossPlatResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using ObjectModelConstants = Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants;

    public class TestRequestHandler : ITestRequestHandler
    {
        private int protocolVersion = 1;
        private int highestSupportedVersion = 3;

        private readonly IDataSerializer dataSerializer;
        private ITestHostManagerFactory testHostManagerFactory;
        private ICommunicationEndPoint communicationEndPoint;
        private ICommunicationEndpointFactory communicationEndpointFactory;
        private ICommunicationChannel channel;

        private JobQueue<Action> jobQueue;
        private ManualResetEventSlim requestSenderConnected;
        private ManualResetEventSlim testHostManagerFactoryReady;
        private ManualResetEventSlim sessionCompleted;
        private Action<Message> onLaunchAdapterProcessWithDebuggerAttachedAckReceived;
        private Action<Message> onAttachDebuggerAckRecieved;

        public TestHostConnectionInfo ConnectionInfo { get; set; } 

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRequestHandler" />.
        /// </summary>
        public TestRequestHandler() : this(JsonDataSerializer.Instance, new CommunicationEndpointFactory())
        {
        }

        protected TestRequestHandler(
            TestHostConnectionInfo connectionInfo,
            ICommunicationEndpointFactory communicationEndpointFactory,
            IDataSerializer dataSerializer,
            JobQueue<Action> jobQueue,
            Action<Message> onLaunchAdapterProcessWithDebuggerAttachedAckReceived,
            Action<Message> onAttachDebuggerAckRecieved)
        {
            this.communicationEndpointFactory = communicationEndpointFactory;
            this.ConnectionInfo = connectionInfo;
            this.dataSerializer = dataSerializer;
            this.requestSenderConnected = new ManualResetEventSlim(false);
            this.testHostManagerFactoryReady = new ManualResetEventSlim(false);
            this.sessionCompleted = new ManualResetEventSlim(false);
            this.onLaunchAdapterProcessWithDebuggerAttachedAckReceived = onLaunchAdapterProcessWithDebuggerAttachedAckReceived;
            this.onAttachDebuggerAckRecieved = onAttachDebuggerAckRecieved;
            this.jobQueue = jobQueue;
        }

        protected TestRequestHandler(IDataSerializer dataSerializer, ICommunicationEndpointFactory communicationEndpointFactory)
        {
            this.dataSerializer = dataSerializer;
            this.communicationEndpointFactory = communicationEndpointFactory;
            this.requestSenderConnected = new ManualResetEventSlim(false);
            this.sessionCompleted = new ManualResetEventSlim(false);
            this.testHostManagerFactoryReady = new ManualResetEventSlim(false);
            this.onLaunchAdapterProcessWithDebuggerAttachedAckReceived = (message) => { throw new NotImplementedException(); };
            this.onAttachDebuggerAckRecieved = (message) => { throw new NotImplementedException(); };

            this.jobQueue = new JobQueue<Action>(
                (action) => { action(); },
                "TestHostOperationQueue",
                500,
                25000000,
                true,
                (message) => EqtTrace.Error(message));
        }

        /// <inheritdoc />
        public virtual void InitializeCommunication()
        {
            this.communicationEndPoint = this.communicationEndpointFactory.Create(this.ConnectionInfo.Role);
            this.communicationEndPoint.Connected += (sender, connectedArgs) =>
            {
                if (!connectedArgs.Connected)
                {
                    requestSenderConnected.Set();
                    throw connectedArgs.Fault;
                }
                this.channel = connectedArgs.Channel;
                this.channel.MessageReceived += this.OnMessageReceived;
                requestSenderConnected.Set();
            };

            this.communicationEndPoint.Start(this.ConnectionInfo.Endpoint);
        }

        /// <inheritdoc />
        public bool WaitForRequestSenderConnection(int connectionTimeout)
        {
            return requestSenderConnected.Wait(connectionTimeout);
        }

        /// <inheritdoc />
        public void ProcessRequests(ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManagerFactory = testHostManagerFactory;
            this.testHostManagerFactoryReady.Set();
            this.sessionCompleted.Wait();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.communicationEndPoint.Stop();
            this.channel?.Dispose();
        }

        /// <inheritdoc />
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection !");
        }

        /// <inheritdoc />
        public void SendTestCases(IEnumerable<TestCase> discoveredTestCases)
        {
            var data = this.dataSerializer.SerializePayload(MessageType.TestCasesFound, discoveredTestCases, this.protocolVersion);
            this.SendData(data);
        }

        /// <inheritdoc />
        public void SendTestRunStatistics(TestRunChangedEventArgs testRunChangedArgs)
        {
            var data = this.dataSerializer.SerializePayload(MessageType.TestRunStatsChange, testRunChangedArgs, this.protocolVersion);
            this.SendData(data);
        }

        /// <inheritdoc />
        public void SendLog(TestMessageLevel messageLevel, string message)
        {
            var data = this.dataSerializer.SerializePayload(
                    MessageType.TestMessage,
                    new TestMessagePayload { MessageLevel = messageLevel, Message = message },
                    this.protocolVersion);
            this.SendData(data);
        }

        /// <inheritdoc />
        public void SendExecutionComplete(
                TestRunCompleteEventArgs testRunCompleteArgs,
                TestRunChangedEventArgs lastChunkArgs,
                ICollection<AttachmentSet> runContextAttachments,
                ICollection<string> executorUris)
        {
            var data = this.dataSerializer.SerializePayload(
                    MessageType.ExecutionComplete,
                    new TestRunCompletePayload
                    {
                        TestRunCompleteArgs = testRunCompleteArgs,
                        LastRunTests = lastChunkArgs,
                        RunAttachments = runContextAttachments,
                        ExecutorUris = executorUris
                    },
                    this.protocolVersion);
            this.SendData(data);
        }

        /// <inheritdoc />
        public void DiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            var data = this.dataSerializer.SerializePayload(
                    MessageType.DiscoveryComplete,
                    new DiscoveryCompletePayload
                    {
                        TotalTests = discoveryCompleteEventArgs.TotalCount,
                        LastDiscoveredTests = discoveryCompleteEventArgs.IsAborted ? null : lastChunk,
                        IsAborted = discoveryCompleteEventArgs.IsAborted,
                        Metrics = discoveryCompleteEventArgs.Metrics
                    },
                    this.protocolVersion);
            this.SendData(data);
        }

        /// <inheritdoc />
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            var waitHandle = new ManualResetEventSlim(false);
            Message ackMessage = null;
            this.onLaunchAdapterProcessWithDebuggerAttachedAckReceived = (ackRawMessage) =>
            {
                ackMessage = ackRawMessage;
                waitHandle.Set();
            };

            var data = dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttached,
                testProcessStartInfo, protocolVersion);

            this.SendData(data);

            EqtTrace.Verbose("Waiting for LaunchAdapterProcessWithDebuggerAttached ack");
            waitHandle.Wait();
            this.onLaunchAdapterProcessWithDebuggerAttachedAckReceived = null;
            return this.dataSerializer.DeserializePayload<int>(ackMessage);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            // If an attach request is issued but there is no support for attaching on the other
            // side of the communication channel, we simply return and let the caller know the
            // request failed.
            if (this.protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
            {
                return false;
            }

            Message ackMessage = null;
            var waitHandle = new ManualResetEventSlim(false);

            this.onAttachDebuggerAckRecieved = (ackRawMessage) =>
            {
                ackMessage = ackRawMessage;
                waitHandle.Set();
            };

            var data = dataSerializer.SerializePayload(
                MessageType.AttachDebugger,
                new TestProcessAttachDebuggerPayload(pid),
                protocolVersion);
            this.SendData(data);

            EqtTrace.Verbose("Waiting for AttachDebuggerToProcess ack ...");
            waitHandle.Wait();

            this.onAttachDebuggerAckRecieved = null;
            return this.dataSerializer.DeserializePayload<bool>(ackMessage);
        }

        public void OnMessageReceived(object sender, MessageReceivedEventArgs messageReceivedArgs)
        {
            var message = this.dataSerializer.DeserializeMessage(messageReceivedArgs.Data);

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("TestRequestHandler.ProcessRequests: received message: {0}", message);
            }

            switch (message.MessageType)
            {
                case MessageType.VersionCheck:
                    var version = this.dataSerializer.DeserializePayload<int>(message);
                    this.protocolVersion = Math.Min(version, highestSupportedVersion);

                    // Send the negotiated protocol to request sender
                    this.channel.Send(this.dataSerializer.SerializePayload(MessageType.VersionCheck, this.protocolVersion));

                    // Can only do this after InitializeCommunication because TestHost cannot "Send Log" unless communications are initialized
                    if (!string.IsNullOrEmpty(EqtTrace.LogFile))
                    {
                        this.SendLog(TestMessageLevel.Informational, string.Format(CrossPlatResources.TesthostDiagLogOutputFile, EqtTrace.LogFile));
                    }
                    else if (!string.IsNullOrEmpty(EqtTrace.ErrorOnInitialization))
                    {
                        this.SendLog(TestMessageLevel.Warning, EqtTrace.ErrorOnInitialization);
                    }
                    break;

                case MessageType.DiscoveryInitialize:
                    {
                        EqtTrace.Info("Discovery Session Initialize.");
                        this.testHostManagerFactoryReady.Wait();
                        var discoveryEventsHandler = new TestDiscoveryEventHandler(this);
                        var pathToAdditionalExtensions = this.dataSerializer.DeserializePayload<IEnumerable<string>>(message);
                        jobQueue.QueueJob(
                                () =>
                                testHostManagerFactory.GetDiscoveryManager().Initialize(pathToAdditionalExtensions, discoveryEventsHandler), 0);
                        break;
                    }

                case MessageType.StartDiscovery:
                    {
                        EqtTrace.Info("Discovery started.");
                        this.testHostManagerFactoryReady.Wait();
                        var discoveryEventsHandler = new TestDiscoveryEventHandler(this);
                        var discoveryCriteria = this.dataSerializer.DeserializePayload<DiscoveryCriteria>(message);
                        jobQueue.QueueJob(
                                () =>
                                testHostManagerFactory.GetDiscoveryManager()
                                .DiscoverTests(discoveryCriteria, discoveryEventsHandler), 0);

                        break;
                    }

                case MessageType.ExecutionInitialize:
                    {
                        EqtTrace.Info("Execution Session Initialize.");
                        this.testHostManagerFactoryReady.Wait();
                        var testInitializeEventsHandler = new TestInitializeEventsHandler(this);
                        var pathToAdditionalExtensions = this.dataSerializer.DeserializePayload<IEnumerable<string>>(message);
                        jobQueue.QueueJob(
                                () =>
                                testHostManagerFactory.GetExecutionManager().Initialize(pathToAdditionalExtensions, testInitializeEventsHandler), 0);
                        break;
                    }

                case MessageType.StartTestExecutionWithSources:
                    {
                        EqtTrace.Info("Execution started.");
                        var testRunEventsHandler = new TestRunEventsHandler(this);
                        this.testHostManagerFactoryReady.Wait();
                        var testRunCriteriaWithSources = this.dataSerializer.DeserializePayload<TestRunCriteriaWithSources>(message);
                        jobQueue.QueueJob(
                                () =>
                                testHostManagerFactory.GetExecutionManager()
                                .StartTestRun(
                                    testRunCriteriaWithSources.AdapterSourceMap,
                                    testRunCriteriaWithSources.Package,
                                    testRunCriteriaWithSources.RunSettings,
                                    testRunCriteriaWithSources.TestExecutionContext,
                                    this.GetTestCaseEventsHandler(testRunCriteriaWithSources.RunSettings),
                                    testRunEventsHandler),
                                0);

                        break;
                    }

                case MessageType.StartTestExecutionWithTests:
                    {
                        EqtTrace.Info("Execution started.");
                        var testRunEventsHandler = new TestRunEventsHandler(this);
                        this.testHostManagerFactoryReady.Wait();
                        var testRunCriteriaWithTests =
                            this.dataSerializer.DeserializePayload<TestRunCriteriaWithTests>(message);

                        jobQueue.QueueJob(
                                () =>
                                testHostManagerFactory.GetExecutionManager()
                                .StartTestRun(
                                    testRunCriteriaWithTests.Tests,
                                    testRunCriteriaWithTests.Package,
                                    testRunCriteriaWithTests.RunSettings,
                                    testRunCriteriaWithTests.TestExecutionContext,
                                    this.GetTestCaseEventsHandler(testRunCriteriaWithTests.RunSettings),
                                    testRunEventsHandler),
                                0);

                        break;
                    }

                case MessageType.CancelTestRun:
                    jobQueue.Pause();
                    this.testHostManagerFactoryReady.Wait();
                    testHostManagerFactory.GetExecutionManager().Cancel(new TestRunEventsHandler(this));
                    break;

                case MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback:
                    this.onLaunchAdapterProcessWithDebuggerAttachedAckReceived?.Invoke(message);
                    break;

                case MessageType.AttachDebuggerCallback:
                    this.onAttachDebuggerAckRecieved?.Invoke(message);
                    break;

                case MessageType.AbortTestRun:
                    jobQueue.Pause();
                    this.testHostManagerFactoryReady.Wait();
                    testHostManagerFactory.GetExecutionManager().Abort(new TestRunEventsHandler(this));
                    break;

                case MessageType.SessionEnd:
                    {
                        EqtTrace.Info("Session End message received from server. Closing the connection.");
                        sessionCompleted.Set();
                        this.Close();
                        break;
                    }

                case MessageType.SessionAbort:
                    {
                        // Don't do anything for now.
                        break;
                    }

                default:
                    {
                        EqtTrace.Info("Invalid Message types");
                        break;
                    }
            }
        }

        private ITestCaseEventsHandler GetTestCaseEventsHandler(string runSettings)
        {
            ITestCaseEventsHandler testCaseEventsHandler = null;

            // Listen to test case events only if data collection is enabled
            if ((XmlRunSettingsUtilities.IsDataCollectionEnabled(runSettings) && DataCollectionTestCaseEventSender.Instance != null) || XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(runSettings))
            {
                testCaseEventsHandler = new TestCaseEventsHandler();
            }

            return testCaseEventsHandler;
        }

        private void SendData(string data)
        {
            EqtTrace.Verbose("TestRequestHandler.SendData:  sending data from testhost: {0}", data);
            this.channel.Send(data);
        }
    }
}
