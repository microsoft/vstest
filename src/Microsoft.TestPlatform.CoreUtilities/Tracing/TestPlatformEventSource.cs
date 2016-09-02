// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing
{
    using System.Diagnostics.Tracing;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;

    /// <summary>
    /// TestPlatformEventSource writes the events to TestPlatform listeners.
    /// </summary>
    [EventSource(Name = "TestPlatform")]
    public class TestPlatformEventSource : EventSource, ITestPlatformEventSource
    {
        /// <summary>
        /// The instance of TestPlatformEventSource.
        /// </summary>
        public static TestPlatformEventSource Instance = new TestPlatformEventSource();

        /// <summary>
        /// The vs test console start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId)]
        public void VsTestConsoleStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId);
        }

        /// <summary>
        /// The vs test console stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId)]
        public void VsTestConsoleStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId);
        }

        /// <summary>
        /// The discovery request start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId)]
        public void DiscoveryRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId);
        }

        /// <summary>
        /// The discovery request stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId)]
        public void DiscoveryRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId);
        }

        /// <summary>
        /// The execution request start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId)]
        public void ExecutionRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId);
        }

        /// <summary>
        /// The execution request stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId)]
        public void ExecutionRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId);
        }

        /// <summary>
        /// The test host start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.TestHostStartEventId)]
        public void TestHostStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStartEventId);
        }

        /// <summary>
        /// The test host stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.TestHostStopEventId)]
        public void TestHostStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStopEventId);
        }

        /// <summary>
        /// The adapter search start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.AdapterSearchStartEventId)]
        public void AdapterSearchStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStartEventId);
        }

        /// <summary>
        /// The adapter search stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.AdapterSearchStopEventId)]
        public void AdapterSearchStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStopEventId);
        }

        /// <summary>
        /// The adapter execution start.
        /// </summary>
        /// <param name="executorUri">
        /// The executor uri.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId)]
        public void AdapterExecutionStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId, executorUri);
        }

        /// <summary>
        /// The adapter execution stop.
        /// </summary>
        /// <param name="numberOfTests">
        /// The number of tests.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId)]
        public void AdapterExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId, numberOfTests);
        }

        /// <summary>
        /// The adapter discovery start.
        /// </summary>
        /// <param name="executorUri">
        /// The executor uri.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId)]
        public void AdapterDiscoveryStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId, executorUri);
        }

        /// <summary>
        /// The adapter discovery stop.
        /// </summary>
        /// <param name="numberOfTests">
        /// The number of tests.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId)]
        public void AdapterDiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId, numberOfTests);
        }

        /// <summary>
        /// The discovery start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.DiscoveryStartEventId)]
        public void DiscoveryStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStartEventId);
        }

        /// <summary>
        /// The discovery stop.
        /// </summary>
        /// <param name="numberOfTests">
        /// The number of tests.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.DiscoveryStopEventId)]
        public void DiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStopEventId, numberOfTests);
        }

        /// <summary>
        /// The execution start.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.ExecutionStartEventId)]
        public void ExecutionStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStartEventId);
        }

        /// <summary>
        /// The execution stop.
        /// </summary>
        /// <param name="numberOfTests">
        /// The number of tests.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.ExecutionStopEventId)]
        public void ExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStopEventId, numberOfTests);
        }

        /// <summary>
        /// The data collection start.
        /// </summary>
        /// <param name="dataCollectorUri">
        /// The data collector uri.
        /// </param>
        [Event(TestPlatformInstrumentationEvents.DataCollectionStartEventId)]
        public void DataCollectionStart(string dataCollectorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStartEventId, dataCollectorUri);
        }

        /// <summary>
        /// The data collection stop.
        /// </summary>
        [Event(TestPlatformInstrumentationEvents.DataCollectionStopEventId)]
        public void DataCollectionStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStopEventId);
        }
    }
}
