// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public class ProxyDiscoveryManager : ProxyOperationManager, IProxyDiscoveryManager
    {
        private readonly ITestHostManager testHostManager;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// </summary>
        /// <param name="testHostManager">Test host manager instance.</param>
        public ProxyDiscoveryManager(ITestHostManager testHostManager)
            : this(new TestRequestSender(), testHostManager, CrossPlatEngine.Constants.ClientConnectionTimeout)
        {
            this.testHostManager = testHostManager;
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
        internal ProxyDiscoveryManager(
            ITestRequestSender requestSender,
            ITestHostManager testHostManager,
            int clientConnectionTimeout)
            : base(requestSender, testHostManager, clientConnectionTimeout)
        {
            this.testHostManager = testHostManager;
        }

        #endregion

        #region IProxyDiscoveryManager implementation.

        /// <summary>
        /// Ensure that the discovery component of engine is ready for discovery usually by loading extensions.
        /// </summary>
        public void Initialize()
        {
            if (this.testHostManager.Shared)
            {
                // If the test host manager supports sharing the test host across sources, we can
                // initialize it early and assign sources later.
                this.InitializeExtensions(Enumerable.Empty<string>());
            }
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler eventHandler)
        {
            try
            {
                if (!this.testHostManager.Shared)
                {
                    // If the test host doesn't support sharing across sources, we must initialize it
                    // with sources.
                    this.InitializeExtensions(discoveryCriteria.Sources);
                }

                this.SetupChannel(discoveryCriteria.Sources);
                this.RequestSender.DiscoverTests(discoveryCriteria, eventHandler);
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyDiscoveryManager.DiscoverTests: Failed to discover tests: {0}", exception);
                eventHandler.HandleLogMessage(TestMessageLevel.Error, exception.Message);
                eventHandler.HandleDiscoveryComplete(0, new List<ObjectModel.TestCase>(), false);
            }
        }

        /// <inheritdoc/>
        public void Abort()
        {
            // This is no-op for the moment. There is no discovery abort message?
        }

        /// <inheritdoc/>
        public override void Close()
        {
            base.Close();
        }

        #endregion

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var sourceList = sources.ToList();
            var extensions = this.testHostManager.GetTestPlatformExtensions(sourceList).ToList();
            if (TestPluginCache.Instance.PathToAdditionalExtensions != null)
            {
                extensions.AddRange(TestPluginCache.Instance.PathToAdditionalExtensions);
            }

            // Only send this if needed.
            if (extensions.Count > 0)
            {
                this.SetupChannel(sourceList);

                this.RequestSender.InitializeDiscovery(extensions, TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }
    }
}
