// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    class InProcessProxyDiscoveryManager : IProxyDiscoveryManager
    {
        private ITestHostManagerFactory testHostManagerFactory;
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessProxyDiscoveryManager"/> class.
        /// </summary>
        public InProcessProxyDiscoveryManager() : this(new TestHostManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessProxyDiscoveryManager"/> class.
        /// </summary>
        /// <param name="testHostManagerFactory">Manager factory</param>
        internal InProcessProxyDiscoveryManager(ITestHostManagerFactory testHostManagerFactory)
        {
            this.testHostManagerFactory = testHostManagerFactory;
        }

        /// <summary>
        /// Initializes test discovery.
        /// </summary>
        public void Initialize()
        {
            if(!this.IsInitialized)
            {
                var discoveryManager = this.testHostManagerFactory.GetDiscoveryManager();

                // We don't need to pass list of extension as we are running inside vstest.console and
                // it will use TestPluginCache of vstest.console
                discoveryManager.Initialize(Enumerable.Empty<string>());
                this.IsInitialized = true;
            }
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            var discoveryManager = this.testHostManagerFactory.GetDiscoveryManager();

            Task.Run(() =>
            {
                try
                {
                    // Initialize extension before discovery if it’s not initialized
                    if (!this.IsInitialized)
                    {
                        discoveryManager.Initialize(Enumerable.Empty<string>());
                    }
                    discoveryManager.DiscoverTests(discoveryCriteria, eventHandler);
                }
                catch (Exception exception)
                {
                    EqtTrace.Error("InProcessProxyDiscoveryManager.DiscoverTests: Failed to discover tests: {0}", exception);

                    // Send a discovery complete to caller.
                    eventHandler.HandleLogMessage(TestMessageLevel.Error, exception.Message);
                    eventHandler.HandleDiscoveryComplete(-1, new List<TestCase>(), true);
                }
            }
            );
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
            Task.Run(() => this.testHostManagerFactory.GetDiscoveryManager().Abort());
        }
    }
}
