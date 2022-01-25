// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CommonResources = Common.Resources.Resources;

    /// <summary>
    /// ParallelDiscoveryEventsHandler for handling the discovery events in case of parallel discovery
    /// </summary>
    internal class ParallelDiscoveryEventsHandler : ITestDiscoveryEventsHandler2
    {
        private readonly IProxyDiscoveryManager proxyDiscoveryManager;

        private readonly ITestDiscoveryEventsHandler2 actualDiscoveryEventsHandler;

        private readonly IParallelProxyDiscoveryManager parallelProxyDiscoveryManager;

        private readonly ParallelDiscoveryDataAggregator discoveryDataAggregator;

        private readonly IDataSerializer dataSerializer;

        private readonly IRequestData requestData;

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
            this.proxyDiscoveryManager = proxyDiscoveryManager;
            this.actualDiscoveryEventsHandler = actualDiscoveryEventsHandler;
            this.parallelProxyDiscoveryManager = parallelProxyDiscoveryManager;
            this.discoveryDataAggregator = discoveryDataAggregator;
            this.dataSerializer = dataSerializer;
            this.requestData = requestData;
        }

        /// <inheritdoc/>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            var totalTests = discoveryCompleteEventArgs.TotalCount;
            var isAborted = discoveryCompleteEventArgs.IsAborted;

            // we get discovery complete events from each host process
            // so we cannot "complete" the actual operation until all sources are consumed
            // We should not block last chunk results while we aggregate overall discovery data
            if (lastChunk != null)
            {
                ConvertToRawMessageAndSend(MessageType.TestCasesFound, lastChunk);
                HandleDiscoveredTests(lastChunk);
            }

            // Aggregate for final discovery complete
            discoveryDataAggregator.Aggregate(totalTests, isAborted);

            // Aggregate Discovery Data Metrics
            discoveryDataAggregator.AggregateDiscoveryDataMetrics(discoveryCompleteEventArgs.Metrics);

            // Do not send TestDiscoveryComplete to actual test discovery handler
            // We need to see if there are still sources left - let the parallel manager decide
            var parallelDiscoveryComplete = parallelProxyDiscoveryManager.HandlePartialDiscoveryComplete(
                    proxyDiscoveryManager,
                    totalTests,
                    null, // lastChunk should be null as we already sent this data above
                    isAborted);

            if (parallelDiscoveryComplete)
            {
                // In case of sequential discovery - RawMessage would have contained a 'DiscoveryCompletePayload' object
                // To send a raw message - we need to create raw message from an aggregated payload object
                var testDiscoveryCompletePayload = new DiscoveryCompletePayload()
                {
                    TotalTests = discoveryDataAggregator.TotalTests,
                    IsAborted = discoveryDataAggregator.IsAborted,
                    LastDiscoveredTests = null
                };

                // Collecting Final Discovery State
                requestData.MetricsCollection.Add(TelemetryDataConstants.DiscoveryState, isAborted ? "Aborted" : "Completed");

                // Collect Aggregated Metrics Data
                var aggregatedDiscoveryDataMetrics = discoveryDataAggregator.GetAggregatedDiscoveryDataMetrics();
                testDiscoveryCompletePayload.Metrics = aggregatedDiscoveryDataMetrics;

                // we have to send raw messages as we block the discovery complete actual raw messages
                ConvertToRawMessageAndSend(MessageType.DiscoveryComplete, testDiscoveryCompletePayload);

                var finalDiscoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(discoveryDataAggregator.TotalTests,
                    discoveryDataAggregator.IsAborted);
                finalDiscoveryCompleteEventArgs.Metrics = aggregatedDiscoveryDataMetrics;

                // send actual test discovery complete to clients
                actualDiscoveryEventsHandler.HandleDiscoveryComplete(finalDiscoveryCompleteEventArgs, null);
            }
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            // In case of parallel - we can send everything but handle complete
            // DiscoveryComplete is not true-end of the overall discovery as we only get completion of one host here
            // Always aggregate data, deserialize and raw for complete events
            var message = dataSerializer.DeserializeMessage(rawMessage);

            // Do not send CancellationRequested message to Output window in IDE, as it is not useful for user
            if (string.Equals(message.MessageType, MessageType.TestMessage)
                && rawMessage.IndexOf(CommonResources.CancellationRequested) >= 0)
            {
                return;
            }

            // Do not deserialize further
            if (!string.Equals(MessageType.DiscoveryComplete, message.MessageType))
            {
                actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
            }
        }

        /// <inheritdoc/>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            actualDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
        }

        /// <inheritdoc/>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            actualDiscoveryEventsHandler.HandleLogMessage(level, message);
        }

        /// <summary>
        /// To send message to IDE output window use HandleRawMessage
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="payload"></param>
        private void ConvertToRawMessageAndSend(string messageType, object payload)
        {
            var rawMessage = dataSerializer.SerializePayload(messageType, payload);
            actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }
    }
}
