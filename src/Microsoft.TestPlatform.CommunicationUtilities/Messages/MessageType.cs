// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    /// <summary>
    /// The message type.
    /// </summary>
    public static class MessageType
    {
        /// <summary>
        /// The session start.
        /// </summary>
        public const string SessionStart = "TestSession.Start";

        /// <summary>
        /// The session end.
        /// </summary>
        public const string SessionEnd = "TestSession.Terminate";

        /// <summary>
        /// The is aborted.
        /// </summary>
        public const string SessionAbort = "TestSession.Abort";

        /// <summary>
        /// The session connected.
        /// </summary>
        public const string SessionConnected = "TestSession.Connected";

        /// <summary>
        /// Test Message
        /// </summary>
        public const string TestMessage = "TestSession.Message";

        /// <summary>
        /// Protocol Version
        /// </summary>
        public const string VersionCheck = "ProtocolVersion";

        /// <summary>
        /// Protocol Error
        /// </summary>
        public const string ProtocolError = "ProtocolError";

        /// <summary>
        /// The session start.
        /// </summary>
        public const string DiscoveryInitialize = "TestDiscovery.Initialize";

        /// <summary>
        /// The discovery started.
        /// </summary>
        public const string StartDiscovery = "TestDiscovery.Start";

        /// <summary>
        /// The test cases found.
        /// </summary>
        public const string TestCasesFound = "TestDiscovery.TestFound";

        /// <summary>
        /// The discovery complete.
        /// </summary>
        public const string DiscoveryComplete = "TestDiscovery.Completed";

        /// <summary>
        /// Cancel Test Discovery
        /// </summary>
        public const string CancelDiscovery = "TestDiscovery.Cancel";

        /// <summary>
        /// The session start.
        /// </summary>
        public const string ExecutionInitialize = "TestExecution.Initialize";

        /// <summary>
        /// Cancel the current test run
        /// </summary>
        public const string CancelTestRun = "TestExecution.Cancel";

        /// <summary>
        /// Cancel the current test run
        /// </summary>
        public const string AbortTestRun = "TestExecution.Abort";

        /// <summary>
        /// Start test execution.
        /// </summary>
        public const string StartTestExecutionWithSources = "TestExecution.StartWithSources";

        /// <summary>
        /// Start test execution.
        /// </summary>
        public const string StartTestExecutionWithTests = "TestExecution.StartWithTests";

        /// <summary>
        /// The test run stats change.
        /// </summary>
        public const string TestRunStatsChange = "TestExecution.StatsChange";

        /// <summary>
        /// The execution complete.
        /// </summary>
        public const string ExecutionComplete = "TestExecution.Completed";

        /// <summary>
        /// The message to get runner process startInfo for run all tests in given sources
        /// </summary>
        public const string GetTestRunnerProcessStartInfoForRunAll = "TestExecution.GetTestRunnerProcessStartInfoForRunAll";

        /// <summary>
        /// The message to get runner process startInfo for run selected tests
        /// </summary>
        public const string GetTestRunnerProcessStartInfoForRunSelected = "TestExecution.GetTestRunnerProcessStartInfoForRunSelected";

        /// <summary>
        /// CustomTestHostLaunch
        /// </summary>
        public const string CustomTestHostLaunch = "TestExecution.CustomTestHostLaunch";

        /// <summary>
        /// Custom Test Host launch callback
        /// </summary>
        public const string CustomTestHostLaunchCallback = "TestExecution.CustomTestHostLaunchCallback";

        /// <summary>
        /// Extensions Initialization
        /// </summary>
        public const string ExtensionsInitialize = "Extensions.Initialize";

        /// <summary>
        /// Start Test Run All Sources
        /// </summary>
        public const string TestRunAllSourcesWithDefaultHost = "TestExecution.RunAllWithDefaultHost";

        /// <summary>
        ///  Start Test Run - Testcases
        /// </summary>
        public const string TestRunSelectedTestCasesDefaultHost = "TestExecution.RunSelectedWithDefaultHost";

        /// <summary>
        /// Launch Adapter Process With DebuggerAttached
        /// </summary>
        public const string LaunchAdapterProcessWithDebuggerAttached = "TestExecution.LaunchAdapterProcessWithDebuggerAttached";

        /// <summary>
        /// Launch Adapter Process With DebuggerAttached
        /// </summary>
        public const string LaunchAdapterProcessWithDebuggerAttachedCallback = "TestExecution.LaunchAdapterProcessWithDebuggerAttachedCallback";

        /// <summary>
        /// Attach debugger to process.
        /// </summary>
        public const string AttachDebugger = "TestExecution.AttachDebugger";

        /// <summary>
        /// Attach debugger to process callback.
        /// </summary>
        public const string AttachDebuggerCallback = "TestExecution.AttachDebuggerCallback";

        /// <summary>
        /// Attach debugger to process.
        /// </summary>
        public const string EditorAttachDebugger = "TestExecution.EditorAttachDebugger";

        /// <summary>
        /// Attach debugger to process callback.
        /// </summary>
        public const string EditorAttachDebuggerCallback = "TestExecution.EditorAttachDebuggerCallback";

        /// <summary>
        /// Data Collection Message
        /// </summary>
        public const string DataCollectionMessage = "DataCollection.SendMessage";

        #region DataCollector messages

        /// <summary>
        /// Event message type sent to datacollector process right after test host process has started.
        /// </summary>
        public const string TestHostLaunched = "DataCollection.TestHostLaunched";

        /// <summary>
        /// Event message type send to datacollector process before test run starts.
        /// </summary>
        public const string BeforeTestRunStart = "DataCollection.BeforeTestRunStart";

        /// <summary>
        /// Event message type used by datacollector to send results  after receiving test run start event.
        /// </summary>
        public const string BeforeTestRunStartResult = "DataCollection.BeforeTestRunStartResult";

        /// <summary>
        /// Event message type send to datacollector process after test run ends.
        /// </summary>
        public const string AfterTestRunEnd = "DataCollection.AfterTestRunEnd";

        /// <summary>
        /// Event message type used by datacollector to send result on receiving test run end event.
        /// </summary>
        public const string AfterTestRunEndResult = "DataCollection.AfterTestRunEndResult";

        /// <summary>
        /// Event message type send to datacollector process before test case execution starts.
        /// </summary>
        public const string DataCollectionTestStart = "DataCollection.TestStart";

        /// <summary>
        /// Event message type used to signal datacollector process that test case execution has ended.
        /// </summary>
        public const string DataCollectionTestEnd = "DataCollection.TestEnd";

        /// <summary>
        /// Event message type used by datacollector to send result on receiving TestEnd.
        /// </summary>
        public const string DataCollectionTestEndResult = "DataCollection.TestEndResult";

        /// <summary>
        /// Ack Event message type send to datacollector process before test case execution starts.
        /// </summary>
        public const string DataCollectionTestStartAck = "DataCollection.TestStartAck";

        #endregion
    }
}
