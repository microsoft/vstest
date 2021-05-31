// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

    /// <summary>
    /// The proxy operation manager with data collection.
    /// </summary>
    public class ProxyOperationManagerWithDataCollection : ProxyOperationManager
    {
        private IDictionary<string, string> dataCollectionEnvironmentVariables;
        private IRequestData requestData;
        private int dataCollectionPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManagerWithDataCollection"/>
        /// class.
        /// </summary>
        /// 
        /// <param name="requestData">The request data.</param>
        /// <param name="requestSender">The request sender.</param>
        /// <param name="testHostManager">The test host manager.</param>
        /// <param name="proxyDataCollectionManager">The data collection proxy.</param>
        public ProxyOperationManagerWithDataCollection(
            IRequestData requestData,
            ITestRequestSender requestSender,
            ITestRuntimeProvider testHostManager,
            IProxyDataCollectionManager proxyDataCollectionManager)
            : base(
                  requestData,
                  requestSender,
                  testHostManager)
        {
            this.ProxyDataCollectionManager = proxyDataCollectionManager;
            this.DataCollectionRunEventsHandler = new DataCollectionRunEventsHandler();
            this.requestData = requestData;
            this.dataCollectionEnvironmentVariables = new Dictionary<string, string>();

            testHostManager.HostLaunched += this.TestHostLaunchedHandler;
        }

        /// <inheritdoc />
        public override void Initialize(bool skipDefaultAdapters)
        {
            this.ProxyDataCollectionManager.Initialize();

            try
            {
                var dataCollectionParameters = this.ProxyDataCollectionManager.BeforeTestRunStart(
                    resetDataCollectors: true,
                    isRunStartingNow: true,
                    runEventsHandler: this.DataCollectionRunEventsHandler);

                if (dataCollectionParameters != null)
                {
                    this.dataCollectionEnvironmentVariables = dataCollectionParameters.EnvironmentVariables;
                    this.dataCollectionPort = dataCollectionParameters.DataCollectionEventsPort;
                }
            }
            catch (Exception)
            {
                // On failure in calling BeforeTestRunStart, call AfterTestRunEnd to end the data
                // collection process.
                this.ProxyDataCollectionManager.AfterTestRunEnd(
                    isCanceled: true,
                    runEventsHandler: this.DataCollectionRunEventsHandler);
                throw;
            }

            base.Initialize(skipDefaultAdapters);
        }

        /// <inheritdoc />
        public override TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            if (testProcessStartInfo.EnvironmentVariables == null)
            {
                testProcessStartInfo.EnvironmentVariables = this.dataCollectionEnvironmentVariables;
            }
            else
            {
                foreach (var kvp in this.dataCollectionEnvironmentVariables)
                {
                    testProcessStartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            // Update telemetry opt in status because by default test host telemetry is opted out.
            var telemetryOptedIn = this.requestData.IsTelemetryOptedIn ? "true" : "false";
            testProcessStartInfo.Arguments += " --datacollectionport " + this.dataCollectionPort
                                              + " --telemetryoptedin " + telemetryOptedIn;

            return testProcessStartInfo;
        }

        /// <inheritdoc />
        public override bool SetupChannel(
            IEnumerable<string> sources,
            string runSettings,
            ITestMessageEventHandler eventHandler)
        {
            // Log all the messages that are reported while initializing the DataCollectionClient.
            if (this.DataCollectionRunEventsHandler.Messages.Count > 0)
            {
                foreach (var message in this.DataCollectionRunEventsHandler.Messages)
                {
                    eventHandler.HandleLogMessage(message.Item1, message.Item2);
                }

                this.DataCollectionRunEventsHandler.Messages.Clear();
            }

            return base.SetupChannel(sources, runSettings);
        }

        /// <summary>
        /// Gets the data collection run events handler.
        /// </summary>
        internal DataCollectionRunEventsHandler DataCollectionRunEventsHandler
        {
            get; private set;
        }

        /// <summary>
        /// Gets the proxy data collection manager.
        /// </summary>
        internal IProxyDataCollectionManager ProxyDataCollectionManager
        {
            get; private set;
        }

        private void TestHostLaunchedHandler(object sender, HostProviderEventArgs e)
        {
            this.ProxyDataCollectionManager.TestHostLaunched(e.ProcessId);
        }
    }
}
