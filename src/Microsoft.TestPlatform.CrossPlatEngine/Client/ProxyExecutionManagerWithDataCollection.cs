// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The proxy execution manager with data collection.
    /// </summary>
    internal class ProxyExecutionManagerWithDataCollection : ProxyExecutionManager
    {
        private IDictionary<string, string> dataCollectionEnvironmentVariables;
        private int dataCollectionPort;
        private IRequestData requestData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManagerWithDataCollection"/> class.
        /// </summary>
        /// <param name="requestSender">
        /// Test request sender instance.
        /// </param>
        /// <param name="testHostManager">
        /// Test host manager for this operation.
        /// </param>
        /// <param name="proxyDataCollectionManager">
        /// The proxy Data Collection Manager.
        /// </param>
        /// <param name="requestData">
        /// The request data for providing execution services and data.
        /// </param>
        public ProxyExecutionManagerWithDataCollection(IRequestData requestData, ITestRequestSender requestSender, ITestRuntimeProvider testHostManager, IProxyDataCollectionManager proxyDataCollectionManager)
            : base(requestData, requestSender, testHostManager)
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

        /// <summary>
        /// Gets the cancellation token for execution.
        /// </summary>
        internal CancellationToken CancellationToken => CancellationTokenSource.Token;

        /// <summary>
        /// Ensure that the Execution component of engine is ready for execution usually by loading extensions.
        /// <param name="skipDefaultAdapters">Skip default adapters flag.</param>
        /// </summary>
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

        /// <summary>
        /// Starts the test run
        /// </summary>
        /// <param name="testRunCriteria"> The settings/options for the test run. </param>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        /// <returns> The process id of the runner executing tests. </returns>
        public override int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            var currentEventHandler = eventHandler;
            if (this.ProxyDataCollectionManager != null)
            {
                currentEventHandler = new DataCollectionTestRunEventsHandler(eventHandler, this.ProxyDataCollectionManager, this.CancellationTokenSource.Token);
            }

            // Log all the messages that are reported while initializing DataCollectionClient
            if (this.DataCollectionRunEventsHandler.Messages.Count > 0)
            {
                foreach (var message in this.DataCollectionRunEventsHandler.Messages)
                {
                    currentEventHandler.HandleLogMessage(message.Item1, message.Item2);
                }

                this.DataCollectionRunEventsHandler.Messages.Clear();
            }

            return base.StartTestRun(testRunCriteria, currentEventHandler);
        }

        public override int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            if (this.dataCollectionEnvironmentVariables != null)
            {
                if (testProcessStartInfo.EnvironmentVariables == null)
                {
                    testProcessStartInfo.EnvironmentVariables = new Dictionary<string, string>();
                }

                foreach(var envVariable in this.dataCollectionEnvironmentVariables)
                {
                    if (testProcessStartInfo.EnvironmentVariables.ContainsKey(envVariable.Key))
                    {
                        testProcessStartInfo.EnvironmentVariables[envVariable.Key] = envVariable.Value;
                    }
                    else
                    {
                        testProcessStartInfo.EnvironmentVariables.Add(envVariable.Key, envVariable.Value);
                    }
                }
            }

            return base.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }

        /// <inheritdoc />
        protected override TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
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
    }

    /// <summary>
    /// Handles Log events and stores them in list. Messages in the list will be logged after test execution begins.
    /// </summary>
    internal class DataCollectionRunEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRunEventsHandler"/> class.
        /// </summary>
        public DataCollectionRunEventsHandler()
        {
            this.Messages = new List<Tuple<TestMessageLevel, string>>();
        }

        /// <summary>
        /// Gets the cached messages.
        /// </summary>
        public List<Tuple<TestMessageLevel, string>> Messages { get; private set; }

        /// <inheritdoc />
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.Messages.Add(new Tuple<TestMessageLevel, string>(level, message));
        }

        /// <inheritdoc />
        public void HandleRawMessage(string rawMessage)
        {
            throw new NotImplementedException();
        }
    }
}