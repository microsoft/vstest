// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// TestPlatform Event Ids and tasks constants
    /// </summary>
    public class TestPlatformInstrumentationEvents
    {
        public const Int32 Discovery = 0x1;
        public const Int32 DiscoveryEnd = 0x2;
        public const EventTask DiscoveryTask = (EventTask)0x3;

        public const Int32 Execution = 0x4;
        public const Int32 ExecutionEnd = 0x5;
        public const EventTask ExecutionTask = (EventTask)0x6;

        public const Int32 Adapter = 0x7;
        public const Int32 AdapterEnd = 0x8;
        public const EventTask AdapterTask = (EventTask)0x9;

        public const Int32 VsTestConsole = 0x10;
        public const Int32 VsTestConsoleEnd = 0x11;
        public const EventTask VsTestConsoleTask = (EventTask)0x12;

        public const Int32 TestHost = 0x13;
        public const Int32 TestHostEnd = 0x14;
        public const EventTask TestHostTask = (EventTask)0x15;

        public const Int32 AdapterSearch = 0x16;
        public const Int32 AdapterSearchEnd = 0x17;
        public const EventTask AdapterSearchTask = (EventTask)0x18;
    }
}
