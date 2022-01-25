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
        private readonly IRequestData requestData;
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
            ProxyDataCollectionManager = proxyDataCollectionManager;
            DataCollectionRunEventsHandler = new DataCollectionRunEventsHandler();
            this.requestData = requestData;
            dataCollectionEnvironmentVariables = new Dictionary<string, string>();

            testHostManager.HostLaunched += TestHostLaunchedHandler;
        }

        /// <inheritdoc />
        public override void Initialize(bool skipDefaultAdapters)
        {
            ProxyDataCollectionManager.Initialize();

            try
            {
                var dataCollectionParameters = ProxyDataCollectionManager.BeforeTestRunStart(
                    resetDataCollectors: true,
                    isRunStartingNow: true,
                    runEventsHandler: DataCollectionRunEventsHandler);

                if (dataCollectionParameters != null)
                {
                    dataCollectionEnvironmentVariables = dataCollectionParameters.EnvironmentVariables;
                    dataCollectionPort = dataCollectionParameters.DataCollectionEventsPort;
                }
            }
            catch (Exception)
            {
                // On failure in calling BeforeTestRunStart, call AfterTestRunEnd to end the data
                // collection process.
                ProxyDataCollectionManager.AfterTestRunEnd(
                    isCanceled: true,
                    runEventsHandler: DataCollectionRunEventsHandler);
                throw;
            }

            base.Initialize(skipDefaultAdapters);
        }

        /// <inheritdoc />
        public override TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            if (testProcessStartInfo.EnvironmentVariables == null)
            {
                testProcessStartInfo.EnvironmentVariables = dataCollectionEnvironmentVariables;
            }
            else
            {
                foreach (var kvp in dataCollectionEnvironmentVariables)
                {
                    testProcessStartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            // Update telemetry opt in status because by default test host telemetry is opted out.
            var telemetryOptedIn = requestData.IsTelemetryOptedIn ? "true" : "false";
            testProcessStartInfo.Arguments += " --datacollectionport " + dataCollectionPort
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
            if (DataCollectionRunEventsHandler.Messages.Count > 0)
            {
                foreach (var message in DataCollectionRunEventsHandler.Messages)
                {
                    eventHandler.HandleLogMessage(message.Item1, message.Item2);
                }

                DataCollectionRunEventsHandler.Messages.Clear();
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
            ProxyDataCollectionManager.TestHostLaunched(e.ProcessId);
        }
    }
}
