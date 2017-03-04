// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The proxy execution manager with data collection.
    /// </summary>
    internal class ProxyExecutionManagerWithDataCollection : ProxyExecutionManager
    {
        private DataCollectionParameters dataCollectionParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyExecutionManagerWithDataCollection"/> class. 
        /// </summary>
        /// <param name="testHostManager">
        /// Test host manager for this operation.
        /// </param>
        /// <param name="proxyDataCollectionManager">
        /// The proxy Data Collection Manager.
        /// </param>
        public ProxyExecutionManagerWithDataCollection(ITestHostManager testHostManager, IProxyDataCollectionManager proxyDataCollectionManager) : base(testHostManager)
        {
            this.ProxyDataCollectionManager = proxyDataCollectionManager;
            this.DataCollectionRunEventsHandler = new DataCollectionRunEventsHandler();
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
        /// Ensure that the Execution component of engine is ready for execution usually by loading extensions.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this.dataCollectionParameters = (this.ProxyDataCollectionManager == null)
                                               ? DataCollectionParameters.CreateDefaultParameterInstance()
                                               : this.ProxyDataCollectionManager.BeforeTestRunStart(
                                                   resetDataCollectors: true,
                                                   isRunStartingNow: true,
                                                   runEventsHandler: this.DataCollectionRunEventsHandler);
            }
            catch
            {
                try
                {
                    // On failure in calling BeforeTestRunStart, call AfterTestRunEnd to end DataCollectionProcess
                    if (this.ProxyDataCollectionManager != null)
                    {
                        this.ProxyDataCollectionManager.AfterTestRunEnd(isCanceled: true, runEventsHandler: this.DataCollectionRunEventsHandler);
                    }
                }
                catch (Exception ex)
                {
                    // There is an issue with Data Collector, skipping data collection and continuing with test run.
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("TestEngine: Error occured while communicating with DataCollection Process: {0}", ex);
                    }

                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("TestEngine: Skipping Data Collection");
                    }
                }
            }

            base.Initialize();
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
                currentEventHandler = new DataCollectionTestRunEventsHandler(eventHandler, this.ProxyDataCollectionManager);
            }

            // Log all the exceptions that has occured while initializing DataCollectionClient
            if (this.DataCollectionRunEventsHandler?.Messages?.Count > 0)
            {
                foreach (var message in this.DataCollectionRunEventsHandler.Messages)
                {
                    currentEventHandler.HandleLogMessage(message.Item1, message.Item2);
                }

                this.DataCollectionRunEventsHandler.Messages.Clear();
            }

            return base.StartTestRun(testRunCriteria, currentEventHandler);
        }

        /// <inheritdoc/>
        public override void Cancel()
        {
            this.ProxyDataCollectionManager?.AfterTestRunEnd(isCanceled: true, runEventsHandler: this.DataCollectionRunEventsHandler);
            base.Cancel();
        }

        /// <inheritdoc/>
        public override void Close()
        {
            base.Close();
        }

        /// <inheritdoc />
        protected override TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            if (testProcessStartInfo.EnvironmentVariables == null)
            {
                testProcessStartInfo.EnvironmentVariables = this.dataCollectionParameters.EnvironmentVariables;
            }
            else
            {
                foreach (var kvp in this.dataCollectionParameters.EnvironmentVariables)
                {
                    testProcessStartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            testProcessStartInfo.Arguments += " --datacollectionport " + this.dataCollectionParameters.DataCollectionEventsPort;

            return testProcessStartInfo;
        }
    }

    /// <summary>
    /// Handles Log events and stores them in list. Messages in the list will be logged after test execution begins.
    /// </summary>
    internal class DataCollectionRunEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        public DataCollectionRunEventsHandler()
        {
            this.Messages = new List<Tuple<TestMessageLevel, string>>();
        }

        /// <summary>
        /// Gets the exception messages.
        /// </summary>
        public List<Tuple<TestMessageLevel, string>> Messages { get; private set; }

        /// <summary>
        /// The handle log message.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.Messages.Add(new Tuple<TestMessageLevel, string>(level, message));
        }

        /// <summary>
        /// The handle raw message.
        /// </summary>
        /// <param name="rawMessage">
        /// The raw message.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public void HandleRawMessage(string rawMessage)
        {
            throw new NotImplementedException();
        }
    }
}