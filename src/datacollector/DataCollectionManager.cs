// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
#if !NET46
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Manages data collection.
    /// </summary>
    internal class DataCollectionManager : IDataCollectionManager
    {
        /// <summary>
        /// The source directory.
        /// </summary>
        private static string sourceDirectory;

        /// <summary>
        /// Value indicating whether data collection is currently enabled.
        /// </summary>
        private bool isDataCollectionEnabled;

        /// <summary>
        /// Data collection environment context.
        /// </summary>
        private DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        /// <summary>
        /// Attachment manager for performing file transfer from datacollector.
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
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        internal DataCollectionManager() : this(new DataCollectionAttachmentManager(), new MessageSink())
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
        internal DataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink)
        {
            this.attachmentManager = datacollectionAttachmentManager;
            this.messageSink = messageSink;
            this.events = new TestPlatformDataCollectionEvents();

            this.RunDataCollectors = new Dictionary<Type, DataCollectorWrapper>();
        }

        /// <summary>
        /// Gets cache of data collectors associated with the run.
        /// </summary>
        internal Dictionary<Type, DataCollectorWrapper> RunDataCollectors { get; private set; }

        /// <inheritdoc/>
        public IDictionary<string, string> InitializeDataCollectors(string settingsXml)
        {
            ValidateArg.NotNull(settingsXml, "settingsXml");

            var sessionId = new SessionId(Guid.NewGuid());
            var dataCollectionContext = new DataCollectionContext(sessionId);
            this.dataCollectionEnvironmentContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(dataCollectionContext);

            this.attachmentManager.Initialize(sessionId, sourceDirectory, this.messageSink);

            var executionEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);
            sourceDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);

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
                this.LoadAndInitialize(dataCollectorSettings);
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
        public Collection<AttachmentSet> SessionEnded()
        {
            // Return null if datacollection is not enabled.
            if (!this.isDataCollectionEnabled)
            {
                return null;
            }

            var endEvent = new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext);
            this.SendEvent(endEvent);

            List<AttachmentSet> result = null;
            try
            {
                result = this.attachmentManager.GetAttachments(endEvent.Context);
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.SessionEnded: Failed to get attachments : {0}", ex.Message);
                }

                return null;
            }

            foreach (var entry in result)
            {
                foreach (var file in entry.Attachments)
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "Run Attachment Description: Collector:'{0}'  Uri:'{1}'  Description:'{2}' Uri:'{3}' ",
                            entry.DisplayName,
                            entry.Uri,
                            file.Description,
                            file.Uri);
                    }
                }
            }

            // Dispose attachment manager.
            this.attachmentManager.Dispose();

            return new Collection<AttachmentSet>(result);
        }

        /// <inheritdoc/>
        public bool SessionStarted()
        {
            // If datacollectors are not configured or datacollection is not enabled, return false.
            if (!this.isDataCollectionEnabled || this.RunDataCollectors.Count == 0)
            {
                return false;
            }

            this.SendEvent(new SessionStartEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));

            return true;
        }

        /// <inheritdoc/>
        public Collection<AttachmentSet> TestCaseEnded(TestCase testCase, TestOutcome testOutcome)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs)
        {
            throw new NotImplementedException();
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
                    // todo : Dispose resources here.
                }

                this.disposed = true;
            }
        }

        #region Load and Initialize DataCollectors

        /// <summary>
        /// Helper method that gets the datacollector config.
        /// </summary>
        /// <param name="collectorTypeName">
        /// Type name of the collector
        /// </param>
        /// <param name="codebase">
        /// The codebase.
        /// </param>
        /// <returns>
        /// Type of the collector type name
        /// </returns>
        private static DataCollectorConfig GetDataCollectorConfig(string collectorTypeName, string codebase)
        {
            try
            {
                DataCollectorConfig dataCollectorConfig = null;
                var dataCollectorType = GetDataCollectorType(codebase, collectorTypeName, out var assembly);

                dataCollectorConfig = new DataCollectorConfig(dataCollectorType);

                return dataCollectorConfig;
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.GetDataCollectorConfig: Failed to get type for Collector '{0}': {1}", collectorTypeName, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Helper method that gets the Type and Assembly from datacollector type name and binary path.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path.
        /// </param>
        /// <param name="dataCollectorTypeName">
        /// The collector type name.
        /// </param>
        /// <param name="assembly">
        /// The assembly.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        private static Type GetDataCollectorType(string binaryPath, string dataCollectorTypeName, out Assembly assembly)
        {
            assembly = null;
            Type dctype = null;
            if (File.Exists(binaryPath))
            {
                assembly = LoadAssemblyFromPath(binaryPath);
            }

            dctype = assembly?.GetTypes().FirstOrDefault(type => type.AssemblyQualifiedName != null && type.AssemblyQualifiedName.Equals(dataCollectorTypeName));

            return dctype;
        }

        /// <summary>
        /// Helper method to load assembly from path.
        /// </summary>
        /// <param name="assemblyPath">
        /// The assembly path.
        /// </param>
        /// <returns>
        /// The <see cref="Assembly"/>.
        /// </returns>
        private static Assembly LoadAssemblyFromPath(string assemblyPath)
        {
#if NET46
            return Assembly.LoadFrom(assemblyPath);
#else
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
        }

        /// <summary>
        /// Creates an instance of datacollector of given type.
        /// </summary>
        /// <param name="dataCollectorType">type of collector plugin to instantiate.</param>
        /// <returns>The dataCollector.</returns>
        private static DataCollector CreateDataCollector(Type dataCollectorType)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DataCollectionManager.CreateDataCollector: Attempting to load data collector: " + dataCollectorType);
            }

            try
            {
                var rawPlugin = Activator.CreateInstance(dataCollectorType);

                // Check if this is a data collector.
                var dataCollector = rawPlugin as DataCollector;
                return dataCollector;
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.CreateDataCollector: Could not create instance of type: " + dataCollectorType + "  Exception: " + ex.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Loads and initializes datacollector using datacollector settings.
        /// </summary>
        /// <param name="dataCollectorSettings">
        /// The data collector settings.
        /// </param>
        private void LoadAndInitialize(DataCollectorSettings dataCollectorSettings)
        {
            var collectorTypeName = dataCollectorSettings.AssemblyQualifiedName;
            var collectorDisplayName = string.IsNullOrWhiteSpace(dataCollectorSettings.FriendlyName) ? collectorTypeName : dataCollectorSettings.FriendlyName;
            DataCollectorWrapper dataCollectorWrapper;
            DataCollectorConfig dataCollectorConfig;

            try
            {
                dataCollectorConfig = GetDataCollectorConfig(collectorTypeName, dataCollectorSettings.CodeBase);
            }
            catch (FileNotFoundException)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorAssemblyNotFound, collectorDisplayName));
                return;
            }
            catch (Exception ex)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorTypeNotFound, collectorDisplayName, ex.Message));
                return;
            }

            if (this.RunDataCollectors.ContainsKey(dataCollectorConfig.DataCollectorType))
            {
                // Collector is already loaded (may be configured twice). Ignore duplicates and return.
                return;
            }

            try
            {
                var dataCollector = CreateDataCollector(dataCollectorConfig.DataCollectorType);

                // Attempt to get the data collector information verifying that all of the required metadata for the collector is available.
                dataCollectorWrapper = dataCollectorConfig == null
                                                ? null
                                                : new DataCollectorWrapper(
                                                    dataCollector,
                                                    dataCollectorSettings.Configuration,
                                                    dataCollectorConfig,
                                                    this.dataCollectionEnvironmentContext,
                                                    this.attachmentManager,
                                                    this.events,
                                                    this.messageSink);

                if (dataCollectorWrapper == null || !dataCollectorWrapper.DataCollectorConfig.TypeUri.Equals(dataCollectorSettings.Uri))
                {
                    // If the data collector was not found, send an error.
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorNotFound, dataCollectorConfig.DataCollectorType.FullName, dataCollectorSettings.Uri));
                    return;
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.LoadAndInitialize: exception while creating data collector {0} : {1}", collectorTypeName, ex);
                }

                // No data collector info, so send the error with no direct association to the collector.
                this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.DataCollectorInitializationError, collectorTypeName, ex.Message));
                return;
            }

            try
            {
                dataCollectorWrapper.InitializeDataCollector();
                lock (this.RunDataCollectors)
                {
                    // Add data collectors to run cache.
                    this.RunDataCollectors[dataCollectorConfig.DataCollectorType] = dataCollectorWrapper;
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.LoadAndInitialize: exception while initializing data collector {0}: " + ex, collectorTypeName);
                }

                // Log error.
                dataCollectorWrapper.Logger.LogError(this.dataCollectionEnvironmentContext.SessionDataCollectionContext, string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorInitializationError, dataCollectorConfig.FriendlyName, ex.Message));

                // Dispose datacollector.
                dataCollectorWrapper.DisposeDataCollector();
                return;
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
                    if (runEnabledDataCollectors.Any(dcSettings => dcSettings.Uri.Equals(settings.Uri)
                        || string.Equals(dcSettings.AssemblyQualifiedName, settings.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                        this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.IgnoredDuplicateConfiguration, settings.AssemblyQualifiedName, settings.Uri));
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

            foreach (var dataCollectorWrapper in this.RunDataCollectors.Values)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionManger:SendEvent: Raising event {0} to collector {1}", args.GetType(), dataCollectorWrapper.DataCollectorConfig.FriendlyName);
                }

                try
                {
                    dataCollectorWrapper.Events.RaiseEvent(args);
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("DataCollectionManger:SendEvent: Error while RaiseEvent {0} to datacollector {1} : {2}.", args.GetType(), dataCollectorWrapper.DataCollectorConfig.FriendlyName, ex.Message);
                    }
                }
            }
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
            var failedCollectors = new List<DataCollectorWrapper>();
            unloadedAnyCollector = false;
            var dataCollectorEnvironmentVariable = new Dictionary<string, DataCollectionEnvironmentVariable>(StringComparer.OrdinalIgnoreCase);
            foreach (var dataCollectorInfo in this.RunDataCollectors.Values)
            {
                try
                {
                    this.AddCollectorEnvironmentVariables(dataCollectorInfo, dataCollectorEnvironmentVariable);
                }
                catch (Exception ex)
                {
                    unloadedAnyCollector = true;

                    var dataCollectorType = dataCollectorInfo.DataCollector.GetType();
                    failedCollectors.Add(dataCollectorInfo);
                    dataCollectorInfo.Logger.LogError(
                        this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                        string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorErrorOnGetVariable, dataCollectorType, ex.ToString()));

                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("DataCollectionManager.GetEnvironmentVariables: Failed to get variable for Collector '{0}': {1}", dataCollectorType, ex);
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
            DataCollectorWrapper dataCollectionWrapper,
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
        private void RemoveDataCollectors(IReadOnlyCollection<DataCollectorWrapper> dataCollectorsToRemove)
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
    }
}
