// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;

    /// <summary>
    /// Responsible for managing logger extensions and broadcasting results
    /// and error/warning/informational messages to them.
    /// </summary>
    internal class TestLoggerManager : ITestDiscoveryEventsRegistrar, ITestRunEventsRegistrar, IDisposable
    {
        #region Fields

        private static readonly object Synclock = new object();
        private static TestLoggerManager testLoggerManager;

        /// <summary>
        /// Test Logger Events instance which will be passed to loggers when they are initialized.
        /// </summary>
        private InternalTestLoggerEvents loggerEvents;

        /// <summary>
        /// Used to keep track of which loggers have been initialized.
        /// </summary>
        private HashSet<String> initializedLoggers = new HashSet<String>();

        /// <summary>
        /// Keeps track if we are disposed.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Run request that we have registered for events on.  Used when
        /// disposing to unregister for the events.
        /// </summary>
        private ITestRunRequest runRequest = null;

        /// <summary>
        /// Gets an instance of the logger.
        /// </summary>
        private IMessageLogger messageLogger;

        private TestLoggerExtensionManager testLoggerExtensionManager;
        private IDiscoveryRequest discoveryRequest;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TestLoggerManager(TestSessionMessageLogger sessionLogger, InternalTestLoggerEvents loggerEvents)
        {
            this.messageLogger = sessionLogger;
            this.testLoggerExtensionManager = TestLoggerExtensionManager.Create(messageLogger);
            this.loggerEvents = loggerEvents;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static TestLoggerManager Instance
        {
            get
            {
                if (testLoggerManager == null)
                {
                    lock (Synclock)
                    {
                        if (testLoggerManager == null)
                        {
                            testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance,
                                new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
                        }
                    }
                }
                else if (TestPluginCache.Instance.AreDefaultExtensionsDiscovered == false)
                {
                    lock (Synclock)
                    {
                        // If extension path has been modified, update testLoggerExtensionManager
                        testLoggerManager.testLoggerExtensionManager = TestLoggerExtensionManager.Create(testLoggerManager.messageLogger);
                    }
                }
                return testLoggerManager;
            }

            protected set
            {
                testLoggerManager = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the logger events.
        /// </summary>
        public TestLoggerEvents LoggerEvents
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
        protected HashSet<string> InitializedLoggers
        {
            get
            {
                return this.initializedLoggers;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add and initialize the logger with the given parameters
        /// </summary>
        /// <param name="logger">The logger that needs to be initialized</param>
        /// <param name="extensionUri">URI of the logger</param>
        /// <param name="parameters">Logger parameters</param>
        public void AddLogger(ITestLogger logger, string extensionUri, Dictionary<string, string> parameters)
        {
            this.CheckDisposed();

            // If the logger has already been initialized just return.
            if (this.initializedLoggers.Contains(extensionUri, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }
            this.initializedLoggers.Add(extensionUri);
            InitializeLogger(logger, extensionUri, parameters);
        }

        /// <summary>
        /// Adds the logger with the specified URI and parameters.
        /// For ex. TfsPublisher takes parameters such as  Platform, Flavor etc.
        /// </summary>
        /// <param name="uri">URI of the logger to add.</param>
        /// <param name="parameters">Logger parameters.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification = "Case insensitive needs to be supported "), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Third party loggers could potentially throw all kinds of exceptions.")]
        public void AddLogger(Uri uri, Dictionary<string, string> parameters)
        {
            ValidateArg.NotNull<Uri>(uri, "uri");

            this.CheckDisposed();

            // If the logger has already been initialized just return.
            if (this.initializedLoggers.Contains(uri.AbsoluteUri, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }
            this.initializedLoggers.Add(uri.AbsoluteUri);

            // Look up the extension and initialize it if one is found.
            var extensionManager = this.testLoggerExtensionManager;
            var logger = extensionManager.TryGetTestExtension(uri.AbsoluteUri);

            if (logger == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentUICulture,
                        CommonResources.LoggerNotFound,
                        uri.OriginalString));
            }

            InitializeLogger(logger.Value, logger.Metadata.ExtensionUri, parameters);
        }

        private void InitializeLogger(ITestLogger logger, string extensionUri, Dictionary<string, string> parameters)
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
                        extensionUri,
                        e));
            }
        }

        /// <summary>
        /// Tries to get uri of the logger corresponding to the friendly name. If no such logger exists return null.
        /// </summary>
        /// <param name="friendlyName">The friendly Name.</param>
        /// <param name="loggerUri">The logger Uri.</param>
        /// <returns><see cref="bool"/></returns>
        public bool TryGetUriFromFriendlyName(string friendlyName, out string loggerUri)
        {
            var extensionManager = this.testLoggerExtensionManager;
            foreach (var extension in extensionManager.TestExtensions)
            {
                if (string.Compare(friendlyName, extension.Metadata.FriendlyName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    loggerUri = extension.Metadata.ExtensionUri;
                    return true;
                }
            }

            loggerUri = null;
            return false;
        }

        /// <summary>
        /// Registers to receive events from the provided test run request.
        /// These events will then be broadcast to any registered loggers.
        /// </summary>
        /// <param name="testRunRequest">The run request to register for events on.</param>
        public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
        {
            ValidateArg.NotNull<ITestRunRequest>(testRunRequest, "testRunRequest");

            this.CheckDisposed();

            // Keep track of the run requests so we can unregister for the
            // events when disposed.
            this.runRequest = testRunRequest;

            // Redirect the events to the InternalTestLoggerEvents
            testRunRequest.TestRunMessage += this.TestRunMessageHandler;
            testRunRequest.OnRunStatsChange += this.TestRunStatsChangedHandler;
            testRunRequest.OnRunCompletion += this.TestRunCompleteHandler;
            testRunRequest.DataCollectionMessage += this.DataCollectionMessageHandler;
        }

        /// <summary>
        /// Registers to receive discovery events from discovery request.
        /// These events will then be broadcast to any registered loggers.
        /// </summary>
        /// <param name="discoveryRequest">The discovery request to register for events on.</param>
        public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        {
            ValidateArg.NotNull<IDiscoveryRequest>(discoveryRequest, "discoveryRequest");

            this.CheckDisposed();
            this.discoveryRequest = discoveryRequest;
            discoveryRequest.OnDiscoveryMessage += this.DiscoveryMessageHandler;
        }

        /// <summary>
        /// Unregisters the events from the test run request. 
        /// </summary>
        /// <param name="testRunRequest">The run request from which events should be unregistered.</param>
        public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
        {
            ValidateArg.NotNull<ITestRunRequest>(testRunRequest, "testRunRequest");

            testRunRequest.TestRunMessage -= this.TestRunMessageHandler;
            testRunRequest.OnRunStatsChange -= this.TestRunStatsChangedHandler;
            testRunRequest.OnRunCompletion -= this.TestRunCompleteHandler;
            this.runRequest.DataCollectionMessage -= this.DiscoveryMessageHandler;
        }

        /// <summary>
        /// Unregister the events from the discovery request.
        /// </summary>
        /// <param name="discoveryRequest">The discovery request from which events should be unregistered.</param>
        public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        {
            ValidateArg.NotNull<IDiscoveryRequest>(discoveryRequest, "discoveryRequest");
            discoveryRequest.OnDiscoveryMessage -= this.DiscoveryMessageHandler;
        }

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

        /// <summary>
        /// Sends the error message to all registered loggers.
        /// This is required so that out of test run execution errors 
        /// can also mark test run test run failure.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        public void SendTestRunError(TestRunMessageEventArgs e)
        {
            this.TestRunMessageHandler(null, e);
        }

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
                    // Unregister from runrequests.
                    if (this.runRequest != null)
                    {
                        this.runRequest.TestRunMessage -= this.TestRunMessageHandler;
                        this.runRequest.OnRunStatsChange -= this.TestRunStatsChangedHandler;
                        this.runRequest.OnRunCompletion -= this.TestRunCompleteHandler;
                        this.runRequest.DataCollectionMessage -= this.DiscoveryMessageHandler;
                    }

                    if (this.discoveryRequest != null)
                    {
                        this.discoveryRequest.OnDiscoveryMessage -= this.DiscoveryMessageHandler;
                    }

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
        /// Called when a test run message is received.
        /// </summary>
        private void TestRunMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            this.loggerEvents.RaiseMessage(e);
        }

        /// <summary>
        /// Called when a test run stats are changed.
        /// </summary>
        private void TestRunStatsChangedHandler(object sender, TestRunChangedEventArgs e)
        {
            foreach (TestResult result in e.NewTestResults)
            {
                this.loggerEvents.RaiseTestResult(new TestResultEventArgs(result));
            }
        }

        /// <summary>
        /// Called when a test run is complete.
        /// </summary>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            this.loggerEvents.CompleteTestRun(e.TestRunStatistics, e.IsCanceled, e.IsAborted, e.Error, e.AttachmentSets, e.ElapsedTimeInRunningTests);
        }

        /// <summary>
        /// Called when data collection message is received.
        /// </summary>
        private void DataCollectionMessageHandler(object sender, DataCollectionMessageEventArgs e)
        {
            string message;
            if (null == e.Uri)
            {
                // Message from data collection framework.
                message = string.Format(CultureInfo.CurrentCulture, CommonResources.DataCollectionMessageFormat, e.Message);
            }
            else
            {
                // Message from individual data collector.
                message = string.Format(CultureInfo.CurrentCulture, CommonResources.DataCollectorMessageFormat, e.FriendlyName, e.Message);
            }
            this.TestRunMessageHandler(sender, new TestRunMessageEventArgs(e.Level, message));
        }


        /// <summary>
        /// Send discovery message to all registered listeners.
        /// </summary>
        private void DiscoveryMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            this.loggerEvents.RaiseMessage(e);
        }
        #endregion

        #endregion
    }
}
