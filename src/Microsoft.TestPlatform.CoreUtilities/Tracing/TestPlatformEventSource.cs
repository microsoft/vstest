// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing
{
    using System.Diagnostics.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;

    /// <inheritdoc/>
    [EventSource(Name = "TestPlatform")]
    public class TestPlatformEventSource : EventSource, ITestPlatformEventSource
    {
        private static readonly TestPlatformEventSource LocalInstance = new TestPlatformEventSource();

        /// <summary>
        /// Gets the instance of <see cref="TestPlatformEventSource"/>.
        /// </summary>
        public static ITestPlatformEventSource Instance
        {
            get
            {
                return LocalInstance;
            }
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId)]
        public void VsTestConsoleStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId)]
        public void VsTestConsoleStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId)]
        public void DiscoveryRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId)]
        public void DiscoveryRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId)]
        public void ExecutionRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId)]
        public void ExecutionRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestHostStartEventId)]
        public void TestHostStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestHostStopEventId)]
        public void TestHostStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestHostAppDomainCreationStartEventId)]
        public void TestHostAppDomainCreationStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostAppDomainCreationStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestHostAppDomainCreationStopEventId)]
        public void TestHostAppDomainCreationStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostAppDomainCreationStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterSearchStartEventId)]
        public void AdapterSearchStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterSearchStopEventId)]
        public void AdapterSearchStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId)]
        public void AdapterExecutionStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId, executorUri);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId)]
        public void AdapterExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId, numberOfTests);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId)]
        public void AdapterDiscoveryStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId, executorUri);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId)]
        public void AdapterDiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId, numberOfTests);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DiscoveryStartEventId)]
        public void DiscoveryStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DiscoveryStopEventId)]
        public void DiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStopEventId, numberOfTests);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.ExecutionStartEventId)]
        public void ExecutionStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.ExecutionStopEventId)]
        public void ExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStopEventId, numberOfTests);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DataCollectionStartEventId)]
        public void DataCollectionStart(string dataCollectorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStartEventId, dataCollectorUri);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.DataCollectionStopEventId)]
        public void DataCollectionStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerInitializeStartEventId)]
        public void TranslationLayerInitializeStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerInitializeStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerInitializeStopEventId)]
        public void TranslationLayerInitializeStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerInitializeStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerDiscoveryStartEventId)]
        public void TranslationLayerDiscoveryStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerDiscoveryStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerDiscoveryStopEventId)]
        public void TranslationLayerDiscoveryStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerDiscoveryStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerExecutionStartEventId)]
        public void TranslationLayerExecutionStart(long customTestHost, long sourcesCount, long testCasesCount, string runSettings)
        {
            this.WriteEvent(
                TestPlatformInstrumentationEvents.TranslationLayerExecutionStartEventId,
                customTestHost,
                sourcesCount,
                testCasesCount,
                runSettings);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerExecutionStopEventId)]
        public void TranslationLayerExecutionStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerExecutionStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.MetricsDisposeStartEventId)]
        public void MetricsDisposeStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.MetricsDisposeStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.MetricsDisposeStopEventId)]
        public void MetricsDisposeStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.MetricsDisposeStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingRequestStartEventId)]
        public void TestRunAttachmentsProcessingRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingRequestStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingRequestStopEventId)]
        public void TestRunAttachmentsProcessingRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingRequestStopEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingStartEventId)]
        public void TestRunAttachmentsProcessingStart(long numberOfAttachments)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingStartEventId, numberOfAttachments);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingStopEventId)]
        public void TestRunAttachmentsProcessingStop(long numberOfAttachments)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestRunAttachmentsProcessingStopEventId, numberOfAttachments);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerTestRunAttachmentsProcessingStartEventId)]
        public void TranslationLayerTestRunAttachmentsProcessingStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerTestRunAttachmentsProcessingStartEventId);
        }

        /// <inheritdoc/>
        [Event(TestPlatformInstrumentationEvents.TranslationLayerTestRunAttachmentsProcessingStopEventId)]
        public void TranslationLayerTestRunAttachmentsProcessingStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TranslationLayerTestRunAttachmentsProcessingStopEventId);
        }
    }
}
