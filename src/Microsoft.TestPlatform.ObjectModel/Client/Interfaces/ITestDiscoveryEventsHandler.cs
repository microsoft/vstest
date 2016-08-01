// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface contract for handling discovery events during test discovery operation
    /// </summary>
    public interface ITestDiscoveryEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch DiscoveryComplete event to listeners.
        /// </summary>
        void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted);


        /// <summary>
        /// Dispatch DiscoveredTest event to listeners.
        /// </summary>
        /// <param name="discoveredTestCases">Discovered  test cases.</param>
        void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases);
    }
}