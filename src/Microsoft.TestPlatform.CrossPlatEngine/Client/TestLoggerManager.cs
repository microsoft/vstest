// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Exceptions;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

    /// <summary>
    /// Responsible for managing logger extensions and broadcasting results
    /// and error/warning/informational messages to them.
    /// </summary>
    internal class TestLoggerManager : ITestLoggerManager, IDisposable
    {
        #region Fields

        private static readonly object Synclock = new object();
        protected List<LoggerInfo> loggersInfoList = new List<LoggerInfo>();

        /// <summary>
        /// Test Logger Events instance which will be passed to loggers when they are initialized.
        /// </summary>
        private InternalTestLoggerEvents loggerEvents;

        /// <summary>
        /// Used to keep track of which loggers have been initialized.
        /// </summary>
        private HashSet<Type> initializedLoggers = new HashSet<Type>();

        /// <summary>
        /// Keeps track if we are disposed.
        /// </summary>
        private bool isDisposed = false;

        ///// <summary>
        ///// Run request that we have registered for events on.  Used when
        ///// disposing to unregister for the events.
        ///// </summary>
        //private ITestRunRequest runRequest = null;

        /// <summary>
        /// Message logger.
        /// </summary>
        private IMessageLogger messageLogger;

        /// <summary>
        /// Request data.
        /// </summary>
        private IRequestData requestData;

        private TestLoggerExtensionManager testLoggerExtensionManager;
        //private IDiscoveryRequest discoveryRequest;

        /// <summary>
        /// AssemblyLoadContext for current platform
        /// </summary>
        private IAssemblyLoadContext assemblyLoadContext;

        #endregion

        #region Constructor

        /// <summary>
        /// Test logger manager.
        /// </summary>
        /// <param name="requestData">Request Data for Providing Common Services/Data for Discovery and Execution.</param>
        /// <param name="messageLogger">Message Logger.</param>
        /// <param name="loggerEvents">Logger events.</param>
        public TestLoggerManager(IRequestData requestData, IMessageLogger messageLogger, InternalTestLoggerEvents loggerEvents) : this(requestData, messageLogger, loggerEvents, new PlatformAssemblyLoadContext())
        {
        }

        /// <summary>
        /// Test logger manager.
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="messageLogger"></param>
        /// <param name="loggerEvents"></param>
        /// <param name="assemblyLoadContext"></param>
        internal TestLoggerManager(IRequestData requestData, IMessageLogger messageLogger,
            InternalTestLoggerEvents loggerEvents, IAssemblyLoadContext assemblyLoadContext)
        {
            this.requestData = requestData;
            this.messageLogger = messageLogger;
            this.testLoggerExtensionManager = null;
            this.loggerEvents = loggerEvents;
            this.assemblyLoadContext = assemblyLoadContext;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the logger events.
        /// </summary>
        public InternalTestLoggerEvents LoggerEvents
        {
            get
            {
                return this.loggerEvents;
            }
        }

        /// <summary>
        /// Gets the initialized loggers.
        /// </summary>
        /// This property is added to assist in testing
        protected HashSet<Type> InitializedLoggers
        {
            get
            {
                return this.initializedLoggers;
            }
        }

        private TestLoggerExtensionManager TestLoggerExtensionManager
        {
            get
            {
                if (this.testLoggerExtensionManager == null)
                {
                    this.testLoggerExtensionManager = TestLoggerExtensionManager.Create(messageLogger);
                }

                return this.testLoggerExtensionManager;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the logger list which will later use to initialize it.
        /// </summary>
        /// <param name="argument"> the actual argument pass by the user through --logger argument</param>
        /// <param name="loggerIdentifier">friendly name of the logger</param>
        /// <param name="parameters">parameter passed to logger</param>
        public void UpdateLoggerList(string argument, string loggerIdentifier, Dictionary<string, string> parameters)
        {
            this.loggersInfoList.Add(new LoggerInfo(argument, loggerIdentifier, parameters));
        }

        /// <summary>
        /// Initializes all the loggers passed by user
        /// </summary>
        public void Initialize(string runSettings)
        {
            var loggers = XmlRunSettingsUtilities.GetLoggerRunSettings(runSettings);

            foreach (var logger in loggers?.LoggerSettingsList ?? Enumerable.Empty<LoggerSettings>())
            {
                if (!logger.IsEnabled)
                {
                    // Dont add logger if its disabled.
                    continue;
                }

                var parameters = GetParametersFromConfigurationElement(logger.Configuration);
                var loggerInitialized = false;

                if (!string.IsNullOrWhiteSpace(logger.AssemblyQualifiedName))
                {
                    loggerInitialized = AddLoggerByType(logger.AssemblyQualifiedName, logger.CodeBase, parameters);
                }

                if (!loggerInitialized &&
                    !string.IsNullOrWhiteSpace(logger.Uri?.ToString()))
                {
                    loggerInitialized = AddLogger(logger.Uri, parameters);
                }

                if (!loggerInitialized &&
                    TryGetUriFromFriendlyName(logger.FriendlyName, out var loggerUri) &&
                    loggerUri != null)
                {
                    loggerInitialized = AddLogger(loggerUri, parameters);
                }

                if (!loggerInitialized)
                {
                    var value = !string.IsNullOrWhiteSpace(logger.AssemblyQualifiedName)
                        ? logger.AssemblyQualifiedName
                        : !string.IsNullOrWhiteSpace(logger.Uri?.ToString())
                            ? logger.Uri.ToString()
                            : logger.FriendlyName;

                    EqtTrace.Error(
                        String.Format(
                            CultureInfo.CurrentUICulture,
                            CommonResources.LoggerNotFound,
                            value));
                }
            }

            requestData.MetricsCollection.Add(TelemetryDataConstants.LoggerUsed, string.Join<Type>(",", this.initializedLoggers.ToArray()));
        }

        /// <summary>
        /// Get parameters from configuration element.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetParametersFromConfigurationElement(XmlElement configuration)
        {
            var configurationManager = new LoggerNameValueConfigurationManager(configuration);
            return configurationManager.NameValuePairs;
        }

        /// <summary>
        /// Add and initialize the logger with the given parameters
        /// </summary>
        /// <param name="logger">The logger that needs to be initialized</param>
        /// <param name="extensionUri">URI of the logger</param>
        /// <param name="parameters">Logger parameters</param>
        public bool AddLogger(ITestLogger logger, string extensionUri, Dictionary<string, string> parameters)
        {
            this.CheckDisposed();

            // If the logger has already been initialized just return.
            if (this.initializedLoggers.Contains(logger.GetType()))
            {
                EqtTrace.Verbose("TestLoggerManager: Skipping duplicate logger initialization: {0}", logger.GetType());
                return false;
            }
            var initialized = InitializeLogger(logger, extensionUri, parameters);

            if (initialized)
            {
                this.initializedLoggers.Add(logger.GetType());
            }

            return initialized;
        }

        /// <summary>
        /// Adds the logger with the specified URI and parameters.
        /// For ex. TfsPublisher takes parameters such as  Platform, Flavor etc.
        /// </summary>
        /// <param name="uri">URI of the logger to add.</param>
        /// <param name="parameters">Logger parameters.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification = "Case insensitive needs to be supported "), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Third party loggers could potentially throw all kinds of exceptions.")]
        public bool AddLogger(Uri uri, Dictionary<string, string> parameters)
        {
            ValidateArg.NotNull<Uri>(uri, "uri");

            this.CheckDisposed();

            // Look up the extension and initialize it if one is found.
            var extensionManager = this.TestLoggerExtensionManager;
            var logger = extensionManager.TryGetTestExtension(uri.AbsoluteUri);

            if (logger == null)
            {
                EqtTrace.Error(
                    String.Format(
                        CultureInfo.CurrentUICulture,
                        CommonResources.LoggerNotFound,
                        uri.OriginalString));
                return false;
            }

            // If the logger has already been initialized just return.
            if (this.initializedLoggers.Contains(logger.Value.GetType()))
            {
                EqtTrace.Verbose("TestLoggerManager: Skipping duplicate logger initialization: {0}", logger.Value.GetType());
                return false;
            }

            var initialized = InitializeLogger(logger.Value, logger.Metadata.ExtensionUri, parameters);

            if (initialized)
            {
                this.initializedLoggers.Add(logger.Value.GetType());
            }

            return initialized;
        }

        /// <summary>
        /// Add logger by type.
        /// </summary>
        /// <param name="assemblyQualifiedName"></param>
        /// <param name="codeBase"></param>
        /// <param name="parameters"></param>
        /// <returns>Returns true if initialized successfully.</returns>
        private bool AddLoggerByType(string assemblyQualifiedName, string codeBase, Dictionary<string, string> parameters)
        {
            this.CheckDisposed();
            Assembly assembly = null;
            try
            {
                assembly = this.assemblyLoadContext.LoadAssemblyFromPath(codeBase);
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "TestLoggerManager: Error occured while loading the Logger assembly : {0} , Exception Details : {1}", codeBase, ex);
                return false;
            }

            var loggerType =
                assembly?.GetTypes()
                    .FirstOrDefault(x => x.AssemblyQualifiedName.Equals(assemblyQualifiedName));

            object logger = null;

            try
            {
                var constructorInfo = loggerType?.GetConstructor(Type.EmptyTypes);
                logger = constructorInfo?.Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "TestLoggerManager: Error occured while creating logger instance: {0} , Exception Details : {1}", loggerType, ex);
                return false;
            }

            if (logger == null)
            {
                EqtTrace.Error(
                    "TestLoggerManager: Unable to find Logger with assemblyQualifieldName: {0}, CodeBase : {1}", assemblyQualifiedName, codeBase);
                return false;
            }

            // If the logger has already been initialized just return.
            if (this.initializedLoggers.Contains(logger.GetType()))
            {
                EqtTrace.Verbose("TestLoggerManager: Skipping duplicate logger initialization: {0}", logger.GetType());
                return false;
            }

            // Get Logger instance and initialize.
            try
            {
                switch (logger)
                {
                    case ITestLoggerWithParameters _:
                        ((ITestLoggerWithParameters) logger).Initialize(loggerEvents,
                            UpdateLoggerParameters(parameters));
                        break;

                    case ITestLogger _:
                        ((ITestLogger) logger).Initialize(loggerEvents,
                            GetResultsDirectory(RunSettingsManager.Instance.ActiveRunSettings));
                        break;

                    default:
                        // If logger is of different type, then logger should not be initialized.
                        EqtTrace.Error(
                            "TestLoggerManager: Incorrect logger type: {0}", logger.GetType());
                        return false;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "TestLoggerManager: Error while initializing logger: {0}, Exception details: {1}", logger.GetType(), ex);

                this.messageLogger.SendMessage(
                    TestMessageLevel.Error,
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommonResources.LoggerInitializationError,
                        "type",
                        logger.GetType(),
                        ex));
                return false;
            }

            this.initializedLoggers.Add(logger.GetType());
            return true;
        }

        private bool InitializeLogger(ITestLogger logger, string extensionUri, Dictionary<string, string> parameters)
        {
            try
            {
                if (logger is ITestLoggerWithParameters)
                {
                    ((ITestLoggerWithParameters)logger).Initialize(this.loggerEvents, this.UpdateLoggerParameters(parameters));
                }
                else
                {
                    ((ITestLogger)logger).Initialize(this.loggerEvents, this.GetResultsDirectory(RunSettingsManager.Instance.ActiveRunSettings));
                }
            }
            catch (Exception e)
            {
                this.messageLogger.SendMessage(
                    TestMessageLevel.Error,
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommonResources.LoggerInitializationError,
                        "uri",
                        extensionUri,
                        e));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get uri of the logger corresponding to the friendly name. If no such logger exists return null.
        /// </summary>
        /// <param name="friendlyName">The friendly Name.</param>
        /// <param name="loggerUri">The logger Uri.</param>
        /// <returns><see cref="bool"/></returns>
        public bool TryGetUriFromFriendlyName(string friendlyName, out Uri loggerUri)
        {
            var extensionManager = this.TestLoggerExtensionManager;
            foreach (var extension in extensionManager.TestExtensions)
            {
                if (string.Compare(friendlyName, extension.Metadata.FriendlyName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    try
                    {
                        loggerUri = new Uri(extension.Metadata.ExtensionUri);
                    }
                    catch (UriFormatException)
                    {
                        EqtTrace.Error(
                            string.Format(
                                CultureInfo.CurrentUICulture,
                                CommonResources.LoggerUriInvalid,
                                extension.Metadata.ExtensionUri));
                        loggerUri = null;
                        return false;
                    }

                    return true;
                }
            }

            loggerUri = null;
            return false;
        }

        ///// <summary>
        ///// Registers to receive events from the provided test run request.
        ///// These events will then be broadcast to any registered loggers.
        ///// </summary>
        ///// <param name="testRunRequest">The run request to register for events on.</param>
        //public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
        //{
        //    ValidateArg.NotNull<ITestRunRequest>(testRunRequest, "testRunRequest");

        //    this.CheckDisposed();

        //    // Keep track of the run requests so we can unregister for the
        //    // events when disposed.
        //    this.runRequest = testRunRequest;

        //    // Redirect the events to the InternalTestLoggerEvents
        //    testRunRequest.TestRunMessage += this.TestRunMessageHandler;
        //    testRunRequest.OnRunStart += this.TestRunStartHandler;
        //    testRunRequest.OnRunStatsChange += this.TestRunStatsChangedHandler;
        //    testRunRequest.OnRunCompletion += this.TestRunCompleteHandler;
        //    testRunRequest.DataCollectionMessage += this.DataCollectionMessageHandler;
        //}

        ///// <summary>
        ///// Registers to receive discovery events from discovery request.
        ///// These events will then be broadcast to any registered loggers.
        ///// </summary>
        ///// <param name="discoveryRequest">The discovery request to register for events on.</param>
        //public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        //{
        //    ValidateArg.NotNull<IDiscoveryRequest>(discoveryRequest, "discoveryRequest");

        //    this.CheckDisposed();
        //    this.discoveryRequest = discoveryRequest;
        //    discoveryRequest.OnDiscoveryMessage += this.DiscoveryMessageHandler;
        //    discoveryRequest.OnDiscoveryStart += this.DiscoveryStartHandler;
        //    discoveryRequest.OnDiscoveredTests += this.DiscoveredTestsHandler;
        //    discoveryRequest.OnDiscoveryComplete += this.DiscoveryCompleteHandler;
        //}

        ///// <summary>
        ///// Unregisters the events from the test run request. 
        ///// </summary>
        ///// <param name="testRunRequest">The run request from which events should be unregistered.</param>
        //public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
        //{
        //    ValidateArg.NotNull<ITestRunRequest>(testRunRequest, "testRunRequest");

        //    testRunRequest.TestRunMessage -= this.TestRunMessageHandler;
        //    testRunRequest.OnRunStart -= this.TestRunStartHandler;
        //    testRunRequest.OnRunStatsChange -= this.TestRunStatsChangedHandler;
        //    testRunRequest.OnRunCompletion -= this.TestRunCompleteHandler;
        //    testRunRequest.DataCollectionMessage -= this.DataCollectionMessageHandler;
        //}

        ///// <summary>
        ///// Unregister the events from the discovery request.
        ///// </summary>
        ///// <param name="discoveryRequest">The discovery request from which events should be unregistered.</param>
        //public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        //{
        //    ValidateArg.NotNull<IDiscoveryRequest>(discoveryRequest, "discoveryRequest");
        //    discoveryRequest.OnDiscoveryMessage -= this.DiscoveryMessageHandler;
        //    discoveryRequest.OnDiscoveryStart -= this.DiscoveryStartHandler;
        //    discoveryRequest.OnDiscoveredTests -= this.DiscoveredTestsHandler;
        //    discoveryRequest.OnDiscoveryComplete -= this.DiscoveryCompleteHandler;
        //}

        /// <summary>
        /// Enables sending of events to the loggers which are registered.
        /// </summary>
        /// <remarks>
        /// By default events are disabled and will not be raised until this method is called.
        /// This is done because during logger initialization, errors could be sent and we do not
        /// want them broadcast out to the loggers until all loggers have been enabled.  Without this
        /// all loggers would not receive the errors which were sent prior to initialization finishing.
        /// </remarks>
        public void EnableLogging()
        {
            this.CheckDisposed();
            this.loggerEvents.EnableEvents();
        }

        /// <summary>
        /// Ensure that all pending messages are sent to the loggers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        ///// <summary>
        ///// Sends the error message to all registered loggers.
        ///// This is required so that out of test run execution errors 
        ///// can also mark test run test run failure.
        ///// </summary>
        ///// <param name="e">
        ///// The e.
        ///// </param>
        //public void SendTestRunMessage(TestRunMessageEventArgs e)
        //{
        //    this.HandleTestRunMessage(e);
        //}

        /// <summary>
        /// Ensure that all pending messages are sent to the loggers.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    //// Unregister from runrequests.
                    //if (this.runRequest != null)
                    //{
                    //    this.runRequest.TestRunMessage -= this.TestRunMessageHandler;
                    //    this.runRequest.OnRunStart -= this.TestRunStartHandler;
                    //    this.runRequest.OnRunStatsChange -= this.TestRunStatsChangedHandler;
                    //    this.runRequest.OnRunCompletion -= this.TestRunCompleteHandler;
                    //    this.runRequest.DataCollectionMessage -= this.DataCollectionMessageHandler;
                    //}

                    //if (this.discoveryRequest != null)
                    //{
                    //    this.discoveryRequest.OnDiscoveryMessage -= this.DiscoveryMessageHandler;
                    //    this.discoveryRequest.OnDiscoveryStart -= this.DiscoveryStartHandler;
                    //    this.discoveryRequest.OnDiscoveredTests -= this.DiscoveredTestsHandler;
                    //    this.discoveryRequest.OnDiscoveryComplete -= this.DiscoveryCompleteHandler;
                    //}

                    this.loggerEvents.Dispose();
                }

                this.isDisposed = true;
            }
        }

        #endregion

        #region Private Members

        /// <summary> 
        /// Gets the test results directory. 
        /// </summary> 
        /// <param name="runSettings">Test run settings.</param> 
        /// <returns>Test results directory</returns>
        internal string GetResultsDirectory(RunSettings runSettings)
        {
            string resultsDirectory = null;
            if (runSettings != null)
            {
                try
                {
                    RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings.SettingsXml);
                    resultsDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);
                }
                catch (SettingsException se)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("TestLoggerManager.GetResultsDirectory: Unable to get the test results directory: Error {0}", se);
                    }
                }
            }

            return resultsDirectory;
        }

        /// <summary>
        /// Populates user supplied and default logger parameters.
        /// </summary>
        private Dictionary<string, string> UpdateLoggerParameters(Dictionary<string, string> parameters)
        {
            var loggerParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (parameters != null)
            {
                loggerParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
            }

            // Add default logger parameters...
            loggerParams[DefaultLoggerParameterNames.TestRunDirectory] = this.GetResultsDirectory(RunSettingsManager.Instance.ActiveRunSettings);
            return loggerParams;
        }

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(typeof(TestLoggerManager).FullName);
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles test run message event.
        /// </summary>
        public void HandleTestRunMessage(TestRunMessageEventArgs e)
        {
            // TODO: UTs for disposed
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseTestRunMessage(e);
            }
        }

        /// <summary>
        /// Handle test run stats change event.
        /// </summary>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs e)
        {
            if (!this.isDisposed)
            {
                foreach (TestResult result in e.NewTestResults)
                {
                    this.loggerEvents.RaiseTestResult(new TestResultEventArgs(result));
                }
            }
        }

        /// <summary>
        /// Handles test run start event.
        /// </summary>
        public void HandleTestRunStart(TestRunStartEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseTestRunStart(e);
            }
        }

        /// <summary>
        /// Handles test run complete.
        /// </summary>
        public void HandleTestRunComplete(TestRunCompleteEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.CompleteTestRun(e.TestRunStatistics, e.IsCanceled, e.IsAborted, e.Error, e.AttachmentSets, e.ElapsedTimeInRunningTests);
            }
        }

        /// <summary>
        /// Handles discovery message event.
        /// </summary>
        /// <param name="e"></param>
        public void HandleDiscoveryMessage(TestRunMessageEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseDiscoveryMessage(e);
            }
        }

        /// <summary>
        /// Handle discovered tests.
        /// </summary>
        /// <param name="e"></param>
        public void HandleDiscoveredTests(DiscoveredTestsEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseDiscoveredTests(e);
            }
        }

        /// <summary>
        /// Handles discovery complete event.
        /// </summary>
        /// <param name="e"></param>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseDiscoveryComplete(e);
            }
        }

        /// <summary>
        /// Handles discovery start event.
        /// </summary>
        /// <param name="e"></param>
        public void HandleDiscoveryStart(DiscoveryStartEventArgs e)
        {
            if (!this.isDisposed)
            {
                this.loggerEvents.RaiseDiscoveryStart(e);
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// Class to store logger information
        /// </summary>
        protected class LoggerInfo
        {
            public string argument;
            public string loggerIdentifier;
            public Dictionary<string, string> parameters = new Dictionary<string, string>();

            /// <summary>
            /// Initializes a new instance of the <see cref="LoggerInfo"/> class.
            /// </summary>
            /// <param name="argument"> the actual argument pass by the user through --logger argument</param>
            /// <param name="loggerIdentifier">friendly name of the logger</param>
            /// <param name="parameters">parameter passed to logger</param>
            public LoggerInfo(string argument, string loggerIdentifier, Dictionary<string, string> parameters)
            {
                this.argument = argument;
                this.loggerIdentifier = loggerIdentifier;
                this.parameters = parameters;
            }
        }
    }
}
