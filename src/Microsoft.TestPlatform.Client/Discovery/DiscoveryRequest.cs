// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Client.Discovery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Common.Telemetry;
using CommunicationUtilities;
using CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using ObjectModel.Engine;
using ObjectModel.Logging;
using Utilities;

/// <summary>
/// The discovery request.
/// </summary>
public sealed class DiscoveryRequest : IDiscoveryRequest, ITestDiscoveryEventsHandler2
{
    private readonly IDataSerializer _dataSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryRequest"/> class.
    /// </summary>
    /// <param name="requestData">The Request Data instance providing services and data for discovery</param>
    /// <param name="criteria">Discovery criterion.</param>
    /// <param name="discoveryManager">Discovery manager instance.</param>
    internal DiscoveryRequest(IRequestData requestData, DiscoveryCriteria criteria, IProxyDiscoveryManager discoveryManager, ITestLoggerManager loggerManager)
        : this(requestData, criteria, discoveryManager, loggerManager, JsonDataSerializer.Instance)
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
        ITestLoggerManager loggerManager,
        IDataSerializer dataSerializer)
    {
        RequestData = requestData;
        DiscoveryCriteria = criteria;
        DiscoveryManager = discoveryManager;
        LoggerManager = loggerManager;
        _dataSerializer = dataSerializer;
    }

    /// <summary>
    /// Start the discovery request
    /// </summary>
    public void DiscoverAsync()
    {
        EqtTrace.Verbose("DiscoveryRequest.DiscoverAsync: Starting.");

        lock (_syncObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DiscoveryRequest");
            }

            // Reset the discovery completion event
            _discoveryCompleted.Reset();

            DiscoveryInProgress = true;
            try
            {
                _discoveryStartTime = DateTime.UtcNow;

                // Collecting Data Point Number of sources sent for discovery
                RequestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfSourcesSentForDiscovery, DiscoveryCriteria.Sources.Count());

                // Invoke OnDiscoveryStart event
                var discoveryStartEvent = new DiscoveryStartEventArgs(DiscoveryCriteria);
                LoggerManager.HandleDiscoveryStart(discoveryStartEvent);
                OnDiscoveryStart.SafeInvoke(this, discoveryStartEvent, "DiscoveryRequest.DiscoveryStart");

                DiscoveryManager.DiscoverTests(DiscoveryCriteria, this);
            }
            catch
            {
                DiscoveryInProgress = false;
                throw;
            }
        }

        EqtTrace.Info("DiscoveryRequest.DiscoverAsync: Started.");
    }

    /// <summary>
    /// Aborts the test discovery.
    /// </summary>
    public void Abort()
    {
        EqtTrace.Verbose("DiscoveryRequest.Abort: Aborting.");

        lock (_syncObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DiscoveryRequest");
            }

            if (DiscoveryInProgress)
            {
                DiscoveryManager.Abort();
            }
            else
            {
                EqtTrace.Info("DiscoveryRequest.Abort: No operation to abort.");
                return;
            }
        }

        EqtTrace.Info("DiscoveryRequest.Abort: Aborted.");
    }

    /// <summary>
    /// Wait for discovery completion
    /// </summary>
    /// <param name="timeout"> The timeout. </param>
    bool IRequest.WaitForCompletion(int timeout)
    {
        EqtTrace.Verbose("DiscoveryRequest.WaitForCompletion: Waiting with timeout {0}.", timeout);

        if (_disposed)
        {
            throw new ObjectDisposedException("DiscoveryRequest");
        }

        // This method is not synchronized as it can lead to dead-lock
        // (the discoveryCompletionEvent cannot be raised unless that lock is released)
        return _discoveryCompleted == null || _discoveryCompleted.WaitOne(timeout);
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
    internal bool DiscoveryInProgress { get; private set; }

    /// <summary>
    /// Parent discovery manager
    /// </summary>
    internal IProxyDiscoveryManager DiscoveryManager { get; }

    /// <summary>
    /// Logger manager.
    /// </summary>
    internal ITestLoggerManager LoggerManager { get; }

    #region ITestDiscoveryEventsHandler2 Methods

    /// <inheritdoc/>
    public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete: Starting. Aborted:{0}, TotalTests:{1}", discoveryCompleteEventArgs.IsAborted, discoveryCompleteEventArgs.TotalCount);

        lock (_syncObject)
        {
            if (_disposed)
            {
                EqtTrace.Warning("DiscoveryRequest.DiscoveryComplete: Ignoring as the object is disposed.");

                return;
            }

            // If discovery event is already raised, ignore current one.
            if (_discoveryCompleted.WaitOne(0))
            {
                EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete:Ignoring duplicate DiscoveryComplete. Aborted:{0}, TotalTests:{1}", discoveryCompleteEventArgs.IsAborted, discoveryCompleteEventArgs.TotalCount);
                return;
            }

            // Close the discovery session and terminate any test host processes. This operation should never
            // throw.
            DiscoveryManager?.Close();

            try
            {
                // Raise onDiscoveredTests event if there are some tests in the last chunk.
                // (We don't want to send the tests in the discovery complete event so that programming on top of
                // RS client is easier i.e. user does not have to listen on discovery complete event.)
                if (lastChunk != null && lastChunk.Any())
                {
                    var discoveredTestsEvent = new DiscoveredTestsEventArgs(lastChunk);
                    LoggerManager.HandleDiscoveredTests(discoveredTestsEvent);
                    OnDiscoveredTests.SafeInvoke(this, discoveredTestsEvent, "DiscoveryRequest.DiscoveryComplete");
                }

                LoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs);
                OnDiscoveryComplete.SafeInvoke(this, discoveryCompleteEventArgs, "DiscoveryRequest.DiscoveryComplete");
            }
            finally
            {
                // Notify the waiting handle that discovery is complete
                if (_discoveryCompleted != null)
                {
                    _discoveryCompleted.Set();
                    EqtTrace.Verbose("DiscoveryRequest.DiscoveryComplete: Notified the discovery complete event.");
                }
                else
                {
                    EqtTrace.Warning("DiscoveryRequest.DiscoveryComplete: Discovery complete event was null.");
                }

                DiscoveryInProgress = false;
                var discoveryFinalTimeTaken = DateTime.UtcNow - _discoveryStartTime;

                // Fill in the Metrics From Test Host Process
                var metrics = discoveryCompleteEventArgs.Metrics;
                if (metrics != null && metrics.Count != 0)
                {
                    foreach (var metric in metrics)
                    {
                        RequestData.MetricsCollection.Add(metric.Key, metric.Value);
                    }
                }

                // Collecting Total Time Taken
                RequestData.MetricsCollection.Add(
                    TelemetryDataConstants.TimeTakenInSecForDiscovery, discoveryFinalTimeTaken.TotalSeconds);
            }
        }

        EqtTrace.Info("DiscoveryRequest.DiscoveryComplete: Completed.");
    }

    /// <inheritdoc/>
    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        EqtTrace.Verbose("DiscoveryRequest.SendDiscoveredTests: Starting.");

        lock (_syncObject)
        {
            if (_disposed)
            {
                EqtTrace.Warning("DiscoveryRequest.SendDiscoveredTests: Ignoring as the object is disposed.");
                return;
            }

            var discoveredTestsEvent = new DiscoveredTestsEventArgs(discoveredTestCases);
            LoggerManager.HandleDiscoveredTests(discoveredTestsEvent);
            OnDiscoveredTests.SafeInvoke(this, discoveredTestsEvent, "DiscoveryRequest.OnDiscoveredTests");
        }

        EqtTrace.Info("DiscoveryRequest.SendDiscoveredTests: Completed.");
    }

    /// <summary>
    /// Dispatch TestRunMessage event to listeners.
    /// </summary>
    /// <param name="level">Output level of the message being sent.</param>
    /// <param name="message">Actual contents of the message</param>
    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        EqtTrace.Verbose("DiscoveryRequest.SendDiscoveryMessage: Starting.");

        lock (_syncObject)
        {
            if (_disposed)
            {
                EqtTrace.Warning("DiscoveryRequest.SendDiscoveryMessage: Ignoring as the object is disposed.");
                return;
            }

            var testRunMessageEvent = new TestRunMessageEventArgs(level, message);
            LoggerManager.HandleDiscoveryMessage(testRunMessageEvent);
            OnDiscoveryMessage.SafeInvoke(this, testRunMessageEvent, "DiscoveryRequest.OnTestMessageRecieved");
        }

        EqtTrace.Info("DiscoveryRequest.SendDiscoveryMessage: Completed.");
    }

    /// <summary>
    /// Handle Raw message directly from the host
    /// </summary>
    /// <param name="rawMessage">Raw message.</param>
    public void HandleRawMessage(string rawMessage)
    {
        // Note: Deserialize rawMessage only if required.

        var message = LoggerManager.LoggersInitialized || RequestData.IsTelemetryOptedIn
            ? _dataSerializer.DeserializeMessage(rawMessage)
            : null;

        if (string.Equals(message?.MessageType, MessageType.DiscoveryComplete))
        {
            var discoveryCompletePayload = _dataSerializer.DeserializePayload<DiscoveryCompletePayload>(message);
            rawMessage = UpdateRawMessageWithTelemetryInfo(discoveryCompletePayload, message) ?? rawMessage;
            HandleLoggerManagerDiscoveryComplete(discoveryCompletePayload);
        }

        OnRawMessageReceived?.SafeInvoke(this, rawMessage, "DiscoveryRequest.RawMessageReceived");
    }

    /// <summary>
    /// Handles LoggerManager's DiscoveryComplete.
    /// </summary>
    /// <param name="discoveryCompletePayload">Discovery complete payload.</param>
    private void HandleLoggerManagerDiscoveryComplete(DiscoveryCompletePayload discoveryCompletePayload)
    {
        if (LoggerManager.LoggersInitialized && discoveryCompletePayload != null)
        {
            // Send last chunk to logger manager.
            if (discoveryCompletePayload.LastDiscoveredTests != null)
            {
                var discoveredTestsEventArgs = new DiscoveredTestsEventArgs(discoveryCompletePayload.LastDiscoveredTests);
                LoggerManager.HandleDiscoveredTests(discoveredTestsEventArgs);
            }

            // Send discovery complete to logger manager.
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(discoveryCompletePayload.TotalTests, discoveryCompletePayload.IsAborted);
            discoveryCompleteEventArgs.Metrics = discoveryCompletePayload.Metrics;
            LoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs);
        }
    }

    /// <summary>
    /// Update raw message with telemetry info.
    /// </summary>
    /// <param name="discoveryCompletePayload">Discovery complete payload.</param>
    /// <param name="message">Message.</param>
    /// <returns>Updated rawMessage.</returns>
    private string UpdateRawMessageWithTelemetryInfo(DiscoveryCompletePayload discoveryCompletePayload, Message message)
    {
        var rawMessage = default(string);

        if (RequestData.IsTelemetryOptedIn)
        {
            if (discoveryCompletePayload != null)
            {
                if (discoveryCompletePayload.Metrics == null)
                {
                    discoveryCompletePayload.Metrics = RequestData.MetricsCollection.Metrics;
                }
                else
                {
                    foreach (var kvp in RequestData.MetricsCollection.Metrics)
                    {
                        discoveryCompletePayload.Metrics[kvp.Key] = kvp.Value;
                    }
                }

                var discoveryFinalTimeTakenForDesignMode = DateTime.UtcNow - _discoveryStartTime;

                // Collecting Total Time Taken
                discoveryCompletePayload.Metrics[TelemetryDataConstants.TimeTakenInSecForDiscovery] = discoveryFinalTimeTakenForDesignMode.TotalSeconds;
            }

            if (message is VersionedMessage message1)
            {
                var version = message1.Version;

                rawMessage = _dataSerializer.SerializePayload(
                    MessageType.DiscoveryComplete,
                    discoveryCompletePayload,
                    version);
            }
            else
            {
                rawMessage = _dataSerializer.SerializePayload(
                    MessageType.DiscoveryComplete,
                    discoveryCompletePayload);
            }
        }

        return rawMessage;
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or
    /// resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        EqtTrace.Verbose("DiscoveryRequest.Dispose: Starting.");

        lock (_syncObject)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_discoveryCompleted != null)
                    {
                        _discoveryCompleted.Dispose();
                    }
                }

                // Indicate that object has been disposed
                _discoveryCompleted = null;
                _disposed = true;
            }
        }

        EqtTrace.Info("DiscoveryRequest.Dispose: Completed.");
    }

    #endregion

    #region privates fields

    /// <summary>
    /// Request Data
    /// </summary>
    internal IRequestData RequestData;

    /// <summary>
    /// If this request has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// It get set when current discovery request is completed.
    /// </summary>
    private ManualResetEvent _discoveryCompleted = new(false);

    /// <summary>
    /// Sync object for various operations
    /// </summary>
    private readonly object _syncObject = new();

    /// <summary>
    /// Discovery Start Time
    /// </summary>
    private DateTime _discoveryStartTime;

    #endregion
}
