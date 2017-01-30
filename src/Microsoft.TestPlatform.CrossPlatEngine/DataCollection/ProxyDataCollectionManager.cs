// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;

    /// <summary>
    /// The test data collection client.
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
        /// The arch.
        /// </param>
        /// <param name="settingsXml">
        /// The settings Xml.
        /// </param>
        public ProxyDataCollectionManager(Architecture arch, string settingsXml)
            : this(arch, settingsXml, new DataCollectionRequestSender(), new DataCollectionLauncher())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="dataCollectionRequestSender">
        /// The data collection request sender.
        /// </param>
        /// <param name="dataCollectionLauncher">
        /// The data collection launcher.
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
               attachmentSet = this.dataCollectionRequestSender.SendAfterTestRunStartAndGetResult(runEventsHandler);
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
            bool areTestCaseLevelEventsRequired = false;
            bool isDataCollectionStarted = false;
            IDictionary<string, string> environmentVariables = null;

            var dataCollectionEventsPort = 0;
            this.InvokeDataCollectionServiceAction(
            () =>
            {
                var result = this.dataCollectionRequestSender.SendBeforeTestRunStartAndGetResult(settingsXml, runEventsHandler);
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

            this.dataCollectionLauncher.Initialize(arch);
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