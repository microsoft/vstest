// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Manages data collection.
    /// </summary>
    internal class DataCollectionManager : IDataCollectionManager
    {
        private static object syncObject = new object();

        /// <summary>
        /// Value indicating whether data collection is currently enabled.
        /// </summary>
        private bool isDataCollectionEnabled;

        /// <summary>
        /// Data collection environment context.
        /// </summary>
        private DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        /// <summary>
        /// Attachment manager for performing file transfers for datacollectors.
        /// </summary>
        private IDataCollectionAttachmentManager attachmentManager;

        /// <summary>
        /// Message sink for sending data collection messages to client..
        /// </summary>
        private IMessageSink messageSink;

        /// <summary>
        /// Events that can be subscribed by datacollectors.
        /// </summary>
        private TestPlatformDataCollectionEvents events;

        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Extension manager for data collectors.
        /// </summary>
        private DataCollectorExtensionManager dataCollectorExtensionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        /// <param name="messageSink">
        /// The message Sink.
        /// </param>
        internal DataCollectionManager(IMessageSink messageSink) : this(new DataCollectionAttachmentManager(), messageSink)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        /// <param name="datacollectionAttachmentManager">
        /// The datacollection Attachment Manager.
        /// </param>
        /// <param name="messageSink">
        /// The message Sink.
        /// </param>
        /// <remarks>
        /// The constructor is not public because the factory method should be used to get instances of this class.
        /// </remarks>
        protected DataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink)
        {
            this.attachmentManager = datacollectionAttachmentManager;
            this.messageSink = messageSink;
            this.events = new TestPlatformDataCollectionEvents();
            this.dataCollectorExtensionManager = null;
            this.RunDataCollectors = new Dictionary<Type, DataCollectorInformation>();
        }

        /// <summary>
        /// Gets the instance of DataCollectionManager.
        /// </summary>
        public static DataCollectionManager Instance { get; private set; }

        /// <summary>
        /// Gets cache of data collectors associated with the run.
        /// </summary>
        internal Dictionary<Type, DataCollectorInformation> RunDataCollectors { get; private set; }

        /// <summary>
        /// Gets the data collector extension manager.
        /// </summary>
        private DataCollectorExtensionManager DataCollectorExtensionManager
        {
            get
            {
                if (this.dataCollectorExtensionManager == null)
                {
                    // todo : change IMessageSink and use IMessageLogger instead.
                    this.dataCollectorExtensionManager = DataCollectorExtensionManager.Create(TestSessionMessageLogger.Instance);
                }

                return this.dataCollectorExtensionManager;
            }
        }

        /// <summary>
        /// Creates an instance of the TestLoggerExtensionManager.
        /// </summary>
        /// <param name="messageSink">
        /// The message sink.
        /// </param>
        /// <returns>
        /// The <see cref="DataCollectionManager"/>.
        /// </returns>
        public static DataCollectionManager Create(IMessageSink messageSink)
        {
            if (Instance == null)
            {
                lock (syncObject)
                {
                    if (Instance == null)
                    {
                        Instance = new DataCollectionManager(messageSink);
                    }
                }
            }

            return Instance;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> InitializeDataCollectors(string settingsXml)
        {
            if (string.IsNullOrEmpty(settingsXml) && EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DataCollectionManager.InitializeDataCollectors : Runsettings is null or empty.");
            }

            ValidateArg.NotNull(settingsXml, "settingsXml");

            var sessionId = new SessionId(Guid.NewGuid());
            var dataCollectionContext = new DataCollectionContext(sessionId);
            this.dataCollectionEnvironmentContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(dataCollectionContext);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);
            var resultsDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);

            this.attachmentManager.Initialize(sessionId, resultsDirectory, this.messageSink);

            // Enviornment variables are passed to testhost process, through ProcessStartInfo.EnvironmentVariables, which handles the key in a case-insensitive manner, which is translated to lowercase.
            // Therefore, using StringComparer.OrdinalIgnoreCase so that same keys with different cases are treated as same.
            var executionEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settingsXml);

            this.isDataCollectionEnabled = dataCollectionRunSettings.IsCollectionEnabled;

            // If dataCollectionRunSettings is null, that means datacollectors are not configured.
            if (dataCollectionRunSettings == null || !dataCollectionRunSettings.IsCollectionEnabled)
            {
                return executionEnvironmentVariables;
            }

            // Get settings for each data collector, load and initialize the data collectors.
            var enabledDataCollectorsSettings = this.GetDataCollectorsEnabledForRun(dataCollectionRunSettings);
            if (enabledDataCollectorsSettings == null || enabledDataCollectorsSettings.Count == 0)
            {
                return executionEnvironmentVariables;
            }

            foreach (var dataCollectorSettings in enabledDataCollectorsSettings)
            {
                this.LoadAndInitialize(dataCollectorSettings, settingsXml);
            }

            // Once all data collectors have been initialized, query for environment variables
            bool unloadedAnyCollector;
            var dataCollectorEnvironmentVariables = this.GetEnvironmentVariables(out unloadedAnyCollector);

            foreach (var variable in dataCollectorEnvironmentVariables.Values)
            {
                executionEnvironmentVariables.Add(variable.Name, variable.Value);
            }

            return executionEnvironmentVariables;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public Collection<AttachmentSet> SessionEnded(bool isCancelled = false)
        {
            // Return null if datacollection is not enabled.
            if (!this.isDataCollectionEnabled)
            {
                return new Collection<AttachmentSet>();
            }

            if (isCancelled)
            {
                this.attachmentManager.Cancel();
                return new Collection<AttachmentSet>();
            }

            var endEvent = new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext);
            this.SendEvent(endEvent);

            var result = new List<AttachmentSet>();
            try
            {
                result = this.attachmentManager.GetAttachments(endEvent.Context);
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.SessionEnded: Failed to get attachments : {0}", ex);
                }

                return new Collection<AttachmentSet>(result);
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                this.LogAttachments(result);
            }

            return new Collection<AttachmentSet>(result);
        }

        /// <inheritdoc/>
        public void TestHostLaunched(int processId)
        {
            if (!this.isDataCollectionEnabled)
            {
                return;
            }

            var testHostLaunchedEventArgs = new TestHostLaunchedEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext, processId);

            this.SendEvent(testHostLaunchedEventArgs);
        }

        /// <inheritdoc/>
        public bool SessionStarted(SessionStartEventArgs sessionStartEventArgs)
        {
            // If datacollectors are not configured or datacollection is not enabled, return false.
            if (!this.isDataCollectionEnabled || this.RunDataCollectors.Count == 0)
            {
                return false;
            }

            sessionStartEventArgs.Context = new DataCollectionContext(this.dataCollectionEnvironmentContext.SessionDataCollectionContext.SessionId);
            this.SendEvent(sessionStartEventArgs);

            return this.events.AreTestCaseEventsSubscribed();
        }

        /// <inheritdoc/>
        public void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs)
        {
            if (!this.isDataCollectionEnabled)
            {
                return;
            }

            var context = new DataCollectionContext(this.dataCollectionEnvironmentContext.SessionDataCollectionContext.SessionId, testCaseStartEventArgs.TestElement);
            testCaseStartEventArgs.Context = context;

            this.SendEvent(testCaseStartEventArgs);
        }

        /// <inheritdoc/>
        public Collection<AttachmentSet> TestCaseEnded(TestCaseEndEventArgs testCaseEndEventArgs)
        {
            if (!this.isDataCollectionEnabled)
            {
                return new Collection<AttachmentSet>();
            }

            var context = new DataCollectionContext(this.dataCollectionEnvironmentContext.SessionDataCollectionContext.SessionId, testCaseEndEventArgs.TestElement);
            testCaseEndEventArgs.Context = context;

            this.SendEvent(testCaseEndEventArgs);

            List<AttachmentSet> result = null;
            try
            {
                result = this.attachmentManager.GetAttachments(testCaseEndEventArgs.Context);
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.TestCaseEnded: Failed to get attachments : {0}", ex);
                }

                return new Collection<AttachmentSet>(result);
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                this.LogAttachments(result);
            }

            return new Collection<AttachmentSet>(result);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    CleanupPlugins();
                }

                this.disposed = true;
            }
        }

        private void CleanupPlugins()
        {
            EqtTrace.Info("DataCollectionManager.CleanupPlugins: CleanupPlugins called");

            if (!this.isDataCollectionEnabled)
            {
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionManager.CleanupPlugins: Cleaning up {0} plugins", this.RunDataCollectors.Count);
            }

            RemoveDataCollectors(new List<DataCollectorInformation>(this.RunDataCollectors.Values));

            EqtTrace.Info("DataCollectionManager.CleanupPlugins: CleanupPlugins finished");
        }

        #region Load and Initialize DataCollectors

        /// <summary>
        /// Tries to get uri of the data collector corresponding to the friendly name. If no such data collector exists return null.
        /// </summary>
        /// <param name="friendlyName">The friendly Name.</param>
        /// <param name="dataCollectorUri">The data collector Uri.</param>
        /// <returns><see cref="bool"/></returns>
        protected virtual bool TryGetUriFromFriendlyName(string friendlyName, out string dataCollectorUri)
        {
            var extensionManager = this.dataCollectorExtensionManager;
            foreach (var extension in extensionManager.TestExtensions)
            {
                if (string.Compare(friendlyName, extension.Metadata.FriendlyName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    dataCollectorUri = extension.Metadata.ExtensionUri;
                    return true;
                }
            }

            dataCollectorUri = null;
            return false;
        }

        /// <summary>
        /// Gets the extension using uri.
        /// </summary>
        /// <param name="extensionUri">
        /// The extension uri.
        /// </param>
        /// <returns>
        /// The <see cref="DataCollector"/>.
        /// </returns>
        protected virtual DataCollector TryGetTestExtension(string extensionUri)
        {
            return this.DataCollectorExtensionManager.TryGetTestExtension(extensionUri).Value;
        }

        /// <summary>
        /// Loads and initializes data collector using data collector settings.
        /// </summary>
        /// <param name="dataCollectorSettings">
        /// The data collector settings.
        /// </param>
        /// <param name="settingsXml"> runsettings Xml</param>
        private void LoadAndInitialize(DataCollectorSettings dataCollectorSettings, string settingsXml)
        {
            DataCollectorInformation dataCollectorInfo;
            DataCollectorConfig dataCollectorConfig;

            try
            {
                // Look up the extension and initialize it if one is found.
                var extensionManager = this.DataCollectorExtensionManager;
                var dataCollectorUri = string.Empty;
                this.TryGetUriFromFriendlyName(dataCollectorSettings.FriendlyName, out dataCollectorUri);

                DataCollector dataCollector = null;
                if (!string.IsNullOrWhiteSpace(dataCollectorUri))
                {
                    dataCollector = this.TryGetTestExtension(dataCollectorUri);
                }

                if (dataCollector == null)
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.DataCollectorNotFound, dataCollectorSettings.FriendlyName));
                    return;
                }

                if (this.RunDataCollectors.ContainsKey(dataCollector.GetType()))
                {
                    // Collector is already loaded (may be configured twice). Ignore duplicates and return.
                    return;
                }

                dataCollectorConfig = new DataCollectorConfig(dataCollector.GetType());

                // Attempt to get the data collector information verifying that all of the required metadata for the collector is available.
                dataCollectorInfo = new DataCollectorInformation(
                                                    dataCollector,
                                                    dataCollectorSettings.Configuration,
                                                    dataCollectorConfig,
                                                    this.dataCollectionEnvironmentContext,
                                                    this.attachmentManager,
                                                    this.events,
                                                    this.messageSink,
                                                    settingsXml);
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.LoadAndInitialize: exception while creating data collector {0} : {1}", dataCollectorSettings.FriendlyName, ex);
                }

                // No data collector info, so send the error with no direct association to the collector.
                this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.DataCollectorInitializationError, dataCollectorSettings.FriendlyName, ex));
                return;
            }

            try
            {
                dataCollectorInfo.InitializeDataCollector();
                lock (this.RunDataCollectors)
                {
                    // Add data collectors to run cache.
                    this.RunDataCollectors[dataCollectorConfig.DataCollectorType] = dataCollectorInfo;
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.LoadAndInitialize: exception while initializing data collector {0} : {1}", dataCollectorSettings.FriendlyName, ex);
                }

                // Log error.
                dataCollectorInfo.Logger.LogError(this.dataCollectionEnvironmentContext.SessionDataCollectionContext, string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorInitializationError, dataCollectorConfig.FriendlyName, ex));

                // Dispose datacollector.
                dataCollectorInfo.DisposeDataCollector();
            }
        }

        /// <summary>
        /// Finds data collector enabled for the run in data collection settings.
        /// </summary>
        /// <param name="dataCollectionSettings">data collection settings</param>
        /// <returns>List of enabled data collectors</returns>
        private List<DataCollectorSettings> GetDataCollectorsEnabledForRun(DataCollectionRunSettings dataCollectionSettings)
        {
            var runEnabledDataCollectors = new List<DataCollectorSettings>();
            foreach (var settings in dataCollectionSettings.DataCollectorSettingsList)
            {
                if (settings.IsEnabled)
                {
                    if (runEnabledDataCollectors.Any(dcSettings => string.Equals(dcSettings.FriendlyName, settings.FriendlyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                        this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.IgnoredDuplicateConfiguration, settings.FriendlyName));
                        continue;
                    }

                    runEnabledDataCollectors.Add(settings);
                }
            }

            return runEnabledDataCollectors;
        }

        #endregion

        /// <summary>
        /// Sends a warning message against the session which is not associated with a data collector.
        /// </summary>
        /// <remarks>
        /// This should only be used when we do not have the data collector info yet.  After we have the data
        /// collector info we can use the data collectors logger for errors.
        /// </remarks>
        /// <param name="warningMessage">The message to be logged.</param>
        private void LogWarning(string warningMessage)
        {
            this.messageSink.SendMessage(new DataCollectionMessageEventArgs(TestMessageLevel.Warning, warningMessage));
        }

        /// <summary>
        /// Sends the event to all data collectors and fires a callback on the sender, letting it
        /// know when all plugins have completed processing the event
        /// </summary>
        /// <param name="args">The context information for the event</param>
        private void SendEvent(DataCollectionEventArgs args)
        {
            ValidateArg.NotNull(args, nameof(args));

            if (!this.isDataCollectionEnabled)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManger:SendEvent: SendEvent called when no collection is enabled.");
                }

                return;
            }

            // do not send events multiple times
            this.events.RaiseEvent(args);
        }

        /// <summary>
        /// The get environment variables.
        /// </summary>
        /// <param name="unloadedAnyCollector">
        /// The unloaded any collector.
        /// </param>
        /// <returns>
        /// Dictionary of variable name as key and collector requested environment variable as value.
        /// </returns>
        private Dictionary<string, DataCollectionEnvironmentVariable> GetEnvironmentVariables(out bool unloadedAnyCollector)
        {
            var failedCollectors = new List<DataCollectorInformation>();
            unloadedAnyCollector = false;
            var dataCollectorEnvironmentVariable = new Dictionary<string, DataCollectionEnvironmentVariable>(StringComparer.OrdinalIgnoreCase);
            foreach (var dataCollectorInfo in this.RunDataCollectors.Values)
            {
                try
                {
                    dataCollectorInfo.SetTestExecutionEnvironmentVariables();
                    this.AddCollectorEnvironmentVariables(dataCollectorInfo, dataCollectorEnvironmentVariable);
                }
                catch (Exception ex)
                {
                    unloadedAnyCollector = true;

                    var friendlyName = dataCollectorInfo.DataCollectorConfig.FriendlyName;
                    failedCollectors.Add(dataCollectorInfo);
                    dataCollectorInfo.Logger.LogError(
                        this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                        string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorErrorOnGetVariable, friendlyName, ex));

                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("DataCollectionManager.GetEnvironmentVariables: Failed to get variable for Collector '{0}': {1}", friendlyName, ex);
                    }
                }
            }

            this.RemoveDataCollectors(failedCollectors);
            return dataCollectorEnvironmentVariable;
        }

        /// <summary>
        /// Collects environment variable to be set in test process by avoiding duplicates
        /// and detecting override of variable value by multiple adapters.
        /// </summary>
        /// <param name="dataCollectionWrapper">
        /// The data Collection Wrapper.
        /// </param>
        /// <param name="dataCollectorEnvironmentVariables">
        /// Environment variables required for already loaded plugin.
        /// </param>
        private void AddCollectorEnvironmentVariables(
            DataCollectorInformation dataCollectionWrapper,
            Dictionary<string, DataCollectionEnvironmentVariable> dataCollectorEnvironmentVariables)
        {
            if (dataCollectionWrapper.TestExecutionEnvironmentVariables != null)
            {
                var collectorFriendlyName = dataCollectionWrapper.DataCollectorConfig.FriendlyName;
                foreach (var namevaluepair in dataCollectionWrapper.TestExecutionEnvironmentVariables)
                {
                    DataCollectionEnvironmentVariable alreadyRequestedVariable;
                    if (dataCollectorEnvironmentVariables.TryGetValue(namevaluepair.Key, out alreadyRequestedVariable))
                    {
                        // Dev10 behavior is to consider environment variables values as case sensitive.
                        if (string.Equals(namevaluepair.Value, alreadyRequestedVariable.Value, StringComparison.Ordinal))
                        {
                            alreadyRequestedVariable.AddRequestingDataCollector(collectorFriendlyName);
                        }
                        else
                        {
                            // Data collector is overriding an already requested variable, possibly an error.                            
                            dataCollectionWrapper.Logger.LogError(
                                this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                                string.Format(
                                    CultureInfo.CurrentUICulture,
                                    Resources.Resources.DataCollectorRequestedDuplicateEnvironmentVariable,
                                    collectorFriendlyName,
                                    namevaluepair.Key,
                                    namevaluepair.Value,
                                    alreadyRequestedVariable.FirstDataCollectorThatRequested,
                                    alreadyRequestedVariable.Value));
                        }
                    }
                    else
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            // new variable, add to the list.
                            EqtTrace.Verbose("DataCollectionManager.AddCollectionEnvironmentVariables: Adding Environment variable '{0}' value '{1}'", namevaluepair.Key, namevaluepair.Value);
                        }

                        dataCollectorEnvironmentVariables.Add(
                            namevaluepair.Key,
                            new DataCollectionEnvironmentVariable(namevaluepair, collectorFriendlyName));
                    }
                }
            }
        }

        /// <summary>
        /// The remove data collectors.
        /// </summary>
        /// <param name="dataCollectorsToRemove">
        /// The data collectors to remove.
        /// </param>
        private void RemoveDataCollectors(IReadOnlyCollection<DataCollectorInformation> dataCollectorsToRemove)
        {
            if (dataCollectorsToRemove == null || !dataCollectorsToRemove.Any())
            {
                return;
            }

            lock (this.RunDataCollectors)
            {
                foreach (var dataCollectorToRemove in dataCollectorsToRemove)
                {
                    dataCollectorToRemove.DisposeDataCollector();
                    this.RunDataCollectors.Remove(dataCollectorToRemove.DataCollector.GetType());
                }

                if (this.RunDataCollectors.Count == 0)
                {
                    this.isDataCollectionEnabled = false;
                }
            }
        }

        private void LogAttachments(List<AttachmentSet> attachmentSets)
        {
            foreach (var entry in attachmentSets)
            {
                foreach (var file in entry.Attachments)
                {
                    EqtTrace.Verbose(
                        "Test Attachment Description: Collector:'{0}'  Uri:'{1}'  Description:'{2}' Uri:'{3}' ",
                        entry.DisplayName,
                        entry.Uri,
                        file.Description,
                        file.Uri);
                }
            }
        }
    }
}
