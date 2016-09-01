// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces
{
    /// <summary>
    /// TestPlatform Instrumentation events
    /// </summary>
    interface ITestPlatformEventSource
    {
        void VsTestConsole();
        void VsTestConsoleEnd();

        void TestHost();
        void TestHostEnd();

        void AdapterSearch();
        void AdapterSearchEnd();

        void Adapter();
        void AdapterEnd();

        void Discovery();
        void DiscoveryEnd();

        void Execution();
        void ExecutionEnd();
    }
}
