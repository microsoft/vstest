// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing
{
    using System.Diagnostics.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;

    // <inheritdoc/>    
    [EventSource(Name = "TestPlatform")]
    public class TestPlatformEventSource : EventSource, ITestPlatformEventSource
    {        
        public static TestPlatformEventSource Instance = new TestPlatformEventSource();

        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId)]
        public void VsTestConsoleStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId)]
        public void VsTestConsoleStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleStopEventId);
        }

        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId)]
        public void DiscoveryRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId)]
        public void DiscoveryRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryRequestStopEventId);
        }

        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId)]
        public void ExecutionRequestStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId)]
        public void ExecutionRequestStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionRequestStopEventId);
        }

        [Event(TestPlatformInstrumentationEvents.TestHostStartEventId)]
        public void TestHostStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.TestHostStopEventId)]
        public void TestHostStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.TestHostStopEventId);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterSearchStartEventId)]
        public void AdapterSearchStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterSearchStopEventId)]
        public void AdapterSearchStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchStopEventId);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId)]
        public void AdapterExecutionStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStartEventId, executorUri);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId)]
        public void AdapterExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterExecutionStopEventId, numberOfTests);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId)]
        public void AdapterDiscoveryStart(string executorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStartEventId, executorUri);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId)]
        public void AdapterDiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.AdapterDiscoveryStopEventId, numberOfTests);
        }

        [Event(TestPlatformInstrumentationEvents.DiscoveryStartEventId)]
        public void DiscoveryStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.DiscoveryStopEventId)]
        public void DiscoveryStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DiscoveryStopEventId, numberOfTests);
        }

        [Event(TestPlatformInstrumentationEvents.ExecutionStartEventId)]
        public void ExecutionStart()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStartEventId);
        }

        [Event(TestPlatformInstrumentationEvents.ExecutionStopEventId)]
        public void ExecutionStop(long numberOfTests)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.ExecutionStopEventId, numberOfTests);
        }

        [Event(TestPlatformInstrumentationEvents.DataCollectionStartEventId)]
        public void DataCollectionStart(string dataCollectorUri)
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStartEventId, dataCollectorUri);
        }

        [Event(TestPlatformInstrumentationEvents.DataCollectionStopEventId)]
        public void DataCollectionStop()
        {
            this.WriteEvent(TestPlatformInstrumentationEvents.DataCollectionStopEventId);
        }
    }
}
