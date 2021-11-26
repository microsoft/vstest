// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
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

    using CommonResources = Common.Resources.Resources;

    /// <summary>
    /// ParallelDiscoveryEventsHandler for handling the discovery events in case of parallel discovery
    /// </summary>
    internal class ParallelDiscoveryEventsHandler : ITestDiscoveryEventsHandler2
    {
        private IProxyDiscoveryManager proxyDiscoveryManager;

        private ITestDiscoveryEventsHandler2 actualDiscoveryEventsHandler;

        private IParallelProxyDiscoveryManager parallelProxyDiscoveryManager;

        private ParallelDiscoveryDataAggregator discoveryDataAggregator;

        private IDataSerializer dataSerializer;

        private IRequestData requestData;

        private readonly object sendMessageLock = new object();

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

            // Aggregate for final discovery complete
            discoveryDataAggregator.Aggregate(totalTests, isAborted);

            // Aggregate Discovery Data Metrics
            discoveryDataAggregator.AggregateDiscoveryDataMetrics(discoveryCompleteEventArgs.Metrics);

            // we get discovery complete events from each host process
            // so we cannot "complete" the actual operation until all sources are consumed
            // We should not block last chunk results while we aggregate overall discovery data
            if (lastChunk != null)
            {
                ConvertToRawMessageAndSend(MessageType.TestCasesFound, lastChunk);
                this.HandleDiscoveredTests(lastChunk);
                // If we come here it means that some source was already fully discovered so we can mark it
                if (!discoveryDataAggregator.IsAborted)
                {
                    AggregateComingSourcesAsFullyDiscovered(lastChunk, discoveryCompleteEventArgs);
                }
            }

            // Do not send TestDiscoveryComplete to actual test discovery handler
            // We need to see if there are still sources left - let the parallel manager decide
            var parallelDiscoveryComplete = this.parallelProxyDiscoveryManager.HandlePartialDiscoveryComplete(
                    this.proxyDiscoveryManager,
                    totalTests,
                    null, // lastChunk should be null as we already sent this data above
                    isAborted);

            if (parallelDiscoveryComplete)
            {
                var fullyDiscovered = discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered) as IReadOnlyCollection<string>;
                var partiallyDiscovered = discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered) as IReadOnlyCollection<string>;
                var notDiscovered = discoveryDataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered) as IReadOnlyCollection<string>;

                // As we immediatelly return results to IDE in case of aborting
                // we need to set isAborted = true and totalTests = -1
                if (this.parallelProxyDiscoveryManager.IsAbortRequested)
                {
                    discoveryDataAggregator.Aggregate(-1, true);
                }

                // In case of sequential discovery - RawMessage would have contained a 'DiscoveryCompletePayload' object
                // To send a raw message - we need to create raw message from an aggregated payload object
                var testDiscoveryCompletePayload = new DiscoveryCompletePayload()
                {
                    TotalTests = discoveryDataAggregator.TotalTests,
                    IsAborted = discoveryDataAggregator.IsAborted,      
                    LastDiscoveredTests = null,
                    FullyDiscoveredSources = fullyDiscovered,
                    PartiallyDiscoveredSources = partiallyDiscovered,
                    NotDiscoveredSources = notDiscovered
                };

                // Collecting Final Discovery State
                this.requestData.MetricsCollection.Add(TelemetryDataConstants.DiscoveryState, isAborted ? "Aborted" : "Completed");

                // Collect Aggregated Metrics Data
                var aggregatedDiscoveryDataMetrics = discoveryDataAggregator.GetAggregatedDiscoveryDataMetrics();
                testDiscoveryCompletePayload.Metrics = aggregatedDiscoveryDataMetrics;

                // Sending discovery complete message to IDE
                this.SendMessage(testDiscoveryCompletePayload);

                var finalDiscoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(this.discoveryDataAggregator.TotalTests,
                                                                                     this.discoveryDataAggregator.IsAborted,
                                                                                     fullyDiscovered,
                                                                                     partiallyDiscovered,
                                                                                     notDiscovered);
      
                finalDiscoveryCompleteEventArgs.Metrics = aggregatedDiscoveryDataMetrics;

                // send actual test discovery complete to clients
                this.actualDiscoveryEventsHandler.HandleDiscoveryComplete(finalDiscoveryCompleteEventArgs, null);
            }
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            // In case of parallel - we can send everything but handle complete
            // DiscoveryComplete is not true-end of the overall discovery as we only get completion of one host here
            // Always aggregate data, deserialize and raw for complete events
            var message = this.dataSerializer.DeserializeMessage(rawMessage);

            // Do not send CancellationRequested message to Output window in IDE, as it is not useful for user
            if (string.Equals(message.MessageType, MessageType.TestMessage)
                && rawMessage.IndexOf(CommonResources.CancellationRequested) >= 0)
            {
                return;
            }

            // Do not deserialize further
            if (!string.Equals(MessageType.DiscoveryComplete, message.MessageType))
            {
                this.actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
            }
        }

        /// <inheritdoc/>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            this.actualDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
        }

        /// <inheritdoc/>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.actualDiscoveryEventsHandler.HandleLogMessage(level, message);
        }

        /// <summary>
        /// To send message to IDE output window use HandleRawMessage
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="payload"></param>
        private void ConvertToRawMessageAndSend(string messageType, object payload)
        {
            var rawMessage = this.dataSerializer.SerializePayload(messageType, payload);
            this.actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }

        /// <summary>
        /// Sending discovery complete message to IDE
        /// </summary>
        /// <param name="discoveryDataAggregator">Discovery aggregator to know if we already sent this message</param>
        /// <param name="testDiscoveryCompletePayload">Discovery complete payload to send</param>
        private void SendMessage(DiscoveryCompletePayload testDiscoveryCompletePayload)
        {
            // In case of abort scenario, we need to send raw message to IDE only once after abortion.
            // All other testhosts which will finish after shouldn't send raw message
            if (!discoveryDataAggregator.IsMessageSent)
            {
                lock (sendMessageLock)
                {
                    if (!discoveryDataAggregator.IsMessageSent)
                    {
                        // we have to send raw messages as we block the discovery complete actual raw messages
                        this.ConvertToRawMessageAndSend(MessageType.DiscoveryComplete, testDiscoveryCompletePayload);
                        discoveryDataAggregator.AggregateIsMessageSent(true);
                    }
                }
            }
        }

        /// <summary>
        /// Aggregate source as fully discovered
        /// </summary>
        /// <param name="discoveryDataAggregator">Aggregator to aggregate results</param>
        /// <param name="lastChunk">Last chunk of discovered test cases</param>
        private void AggregateComingSourcesAsFullyDiscovered(IEnumerable<TestCase> lastChunk, DiscoveryCompleteEventArgs discoveryCompleteEventArgs)
        {
            if (lastChunk == null) return;

            IEnumerable<string> lastChunkSources;

            // Sometimes we get lastChunk as empty list (when number of tests in project dividable by 10)
            // Then we will take sources from discoveryCompleteEventArgs coming from testhost
            if (lastChunk.Count() == 0)
            {
                lastChunkSources = discoveryCompleteEventArgs.FullyDiscoveredSources;
            }
            else
            {
                lastChunkSources = lastChunk.Select(testcase => testcase.Source);
            }

            discoveryDataAggregator.AggregateTheSourcesWithDiscoveryStatus(lastChunkSources, DiscoveryStatus.FullyDiscovered);
        }
    }
}
