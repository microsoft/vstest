// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    class NoIsolationProxyDiscoveryManager : IProxyDiscoveryManager
    {
        private ITestHostManagerFactory testHostManagerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyDiscoveryManager"/> class.
        /// </summary>
        public NoIsolationProxyDiscoveryManager() : this(new TestHostManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyDiscoveryManager"/> class.
        /// </summary>
        /// <param name="testHostManagerFactory">
        /// Manager factory
        /// </param>
        protected NoIsolationProxyDiscoveryManager(ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManagerFactory = testHostManagerFactory;
        }

        /// <summary>
        /// Initializes test discovery. Create the test host, setup channel and initialize extensions.
        /// This function is of no use in this context as we are not creating any testhost
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            var discoveryManager = this.testHostManagerFactory.GetDiscoveryManager();

            // Initialize extension before discovery
            discoveryManager.Initialize(Enumerable.Empty<string>());
            discoveryManager.DiscoverTests(discoveryCriteria, eventHandler);
        }

        /// <summary>
        /// Closes the current test operation.
        /// This function is of no use in this context as we are not creating any testhost
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        public void Abort()
        {
            this.testHostManagerFactory.GetDiscoveryManager().Abort();
        }
    }
}
