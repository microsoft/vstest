// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public interface IProxyDiscoveryManager : IProxyOperationManager
    {
        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler);
    }
}
