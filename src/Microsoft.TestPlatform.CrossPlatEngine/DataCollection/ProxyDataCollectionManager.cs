// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    /// <summary>
    /// Managed datacollector interaction from runner process.
    /// </summary>
    internal class ProxyDataCollectionManager : IProxyDataCollectionManager
    {
        private const string PortOption = "--port";
        private const string DiagOption = "--diag";
        private const string ParentProcessIdOption = "--parentprocessid";
        private const string TraceLevelOption = "--tracelevel";
        public const string DebugEnvironmentVaribleName = "VSTEST_DATACOLLECTOR_DEBUG";

        private IDataCollectionRequestSender dataCollectionRequestSender;
        private IDataCollectionLauncher dataCollectionLauncher;
        private IProcessHelper processHelper;
        private IRequestData requestData;
        private int dataCollectionPort;
        private int dataCollectionProcessId;

        /// <summary>
        /// The settings xml
        /// </summary>
        public string SettingsXml { get; }

        /// <summary>
        /// List of test sources
        /// </summary>
        public IEnumerable<string> Sources { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="requestData">
        /// Request Data providing common execution/discovery services.
        /// </param>
        /// <param name="settingsXml">
        ///     Runsettings that contains the datacollector related configuration.
        /// </param>
        /// <param name="sources">
        ///     Test Run sources
        /// </param>
        public ProxyDataCollectionManager(IRequestData requestData, string settingsXml, IEnumerable<string> sources)
            : this(requestData, settingsXml, sources, new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="requestData">
        ///     Request Data providing common execution/discovery services.
        /// </param>
        /// <param name="settingsXml">
        ///     The settings xml.
        /// </param>
        /// <param name="sources">
        ///     Test Run sources
        /// </param>
        /// <param name="processHelper">
        ///     The process helper.
        /// </param>
        internal ProxyDataCollectionManager(IRequestData requestData, string settingsXml, IEnumerable<string> sources, IProcessHelper processHelper) : this(requestData, settingsXml, sources, new DataCollectionRequestSender(), processHelper, DataCollectionLauncherFactory.GetDataCollectorLauncher(processHelper, settingsXml))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDataCollectionManager"/> class.
        /// </summary>
        /// <param name="requestData">
        ///     Request Data providing common execution/discovery services.
        /// </param>
        /// <param name="settingsXml">
        ///     Runsettings that contains the datacollector related configuration.
        /// </param>
        /// <param name="sources">
        ///     Test Run sources
        /// </param>
        /// <param name="dataCollectionRequestSender">
        ///     Handles communication with datacollector process.
        /// </param>
        /// <param name="processHelper">
        ///     The process Helper.
        /// </param>
        /// <param name="dataCollectionLauncher">
        ///     Launches datacollector process.
        /// </param>
        internal ProxyDataCollectionManager(IRequestData requestData, string settingsXml,
            IEnumerable<string> sources,
            IDataCollectionRequestSender dataCollectionRequestSender, IProcessHelper processHelper,
            IDataCollectionLauncher dataCollectionLauncher)
        {
            // DataCollector process needs the information of the Extensions folder
            // Add the Extensions folder path to runsettings.
            this.SettingsXml = UpdateExtensionsFolderInRunSettings(settingsXml);
            this.Sources = sources;
            this.requestData = requestData;

            this.dataCollectionRequestSender = dataCollectionRequestSender;
            this.dataCollectionLauncher = dataCollectionLauncher;
            this.processHelper = processHelper;
            this.LogEnabledDataCollectors();

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
               EqtTrace.Info("ProxyDataCollectionManager.AfterTestRunEnd: Get attachment set for datacollector processId: {0} port: {1}", dataCollectionProcessId, dataCollectionPort);
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
            IDictionary<string, string> environmentVariables = new Dictionary<string, string>();

            var dataCollectionEventsPort = 0;
            this.InvokeDataCollectionServiceAction(
            () =>
            {
                EqtTrace.Info("ProxyDataCollectionManager.BeforeTestRunStart: Get env variable and port for datacollector processId: {0} port: {1}", this.dataCollectionProcessId, this.dataCollectionPort);
                var result = this.dataCollectionRequestSender.SendBeforeTestRunStartAndGetResult(this.SettingsXml, this.Sources, runEventsHandler);
                environmentVariables = result.EnvironmentVariables;
                dataCollectionEventsPort = result.DataCollectionEventsPort;

                EqtTrace.Info(
                    "ProxyDataCollectionManager.BeforeTestRunStart: SendBeforeTestRunStartAndGetResult successful, env variable from datacollector: {0}  and testhost port: {1}",
                    string.Join(";", environmentVariables),
                    dataCollectionEventsPort);
            },
                runEventsHandler);
            return new DataCollectionParameters(
                            areTestCaseLevelEventsRequired,
                            environmentVariables,
                            dataCollectionEventsPort);
        }

        /// <inheritdoc />
        public void TestHostLaunched(int processId)
        {
            var payload = new TestHostLaunchedPayload();
            payload.ProcessId = processId;

            this.dataCollectionRequestSender.SendTestHostLaunched(payload);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            EqtTrace.Info("ProxyDataCollectionManager.Dispose: calling dospose for datacollector processId: {0} port: {1}", this.dataCollectionProcessId, this.dataCollectionPort);
            this.dataCollectionRequestSender.Close();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            this.dataCollectionPort = this.dataCollectionRequestSender.InitializeCommunication();

            // Warn the user that execution will wait for debugger attach.
            this.dataCollectionProcessId = this.dataCollectionLauncher.LaunchDataCollector(null, this.GetCommandLineArguments(this.dataCollectionPort));
            EqtTrace.Info("ProxyDataCollectionManager.Initialize: Launched datacollector processId: {0} port: {1}", this.dataCollectionProcessId, this.dataCollectionPort);

            var connectionTimeout = this.GetConnectionTimeout(dataCollectionProcessId);

            EqtTrace.Info("ProxyDataCollectionManager.Initialize: waiting for connection with timeout: {0} seconds", connectionTimeout);

            var connected = this.dataCollectionRequestSender.WaitForRequestHandlerConnection(connectionTimeout * 1000);
            if (connected == false)
            {
                EqtTrace.Error("ProxyDataCollectionManager.Initialize: failed to connect to datacollector process, processId: {0} port: {1}", this.dataCollectionProcessId, this.dataCollectionPort);
                throw new TestPlatformException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                        CoreUtilitiesConstants.VstestConsoleProcessName,
                        CoreUtilitiesConstants.DatacollectorProcessName,
                        connectionTimeout,
                        EnvironmentHelper.VstestConnectionTimeout)
                    );
            }
        }

        private int GetConnectionTimeout(int processId)
        {
            var connectionTimeout = EnvironmentHelper.GetConnectionTimeout();

            // Increase connection timeout when debugging is enabled.
            var dataCollectorDebugEnabled = Environment.GetEnvironmentVariable(DebugEnvironmentVaribleName);
            if (!string.IsNullOrEmpty(dataCollectorDebugEnabled) &&
                dataCollectorDebugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.DataCollectorDebuggerWarning, OutputLevel.Warning);
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", processId, this.processHelper.GetProcessName(processId)),
                    OutputLevel.Information);

                // Increase connection timeout when debugging is enabled.
                connectionTimeout *= 5;
            }

            return connectionTimeout;
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

            runEventsHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, exception.ToString());
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

                commandlineArguments.Add(TraceLevelOption);
                commandlineArguments.Add(((int)EqtTrace.TraceLevel).ToString());
            }

            return commandlineArguments;
        }

        private string GetTimestampedLogFile(string logFile)
        {
            return Path.ChangeExtension(
                logFile,
                string.Format(
                    "datacollector.{0}_{1}{2}",
                    DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    new PlatformEnvironment().GetCurrentManagedThreadId(),
                    Path.GetExtension(logFile))).AddDoubleQuote();
        }

        /// <summary>
        /// Update Extensions path folder in testadapterspaths in runsettings.
        /// </summary>
        /// <param name="settingsXml"></param>
        private static string UpdateExtensionsFolderInRunSettings(string settingsXml)
        {
            if (string.IsNullOrWhiteSpace(settingsXml))
            {
                return settingsXml;
            }

            var extensionsFolder = Path.Combine(Path.GetDirectoryName(typeof(ITestPlatform).GetTypeInfo().Assembly.GetAssemblyLocation()), "Extensions");

            using (var stream = new StringReader(settingsXml))
            using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);

                var tapNode = RunSettingsProviderExtensions.GetXmlNode(document, "RunConfiguration.TestAdaptersPaths");

                if (tapNode != null && !string.IsNullOrWhiteSpace(tapNode.InnerText))
                {
                    extensionsFolder = string.Concat(tapNode.InnerText, ';', extensionsFolder);
                }

                RunSettingsProviderExtensions.UpdateRunSettingsXmlDocument(document, "RunConfiguration.TestAdaptersPaths", extensionsFolder);

                return document.OuterXml;
            }
        }

        /// <summary>
        /// Log Enabled Data Collectors
        /// </summary>
        private void LogEnabledDataCollectors()
        {
            if (!this.requestData.IsTelemetryOptedIn)
            {
                return;
            }

            var dataCollectionSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(this.SettingsXml);

            if (dataCollectionSettings == null || !dataCollectionSettings.IsCollectionEnabled)
            {
                return;
            }

            var enabledDataCollectors = new List<DataCollectorSettings>();
            foreach (var settings in dataCollectionSettings.DataCollectorSettingsList)
            {
                if (settings.IsEnabled)
                {
                    if (enabledDataCollectors.Any(dcSettings => string.Equals(dcSettings.FriendlyName, settings.FriendlyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                        continue;
                    }

                    enabledDataCollectors.Add(settings);
                }
            }

            var dataCollectors = enabledDataCollectors.Select(x => new { x.FriendlyName, x.Uri }.ToString());
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.DataCollectorsEnabled, string.Join(",", dataCollectors.ToArray()));
        }
    }
}