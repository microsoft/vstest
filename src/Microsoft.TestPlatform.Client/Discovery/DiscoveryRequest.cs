// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The discovery request.
    /// </summary>
    public sealed class DiscoveryRequest : IDiscoveryRequest, ITestDiscoveryEventsHandler2
    {
        private IDataSerializer dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryRequest"/> class.
        /// </summary>
        /// <param name="requestData">The Request Data instance providing services and data for discovery</param>
        /// <param name="criteria">Discovery criterion.</param>
        /// <param name="discoveryManager">Discovery manager instance.</param>
        internal DiscoveryRequest(IRequestData requestData, DiscoveryCriteria criteria, IProxyDiscoveryManager discoveryManager)
            : this(requestData, criteria, discoveryManager, JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryRequest"/> class.
        /// </summary>
        /// <param name="requestData">The Request Data instance providing services and data for discovery</param>
        /// <param name="criteria">Discovery criterion.</param>
        /// <param name="discoveryManager">Discovery manager instance.</param>
        /// <param name="dataSerializer">Data Serializer</param>
        internal DiscoveryRequest(
            IRequestData requestData,
            DiscoveryCriteria criteria,
            IProxyDiscoveryManager discoveryManager,
            IDataSerializer dataSerializer)
        {
            this.requestData = requestData;
            this.DiscoveryCriteria = criteria;
            this.DiscoveryManager = discoveryManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// Start the discovery request
        /// </summary>
        public void DiscoverAsync()
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.DiscoverAsync: Starting.");
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("DiscoveryRequest");
                }

                // Reset the discovery completion event
                this.discoveryCompleted.Reset();

                this.discoveryInProgress = true;
                try
                {
                    this.discoveryStartTime = DateTime.UtcNow;

                    // Collecting Data Point Number of sources sent for discovery
                    this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfSourcesSentForDiscovery, this.DiscoveryCriteria.Sources.Count());

                    // Invoke OnDiscoveryStart event
                    this.OnDiscoveryStart.SafeInvoke(this, new DiscoveryStartEventArgs(this.DiscoveryCriteria), "DiscoveryRequest.DiscoveryStart");

                    this.DiscoveryManager.DiscoverTests(this.DiscoveryCriteria, this);
                }
                catch
                {
                    this.discoveryInProgress = false;
                    throw;
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.DiscoverAsync: Started.");
            }
        }

        /// <summary>
        /// Aborts the test discovery.
        /// </summary>
        public void Abort()
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.Abort: Aborting.");
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("DiscoveryRequest");
                }

                if (this.discoveryInProgress)
                {
                    this.DiscoveryManager.Abort();
                }
                else
                {
                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("DiscoveryRequest.Abort: No operation to abort.");
                    }

                    return;
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.Abort: Aborted.");
            }
        }

        /// <summary>
        /// Wait for discovery completion completion
        /// </summary>
        /// <param name="timeout"> The timeout. </param>
        bool IRequest.WaitForCompletion(int timeout)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.WaitForCompletion: Waiting with timeout {0}.", timeout);
            }

            if (this.disposed)
            {
                throw new ObjectDisposedException("DiscoveryRequest");
            }

            // This method is not synchronized as it can lead to dead-lock 
            // (the discoveryCompletionEvent cannot be raised unless that lock is released)
            if (this.discoveryCompleted != null)
            {
                return this.discoveryCompleted.WaitOne(timeout);
            }

            return true;
        }

        /// <summary>
        /// Raised when the test discovery starts.
        /// </summary>
        public event EventHandler<DiscoveryStartEventArgs> OnDiscoveryStart;

        /// <summary>
        /// Raised when the test discovery completes.
        /// </summary>
        public event EventHandler<DiscoveryCompleteEventArgs> OnDiscoveryComplete;

        /// <summary>
        /// Raised when the message is received.
        /// </summary>
        /// <remarks>TestRunMessageEventArgs should be renamed to more generic</remarks>
        public event EventHandler<TestRunMessageEventArgs> OnDiscoveryMessage;

        /// <summary>
        /// Raised when new tests are discovered in this discovery request.  
        /// </summary>
        public event EventHandler<DiscoveredTestsEventArgs> OnDiscoveredTests;

        /// <summary>
        ///  Raised when a discovery event related message is received from host
        ///  This is required if one wants to re-direct the message over the process boundary without any processing overhead
        ///  All the discovery events should come as raw messages as well as proper serialized events like OnDiscoveredTests
        /// </summary>
        public event EventHandler<string> OnRawMessageReceived;

        /// <summary>
        /// Specifies the discovery criterion
        /// </summary>
        public DiscoveryCriteria DiscoveryCriteria
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the status for the discovery
        /// Returns true if discovery is in progress
        /// </summary>
        internal bool DiscoveryInProgress
        {
            get { return this.discoveryInProgress; }
        }

        /// <summary>
        /// Parent discovery manager
        /// </summary>
        internal IProxyDiscoveryManager DiscoveryManager { get; private set; }

        #region ITestDiscoveryEventsHandler2 Methods

        /// <inheritdoc/>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete: Starting. Aborted:{0}, TotalTests:{1}", discoveryCompleteEventArgs.IsAborted, discoveryCompleteEventArgs.TotalCount);
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("DiscoveryRequest.DiscoveryComplete: Ignoring as the object is disposed.");
                    }

                    return;
                }

                // If discovery event is already raised, ignore current one.
                if (this.discoveryCompleted.WaitOne(0))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete:Ignoring duplicate DiscoveryComplete. Aborted:{0}, TotalTests:{1}", discoveryCompleteEventArgs.IsAborted, discoveryCompleteEventArgs.TotalCount);
                    }

                    return;
                }

                // Close the discovery session and terminate any test host processes. This operation should never
                // throw.
                this.DiscoveryManager?.Close();

                try
                {
                    // Raise onDiscoveredTests event if there are some tests in the last chunk. 
                    // (We dont want to send the tests in the discovery complete event so that programming on top of 
                    // RS client is easier i.e. user does not have to listen on discovery complete event.)
                    if (lastChunk != null && lastChunk.Count() > 0)
                    {
                        this.OnDiscoveredTests.SafeInvoke(this, new DiscoveredTestsEventArgs(lastChunk), "DiscoveryRequest.DiscoveryComplete");
                    }

                    this.OnDiscoveryComplete.SafeInvoke(this, discoveryCompleteEventArgs, "DiscoveryRequest.DiscoveryComplete");
                }
                finally
                {
                    // Notify the waiting handle that discovery is complete
                    if (this.discoveryCompleted != null)
                    {
                        this.discoveryCompleted.Set();
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete: Notified the discovery complete event.");
                        }
                    }
                    else
                    {
                        if (EqtTrace.IsWarningEnabled)
                        {
                            EqtTrace.Warning("DiscoveryRequest.DiscoveryComplete: Discovery complete event was null.");
                        }
                    }

                    this.discoveryInProgress = false;
                    var discoveryFinalTimeTaken = DateTime.UtcNow - this.discoveryStartTime;

                    // Fill in the Metrics From Test Host Process
                    var metrics = discoveryCompleteEventArgs.Metrics;
                    if (metrics != null && metrics.Count != 0)
                    {
                        foreach (var metric in metrics)
                        {
                            this.requestData.MetricsCollection.Add(metric.Key, metric.Value);
                        }
                    }

                    // Collecting Total Time Taken
                    this.requestData.MetricsCollection.Add(
                        TelemetryDataConstants.TimeTakenInSecForDiscovery, discoveryFinalTimeTaken.TotalSeconds);
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.DiscoveryComplete: Completed.");
            }
        }

        /// <inheritdoc/>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.SendDiscoveredTests: Starting.");
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("DiscoveryRequest.SendDiscoveredTests: Ignoring as the object is disposed.");
                    }

                    return;
                }

                this.OnDiscoveredTests.SafeInvoke(this, new DiscoveredTestsEventArgs(discoveredTestCases), "DiscoveryRequest.OnDiscoveredTests");
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.SendDiscoveredTests: Completed.");
            }
        }

        /// <summary>
        /// Dispatch TestRunMessage event to listeners.
        /// </summary>
        /// <param name="level">Output level of the message being sent.</param>
        /// <param name="message">Actual contents of the message</param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.SendDiscoveryMessage: Starting.");
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("DiscoveryRequest.SendDiscoveryMessage: Ignoring as the object is disposed.");
                    }

                    return;
                }

                this.OnDiscoveryMessage.SafeInvoke(this, new TestRunMessageEventArgs(level, message), "DiscoveryRequest.OnTestMessageRecieved");
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.SendDiscoveryMessage: Completed.");
            }
        }

        /// <summary>
        /// Handle Raw message directly from the host
        /// </summary>
        /// <param name="rawMessage">Raw message.</param>
        public void HandleRawMessage(string rawMessage)
        {
            if (this.requestData.IsTelemetryOptedIn)
            {
                var message = this.dataSerializer.DeserializeMessage(rawMessage);

                if (string.Equals(message.MessageType, MessageType.DiscoveryComplete))
                {
                    var discoveryCompletePayload =
                        this.dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);

                    if (discoveryCompletePayload != null)
                    {
                        if (discoveryCompletePayload.Metrics == null)
                        {
                            discoveryCompletePayload.Metrics = this.requestData.MetricsCollection.Metrics;
                        }
                        else
                        {
                            foreach (var kvp in this.requestData.MetricsCollection.Metrics)
                            {
                                discoveryCompletePayload.Metrics.Add(kvp.Key, kvp.Value);
                            }
                        }

                        var discoveryFinalTimeTakenForDesignMode = DateTime.UtcNow - this.discoveryStartTime;

                        // Collecting Total Time Taken
                        discoveryCompletePayload.Metrics[TelemetryDataConstants.TimeTakenInSecForDiscovery] = discoveryFinalTimeTakenForDesignMode.TotalSeconds;
                    }

                    if (message is VersionedMessage)
                    {
                        var version = ((VersionedMessage)message).Version;

                        rawMessage = this.dataSerializer.SerializePayload(
                            MessageType.DiscoveryComplete,
                            discoveryCompletePayload,
                            version);
                    }
                    else
                    {
                        rawMessage = this.dataSerializer.SerializePayload(
                            MessageType.DiscoveryComplete,
                            discoveryCompletePayload);
                    }
                }
            }

            this.OnRawMessageReceived?.Invoke(this, rawMessage);
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DiscoveryRequest.Dispose: Starting.");
            }

            lock (this.syncObject)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        if (this.discoveryCompleted != null)
                        {
                            this.discoveryCompleted.Dispose();
                        }
                    }

                    // Indicate that object has been disposed
                    this.discoveryCompleted = null;
                    this.disposed = true;
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.Dispose: Completed.");
            }
        }

        #endregion

        #region privates fields

        /// <summary>
        /// Request Data
        /// </summary>
        internal IRequestData requestData;

        /// <summary>
        /// If this request has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// It get set when current discovery request is completed.
        /// </summary>
        private ManualResetEvent discoveryCompleted = new ManualResetEvent(false);

        /// <summary>
        /// Sync object for various operations
        /// </summary>
        private object syncObject = new Object();

        /// <summary>
        /// Whether or not the test discovery is in progress.
        /// </summary>
        private bool discoveryInProgress;

        /// <summary>
        /// Discovery Start Time
        /// </summary>
        private DateTime discoveryStartTime;

        #endregion
    }
}
