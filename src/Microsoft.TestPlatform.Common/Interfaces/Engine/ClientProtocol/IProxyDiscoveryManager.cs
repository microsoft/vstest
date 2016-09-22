// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public interface IProxyDiscoveryManager
    {
        /// <summary>
        /// Initializes test discovery. Create the test host, setup channel and initialize extensions.
        /// </summary>
        /// <param name="testHostManager">Test host manager for this operation.</param>
        void Initialize(ITestHostManager testHostManager);

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler);

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        void Abort();
    }
}
