// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Managed datacollector interaction from runner process.
    /// </summary>
    internal class ProxyDataCollectionManager : IProxyDataCollectionManager
    {
        private const string PortOption = "--port";
        private const string DiagOption = "--diag";
        private const string ParentProcessIdOption = "--parentprocessid";

        private IDataCollectionRequestSender dataCollectionRequestSender;
        private IDataCollectionLauncher dataCollectionLauncher;
        private IProcessHelper processHelper;
        private string settingsXml;
        private int connectionTimeout;


        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="settingsXml">
        /// Runsettings that contains the datacollector related configuration.
        /// </param>
        public ProxyDataCollectionManager(string settingsXml)
            : this(settingsXml, new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="settingsXml">
        /// The settings xml.
        /// </param>
        /// <param name="processHelper">
        /// The process helper.
        /// </param>
        internal ProxyDataCollectionManager(string settingsXml, IProcessHelper processHelper) : this(settingsXml, new DataCollectionRequestSender(), processHelper, DataCollectionLauncherFactory.GetDataCollectorLauncher(processHelper, settingsXml))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="settingsXml">
        /// Runsettings that contains the datacollector related configuration.
        /// </param>
        /// <param name="dataCollectionRequestSender">
        /// Handles communication with datacollector process.
        /// </param>
        /// <param name="processHelper">
        /// The process Helper.
        /// </param>
        /// <param name="dataCollectionLauncher">
        /// Launches datacollector process.
        /// </param>
        internal ProxyDataCollectionManager(string settingsXml, IDataCollectionRequestSender dataCollectionRequestSender, IProcessHelper processHelper, IDataCollectionLauncher dataCollectionLauncher)
        {
            this.settingsXml = settingsXml;
            this.dataCollectionRequestSender = dataCollectionRequestSender;
            this.dataCollectionLauncher = dataCollectionLauncher;
            this.processHelper = processHelper;
            this.connectionTimeout = 5 * 1000;
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
            IDictionary<string, string> environmentVariables = null;

            var dataCollectionEventsPort = 0;
            this.InvokeDataCollectionServiceAction(
            () =>
            {
                var result = this.dataCollectionRequestSender.SendBeforeTestRunStartAndGetResult(this.settingsXml, runEventsHandler);
                environmentVariables = result.EnvironmentVariables;
                dataCollectionEventsPort = result.DataCollectionEventsPort;
            },
                runEventsHandler);
            return new DataCollectionParameters(
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

        /// <inheritdoc />
        public void Initialize()
        {
            var port = this.dataCollectionRequestSender.InitializeCommunication();

            // Warn the user that execution will wait for debugger attach.
            var processId = this.dataCollectionLauncher.LaunchDataCollector(null, this.GetCommandLineArguments(port));

            var dataCollectorDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_DATACOLLECTOR_DEBUG");
            if (!string.IsNullOrEmpty(dataCollectorDebugEnabled) && dataCollectorDebugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.DataCollectorDebuggerWarning, OutputLevel.Warning);
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", processId, this.processHelper.GetProcessName(processId)),
                    OutputLevel.Information);

                // Increase connection timeout when debugging is enabled.
                this.connectionTimeout = 5 * this.connectionTimeout;
            }

            this.dataCollectionRequestSender.WaitForRequestHandlerConnection(this.connectionTimeout);
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

            commandlineArguments.Add(ParentProcessIdOption);
            commandlineArguments.Add(this.processHelper.GetCurrentProcessId().ToString());

            if (!string.IsNullOrEmpty(EqtTrace.LogFile))
            {
                commandlineArguments.Add(DiagOption);
                commandlineArguments.Add(this.GetTimestampedLogFile(EqtTrace.LogFile));
            }

            return commandlineArguments;
        }

        private string GetTimestampedLogFile(string logFile)
        {
            return Path.ChangeExtension(logFile,
                string.Format("datacollector.{0}_{1}{2}", DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    Thread.CurrentThread.ManagedThreadId, Path.GetExtension(logFile))).AddDoubleQuote();
        }
    }
}