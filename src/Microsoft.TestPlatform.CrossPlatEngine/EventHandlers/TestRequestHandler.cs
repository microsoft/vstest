// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using System;
using System.Collections.Generic;
using System.Threading;

using EventHandlers;
using Interfaces;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Utilities;

using CrossPlatResources = CrossPlatEngine.Resources.Resources;
using ObjectModelConstants = TestPlatform.ObjectModel.Constants;

public class TestRequestHandler : ITestRequestHandler
{
    private int _protocolVersion = 1;

    // Must be in sync with the highest supported version in
    // src/Microsoft.TestPlatform.CommunicationUtilities/TestRequestSender.cs file.
    private readonly int _highestSupportedVersion = 5;

    private readonly IDataSerializer _dataSerializer;
    private ITestHostManagerFactory _testHostManagerFactory;
    private ICommunicationEndPoint _communicationEndPoint;
    private readonly ICommunicationEndpointFactory _communicationEndpointFactory;
    private ICommunicationChannel _channel;

    private readonly JobQueue<Action> _jobQueue;
    private readonly ManualResetEventSlim _requestSenderConnected;
    private readonly ManualResetEventSlim _testHostManagerFactoryReady;
    private readonly ManualResetEventSlim _sessionCompleted;
    private Action<Message> _onLaunchAdapterProcessWithDebuggerAttachedAckReceived;
    private Action<Message> _onAttachDebuggerAckRecieved;
    private Exception _messageProcessingUnrecoverableError;

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
        _communicationEndpointFactory = communicationEndpointFactory;
        ConnectionInfo = connectionInfo;
        _dataSerializer = dataSerializer;
        _requestSenderConnected = new ManualResetEventSlim(false);
        _testHostManagerFactoryReady = new ManualResetEventSlim(false);
        _sessionCompleted = new ManualResetEventSlim(false);
        _onLaunchAdapterProcessWithDebuggerAttachedAckReceived = onLaunchAdapterProcessWithDebuggerAttachedAckReceived;
        _onAttachDebuggerAckRecieved = onAttachDebuggerAckRecieved;
        _jobQueue = jobQueue;
    }

    protected TestRequestHandler(IDataSerializer dataSerializer, ICommunicationEndpointFactory communicationEndpointFactory)
    {
        _dataSerializer = dataSerializer;
        _communicationEndpointFactory = communicationEndpointFactory;
        _requestSenderConnected = new ManualResetEventSlim(false);
        _sessionCompleted = new ManualResetEventSlim(false);
        _testHostManagerFactoryReady = new ManualResetEventSlim(false);
        _onLaunchAdapterProcessWithDebuggerAttachedAckReceived = (message) => throw new NotImplementedException();
        _onAttachDebuggerAckRecieved = (message) => throw new NotImplementedException();

        _jobQueue = new JobQueue<Action>(
            (action) => action(),
            "TestHostOperationQueue",
            500,
            25000000,
            true,
            (message) => EqtTrace.Error(message));
    }

    /// <inheritdoc />
    public virtual void InitializeCommunication()
    {
        _communicationEndPoint = _communicationEndpointFactory.Create(ConnectionInfo.Role);
        _communicationEndPoint.Connected += (sender, connectedArgs) =>
        {
            if (!connectedArgs.Connected)
            {
                _requestSenderConnected.Set();
                throw connectedArgs.Fault;
            }
            _channel = connectedArgs.Channel;
            _channel.MessageReceived += OnMessageReceived;
            _requestSenderConnected.Set();
        };

        _communicationEndPoint.Start(ConnectionInfo.Endpoint);
    }

    /// <inheritdoc />
    public bool WaitForRequestSenderConnection(int connectionTimeout)
    {
        return _requestSenderConnected.Wait(connectionTimeout);
    }

    /// <inheritdoc />
    public void ProcessRequests(ITestHostManagerFactory testHostManagerFactory)
    {
        _testHostManagerFactory = testHostManagerFactory;
        _testHostManagerFactoryReady.Set();
        _sessionCompleted.Wait();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _communicationEndPoint.Stop();
        _channel?.Dispose();
    }

    /// <inheritdoc />
    public void Close()
    {
        Dispose();
        EqtTrace.Info("Closing the connection !");
    }

    /// <inheritdoc />
    public void SendTestCases(IEnumerable<TestCase> discoveredTestCases)
    {
        var data = _dataSerializer.SerializePayload(MessageType.TestCasesFound, discoveredTestCases, _protocolVersion);
        SendData(data);
    }

    /// <inheritdoc />
    public void SendTestRunStatistics(TestRunChangedEventArgs testRunChangedArgs)
    {
        var data = _dataSerializer.SerializePayload(MessageType.TestRunStatsChange, testRunChangedArgs, _protocolVersion);
        SendData(data);
    }

    /// <inheritdoc />
    public void SendLog(TestMessageLevel messageLevel, string message)
    {
        var data = _dataSerializer.SerializePayload(
            MessageType.TestMessage,
            new TestMessagePayload { MessageLevel = messageLevel, Message = message },
            _protocolVersion);
        SendData(data);
    }

    /// <inheritdoc />
    public void SendExecutionComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs lastChunkArgs,
        ICollection<AttachmentSet> runContextAttachments,
        ICollection<string> executorUris)
    {
        // When we abort the run we might have saved the error before we gave the handler the chance to abort
        // if the handler does not return with any new error we report the original one.
        if (testRunCompleteArgs.IsAborted && testRunCompleteArgs.Error == null && _messageProcessingUnrecoverableError != null)
        {
            var curentArgs = testRunCompleteArgs;
            testRunCompleteArgs = new TestRunCompleteEventArgs(
                curentArgs.TestRunStatistics,
                curentArgs.IsCanceled,
                curentArgs.IsAborted,
                _messageProcessingUnrecoverableError,
                curentArgs.AttachmentSets, curentArgs.InvokedDataCollectors, curentArgs.ElapsedTimeInRunningTests
            );
        }
        var data = _dataSerializer.SerializePayload(
            MessageType.ExecutionComplete,
            new TestRunCompletePayload
            {
                TestRunCompleteArgs = testRunCompleteArgs,
                LastRunTests = lastChunkArgs,
                RunAttachments = runContextAttachments,
                ExecutorUris = executorUris
            },
            _protocolVersion);
        SendData(data);
    }

    /// <inheritdoc />
    public void DiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        var data = _dataSerializer.SerializePayload(
            MessageType.DiscoveryComplete,
            new DiscoveryCompletePayload
            {
                TotalTests = discoveryCompleteEventArgs.TotalCount,
                LastDiscoveredTests = discoveryCompleteEventArgs.IsAborted ? null : lastChunk,
                IsAborted = discoveryCompleteEventArgs.IsAborted,
                Metrics = discoveryCompleteEventArgs.Metrics
            },
            _protocolVersion);
        SendData(data);
    }

    /// <inheritdoc />
    public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
    {
        var waitHandle = new ManualResetEventSlim(false);
        Message ackMessage = null;
        _onLaunchAdapterProcessWithDebuggerAttachedAckReceived = (ackRawMessage) =>
        {
            ackMessage = ackRawMessage;
            waitHandle.Set();
        };

        var data = _dataSerializer.SerializePayload(MessageType.LaunchAdapterProcessWithDebuggerAttached,
            testProcessStartInfo, _protocolVersion);

        SendData(data);

        EqtTrace.Verbose("Waiting for LaunchAdapterProcessWithDebuggerAttached ack");
        waitHandle.Wait();
        _onLaunchAdapterProcessWithDebuggerAttachedAckReceived = null;
        return _dataSerializer.DeserializePayload<int>(ackMessage);
    }

    /// <inheritdoc />
    public bool AttachDebuggerToProcess(int pid)
    {
        // If an attach request is issued but there is no support for attaching on the other
        // side of the communication channel, we simply return and let the caller know the
        // request failed.
        if (_protocolVersion < ObjectModelConstants.MinimumProtocolVersionWithDebugSupport)
        {
            return false;
        }

        Message ackMessage = null;
        var waitHandle = new ManualResetEventSlim(false);

        _onAttachDebuggerAckRecieved = (ackRawMessage) =>
        {
            ackMessage = ackRawMessage;
            waitHandle.Set();
        };

        var data = _dataSerializer.SerializePayload(
            MessageType.AttachDebugger,
            new TestProcessAttachDebuggerPayload(pid),
            _protocolVersion);
        SendData(data);

        EqtTrace.Verbose("Waiting for AttachDebuggerToProcess ack ...");
        waitHandle.Wait();

        _onAttachDebuggerAckRecieved = null;
        return _dataSerializer.DeserializePayload<bool>(ackMessage);
    }

    public void OnMessageReceived(object sender, MessageReceivedEventArgs messageReceivedArgs)
    {
        var message = _dataSerializer.DeserializeMessage(messageReceivedArgs.Data);

        if (EqtTrace.IsInfoEnabled)
        {
            EqtTrace.Info("TestRequestHandler.OnMessageReceived: received message: {0}", message);
        }

        switch (message.MessageType)
        {
            case MessageType.VersionCheck:
                try
                {
                    var version = _dataSerializer.DeserializePayload<int>(message);
                    // choose the highest version that we both support
                    var negotiatedVersion = Math.Min(version, _highestSupportedVersion);
                    // BUT don't choose 3, because protocol version 3 has performance problems in 16.7.1-16.8. Those problems are caused
                    // by choosing payloadSerializer instead of payloadSerializer2 for protocol version 3.
                    //
                    // We cannot just update the code to choose the new serializer, because then that change would apply only to testhost.
                    // Testhost is is delivered by Microsoft.NET.Test.SDK nuget package, and can be used with an older vstest.console.
                    // An older vstest.console, that supports protocol version 3, would serialize its messages using payloadSerializer,
                    // but the fixed testhost would serialize it using payloadSerializer2, resulting in incompatible messages.
                    //
                    // Instead we must downgrade to protocol version 2 when 3 would be negotiated. Or higher when higher version
                    // would be negotiated.
                    if (negotiatedVersion != 3)
                    {
                        _protocolVersion = negotiatedVersion;
                    }
                    else
                    {
                        var flag = Environment.GetEnvironmentVariable("VSTEST_DISABLE_PROTOCOL_3_VERSION_DOWNGRADE");
                        var flagIsEnabled = flag != null && flag != "0";
                        var dowgradeIsDisabled = flagIsEnabled;
                        _protocolVersion = dowgradeIsDisabled ? negotiatedVersion : 2;
                    }

                    // Send the negotiated protocol to request sender
                    _channel.Send(_dataSerializer.SerializePayload(MessageType.VersionCheck, _protocolVersion));

                    // Can only do this after InitializeCommunication because TestHost cannot "Send Log" unless communications are initialized
                    if (!string.IsNullOrEmpty(EqtTrace.LogFile))
                    {
                        SendLog(TestMessageLevel.Informational, string.Format(CrossPlatResources.TesthostDiagLogOutputFile, EqtTrace.LogFile));
                    }
                    else if (!string.IsNullOrEmpty(EqtTrace.ErrorOnInitialization))
                    {
                        SendLog(TestMessageLevel.Warning, EqtTrace.ErrorOnInitialization);
                    }
                }
                catch (Exception ex)
                {
                    _messageProcessingUnrecoverableError = ex;
                    EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                    EqtTrace.Error(ex);
                    goto case MessageType.AbortTestRun;
                }
                break;

            case MessageType.DiscoveryInitialize:
                {
                    try
                    {
                        _testHostManagerFactoryReady.Wait();
                        var discoveryEventsHandler = new TestDiscoveryEventHandler(this);
                        var pathToAdditionalExtensions = _dataSerializer.DeserializePayload<IEnumerable<string>>(message);
                        Action job = () =>
                        {
                            EqtTrace.Info("TestRequestHandler.OnMessageReceived: Running job '{0}'.", message.MessageType);
                            _testHostManagerFactory.GetDiscoveryManager().Initialize(pathToAdditionalExtensions, discoveryEventsHandler);
                        };
                        _jobQueue.QueueJob(job, 0);
                    }
                    catch (Exception ex)
                    {
                        _messageProcessingUnrecoverableError = ex;
                        EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                        EqtTrace.Error(ex);
                        goto case MessageType.AbortTestRun;
                    }
                    break;
                }

            case MessageType.StartDiscovery:
                {
                    try
                    {
                        _testHostManagerFactoryReady.Wait();
                        var discoveryEventsHandler = new TestDiscoveryEventHandler(this);
                        var discoveryCriteria = _dataSerializer.DeserializePayload<DiscoveryCriteria>(message);
                        Action job = () =>
                        {
                            EqtTrace.Info("TestRequestHandler.OnMessageReceived: Running job '{0}'.", message.MessageType);
                            _testHostManagerFactory.GetDiscoveryManager()
                                .DiscoverTests(discoveryCriteria, discoveryEventsHandler);
                        };

                        _jobQueue.QueueJob(job, 0);
                    }
                    catch (Exception ex)
                    {
                        _messageProcessingUnrecoverableError = ex;
                        EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                        EqtTrace.Error(ex);
                        goto case MessageType.AbortTestRun;
                    }
                    break;
                }

            case MessageType.ExecutionInitialize:
                {
                    try
                    {
                        _testHostManagerFactoryReady.Wait();
                        var testInitializeEventsHandler = new TestInitializeEventsHandler(this);
                        var pathToAdditionalExtensions = _dataSerializer.DeserializePayload<IEnumerable<string>>(message);
                        Action job = () =>
                        {
                            EqtTrace.Info("TestRequestHandler.OnMessageReceived: Running job '{0}'.", message.MessageType);
                            _testHostManagerFactory.GetExecutionManager().Initialize(pathToAdditionalExtensions, testInitializeEventsHandler);
                        };
                        _jobQueue.QueueJob(job, 0);
                    }
                    catch (Exception ex)
                    {
                        _messageProcessingUnrecoverableError = ex;
                        EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                        EqtTrace.Error(ex);
                        goto case MessageType.AbortTestRun;
                    }
                    break;
                }

            case MessageType.StartTestExecutionWithSources:
                {
                    try
                    {
                        var testRunEventsHandler = new TestRunEventsHandler(this);
                        _testHostManagerFactoryReady.Wait();
                        var testRunCriteriaWithSources = _dataSerializer.DeserializePayload<TestRunCriteriaWithSources>(message);
                        Action job = () =>
                        {
                            EqtTrace.Info("TestRequestHandler.OnMessageReceived: Running job '{0}'.", message.MessageType);
                            _testHostManagerFactory.GetExecutionManager()
                                .StartTestRun(
                                    testRunCriteriaWithSources.AdapterSourceMap,
                                    testRunCriteriaWithSources.Package,
                                    testRunCriteriaWithSources.RunSettings,
                                    testRunCriteriaWithSources.TestExecutionContext,
                                    GetTestCaseEventsHandler(testRunCriteriaWithSources.RunSettings),
                                    testRunEventsHandler);
                        };
                        _jobQueue.QueueJob(job, 0);
                    }
                    catch (Exception ex)
                    {
                        _messageProcessingUnrecoverableError = ex;
                        EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                        EqtTrace.Error(ex);
                        goto case MessageType.AbortTestRun;
                    }
                    break;
                }

            case MessageType.StartTestExecutionWithTests:
                {
                    try
                    {
                        var testRunEventsHandler = new TestRunEventsHandler(this);
                        _testHostManagerFactoryReady.Wait();
                        var testRunCriteriaWithTests =
                            _dataSerializer.DeserializePayload<TestRunCriteriaWithTests>(message);

                        Action job = () =>
                        {
                            EqtTrace.Info("TestRequestHandler.OnMessageReceived: Running job '{0}'.", message.MessageType);
                            _testHostManagerFactory.GetExecutionManager()
                                .StartTestRun(
                                    testRunCriteriaWithTests.Tests,
                                    testRunCriteriaWithTests.Package,
                                    testRunCriteriaWithTests.RunSettings,
                                    testRunCriteriaWithTests.TestExecutionContext,
                                    GetTestCaseEventsHandler(testRunCriteriaWithTests.RunSettings),
                                    testRunEventsHandler);
                        };
                        _jobQueue.QueueJob(job, 0);
                    }
                    catch (Exception ex)
                    {
                        _messageProcessingUnrecoverableError = ex;
                        EqtTrace.Error("Failed processing message {0}, aborting test run.", message.MessageType);
                        EqtTrace.Error(ex);
                        goto case MessageType.AbortTestRun;
                    }
                    break;
                }

            case MessageType.CancelTestRun:
                _jobQueue.Pause();
                _testHostManagerFactoryReady.Wait();
                _testHostManagerFactory.GetExecutionManager().Cancel(new TestRunEventsHandler(this));
                break;

            case MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback:
                _onLaunchAdapterProcessWithDebuggerAttachedAckReceived?.Invoke(message);
                break;

            case MessageType.AttachDebuggerCallback:
                _onAttachDebuggerAckRecieved?.Invoke(message);
                break;

            case MessageType.AbortTestRun:
                try
                {
                    _jobQueue.Pause();
                    _testHostManagerFactoryReady.Wait();
                    _testHostManagerFactory.GetExecutionManager().Abort(new TestRunEventsHandler(this));
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("Failed processing message {0}. Stopping communication.", message.MessageType);
                    EqtTrace.Error(ex);
                    _sessionCompleted.Set();
                    Close();
                }
                break;

            case MessageType.SessionEnd:
                {
                    EqtTrace.Info("Session End message received from server. Closing the connection.");
                    _sessionCompleted.Set();
                    Close();
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
        _channel.Send(data);
    }
}