// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public class ProxyDiscoveryManager : ProxyOperationManager, IProxyDiscoveryManager
    {
        private readonly ITestRuntimeProvider testHostManager;
        private IDataSerializer dataSerializer;
        private CancellationTokenSource cancellationTokenSource;
        private bool isCommunicationEstablished;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// </summary>
        /// <param name="testRequestSender">Test request sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        public ProxyDiscoveryManager(ITestRequestSender testRequestSender, ITestRuntimeProvider testHostManager)
            : this(testRequestSender, testHostManager, JsonDataSerializer.Instance, CrossPlatEngine.Constants.ClientConnectionTimeout)
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
        /// <param name="dataSerializer"></param>
        /// <param name="clientConnectionTimeout">
        /// The client Connection Timeout
        /// </param>
        internal ProxyDiscoveryManager(
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager,
            IDataSerializer dataSerializer,
            int clientConnectionTimeout)
            : base(requestSender, testHostManager, clientConnectionTimeout)
        {
            this.dataSerializer = dataSerializer;
            this.testHostManager = testHostManager;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.isCommunicationEstablished = false;
        }

        #endregion

        #region IProxyDiscoveryManager implementation.

        /// <summary>
        /// Ensure that the discovery component of engine is ready for discovery usually by loading extensions.
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
            try
            {
                this.isCommunicationEstablished = this.SetupChannel(discoveryCriteria.Sources, this.cancellationTokenSource.Token);

                if (this.isCommunicationEstablished)
                {
                    this.InitializeExtensions(discoveryCriteria.Sources);
                    this.RequestSender.DiscoverTests(discoveryCriteria, eventHandler);
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyDiscoveryManager.DiscoverTests: Failed to discover tests: {0}", exception);

                // Log to vs ide test output
                var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = exception.Message };
                var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
                eventHandler.HandleRawMessage(rawMessage);

                // Log to vstest.console
                // Send a discovery complete to caller. Similar logic is also used in ParallelProxyDiscoveryManager.DiscoverTestsOnConcurrentManager
                // Aborted is `true`: in case of parallel discovery (or non shared host), an aborted message ensures another discovery manager
                // created to replace the current one. This will help if the current discovery manager is aborted due to irreparable error
                // and the test host is lost as well.
                eventHandler.HandleLogMessage(TestMessageLevel.Error, exception.Message);
                eventHandler.HandleDiscoveryComplete(-1, new List<ObjectModel.TestCase>(), true);
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
            var extensions = new List<string>();

            if (TestPluginCache.Instance.PathToExtensions != null)
            {
                extensions.AddRange(TestPluginCache.Instance.PathToExtensions.Where(ext => ext.EndsWith(TestPlatformConstants.TestAdapterEndsWithPattern, StringComparison.OrdinalIgnoreCase)));
            }

            extensions.AddRange(TestPluginCache.Instance.DefaultExtensionPaths);
            var sourceList = sources.ToList();
            var platformExtensions = this.testHostManager.GetTestPlatformExtensions(sourceList, extensions).ToList();

            // Only send this if needed.
            if (platformExtensions.Any())
            {
                this.RequestSender.InitializeDiscovery(platformExtensions, TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            }
        }
    }
}
