// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// ParallelDiscoveryEventsHandler for handling the discovery events in case of parallel discovery
    /// </summary>
    internal class ParallelDiscoveryEventsHandler : ITestDiscoveryEventsHandler
    {
        private IProxyDiscoveryManager proxyDiscoveryManager;

        private ITestDiscoveryEventsHandler actualDiscoveryEventsHandler;

        private IParallelProxyDiscoveryManager parallelProxyDiscoveryManager;

        private ParallelDiscoveryDataAggregator discoveryDataAggregator;

        private IDataSerializer dataSerializer;

        public ParallelDiscoveryEventsHandler(IProxyDiscoveryManager proxyDiscoveryManager,
            ITestDiscoveryEventsHandler actualDiscoveryEventsHandler,
            IParallelProxyDiscoveryManager parallelProxyDiscoveryManager,
            ParallelDiscoveryDataAggregator discoveryDataAggregator) :
            this(proxyDiscoveryManager, actualDiscoveryEventsHandler, parallelProxyDiscoveryManager, discoveryDataAggregator, JsonDataSerializer.Instance)
        {
        }
        
        internal ParallelDiscoveryEventsHandler(IProxyDiscoveryManager proxyDiscoveryManager,
            ITestDiscoveryEventsHandler actualDiscoveryEventsHandler,
            IParallelProxyDiscoveryManager parallelProxyDiscoveryManager,
            ParallelDiscoveryDataAggregator discoveryDataAggregator,
            IDataSerializer dataSerializer)
        {
            this.proxyDiscoveryManager = proxyDiscoveryManager;
            this.actualDiscoveryEventsHandler = actualDiscoveryEventsHandler;
            this.parallelProxyDiscoveryManager = parallelProxyDiscoveryManager;
            this.discoveryDataAggregator = discoveryDataAggregator;
            this.dataSerializer = dataSerializer;
        }

        /// <inheritdoc/>
        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            // we get discovery complete events from each host process
            // so we cannot "complete" the actual operation until all sources are consumed
            // We should not block last chunk results while we aggregate overall discovery data 
            if (lastChunk != null)
            {
                ConvertToRawMessageAndSend(MessageType.TestCasesFound, lastChunk);
                this.HandleDiscoveredTests(lastChunk);
            }
            
            // Aggregate for final discoverycomplete 
            discoveryDataAggregator.Aggregate(totalTests, isAborted);

            // Do not send TestDiscoveryComplete to actual test discovery handler
            // We need to see if there are still sources left - let the parallel manager decide
            var parallelDiscoveryComplete = this.parallelProxyDiscoveryManager.HandlePartialDiscoveryComplete(
                    this.proxyDiscoveryManager,
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

                // we have to send raw messages as we block the discoverycomplete actual raw messages
                this.ConvertToRawMessageAndSend(MessageType.DiscoveryComplete, testDiscoveryCompletePayload);

                // send actual test discoverycomplete to clients
                this.actualDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryDataAggregator.TotalTests, null, discoveryDataAggregator.IsAborted);
            }
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            // In case of parallel - we can send everything but handle complete
            // DiscoveryComplete is not true-end of the overall discovery as we only get completion of one host here
            // Always aggregate data, deserialize and raw for complete events
            var message = this.dataSerializer.DeserializeMessage(rawMessage);

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
        
        private void ConvertToRawMessageAndSend(string messageType, object payload)
        {
            var rawMessage = this.dataSerializer.SerializePayload(messageType, payload);
            this.actualDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }
    }
}
