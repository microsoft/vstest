// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Orchestrates discovery operations for the engine communicating with the client.
    /// </summary>
    public class ProxyDiscoveryManager : ProxyOperationManager, IProxyDiscoveryManager, ITestDiscoveryEventsHandler2
    {
        private readonly ITestRuntimeProvider testHostManager;
        private IDataSerializer dataSerializer;
        private bool isCommunicationEstablished;
        private IRequestData requestData;
        private ITestDiscoveryEventsHandler2 baseTestDiscoveryEventsHandler;
        private bool skipDefaultAdapters;
        private readonly IFileHelper fileHelper;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// </summary>
        /// <param name="requestData">The Request Data for providing discovery services and data.</param>
        /// <param name="testRequestSender">Test request sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        public ProxyDiscoveryManager(IRequestData requestData, ITestRequestSender testRequestSender, ITestRuntimeProvider testHostManager)
            : this(requestData, testRequestSender, testHostManager, JsonDataSerializer.Instance, new FileHelper())
        {
            this.testHostManager = testHostManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="requestSender">
        ///     The request Sender.
        /// </param>
        /// <param name="testHostManager">
        ///     Test host Manager instance
        /// </param>
        /// <param name="dataSerializer"></param>
        internal ProxyDiscoveryManager(IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager,
            IDataSerializer dataSerializer,
            IFileHelper fileHelper)
            : base(requestData, requestSender, testHostManager)
        {
            this.dataSerializer = dataSerializer;
            this.testHostManager = testHostManager;
            this.isCommunicationEstablished = false;
            this.requestData = requestData;
            this.fileHelper = fileHelper;
        }

        #endregion

        #region IProxyDiscoveryManager implementation.

        /// <summary>
        /// Ensure that the discovery component of engine is ready for discovery usually by loading extensions.
        /// <param name="skipDefaultAdapters">Skip default adapters flag.</param>
        /// </summary>
        public void Initialize(bool skipDefaultAdapters)
        {
            this.skipDefaultAdapters = skipDefaultAdapters;
        }

        /// <summary>
        /// Discovers tests
        /// </summary>
        /// <param name="discoveryCriteria">Settings, parameters for the discovery request</param>
        /// <param name="eventHandler">EventHandler for handling discovery events from Engine</param>
        public void DiscoverTests(DiscoveryCriteria discoveryCriteria, ITestDiscoveryEventsHandler2 eventHandler)
        {
            this.baseTestDiscoveryEventsHandler = eventHandler;
            try
            {
                this.isCommunicationEstablished = this.SetupChannel(discoveryCriteria.Sources, discoveryCriteria.RunSettings);

                if (this.isCommunicationEstablished)
                {
                    this.InitializeExtensions(discoveryCriteria.Sources);
                    discoveryCriteria.UpdateDiscoveryCriteria(testHostManager);

                    this.RequestSender.DiscoverTests(discoveryCriteria, this);
                }
            }
            catch (Exception exception)
            {
                EqtTrace.Error("ProxyDiscoveryManager.DiscoverTests: Failed to discover tests: {0}", exception);

                // Log to vs ide test output
                var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = exception.ToString() };
                var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
                this.HandleRawMessage(rawMessage);

                // Log to vstest.console
                // Send a discovery complete to caller. Similar logic is also used in ParallelProxyDiscoveryManager.DiscoverTestsOnConcurrentManager
                // Aborted is `true`: in case of parallel discovery (or non shared host), an aborted message ensures another discovery manager
                // created to replace the current one. This will help if the current discovery manager is aborted due to irreparable error
                // and the test host is lost as well.
                this.HandleLogMessage(TestMessageLevel.Error, exception.ToString());

                var discoveryCompletePayload = new DiscoveryCompletePayload()
                {
                    IsAborted = true,
                    LastDiscoveredTests = null,
                    TotalTests = -1
                };
                this.HandleRawMessage(this.dataSerializer.SerializePayload(MessageType.DiscoveryComplete, discoveryCompletePayload));
                var discoveryCompleteEventsArgs = new DiscoveryCompleteEventArgs(-1, true);

                this.HandleDiscoveryComplete(discoveryCompleteEventsArgs, new List<ObjectModel.TestCase>());
            }
        }

        /// <inheritdoc/>
        public void Abort()
        {
            // Cancel fast, try to stop testhost deployment/launch
            this.CancellationTokenSource.Cancel();
            this.Close();
        }

        /// <inheritdoc/>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            this.baseTestDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, lastChunk);
        }

        /// <inheritdoc/>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            this.baseTestDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            var message = this.dataSerializer.DeserializeMessage(rawMessage);
            if(string.Equals(message.MessageType, MessageType.DiscoveryComplete))
            {
                this.Close();
            }

            this.baseTestDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }

        /// <inheritdoc/>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.baseTestDiscoveryEventsHandler.HandleLogMessage(level, message);
        }

        #endregion

        private void InitializeExtensions(IEnumerable<string> sources)
        {
            var extensions = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, this.skipDefaultAdapters);

            // Filter out non existing extensions
            var nonExistingExtensions = extensions.Where(extension => !this.fileHelper.Exists(extension));
            if (nonExistingExtensions.Any())
            {
                this.LogMessage(TestMessageLevel.Warning, string.Format(Resources.Resources.NonExistingExtensions, string.Join(",", nonExistingExtensions)));
            }

            var sourceList = sources.ToList();
            var platformExtensions = this.testHostManager.GetTestPlatformExtensions(sourceList, extensions.Except(nonExistingExtensions));

            // Only send this if needed.
            if (platformExtensions.Any())
            {
                this.RequestSender.InitializeDiscovery(platformExtensions);
            }
        }

        private void LogMessage(TestMessageLevel testMessageLevel, string message)
        {
            // Log to translation layer.
            var testMessagePayload = new TestMessagePayload { MessageLevel = testMessageLevel, Message = message };
            var rawMessage = this.dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            this.HandleRawMessage(rawMessage);

            // Log to vstest.console layer.
            this.HandleLogMessage(testMessageLevel, message);
        }
    }
}
