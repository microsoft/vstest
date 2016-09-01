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
        public static TestPlatformEventSource Instance => new TestPlatformEventSource();

        [Event(TestPlatformInstrumentationEvents.VsTestConsole, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.VsTestConsoleTask, Message = "VsTestConsole Started")]
        public void VsTestConsole()
        {
            WriteEvent(TestPlatformInstrumentationEvents.VsTestConsole);
        }

        [Event(TestPlatformInstrumentationEvents.VsTestConsoleEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.VsTestConsoleTask, Message = "VsTestConsole Ended")]
        public void VsTestConsoleEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.VsTestConsoleEnd);
        }

        [Event(TestPlatformInstrumentationEvents.TestHost, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.TestHostTask, Message = "TestHost Started")]
        public void TestHost()
        {
            WriteEvent(TestPlatformInstrumentationEvents.TestHost);
        }

        [Event(TestPlatformInstrumentationEvents.TestHostEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.TestHostTask, Message = "TestHost Stopped")]
        public void TestHostEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.TestHostEnd);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterSearch, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.AdapterSearchTask, Message = "AdapterSearch Started")]
        public void AdapterSearch()
        {
            WriteEvent(TestPlatformInstrumentationEvents.AdapterSearch);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterSearchEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.AdapterSearchTask, Message = "AdapterSearch Ended")]
        public void AdapterSearchEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.AdapterSearchEnd);
        }

        [Event(TestPlatformInstrumentationEvents.Adapter, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.AdapterTask, Message = "Adapter Started")]
        public void Adapter()
        {
            WriteEvent(TestPlatformInstrumentationEvents.Adapter);
        }

        [Event(TestPlatformInstrumentationEvents.AdapterEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.AdapterTask, Message = "Adapter Ended")]
        public void AdapterEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.AdapterEnd);
        }

        [Event(TestPlatformInstrumentationEvents.Discovery, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.DiscoveryTask, Message = "Discovery started")]
        public void Discovery()
        {
            WriteEvent(TestPlatformInstrumentationEvents.Discovery);
        }

        [Event(TestPlatformInstrumentationEvents.DiscoveryEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.DiscoveryTask, Message = "Discovery Ended")]
        public void DiscoveryEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.DiscoveryEnd);
        }

        [Event(TestPlatformInstrumentationEvents.Execution, Opcode = EventOpcode.Start, Task = TestPlatformInstrumentationEvents.ExecutionTask, Message = "Execution Started")]
        public void Execution()
        {
            WriteEvent(TestPlatformInstrumentationEvents.Execution);
        }

        [Event(TestPlatformInstrumentationEvents.ExecutionEnd, Opcode = EventOpcode.Stop, Task = TestPlatformInstrumentationEvents.ExecutionTask, Message = "Execution ended")]
        public void ExecutionEnd()
        {
            WriteEvent(TestPlatformInstrumentationEvents.ExecutionEnd);
        }
    }
}
