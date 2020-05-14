// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using ClientResources = Microsoft.VisualStudio.TestPlatform.Client.Resources.Resources;
    using CommunicationObjectModel = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

    public class TestRunRequest : ITestRunRequest, ITestRunEventsHandler2
    {
        /// <summary>
        /// The criteria/config for this test run request.
        /// </summary>
        internal TestRunCriteria testRunCriteria;

        /// <summary>
        /// Specifies whether the run is disposed or not
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Sync object for various operations
        /// </summary>
        private object syncObject = new Object();

        /// <summary>
        /// Sync object for cancel operation
        /// </summary>
        private object cancelSyncObject = new Object();

        /// <summary>
        /// The run completion event which will be signaled on completion of test run.
        /// </summary>
        private ManualResetEvent runCompletionEvent = new ManualResetEvent(true);

        /// <summary>
        /// Tracks the time taken by each run request
        /// </summary>
        private Stopwatch runRequestTimeTracker;

        private IDataSerializer dataSerializer;

        /// <summary>
        /// Time out for run provided by client.
        /// </summary>
        private long testSessionTimeout;

        private Timer timer;

        /// <summary>
        /// Execution Start Time
        /// </summary>
        private DateTime executionStartTime;

        /// <summary>
        /// Request Data
        /// </summary>
        private IRequestData requestData;

        internal TestRunRequest(IRequestData requestData, TestRunCriteria testRunCriteria, IProxyExecutionManager executionManager, ITestLoggerManager loggerManager) :
            this(requestData, testRunCriteria, executionManager, loggerManager, JsonDataSerializer.Instance)
        {
        }

        internal TestRunRequest(IRequestData requestData, TestRunCriteria testRunCriteria, IProxyExecutionManager executionManager, ITestLoggerManager loggerManager, IDataSerializer dataSerializer)
        {
            Debug.Assert(testRunCriteria != null, "Test run criteria cannot be null");
            Debug.Assert(executionManager != null, "ExecutionManager cannot be null");
            Debug.Assert(requestData != null, "request Data is null");
            Debug.Assert(loggerManager != null, "LoggerManager cannot be null");

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TestRunRequest.ExecuteAsync: Creating test run request.");
            }

            this.testRunCriteria = testRunCriteria;
            this.ExecutionManager = executionManager;
            this.LoggerManager = loggerManager;
            this.State = TestRunState.Pending;
            this.dataSerializer = dataSerializer;
            this.requestData = requestData;
        }

        #region ITestRunRequest

        /// <summary>
        /// Execute the test run asynchronously
        /// </summary>
        /// <returns>The process id of test host.</returns>
        public int ExecuteAsync()
        {
            EqtTrace.Verbose("TestRunRequest.ExecuteAsync: Starting.");

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("testRunRequest");
                }

                if (this.State != TestRunState.Pending)
                {
                    throw new InvalidOperationException(ClientResources.InvalidStateForExecution);
                }

                this.executionStartTime = DateTime.UtcNow;

                // Collecting Number of sources Sent For Execution
                var numberOfSources = (uint)(testRunCriteria.Sources != null ? testRunCriteria.Sources.Count<string>() : 0);
                this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfSourcesSentForRun, numberOfSources);

                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("TestRunRequest.ExecuteAsync: Starting run with settings:{0}", this.testRunCriteria);
                }

                if (EqtTrace.IsVerboseEnabled)
                {
                    // Waiting for warm up to be over.
                    EqtTrace.Verbose("TestRunRequest.ExecuteAsync: Wait for the first run request is over.");
                }

                this.State = TestRunState.InProgress;

                // Reset the run completion event 
                // (This needs to be done before queuing the test run because if the test run finishes fast then runCompletion event can 
                // remain in non-signaled state even though run is actually complete.
                this.runCompletionEvent.Reset();

                try
                {
                    var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(this.TestRunCriteria.TestRunSettings);
                    this.testSessionTimeout = runConfiguration.TestSessionTimeout;

                    if (testSessionTimeout > 0)
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose(String.Format("TestRunRequest.ExecuteAsync: TestSessionTimeout is {0} milliseconds.", testSessionTimeout));
                        }

                        this.timer = new Timer(this.OnTestSessionTimeout, null, TimeSpan.FromMilliseconds(testSessionTimeout), TimeSpan.FromMilliseconds(0));
                    }

                    this.runRequestTimeTracker = new Stopwatch();

                    // Start the stop watch for calculating the test run time taken overall
                    this.runRequestTimeTracker.Start();
                    var testRunStartEvent = new TestRunStartEventArgs(this.testRunCriteria);
                    this.LoggerManager.HandleTestRunStart(testRunStartEvent);
                    this.OnRunStart.SafeInvoke(this, testRunStartEvent, "TestRun.TestRunStart");
                    int processId = this.ExecutionManager.StartTestRun(this.testRunCriteria, this);

                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("TestRunRequest.ExecuteAsync: Started.");
                    }

                    return processId;
                }
                catch
                {
                    this.State = TestRunState.Pending;
                    throw;
                }
            }
        }

        internal void OnTestSessionTimeout(object obj)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(String.Format("TestRunRequest.OnTestSessionTimeout: calling cancellation as test run exceeded testSessionTimeout {0} milliseconds", testSessionTimeout));
            }

            string message = String.Format(ClientResources.TestSessionTimeoutMessage, this.testSessionTimeout);
            var testMessagePayload = new CommunicationObjectModel.TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = message };
            var rawMessage = this.dataSerializer.SerializePayload(CommunicationObjectModel.MessageType.TestMessage, testMessagePayload);

            this.HandleLogMessage(TestMessageLevel.Error, message);
            this.HandleRawMessage(rawMessage);
            this.Abort();
        }

        /// <summary>
        /// Wait for the run completion
        /// </summary>
        public bool WaitForCompletion(int timeout)
        {
            EqtTrace.Verbose("TestRunRequest.WaitForCompletion: Waiting with timeout {0}.", timeout);

            if (this.disposed)
            {
                throw new ObjectDisposedException("testRunRequest");
            }

            if (this.State != TestRunState.InProgress
                && !(this.State == TestRunState.Completed
                        || this.State == TestRunState.Canceled
                        || this.State == TestRunState.Aborted))
            {
                // If run is already terminated, then we should not throw an exception.
                throw new InvalidOperationException(ClientResources.WaitForCompletionOperationIsNotAllowedWhenNoTestRunIsActive);
            }

            // This method is not synchronized as it can lead to dead-lock
            // (the runCompletionEvent cannot be raised unless that lock is released)

            // Wait for run completion (In case m_runCompletionEvent is closed, then waitOne will throw nice error)
            if (this.runCompletionEvent != null)
            {
                return this.runCompletionEvent.WaitOne(timeout);
            }

            return true;
        }

        /// <summary>
        /// Cancel the test run asynchronously
        /// </summary>
        public void CancelAsync()
        {
            EqtTrace.Verbose("TestRunRequest.CancelAsync: Canceling.");

            lock (this.cancelSyncObject)
            {
                if (this.disposed)
                {
                    EqtTrace.Warning("Ignoring TestRunRequest.CancelAsync() as testRunRequest object has already been disposed.");
                    return;
                }

                if (this.State != TestRunState.InProgress)
                {
                    EqtTrace.Info("Ignoring TestRunRequest.CancelAsync(). No test run in progress.");
                }
                else
                {
                    // Inform the service about run cancellation
                    this.ExecutionManager.Cancel(this);
                }
            }

            EqtTrace.Info("TestRunRequest.CancelAsync: Canceled.");
        }

        /// <summary>
        /// Aborts the test run execution process.
        /// </summary>
        public void Abort()
        {
            EqtTrace.Verbose("TestRunRequest.Abort: Aborting.");

            lock (this.cancelSyncObject)
            {
                if (this.disposed)
                {
                    EqtTrace.Warning("Ignoring TestRunRequest.Abort() as testRunRequest object has already been disposed");
                    return;
                }

                if (this.State != TestRunState.InProgress)
                {
                    EqtTrace.Info("Ignoring TestRunRequest.Abort(). No test run in progress.");
                }
                else
                {
                    this.ExecutionManager.Abort(this);
                }
            }

            EqtTrace.Info("TestRunRequest.Abort: Aborted.");
        }


        /// <summary>
        /// Specifies the test run criteria
        /// </summary>
        public ITestRunConfiguration TestRunConfiguration
        {
            get { return this.testRunCriteria; }
        }

        /// <summary>
        /// State of the test run
        /// </summary>
        public TestRunState State { get; private set; }

        /// <summary>
        /// Raised when the test run statistics change.
        /// </summary>
        public event EventHandler<TestRunChangedEventArgs> OnRunStatsChange;

        /// <summary>
        /// Raised when the test run starts.
        /// </summary>
        public event EventHandler<TestRunStartEventArgs> OnRunStart;

        /// <summary>
        /// Raised when the test message is received.
        /// </summary>
        public event EventHandler<TestRunMessageEventArgs> TestRunMessage;


        /// <summary>
        /// Raised when the test run completes.
        /// </summary>
        public event EventHandler<TestRunCompleteEventArgs> OnRunCompletion;


        /// <summary>
        /// Raised when data collection message is received.
        /// </summary>
#pragma warning disable 67
        public event EventHandler<DataCollectionMessageEventArgs> DataCollectionMessage;
#pragma warning restore 67

        /// <summary>
        ///  Raised when a test run event raw message is received from host
        ///  This is required if one wants to re-direct the message over the process boundary without any processing overhead
        ///  All the run events should come as raw messages as well as proper serialized events like OnRunStatsChange
        /// </summary>
        public event EventHandler<string> OnRawMessageReceived;

        /// <summary>
        /// Parent execution manager
        /// </summary>
        internal IProxyExecutionManager ExecutionManager
        {
            get; private set;
        }

        /// <summary>
        /// Logger manager.
        /// </summary>
        internal ITestLoggerManager LoggerManager
        {
            get; private set;
        }

        #endregion

        #region IDisposable implementation

        // Summary:
        // Performs application-defined tasks associated with freeing, releasing, or
        // resetting unmanaged resources.
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion

        public TestRunCriteria TestRunCriteria
        {
            get { return this.testRunCriteria; }
        }

        /// <summary>
        /// Invoked when test run is complete
        /// </summary>
        public void HandleTestRunComplete(TestRunCompleteEventArgs runCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            if (runCompleteArgs == null)
            {
                throw new ArgumentNullException(nameof(runCompleteArgs));
            }

            bool isAborted = runCompleteArgs.IsAborted;
            bool isCanceled = runCompleteArgs.IsCanceled;

            EqtTrace.Verbose("TestRunRequest:TestRunComplete: Starting. IsAborted:{0} IsCanceled:{1}.", isAborted, isCanceled);

            lock (this.syncObject)
            {
                // If this object is disposed, don't do anything
                if (this.disposed)
                {
                    EqtTrace.Warning("TestRunRequest.TestRunComplete: Ignoring as the object is disposed.");
                    return;
                }

                if (this.runCompletionEvent.WaitOne(0))
                {
                    EqtTrace.Info("TestRunRequest:TestRunComplete:Ignoring duplicate event. IsAborted:{0} IsCanceled:{1}.", isAborted, isCanceled);
                    return;
                }

                // Disposing off the resources held by the execution manager so that the test host process can shut down.
                this.ExecutionManager?.Close();

                try
                {
                    this.runRequestTimeTracker.Stop();

                    if (lastChunkArgs != null)
                    {
                        // Raised the changed event also
                        this.LoggerManager.HandleTestRunStatsChange(lastChunkArgs);
                        this.OnRunStatsChange.SafeInvoke(this, lastChunkArgs, "TestRun.RunStatsChanged");
                    }

                    TestRunCompleteEventArgs runCompletedEvent =
                        new TestRunCompleteEventArgs(
                            runCompleteArgs.TestRunStatistics,
                            runCompleteArgs.IsCanceled,
                            runCompleteArgs.IsAborted,
                            runCompleteArgs.Error,
                            // This is required as TMI adapter is sending attachments as List which cannot be type casted to Collection.
                            runContextAttachments != null ? new Collection<AttachmentSet>(runContextAttachments.ToList()) : null,
                            this.runRequestTimeTracker.Elapsed);

                    // Ignore the time sent (runCompleteArgs.ElapsedTimeInRunningTests)
                    // by either engines - as both calculate at different points
                    // If we use them, it would be an incorrect comparison between TAEF and Rocksteady
                    this.LoggerManager.HandleTestRunComplete(runCompletedEvent);
                    this.OnRunCompletion.SafeInvoke(this, runCompletedEvent, "TestRun.TestRunComplete");
                }
                finally
                {
                    if (isCanceled)
                    {
                        this.State = TestRunState.Canceled;
                    }
                    else if (isAborted)
                    {
                        this.State = TestRunState.Aborted;
                    }
                    else
                    {
                        this.State = TestRunState.Completed;
                    }

                    // Notify the waiting handle that run is complete
                    this.runCompletionEvent.Set();


                    var executionTotalTimeTaken = DateTime.UtcNow - this.executionStartTime;

                    // Fill in the time taken to complete the run
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenInSecForRun, executionTotalTimeTaken.TotalSeconds);

                    // Fill in the Metrics From Test Host Process
                    var metrics = runCompleteArgs.Metrics;
                    if (metrics != null && metrics.Count != 0)
                    {
                        foreach (var metric in metrics)
                        {
                            this.requestData.MetricsCollection.Add(metric.Key, metric.Value);
                        }
                    }
                }

                EqtTrace.Info("TestRunRequest:TestRunComplete: Completed.");
            }
        }

        /// <summary>
        /// Invoked when test run statistics change.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "This is not an external event")]
        public virtual void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            if (testRunChangedArgs != null)
            {
                EqtTrace.Verbose("TestRunRequest:SendTestRunStatsChange: Starting.");
                if (testRunChangedArgs.ActiveTests != null)
                {
                    // Do verbose check to save performance in iterating test cases
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        foreach (TestCase testCase in testRunChangedArgs.ActiveTests)
                        {
                            EqtTrace.Verbose("InProgress is {0}", testCase.DisplayName);
                        }
                    }
                }

                lock (this.syncObject)
                {
                    // If this object is disposed, don't do anything
                    if (this.disposed)
                    {
                        EqtTrace.Warning("TestRunRequest.SendTestRunStatsChange: Ignoring as the object is disposed.");
                        return;
                    }

                    // TODO: Invoke this event in a separate thread.
                    // For now, I am setting the ConcurrencyMode on the callback attribute to Multiple
                    this.LoggerManager.HandleTestRunStatsChange(testRunChangedArgs);
                    this.OnRunStatsChange.SafeInvoke(this, testRunChangedArgs, "TestRun.RunStatsChanged");
                }

                EqtTrace.Info("TestRunRequest:SendTestRunStatsChange: Completed.");
            }
        }

        /// <summary>
        /// Invoked when log messages are received
        /// </summary>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            EqtTrace.Verbose("TestRunRequest:SendTestRunMessage: Starting.");

            lock (this.syncObject)
            {
                // If this object is disposed, don't do anything
                if (this.disposed)
                {
                    EqtTrace.Warning("TestRunRequest.SendTestRunMessage: Ignoring as the object is disposed.");
                    return;
                }

                var testRunMessageEvent = new TestRunMessageEventArgs(level, message);
                this.LoggerManager.HandleTestRunMessage(testRunMessageEvent);
                this.TestRunMessage.SafeInvoke(this, testRunMessageEvent, "TestRun.LogMessages");
            }

            EqtTrace.Info("TestRunRequest:SendTestRunMessage: Completed.");
        }

        /// <summary>
        /// Handle Raw message directly from the host
        /// </summary>
        /// <param name="rawMessage"></param>
        public void HandleRawMessage(string rawMessage)
        {
            // Note: Deserialize rawMessage only if required.

            var message = this.LoggerManager.LoggersInitialized || this.requestData.IsTelemetryOptedIn ?
                this.dataSerializer.DeserializeMessage(rawMessage) : null;

            if (string.Equals(message?.MessageType, MessageType.ExecutionComplete))
            {
                var testRunCompletePayload = this.dataSerializer.DeserializePayload<TestRunCompletePayload>(message);
                rawMessage = UpdateRawMessageWithTelemetryInfo(testRunCompletePayload, message) ?? rawMessage;
                HandleLoggerManagerTestRunComplete(testRunCompletePayload);
            }

            this.OnRawMessageReceived?.Invoke(this, rawMessage);
        }

        /// <summary>
        /// Handles LoggerManager's TestRunComplete.
        /// </summary>
        /// <param name="testRunCompletePayload">TestRun complete payload.</param>
        private void HandleLoggerManagerTestRunComplete(TestRunCompletePayload testRunCompletePayload)
        {
            if (this.LoggerManager.LoggersInitialized && testRunCompletePayload != null)
            {
                // Send last chunk to logger manager.
                if (testRunCompletePayload.LastRunTests != null)
                {
                    this.LoggerManager.HandleTestRunStatsChange(testRunCompletePayload.LastRunTests);
                }

                // Note: In HandleRawMessage attachments are considered from TestRunCompleteArgs, while in HandleTestRunComplete attachments are considered directly from testRunCompletePayload.
                // Ideally we should have attachmentSets at one place only.
                // Send test run complete to logger manager.
                TestRunCompleteEventArgs testRunCompleteArgs =
                    new TestRunCompleteEventArgs(
                        testRunCompletePayload.TestRunCompleteArgs.TestRunStatistics,
                        testRunCompletePayload.TestRunCompleteArgs.IsCanceled,
                        testRunCompletePayload.TestRunCompleteArgs.IsAborted,
                        testRunCompletePayload.TestRunCompleteArgs.Error,
                        testRunCompletePayload.TestRunCompleteArgs.AttachmentSets,
                        this.runRequestTimeTracker.Elapsed);
                this.LoggerManager.HandleTestRunComplete(testRunCompleteArgs);
            }
        }

        /// <summary>
        /// Update raw message with telemetry info.
        /// </summary>
        /// <param name="testRunCompletePayload">Test run complete payload.</param>
        /// <param name="message">Updated rawMessage.</param>
        /// <returns></returns>
        private string UpdateRawMessageWithTelemetryInfo(TestRunCompletePayload testRunCompletePayload, Message message)
        {
            var rawMessage = default(string);
            if (this.requestData.IsTelemetryOptedIn)
            {
                if (testRunCompletePayload?.TestRunCompleteArgs != null)
                {
                    if (testRunCompletePayload.TestRunCompleteArgs.Metrics == null)
                    {
                        testRunCompletePayload.TestRunCompleteArgs.Metrics = this.requestData.MetricsCollection.Metrics;
                    }
                    else
                    {
                        foreach (var kvp in this.requestData.MetricsCollection.Metrics)
                        {
                            testRunCompletePayload.TestRunCompleteArgs.Metrics[kvp.Key] = kvp.Value;
                        }
                    }

                    // Fill in the time taken to complete the run
                    var executionTotalTimeTakenForDesignMode = DateTime.UtcNow - this.executionStartTime;
                    testRunCompletePayload.TestRunCompleteArgs.Metrics[TelemetryDataConstants.TimeTakenInSecForRun] = executionTotalTimeTakenForDesignMode.TotalSeconds;
                }

                if (message is VersionedMessage)
                {
                    var version = ((VersionedMessage)message).Version;

                    rawMessage = this.dataSerializer.SerializePayload(
                        MessageType.ExecutionComplete,
                        testRunCompletePayload,
                        version);
                }
                else
                {
                    rawMessage = this.dataSerializer.SerializePayload(
                        MessageType.ExecutionComplete,
                        testRunCompletePayload);
                }
            }

            return rawMessage;
        }

        /// <summary>
        /// Launch process with debugger attached
        /// </summary>
        /// <param name="testProcessStartInfo"></param>
        /// <returns>processid</returns>
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            int processId = -1;

            // Only launch while the test run is in progress and the launcher is a debug one
            if (this.State == TestRunState.InProgress && this.testRunCriteria.TestHostLauncher.IsDebug)
            {
                processId = this.testRunCriteria.TestHostLauncher.LaunchTestHost(testProcessStartInfo);
            }

            return processId;
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return this.testRunCriteria.TestHostLauncher is ITestHostLauncher2 launcher
                ? launcher.AttachDebuggerToProcess(pid)
                : false;
        }

        /// <summary>
        /// Dispose the run
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            EqtTrace.Verbose("TestRunRequest.Dispose: Starting.");

            lock (this.syncObject)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        this.runCompletionEvent?.Dispose();
                    }

                    // Indicate that object has been disposed
                    this.runCompletionEvent = null;
                    this.disposed = true;
                }
            }

            EqtTrace.Info("TestRunRequest.Dispose: Completed.");
        }
    }
}
