// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

    public class ProxyOperationManagerWithDataCollection : ProxyOperationManager
    {
        private IDictionary<string, string> dataCollectionEnvironmentVariables;
        private int dataCollectionPort;
        private IRequestData requestData;

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

        private void TestHostLaunchedHandler(object sender, HostProviderEventArgs e)
        {
            this.ProxyDataCollectionManager.TestHostLaunched(e.ProcessId);
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
                // On failure in calling BeforeTestRunStart, call AfterTestRunEnd to end DataCollectionProcess
                this.ProxyDataCollectionManager.AfterTestRunEnd(isCanceled: true, runEventsHandler: this.DataCollectionRunEventsHandler);
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

            // Update Telemetry Opt in status because by default in Test Host Telemetry is opted out
            var telemetryOptedIn = this.requestData.IsTelemetryOptedIn ? "true" : "false";
            testProcessStartInfo.Arguments += " --datacollectionport " + this.dataCollectionPort
                                              + " --telemetryoptedin " + telemetryOptedIn;

            return testProcessStartInfo;
        }

        public override bool SetupChannel(IEnumerable<string> sources, string runSettings, ITestMessageEventHandler eventHandler)
        {
            // Log all the messages that are reported while initializing DataCollectionClient
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
    }
}
