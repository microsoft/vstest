// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public class ProxyDiscoveryManager : ProxyOperationManager, IProxyDiscoveryManager
    {
        private ITestHostManager testHostManager;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// </summary>
        public ProxyDiscoveryManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestSender">
        /// The request Sender.
        /// </param>
        /// <param name="testHostManager">
        /// Test host Manager instance
        /// </param>
        /// <param name="clientConnectionTimeout">
        /// The client Connection Timeout
        /// </param>
        internal ProxyDiscoveryManager(ITestRequestSender requestSender, ITestHostManager testHostManager, int clientConnectionTimeout)
            : base(requestSender, testHostManager, clientConnectionTimeout)
        {
            this.testHostManager = testHostManager;
        }

        #endregion

        #region IProxyDiscoveryManager implementation.

        /// <summary>
        /// Ensure that the discovery component of engine is ready for discovery usually by loading extensions.
        /// </summary>
        /// <param name="testHostManager">
        /// The manager for the test host.
        /// </param>
        public override void Initialize(ITestHostManager testHostManager)
        {
            // Only send this if needed.
            if (TestPluginCache.Instance.PathToAdditionalExtensions != null
                && TestPluginCache.Instance.PathToAdditionalExtensions.Any())
            {
                base.Initialize(testHostManager);

                this.RequestSender.InitializeDiscovery(
                    TestPluginCache.Instance.PathToAdditionalExtensions,
                    TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            base.Initialize(this.testHostManager);

            this.RequestSender.DiscoverTests(discoveryCriteria, eventHandler);

            this.RequestSender.EndSession();
        }

        #endregion
    }
}
