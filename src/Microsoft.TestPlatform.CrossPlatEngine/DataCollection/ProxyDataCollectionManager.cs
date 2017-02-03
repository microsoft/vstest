// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Managed datacollector interaction from runner process.
    /// </summary>
    internal class ProxyDataCollectionManager : IProxyDataCollectionManager
    {
        private const string PortOption = "--port";

        private IDataCollectionRequestSender dataCollectionRequestSender;
        private IDataCollectionLauncher dataCollectionLauncher;
        private string settingsXml;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="arch">
        /// Architecture for datacollection process.
        /// </param>
        /// <param name="settingsXml">
        /// Runsettings that contains the datacollector related configuration.
        /// </param>
        public ProxyDataCollectionManager(Architecture arch, string settingsXml, string targetFramework)
            : this(arch, settingsXml, new DataCollectionRequestSender(), DataCollectionLauncherFactory.GetDataCollectorLauncher(targetFramework))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="arch">
        /// Architecture for datacollection process.
        /// </param>
        /// <param name="settingsXml">
        /// Runsettings that contains the datacollector related configuration.
        /// </param>
        /// <param name="dataCollectionRequestSender">
        /// Handles communication with datacollector process.
        /// </param>
        /// <param name="dataCollectionLauncher">
        /// Launches datacollector process.
        /// </param>
        internal ProxyDataCollectionManager(Architecture arch, string settingsXml, IDataCollectionRequestSender dataCollectionRequestSender, IDataCollectionLauncher dataCollectionLauncher)
        {
            this.settingsXml = settingsXml;
            this.dataCollectionRequestSender = dataCollectionRequestSender;
            this.dataCollectionLauncher = dataCollectionLauncher;
            this.InitializeSocketCommunication(arch);
        }

        /// <summary>
        /// Invoked after ending of test run
        /// </summary>
        /// <param name="isCanceled">
        /// The is Canceled.
        /// </param>
        /// <param name="runEventsHandler">
        /// The run Events Handler.
        /// </param>
        /// <returns>
        /// The <see cref="Collection"/>.
        /// </returns>
        public Collection<AttachmentSet> AfterTestRunEnd(bool isCanceled, ITestMessageEventHandler runEventsHandler)
        {
            Collection<AttachmentSet> attachmentSet = null;
            this.InvokeDataCollectionServiceAction(
           () =>
           {
               attachmentSet = this.dataCollectionRequestSender.SendAfterTestRunStartAndGetResult(runEventsHandler, isCanceled);
           },
                runEventsHandler);
            return attachmentSet;
        }

        /// <summary>
        /// Invoked before starting of test run
        /// </summary>
        /// <param name="resetDataCollectors">
        /// The reset Data Collectors.
        /// </param>
        /// <param name="isRunStartingNow">
        /// The is Run Starting Now.
        /// </param>
        /// <param name="runEventsHandler">
        /// The run Events Handler.
        /// </param>
        /// <returns>
        /// BeforeTestRunStartResult object
        /// </returns>
        public DataCollectionParameters BeforeTestRunStart(
            bool resetDataCollectors,
            bool isRunStartingNow,
            ITestMessageEventHandler runEventsHandler)
        {
            var areTestCaseLevelEventsRequired = false;
            var isDataCollectionStarted = false;
            IDictionary<string, string> environmentVariables = null;

            var dataCollectionEventsPort = 0;
            this.InvokeDataCollectionServiceAction(
            () =>
            {
                var result = this.dataCollectionRequestSender.SendBeforeTestRunStartAndGetResult(this.settingsXml, runEventsHandler);
                areTestCaseLevelEventsRequired = result.AreTestCaseLevelEventsRequired;
                environmentVariables = result.EnvironmentVariables;
                dataCollectionEventsPort = result.DataCollectionEventsPort;
            },
                runEventsHandler);
            return new DataCollectionParameters(
                            isDataCollectionStarted,
                            areTestCaseLevelEventsRequired,
                            environmentVariables,
                            dataCollectionEventsPort);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.dataCollectionRequestSender.Close();
        }

        /// <summary>
        /// The initialize socket communication.
        /// </summary>
        /// <param name="arch">
        /// The arch.
        /// </param>
        internal void InitializeSocketCommunication(Architecture arch)
        {
            var port = this.dataCollectionRequestSender.InitializeCommunication();

            this.dataCollectionLauncher.LaunchDataCollector(null, this.GetCommandLineArguments(port));
            this.dataCollectionRequestSender.WaitForRequestHandlerConnection(connectionTimeout: 5000);
        }

        private void InvokeDataCollectionServiceAction(Action action, ITestMessageEventHandler runEventsHandler)
        {
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("ProxyDataCollectionManager.InvokeDataCollectionServiceAction: Starting.");
                }

                action();
                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("ProxyDataCollectionManager.InvokeDataCollectionServiceAction: Completed.");
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("ProxyDataCollectionManager.InvokeDataCollectionServiceAction: TestPlatformException = {0}.", ex);
                }

                this.HandleExceptionMessage(runEventsHandler, ex);
            }
        }

        private void HandleExceptionMessage(ITestMessageEventHandler runEventsHandler, Exception exception)
        {
            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Error(exception);
            }

            runEventsHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, exception.Message);
        }

        private IList<string> GetCommandLineArguments(int portNumber)
        {
            var commandlineArguments = new List<string>();

            commandlineArguments.Add(PortOption);
            commandlineArguments.Add(portNumber.ToString());

            return commandlineArguments;
        }
    }
}