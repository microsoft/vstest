// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using TranslationLayerResources = Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Resources.Resources;

    /// <summary>
    /// Vstest console request sender for sending requests to vstest.console.exe
    /// </summary>
    internal class VsTestConsoleRequestSender : ITranslationLayerRequestSender
    {
        /// <summary>
        /// The minimum protocol version that has test session support.
        /// </summary>
        private const int MinimumProtocolVersionWithTestSessionSupport = 5;

        private readonly ICommunicationManager communicationManager;

        private readonly IDataSerializer dataSerializer;
        private readonly ITestPlatformEventSource testPlatformEventSource;

        private readonly ManualResetEvent handShakeComplete = new ManualResetEvent(false);

        private bool handShakeSuccessful = false;

        private int protocolVersion = 5;

        /// <summary>
        /// Used to cancel blocking tasks associated with the vstest.console process.
        /// </summary>
        private CancellationTokenSource processExitCancellationTokenSource;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleRequestSender"/> class.
        /// </summary>
        public VsTestConsoleRequestSender()
            : this(
                  new SocketCommunicationManager(),
                  JsonDataSerializer.Instance,
                  TestPlatformEventSource.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleRequestSender"/> class.
        /// </summary>
        /// 
        /// <param name="communicationManager">The communication manager.</param>
        /// <param name="dataSerializer">The data serializer.</param>
        /// <param name="testPlatformEventSource">The test platform event source.</param>
        internal VsTestConsoleRequestSender(
            ICommunicationManager communicationManager,
            IDataSerializer dataSerializer,
            ITestPlatformEventSource testPlatformEventSource)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
            this.testPlatformEventSource = testPlatformEventSource;
        }

        #endregion

        #region ITranslationLayerRequestSender

        /// <inheritdoc/>
        public int InitializeCommunication()
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunication: Started.");
            }

            this.processExitCancellationTokenSource = new CancellationTokenSource();
            this.handShakeSuccessful = false;
            this.handShakeComplete.Reset();
            int port = -1;
            try
            {
                port = this.communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0)).Port;
                this.communicationManager.AcceptClientAsync();

                Task.Run(() =>
                {
                    this.communicationManager.WaitForClientConnection(Timeout.Infinite);
                    this.handShakeSuccessful = this.HandShakeWithVsTestConsole();
                    this.handShakeComplete.Set();
                });
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "VsTestConsoleRequestSender.InitializeCommunication: Error initializing communication with VstestConsole: {0}",
                    ex);
                this.handShakeComplete.Set();
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunication: Ended.");
            }

            return port;
        }

        /// <inheritdoc/>
        public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
        {
            var waitSucess = this.handShakeComplete.WaitOne(clientConnectionTimeout);
            return waitSucess && this.handShakeSuccessful;
        }

        /// <inheritdoc/>
        public async Task<int> InitializeCommunicationAsync(int clientConnectionTimeout)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"VsTestConsoleRequestSender.InitializeCommunicationAsync: Started with client connection timeout {clientConnectionTimeout} milliseconds.");
            }

            this.processExitCancellationTokenSource = new CancellationTokenSource();
            this.handShakeSuccessful = false;
            this.handShakeComplete.Reset();
            int port = -1;
            try
            {
                port = this.communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0)).Port;
                var timeoutSource = new CancellationTokenSource(clientConnectionTimeout);
                await Task.Run(() =>
                    this.communicationManager.AcceptClientAsync(), timeoutSource.Token).ConfigureAwait(false);

                this.handShakeSuccessful = await this.HandShakeWithVsTestConsoleAsync().ConfigureAwait(false);
                this.handShakeComplete.Set();
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "VsTestConsoleRequestSender.InitializeCommunicationAsync: Error initializing communication with VstestConsole: {0}",
                    ex);
                this.handShakeComplete.Set();
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunicationAsync: Ended.");
            }

            return this.handShakeSuccessful ? port : -1;
        }

        /// <inheritdoc/>
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"VsTestConsoleRequestSender.InitializeExtensions: Initializing extensions with additional extensions path {string.Join(",", pathToAdditionalExtensions.ToList())}.");
            }

            this.communicationManager.SendMessage(
                MessageType.ExtensionsInitialize,
                pathToAdditionalExtensions,
                this.protocolVersion);
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 eventHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.DiscoverTests: Starting test discovery.");
            }

            this.SendMessageAndListenAndReportTestCases(
                sources,
                runSettings,
                options,
                testSessionInfo,
                eventHandler);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 eventHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.DiscoverTestsAsync: Starting test discovery.");
            }

            await this.SendMessageAndListenAndReportTestCasesAsync(
                sources,
                runSettings,
                options,
                testSessionInfo,
                eventHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void StartTestRun(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRun: Starting test run.");
            }

            this.SendMessageAndListenAndReportTestResults(
                MessageType.TestRunAllSourcesWithDefaultHost,
                new TestRunRequestPayload()
                {
                    Sources = sources.ToList(),
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                null);
        }

        /// <inheritdoc/>
        public async Task StartTestRunAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunAsync: Starting test run.");
            }

            await this.SendMessageAndListenAndReportTestResultsAsync(
                MessageType.TestRunAllSourcesWithDefaultHost,
                new TestRunRequestPayload()
                {
                    Sources = sources.ToList(),
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                null).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void StartTestRun(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRun: Starting test run.");
            }

            this.SendMessageAndListenAndReportTestResults(
                MessageType.TestRunAllSourcesWithDefaultHost,
                new TestRunRequestPayload()
                {
                    TestCases = testCases.ToList(),
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                null);
        }

        /// <inheritdoc/>
        public async Task StartTestRunAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunAsync: Starting test run.");
            }

            await this.SendMessageAndListenAndReportTestResultsAsync(
                MessageType.TestRunAllSourcesWithDefaultHost,
                new TestRunRequestPayload()
                {
                    TestCases = testCases.ToList(),
                    RunSettings = runSettings,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                null).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void StartTestRunWithCustomHost(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler,
            ITestHostLauncher customHostLauncher)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHost: Starting test run.");
            }

            this.SendMessageAndListenAndReportTestResults(
                MessageType.GetTestRunnerProcessStartInfoForRunAll,
                new TestRunRequestPayload()
                {
                    Sources = sources.ToList(),
                    RunSettings = runSettings,
                    DebuggingEnabled = customHostLauncher.IsDebug,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                customHostLauncher);
        }

        /// <inheritdoc/>
        public async Task StartTestRunWithCustomHostAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler,
            ITestHostLauncher customHostLauncher)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHostAsync: Starting test run.");
            }

            await this.SendMessageAndListenAndReportTestResultsAsync(
                MessageType.GetTestRunnerProcessStartInfoForRunAll,
                new TestRunRequestPayload()
                {
                    Sources = sources.ToList(),
                    RunSettings = runSettings,
                    DebuggingEnabled = customHostLauncher.IsDebug,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                customHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void StartTestRunWithCustomHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler,
            ITestHostLauncher customHostLauncher)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHost: Starting test run.");
            }

            this.SendMessageAndListenAndReportTestResults(
                MessageType.GetTestRunnerProcessStartInfoForRunSelected,
                new TestRunRequestPayload
                {
                    TestCases = testCases.ToList(),
                    RunSettings = runSettings,
                    DebuggingEnabled = customHostLauncher.IsDebug,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                customHostLauncher);
        }

        /// <inheritdoc/>
        public async Task StartTestRunWithCustomHostAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler runEventsHandler,
            ITestHostLauncher customHostLauncher)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestRunWithCustomHostAsync: Starting test run.");
            }

            await this.SendMessageAndListenAndReportTestResultsAsync(
                MessageType.GetTestRunnerProcessStartInfoForRunSelected,
                new TestRunRequestPayload()
                {
                    TestCases = testCases.ToList(),
                    RunSettings = runSettings,
                    DebuggingEnabled = customHostLauncher.IsDebug,
                    TestPlatformOptions = options,
                    TestSessionInfo = testSessionInfo
                },
                runEventsHandler,
                customHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public TestSessionInfo StartTestSession(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler,
            ITestHostLauncher testHostLauncher)
        {
            // Make sure vstest.console knows how to handle start/stop test session messages.
            // Bail out if it doesn't, otherwise we'll hang waiting for a reply from the console
            // that will never come.
            if (this.protocolVersion < MinimumProtocolVersionWithTestSessionSupport)
            {
                eventsHandler?.HandleStartTestSessionComplete(null);
                return null;
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestSession: Starting test session.");
            }

            try
            {
                var payload = new StartTestSessionPayload
                {
                    // TODO (copoiena): When sharing the test host between test discovery and test
                    // execution, should we use the test host launcher to launch it ? What side
                    // effects does this have ?
                    //
                    // This is useful for profiling and maybe for launching hosts other than the
                    // ones managed by us (i.e., the default host and the dotnet host), examples
                    // including UWP and other hosts that don't implement the ITestRuntimeProvider2
                    // interface and/or are not aware of the possibility of attaching to an already
                    // running process.
                    Sources = sources,
                    RunSettings = runSettings,
                    HasCustomHostLauncher = testHostLauncher != null,
                    IsDebuggingEnabled = (testHostLauncher != null)
                        ? testHostLauncher.IsDebug
                        : false,
                    TestPlatformOptions = options
                };

                this.communicationManager.SendMessage(
                    MessageType.StartTestSession,
                    payload,
                    this.protocolVersion);

                while (true)
                {
                    var message = this.TryReceiveMessage();

                    switch (message.MessageType)
                    {
                        case MessageType.StartTestSessionCallback:
                            var ackPayload = this.dataSerializer
                                .DeserializePayload<StartTestSessionAckPayload>(message);
                            eventsHandler?.HandleStartTestSessionComplete(
                                ackPayload.TestSessionInfo);
                            return ackPayload.TestSessionInfo;

                        case MessageType.CustomTestHostLaunch:
                            this.HandleCustomHostLaunch(testHostLauncher, message);
                            break;

                        case MessageType.EditorAttachDebugger:
                            this.AttachDebuggerToProcess(testHostLauncher, message);
                            break;

                        case MessageType.TestMessage:
                            var testMessagePayload = this.dataSerializer
                                .DeserializePayload<TestMessagePayload>(message);
                            eventsHandler?.HandleLogMessage(
                                testMessagePayload.MessageLevel,
                                testMessagePayload.Message);
                            break;

                        default:
                            EqtTrace.Warning(
                                "VsTestConsoleRequestSender.StartTestSession: Unexpected message received: {0}",
                                message.MessageType);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error(
                    "Aborting StartTestSession operation due to error: {0}",
                    exception);
                eventsHandler?.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedStartTestSession);

                eventsHandler?.HandleStartTestSessionComplete(null);
            }
            finally
            {
                this.testPlatformEventSource.TranslationLayerStartTestSessionStop();
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<TestSessionInfo> StartTestSessionAsync(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler,
            ITestHostLauncher testHostLauncher)
        {
            // Make sure vstest.console knows how to handle start/stop test session messages.
            // Bail out if it doesn't, otherwise we'll hang waiting for a reply from the console
            // that will never come.
            if (this.protocolVersion < MinimumProtocolVersionWithTestSessionSupport)
            {
                eventsHandler?.HandleStartTestSessionComplete(null);
                return await Task.FromResult((TestSessionInfo)null);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StartTestSession: Starting test session.");
            }

            try
            {
                var payload = new StartTestSessionPayload
                {
                    // TODO (copoiena): When sharing the test host between test discovery and test
                    // execution, should we use the test host launcher to launch it ? What side
                    // effects does this have ?
                    //
                    // This is useful for profiling and maybe for launching hosts other than the
                    // ones managed by us (i.e., the default host and the dotnet host), examples
                    // including UWP and other hosts that don't implement the ITestRuntimeProvider2
                    // interface and/or are not aware of the possibility of attaching to an already
                    // running process.
                    Sources = sources,
                    RunSettings = runSettings,
                    HasCustomHostLauncher = testHostLauncher != null,
                    IsDebuggingEnabled = (testHostLauncher != null) ? testHostLauncher.IsDebug : false,
                    TestPlatformOptions = options
                };

                this.communicationManager.SendMessage(
                    MessageType.StartTestSession,
                    payload,
                    this.protocolVersion);

                while (true)
                {
                    var message = await this.TryReceiveMessageAsync().ConfigureAwait(false);

                    switch (message.MessageType)
                    {
                        case MessageType.StartTestSessionCallback:
                            var ackPayload = this.dataSerializer
                                .DeserializePayload<StartTestSessionAckPayload>(message);
                            eventsHandler?.HandleStartTestSessionComplete(
                                ackPayload.TestSessionInfo);
                            return ackPayload.TestSessionInfo;

                        case MessageType.CustomTestHostLaunch:
                            this.HandleCustomHostLaunch(testHostLauncher, message);
                            break;

                        case MessageType.EditorAttachDebugger:
                            this.AttachDebuggerToProcess(testHostLauncher, message);
                            break;

                        case MessageType.TestMessage:
                            var testMessagePayload = this.dataSerializer
                                .DeserializePayload<TestMessagePayload>(message);
                            eventsHandler?.HandleLogMessage(
                                testMessagePayload.MessageLevel,
                                testMessagePayload.Message);
                            break;

                        default:
                            EqtTrace.Warning(
                                "VsTestConsoleRequestSender.StartTestSession: Unexpected message received: {0}",
                                message.MessageType);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting StartTestSession operation due to error: {0}", exception);
                eventsHandler?.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedStartTestSession);

                eventsHandler?.HandleStartTestSessionComplete(null);
            }
            finally
            {
                this.testPlatformEventSource.TranslationLayerStartTestSessionStop();
            }

            return null;
        }

        /// <inheritdoc/>
        public bool StopTestSession(
            TestSessionInfo testSessionInfo,
            ITestSessionEventsHandler eventsHandler)
        {
            // Make sure vstest.console knows how to handle start/stop test session messages.
            // Bail out if it doesn't, otherwise we'll hang waiting for a reply from the console
            // that will never come.
            if (this.protocolVersion < MinimumProtocolVersionWithTestSessionSupport)
            {
                eventsHandler?.HandleStopTestSessionComplete(testSessionInfo, false);
                return false;
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StopTestSession: Stop test session.");
            }

            // Due to various considertaions it is possible to end up with a null test session
            // after doing the start test session call. However, we should filter out requests
            // to stop such a session as soon as possible, at the request sender level.
            //
            // We do this here instead of on the wrapper level in order to benefit from the
            // testplatform events being fired still.
            if (testSessionInfo == null)
            {
                this.testPlatformEventSource.TranslationLayerStopTestSessionStop();
                return true;
            }

            try
            {
                this.communicationManager.SendMessage(
                    MessageType.StopTestSession,
                    testSessionInfo,
                    this.protocolVersion);

                while (true)
                {
                    var message = this.TryReceiveMessage();

                    switch (message.MessageType)
                    {
                        case MessageType.StopTestSessionCallback:
                            var payload = this.dataSerializer.DeserializePayload<StopTestSessionAckPayload>(message);
                            eventsHandler?.HandleStopTestSessionComplete(payload.TestSessionInfo, payload.IsStopped);
                            return payload.IsStopped;

                        case MessageType.TestMessage:
                            var testMessagePayload = this.dataSerializer
                                .DeserializePayload<TestMessagePayload>(message);
                            eventsHandler?.HandleLogMessage(
                                testMessagePayload.MessageLevel,
                                testMessagePayload.Message);
                            break;

                        default:
                            EqtTrace.Warning(
                                "VsTestConsoleRequestSender.StopTestSession: Unexpected message received: {0}",
                                message.MessageType);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error(
                    "Aborting StopTestSession operation for id {0} due to error: {1}",
                    testSessionInfo?.Id,
                    exception);
                eventsHandler?.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedStopTestSession);

                eventsHandler?.HandleStopTestSessionComplete(testSessionInfo, false);
            }
            finally
            {
                this.testPlatformEventSource.TranslationLayerStopTestSessionStop();
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> StopTestSessionAsync(
            TestSessionInfo testSessionInfo,
            ITestSessionEventsHandler eventsHandler)
        {
            // Make sure vstest.console knows how to handle start/stop test session messages.
            // Bail out if it doesn't, otherwise we'll hang waiting for a reply from the console
            // that will never come.
            if (this.protocolVersion < MinimumProtocolVersionWithTestSessionSupport)
            {
                eventsHandler?.HandleStopTestSessionComplete(testSessionInfo, false);
                return await Task.FromResult(false);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.StopTestSession: Stop test session.");
            }

            // Due to various considertaions it is possible to end up with a null test session
            // after doing the start test session call. However, we should filter out requests
            // to stop such a session as soon as possible, at the request sender level.
            //
            // We do this here instead of on the wrapper level in order to benefit from the
            // testplatform events being fired still.
            if (testSessionInfo == null)
            {
                this.testPlatformEventSource.TranslationLayerStopTestSessionStop();
                return true;
            }

            try
            {
                this.communicationManager.SendMessage(
                    MessageType.StopTestSession,
                    testSessionInfo,
                    this.protocolVersion);

                while (true)
                {
                    var message = await this.TryReceiveMessageAsync().ConfigureAwait(false);

                    switch (message.MessageType)
                    {
                        case MessageType.StopTestSessionCallback:
                            var payload = this.dataSerializer.DeserializePayload<StopTestSessionAckPayload>(message);
                            eventsHandler?.HandleStopTestSessionComplete(payload.TestSessionInfo, payload.IsStopped);
                            return payload.IsStopped;

                        case MessageType.TestMessage:
                            var testMessagePayload = this.dataSerializer
                                .DeserializePayload<TestMessagePayload>(message);
                            eventsHandler?.HandleLogMessage(
                                testMessagePayload.MessageLevel,
                                testMessagePayload.Message);
                            break;

                        default:
                            EqtTrace.Warning(
                                "VsTestConsoleRequestSender.StopTestSession: Unexpected message received: {0}",
                                message.MessageType);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error(
                    "Aborting StopTestSession operation for id {0} due to error: {1}",
                    testSessionInfo?.Id,
                    exception);
                eventsHandler?.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedStopTestSession);

                eventsHandler?.HandleStopTestSessionComplete(testSessionInfo, false);
            }
            finally
            {
                this.testPlatformEventSource.TranslationLayerStopTestSessionStop();
            }

            return false;
        }

        /// <inheritdoc/>
        public void CancelTestRun()
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.CancelTestRun: Canceling test run.");
            }

            this.communicationManager.SendMessage(MessageType.CancelTestRun);
        }

        /// <inheritdoc/>
        public void AbortTestRun()
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.AbortTestRun: Aborting test run.");
            }

            this.communicationManager.SendMessage(MessageType.AbortTestRun);
        }

        /// <inheritdoc/>
        public void CancelDiscovery()
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleRequestSender.CancelDiscovery: Canceling test discovery.");
            }

            this.communicationManager.SendMessage(MessageType.CancelDiscovery);
        }

        /// <inheritdoc/>
        public void OnProcessExited()
        {
            this.processExitCancellationTokenSource.Cancel();
        }

        /// <inheritdoc/>
        public void Close()
        {
            this.Dispose();
        }

        /// <inheritdoc/>
        public void EndSession()
        {
            this.communicationManager.SendMessage(MessageType.SessionEnd);
        }

        /// <inheritdoc/>
        public Task ProcessTestRunAttachmentsAsync(
            IEnumerable<AttachmentSet> attachments,
            bool collectMetrics,
            ITestRunAttachmentsProcessingEventsHandler testSessionEventsHandler,
            CancellationToken cancellationToken)
        {
            return this.SendMessageAndListenAndReportAttachmentsProcessingResultAsync(
                attachments,
                collectMetrics,
                testSessionEventsHandler,
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.communicationManager?.StopServer();
        }

        #endregion

        private bool HandShakeWithVsTestConsole()
        {
            var success = false;
            var message = this.communicationManager.ReceiveMessage();

            if (message.MessageType == MessageType.SessionConnected)
            {
                this.communicationManager.SendMessage(
                    MessageType.VersionCheck,
                    this.protocolVersion);

                message = this.communicationManager.ReceiveMessage();

                if (message.MessageType == MessageType.VersionCheck)
                {
                    this.protocolVersion = this.dataSerializer
                        .DeserializePayload<int>(message);
                    success = true;
                }
                else if (message.MessageType == MessageType.ProtocolError)
                {
                    // TODO : Payload for ProtocolError needs to finalized.
                    EqtTrace.Error(
                        "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: Version Check failed. ProtolError was received from the runner");
                }
                else
                {
                    EqtTrace.Error(
                        "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: VersionCheck Message Expected but different message received: Received MessageType: {0}",
                        message.MessageType);
                }
            }
            else
            {
                EqtTrace.Error(
                    "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: SessionConnected Message Expected but different message received: Received MessageType: {0}",
                    message.MessageType);
            }

            return success;
        }

        private async Task<bool> HandShakeWithVsTestConsoleAsync()
        {
            var success = false;
            var message = await this.communicationManager.ReceiveMessageAsync(
                this.processExitCancellationTokenSource.Token).ConfigureAwait(false);

            if (message.MessageType == MessageType.SessionConnected)
            {
                this.communicationManager.SendMessage(
                    MessageType.VersionCheck,
                    this.protocolVersion);

                message = await this.communicationManager.ReceiveMessageAsync(
                    this.processExitCancellationTokenSource.Token).ConfigureAwait(false);

                if (message.MessageType == MessageType.VersionCheck)
                {
                    this.protocolVersion = this.dataSerializer.DeserializePayload<int>(message);
                    success = true;
                }
                else if (message.MessageType == MessageType.ProtocolError)
                {
                    // TODO : Payload for ProtocolError needs to finalized.
                    EqtTrace.Error(
                        "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: Version Check failed. ProtolError was received from the runner");
                }
                else
                {
                    EqtTrace.Error(
                        "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: VersionCheck Message Expected but different message received: Received MessageType: {0}",
                        message.MessageType);
                }
            }
            else
            {
                EqtTrace.Error(
                    "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: SessionConnected Message Expected but different message received: Received MessageType: {0}",
                    message.MessageType);
            }

            return success;
        }

        private void SendMessageAndListenAndReportTestCases(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 eventHandler)
        {
            try
            {
                this.communicationManager.SendMessage(
                    MessageType.StartDiscovery,
                    new DiscoveryRequestPayload()
                    {
                        Sources = sources,
                        RunSettings = runSettings,
                        TestPlatformOptions = options,
                        TestSessionInfo = testSessionInfo
                    },
                    this.protocolVersion);
                var isDiscoveryComplete = false;

                // Cycle through the messages that vstest.console sends.
                // Currently each operation is not a separate task since it should not take that
                // much time to complete.
                //
                // This is just a notification.
                while (!isDiscoveryComplete)
                {
                    var message = this.TryReceiveMessage();

                    if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                    {
                        var testCases = this.dataSerializer
                            .DeserializePayload<IEnumerable<TestCase>>(message);

                        eventHandler.HandleDiscoveredTests(testCases);
                    }
                    else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                    {
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info(
                                "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestCases: Discovery complete.");
                        }

                        var discoveryCompletePayload =
                            this.dataSerializer
                                .DeserializePayload<DiscoveryCompletePayload>(message);

                        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(
                            discoveryCompletePayload.TotalTests,
                            discoveryCompletePayload.IsAborted);

                        // Adding metrics from vstest.console.
                        discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;

                        eventHandler.HandleDiscoveryComplete(
                            discoveryCompleteEventArgs,
                            discoveryCompletePayload.LastDiscoveredTests);
                        isDiscoveryComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer
                            .DeserializePayload<TestMessagePayload>(message);
                        eventHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting Test Discovery Operation: {0}", exception);
                eventHandler.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedTestsDiscovery);
                var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
                eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);

                // Earlier we were closing the connection with vstest.console in case of exceptions.
                // Removing that code because vstest.console might be in a healthy state and letting
                // the client know of the error, so that the TL can wait for the next instruction
                // from the client itself.
                // Also, connection termination might not kill the process which could result in
                // files being locked by testhost.
            }

            this.testPlatformEventSource.TranslationLayerDiscoveryStop();
        }

        private async Task SendMessageAndListenAndReportTestCasesAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 eventHandler)
        {
            try
            {
                this.communicationManager.SendMessage(
                    MessageType.StartDiscovery,
                    new DiscoveryRequestPayload()
                    {
                        Sources = sources,
                        RunSettings = runSettings,
                        TestPlatformOptions = options,
                        TestSessionInfo = testSessionInfo
                    },
                    this.protocolVersion);
                var isDiscoveryComplete = false;

                // Cycle through the messages that vstest.console sends.
                // Currently each operation is not a separate task since it should not take that
                // much time to complete.
                //
                // This is just a notification.
                while (!isDiscoveryComplete)
                {
                    var message = await this.TryReceiveMessageAsync().ConfigureAwait(false);

                    if (string.Equals(MessageType.TestCasesFound, message.MessageType))
                    {
                        var testCases = this.dataSerializer
                            .DeserializePayload<IEnumerable<TestCase>>(message);

                        eventHandler.HandleDiscoveredTests(testCases);
                    }
                    else if (string.Equals(MessageType.DiscoveryComplete, message.MessageType))
                    {
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info(
                                "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestCasesAsync: Discovery complete.");
                        }

                        var discoveryCompletePayload =
                            this.dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);

                        var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(
                            discoveryCompletePayload.TotalTests,
                            discoveryCompletePayload.IsAborted);

                        // Adding Metrics from VsTestConsole
                        discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;

                        eventHandler.HandleDiscoveryComplete(
                            discoveryCompleteEventArgs,
                            discoveryCompletePayload.LastDiscoveredTests);
                        isDiscoveryComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer
                            .DeserializePayload<TestMessagePayload>(message);
                        eventHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting Test Discovery Operation: {0}", exception);

                eventHandler.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedTestsDiscovery);

                var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(-1, true);
                eventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);

                // Earlier we were closing the connection with vstest.console in case of exceptions.
                // Removing that code because vstest.console might be in a healthy state and letting
                // the client know of the error, so that the TL can wait for the next instruction
                // from the client itself.
                // Also, connection termination might not kill the process which could result in
                // files being locked by testhost.
            }

            this.testPlatformEventSource.TranslationLayerDiscoveryStop();
        }

        private void SendMessageAndListenAndReportTestResults(
            string messageType,
            object payload,
            ITestRunEventsHandler eventHandler,
            ITestHostLauncher customHostLauncher)
        {
            try
            {
                this.communicationManager.SendMessage(messageType, payload, this.protocolVersion);
                var isTestRunComplete = false;

                // Cycle through the messages that vstest.console sends.
                // Currently each operation is not a separate task since it should not take that
                // much time to complete.
                //
                // This is just a notification.
                while (!isTestRunComplete)
                {
                    var message = this.TryReceiveMessage();

                    if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                    {
                        var testRunChangedArgs = this.dataSerializer
                            .DeserializePayload<TestRunChangedEventArgs>(
                            message);
                        eventHandler.HandleTestRunStatsChange(testRunChangedArgs);
                    }
                    else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                    {
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info(
                                "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestResults: Execution complete.");
                        }

                        var testRunCompletePayload = this.dataSerializer
                            .DeserializePayload<TestRunCompletePayload>(message);

                        eventHandler.HandleTestRunComplete(
                            testRunCompletePayload.TestRunCompleteArgs,
                            testRunCompletePayload.LastRunTests,
                            testRunCompletePayload.RunAttachments,
                            testRunCompletePayload.ExecutorUris);
                        isTestRunComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer
                            .DeserializePayload<TestMessagePayload>(message);
                        eventHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                    else if (string.Equals(MessageType.CustomTestHostLaunch, message.MessageType))
                    {
                        this.HandleCustomHostLaunch(customHostLauncher, message);
                    }
                    else if (string.Equals(MessageType.EditorAttachDebugger, message.MessageType))
                    {
                        this.AttachDebuggerToProcess(customHostLauncher, message);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting Test Run Operation: {0}", exception);
                eventHandler.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedTestsRun);
                var completeArgs = new TestRunCompleteEventArgs(
                    null, false, true, exception, null, TimeSpan.Zero);
                eventHandler.HandleTestRunComplete(completeArgs, null, null, null);

                // Earlier we were closing the connection with vstest.console in case of exceptions.
                // Removing that code because vstest.console might be in a healthy state and letting
                // the client know of the error, so that the TL can wait for the next instruction
                // from the client itself.
                // Also, connection termination might not kill the process which could result in
                // files being locked by testhost.
            }

            this.testPlatformEventSource.TranslationLayerExecutionStop();
        }

        private async Task SendMessageAndListenAndReportTestResultsAsync(
            string messageType,
            object payload,
            ITestRunEventsHandler eventHandler,
            ITestHostLauncher customHostLauncher)
        {
            try
            {
                this.communicationManager.SendMessage(messageType, payload, this.protocolVersion);
                var isTestRunComplete = false;

                // Cycle through the messages that vstest.console sends.
                // Currently each operation is not a separate task since it should not take that
                // much time to complete.
                //
                // This is just a notification.
                while (!isTestRunComplete)
                {
                    var message = await this.TryReceiveMessageAsync().ConfigureAwait(false);

                    if (string.Equals(MessageType.TestRunStatsChange, message.MessageType))
                    {
                        var testRunChangedArgs = this.dataSerializer
                            .DeserializePayload<TestRunChangedEventArgs>(message);
                        eventHandler.HandleTestRunStatsChange(testRunChangedArgs);
                    }
                    else if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
                    {
                        if (EqtTrace.IsInfoEnabled)
                        {
                            EqtTrace.Info(
                                "VsTestConsoleRequestSender.SendMessageAndListenAndReportTestResultsAsync: Execution complete.");
                        }

                        var testRunCompletePayload = this.dataSerializer
                            .DeserializePayload<TestRunCompletePayload>(message);

                        eventHandler.HandleTestRunComplete(
                            testRunCompletePayload.TestRunCompleteArgs,
                            testRunCompletePayload.LastRunTests,
                            testRunCompletePayload.RunAttachments,
                            testRunCompletePayload.ExecutorUris);
                        isTestRunComplete = true;
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = this.dataSerializer
                            .DeserializePayload<TestMessagePayload>(message);
                        eventHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                    else if (string.Equals(MessageType.CustomTestHostLaunch, message.MessageType))
                    {
                        this.HandleCustomHostLaunch(customHostLauncher, message);
                    }
                    else if (string.Equals(MessageType.EditorAttachDebugger, message.MessageType))
                    {
                        this.AttachDebuggerToProcess(customHostLauncher, message);
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting Test Run Operation: {0}", exception);
                eventHandler.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedTestsRun);
                var completeArgs = new TestRunCompleteEventArgs(
                    null, false, true, exception, null, TimeSpan.Zero);
                eventHandler.HandleTestRunComplete(completeArgs, null, null, null);

                // Earlier we were closing the connection with vstest.console in case of exceptions.
                // Removing that code because vstest.console might be in a healthy state and letting
                // the client know of the error, so that the TL can wait for the next instruction
                // from the client itself.
                // Also, connection termination might not kill the process which could result in
                // files being locked by testhost.
            }

            this.testPlatformEventSource.TranslationLayerExecutionStop();
        }

        private async Task SendMessageAndListenAndReportAttachmentsProcessingResultAsync(
            IEnumerable<AttachmentSet> attachments,
            bool collectMetrics,
            ITestRunAttachmentsProcessingEventsHandler eventHandler,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = new TestRunAttachmentsProcessingPayload
                {
                    Attachments = attachments,
                    CollectMetrics = collectMetrics
                };

                this.communicationManager.SendMessage(
                    MessageType.TestRunAttachmentsProcessingStart,
                    payload);
                var isTestRunAttachmentsProcessingComplete = false;

                using (cancellationToken.Register(() =>
                    this.communicationManager.SendMessage(MessageType.TestRunAttachmentsProcessingCancel)))
                {
                    // Cycle through the messages that vstest.console sends.
                    // Currently each operation is not a separate task since it should not take that
                    // much time to complete.
                    //
                    // This is just a notification.
                    while (!isTestRunAttachmentsProcessingComplete)
                    {
                        var message = await this.TryReceiveMessageAsync().ConfigureAwait(false);

                        if (string.Equals(
                            MessageType.TestRunAttachmentsProcessingComplete,
                            message.MessageType))
                        {
                            if (EqtTrace.IsInfoEnabled)
                            {
                                EqtTrace.Info(
                                    "VsTestConsoleRequestSender.SendMessageAndListenAndReportAttachments: Process complete.");
                            }

                            var testRunAttachmentsProcessingCompletePayload = this.dataSerializer
                                .DeserializePayload<TestRunAttachmentsProcessingCompletePayload>(message);

                            eventHandler.HandleTestRunAttachmentsProcessingComplete(
                                testRunAttachmentsProcessingCompletePayload.AttachmentsProcessingCompleteEventArgs,
                                testRunAttachmentsProcessingCompletePayload.Attachments);
                            isTestRunAttachmentsProcessingComplete = true;
                        }
                        else if (string.Equals(
                            MessageType.TestRunAttachmentsProcessingProgress,
                            message.MessageType))
                        {
                            var testRunAttachmentsProcessingProgressPayload = this.dataSerializer
                                .DeserializePayload<TestRunAttachmentsProcessingProgressPayload>(message);
                            eventHandler.HandleTestRunAttachmentsProcessingProgress(
                                testRunAttachmentsProcessingProgressPayload.AttachmentsProcessingProgressEventArgs);
                        }
                        else if (string.Equals(MessageType.TestMessage, message.MessageType))
                        {
                            var testMessagePayload = this.dataSerializer
                                .DeserializePayload<TestMessagePayload>(message);
                            eventHandler.HandleLogMessage(
                                testMessagePayload.MessageLevel,
                                testMessagePayload.Message);
                        }
                        else
                        {
                            EqtTrace.Warning(
                                $"VsTestConsoleRequestSender.SendMessageAndListenAndReportAttachments: Unexpected message received {message.MessageType}.");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("Aborting Test Session End Operation: {0}", exception);
                eventHandler.HandleLogMessage(
                    TestMessageLevel.Error,
                    TranslationLayerResources.AbortedTestRunAttachmentsProcessing);               
                eventHandler.HandleTestRunAttachmentsProcessingComplete(
                    new TestRunAttachmentsProcessingCompleteEventArgs(false, exception),
                    null);

                // Earlier we were closing the connection with vstest.console in case of exceptions.
                // Removing that code because vstest.console might be in a healthy state and letting
                // the client know of the error, so that the TL can wait for the next instruction
                // from the client itself.
                // Also, connection termination might not kill the process which could result in
                // files being locked by testhost.
            }
            finally
            {
                this.testPlatformEventSource.TranslationLayerTestRunAttachmentsProcessingStop();
            }
        }

        private Message TryReceiveMessage()
        {
            Message message = null;
            var receiverMessageTask = this.communicationManager.ReceiveMessageAsync(
                this.processExitCancellationTokenSource.Token);
            receiverMessageTask.Wait();
            message = receiverMessageTask.Result;

            if (message == null)
            {
                throw new TransationLayerException(
                    TranslationLayerResources.FailedToReceiveMessage);
            }

            return message;
        }

        private async Task<Message> TryReceiveMessageAsync()
        {
            Message message = await this.communicationManager.ReceiveMessageAsync(
                this.processExitCancellationTokenSource.Token).ConfigureAwait(false);

            if (message == null)
            {
                throw new TransationLayerException(
                    TranslationLayerResources.FailedToReceiveMessage);
            }

            return message;
        }

        private void HandleCustomHostLaunch(ITestHostLauncher customHostLauncher, Message message)
        {
            var ackPayload = new CustomHostLaunchAckPayload()
            {
                HostProcessId = -1,
                ErrorMessage = null
            };

            try
            {
                var testProcessStartInfo = this.dataSerializer
                    .DeserializePayload<TestProcessStartInfo>(message);

                ackPayload.HostProcessId = customHostLauncher != null
                    ? customHostLauncher.LaunchTestHost(testProcessStartInfo)
                    : -1;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Error while launching custom host: {0}", ex);

                // Vstest.console will send the abort message properly while cleaning up all the
                // flow, so do not abort here.
                // Let the ack go through and let vstest.console handle the error.
                ackPayload.ErrorMessage = ex.Message;
            }
            finally
            {
                // Always unblock the vstest.console thread which is indefinitely waiting on this
                // ACK.
                this.communicationManager.SendMessage(
                    MessageType.CustomTestHostLaunchCallback,
                    ackPayload,
                    this.protocolVersion);
            }
        }

        private void AttachDebuggerToProcess(ITestHostLauncher customHostLauncher, Message message)
        {
            var ackPayload = new EditorAttachDebuggerAckPayload()
            {
                Attached = false,
                ErrorMessage = null
            };

            try
            {
                var pid = this.dataSerializer.DeserializePayload<int>(message);

                ackPayload.Attached = customHostLauncher is ITestHostLauncher2 launcher
                    ? launcher.AttachDebuggerToProcess(pid)
                    : false;
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "VsTestConsoleRequestSender.AttachDebuggerToProcess: Error while attaching debugger to process: {0}",
                    ex);

                // vstest.console will send the abort message properly while cleaning up all the
                // flow, so do not abort here.
                // Let the ack go through and let vstest.console handle the error.
                ackPayload.ErrorMessage = ex.Message;
            }
            finally
            {
                // Always unblock the vstest.console thread which is indefintitely waiting on this
                // ACK.
                this.communicationManager.SendMessage(
                    MessageType.EditorAttachDebuggerCallback,
                    ackPayload,
                    this.protocolVersion);
            }
        }
    }
}
