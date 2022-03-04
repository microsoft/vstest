// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;

/// <summary>
/// ParallelDiscoveryEventsHandler for handling the discovery events in case of parallel discovery
/// </summary>
internal class ParallelDiscoveryEventsHandler : ITestDiscoveryEventsHandler2
{
    private readonly IProxyDiscoveryManager _proxyDiscoveryManager;
    private readonly ITestDiscoveryEventsHandler2 _actualDiscoveryEventsHandler;
    private readonly IParallelProxyDiscoveryManager _parallelProxyDiscoveryManager;
    private readonly ParallelDiscoveryDataAggregator _discoveryDataAggregator;
    private readonly IDataSerializer _dataSerializer;
    private readonly IRequestData _requestData;
    private readonly object _sendMessageLock = new();

    public ParallelDiscoveryEventsHandler(IRequestData requestData,
        IProxyDiscoveryManager proxyDiscoveryManager,
        ITestDiscoveryEventsHandler2 actualDiscoveryEventsHandler,
        IParallelProxyDiscoveryManager parallelProxyDiscoveryManager,
        ParallelDiscoveryDataAggregator discoveryDataAggregator) :
        this(requestData, proxyDiscoveryManager, actualDiscoveryEventsHandler, parallelProxyDiscoveryManager, discoveryDataAggregator, JsonDataSerializer.Instance)
    {
    }

    internal ParallelDiscoveryEventsHandler(IRequestData requestData,
        IProxyDiscoveryManager proxyDiscoveryManager,
        ITestDiscoveryEventsHandler2 actualDiscoveryEventsHandler,
        IParallelProxyDiscoveryManager parallelProxyDiscoveryManager,
        ParallelDiscoveryDataAggregator discoveryDataAggregator,
        IDataSerializer dataSerializer)
    {
        _proxyDiscoveryManager = proxyDiscoveryManager;
        _actualDiscoveryEventsHandler = actualDiscoveryEventsHandler;
        _parallelProxyDiscoveryManager = parallelProxyDiscoveryManager;
        _discoveryDataAggregator = discoveryDataAggregator;
        _dataSerializer = dataSerializer;
        _requestData = requestData;
    }

    /// <inheritdoc/>
    public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
    {
        // Aggregate data for final discovery complete
        _discoveryDataAggregator.Aggregate(discoveryCompleteEventArgs, discoveryCompleteEventArgs.DiscoveredExtensions);


        // Aggregate Discovery Data Metrics
        _discoveryDataAggregator.AggregateDiscoveryDataMetrics(discoveryCompleteEventArgs.Metrics);

        // we get discovery complete events from each host process
        // so we cannot "complete" the actual operation until all sources are consumed
        // We should not block last chunk results while we aggregate overall discovery data
        if (lastChunk?.Any() == true)
        {
            ConvertToRawMessageAndSend(MessageType.TestCasesFound, lastChunk);
            HandleDiscoveredTests(lastChunk);
        }

        // Do not send TestDiscoveryComplete to actual test discovery handler
        // We need to see if there are still sources left - let the parallel manager decide
        var parallelDiscoveryComplete = _parallelProxyDiscoveryManager.HandlePartialDiscoveryComplete(
            _proxyDiscoveryManager,
            discoveryCompleteEventArgs.TotalCount,
            null, // lastChunk should be null as we already sent this data above
            discoveryCompleteEventArgs.IsAborted);
        if (!parallelDiscoveryComplete)
        {
            return;
        }

        // As we immediately return results to IDE in case of aborting
        // we need to set isAborted = true and totalTests = -1
        if (_parallelProxyDiscoveryManager.IsAbortRequested)
        {
            _discoveryDataAggregator.Aggregate(new(-1, true), null);
        }

        // Manager said we are ready to publish the test discovery completed.
        var fullyDiscovered = _discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered);
        var partiallyDiscovered = _discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered);
        var notDiscovered = _discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered);

        // In case of sequential discovery - RawMessage would have contained a 'DiscoveryCompletePayload' object
        // To send a raw message - we need to create raw message from an aggregated payload object
        var testDiscoveryCompletePayload = new DiscoveryCompletePayload()
        {
            TotalTests = _discoveryDataAggregator.TotalTests,
            IsAborted = _discoveryDataAggregator.IsAborted,
            LastDiscoveredTests = null,
            FullyDiscoveredSources = fullyDiscovered,
            PartiallyDiscoveredSources = partiallyDiscovered,
            NotDiscoveredSources = notDiscovered,
            DiscoveredExtensions = _discoveryDataAggregator.DiscoveredExtensions,
        };

        // Collecting Final Discovery State
        _requestData.MetricsCollection.Add(
            TelemetryDataConstants.DiscoveryState,
            discoveryCompleteEventArgs.IsAborted ? "Aborted" : "Completed");

        // Collect Aggregated Metrics Data
        var aggregatedDiscoveryDataMetrics = _discoveryDataAggregator.GetAggregatedDiscoveryDataMetrics();
        testDiscoveryCompletePayload.Metrics = aggregatedDiscoveryDataMetrics;

        // Sending discovery complete message to IDE
        ConvertToRawMessageAndSend(testDiscoveryCompletePayload);

        var finalDiscoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(
            _discoveryDataAggregator.TotalTests,
            _discoveryDataAggregator.IsAborted,
            fullyDiscovered,
            partiallyDiscovered,
            notDiscovered,
            _discoveryDataAggregator.DiscoveredExtensions);

        finalDiscoveryCompleteEventArgs.Metrics = aggregatedDiscoveryDataMetrics;

        // send actual test discovery complete to clients
        _actualDiscoveryEventsHandler.HandleDiscoveryComplete(finalDiscoveryCompleteEventArgs, null);
    }

    /// <inheritdoc/>
    public void HandleRawMessage(string rawMessage)
    {
        // In case of parallel - we can send everything but handle complete
        // DiscoveryComplete is not true-end of the overall discovery as we only get completion of one host here
        // Always aggregate data, deserialize and raw for complete events
        var message = _dataSerializer.DeserializeMessage(rawMessage);

        // Do not send CancellationRequested message to Output window in IDE, as it is not useful for user
        if (string.Equals(message.MessageType, MessageType.TestMessage)
            && rawMessage.IndexOf(CommonResources.CancellationRequested) >= 0)
        {
            return;
        }

        // Do not deserialize further
        if (!string.Equals(MessageType.DiscoveryComplete, message.MessageType))
        {
            _actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }
    }

    /// <inheritdoc/>
    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        _actualDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
    }

    /// <inheritdoc/>
    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        _actualDiscoveryEventsHandler.HandleLogMessage(level, message);
    }

    /// <summary>
    /// To send message to IDE output window use HandleRawMessage
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="payload"></param>
    private void ConvertToRawMessageAndSend(string messageType, object payload)
    {
        var rawMessage = _dataSerializer.SerializePayload(messageType, payload);
        _actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
    }

    /// <summary>
    /// Sending discovery complete message to IDE
    /// </summary>
    /// <param name="discoveryDataAggregator">Discovery aggregator to know if we already sent this message</param>
    /// <param name="testDiscoveryCompletePayload">Discovery complete payload to send</param>
    private void ConvertToRawMessageAndSend(DiscoveryCompletePayload testDiscoveryCompletePayload)
    {
        // When we abort we should send raw message to IDE only once.
        // All other testhosts which will finish after shouldn't send abort raw message.
        if (_discoveryDataAggregator.IsMessageSent)
        {
            return;
        }

        lock (_sendMessageLock)
        {
            if (!_discoveryDataAggregator.IsMessageSent)
            {
                // we have to send raw messages as we block the discovery complete actual raw messages
                ConvertToRawMessageAndSend(MessageType.DiscoveryComplete, testDiscoveryCompletePayload);
                _discoveryDataAggregator.AggregateIsMessageSent(true);
            }
        }
    }
}
