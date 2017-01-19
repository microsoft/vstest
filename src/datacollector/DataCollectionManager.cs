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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The data collection manager.
    /// </summary>
    internal class DataCollectionManager : IDataCollectionManager
    {
        /// <summary>
        /// Gets the source directory.
        /// </summary>
        private static string SourceDirectory;



        /// <summary>
        /// Gets a value indicating whether data collection currently enabled.
        /// </summary>
        private bool IsDataCollectionEnabled;

        /// <summary>
        /// Data collection environment context.
        /// </summary>
        private DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        /// <summary>
        /// File manager for performing file transfer from data collector.
        /// </summary>
        private IDataCollectionAttachmentManager attachmentManager;

        private IMessageSink MessageSink;

        private TestPlatformDataCollectionEvents Events;

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
            this.MessageSink = messageSink;
            this.Events = new TestPlatformDataCollectionEvents();

            this.RunDataCollectors = new Dictionary<Type, TestPlatformDataCollector>();
        }

        /// <summary>
        /// Gets cache of data collectors associated with the run.
        /// </summary>
        internal Dictionary<Type, TestPlatformDataCollector> RunDataCollectors { get; private set; }


        /// <inheritdoc/>
        public IDictionary<string, string> InitializeDataCollectors(string settingsXml)
        {
            ValidateArg.NotNull(settingsXml, "settingsXml");

            var sessionId = new SessionId(Guid.NewGuid());
            var dataCollectionContext = new DataCollectionContext(sessionId);
            this.dataCollectionEnvironmentContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(dataCollectionContext);

            this.attachmentManager.Initialize(sessionId, SourceDirectory, this.MessageSink);

            var executionEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);
            SourceDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settingsXml);

            this.IsDataCollectionEnabled = dataCollectionRunSettings.IsCollectionEnabled;

            // If dataCollectionRunSettings is null, that means data collectors are not configured.
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

            // todo : populate environment variables from loaded datacollectors.
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
        public Collection<AttachmentSet> SessionEnded(bool isCancelled)
        {
            if (!this.IsDataCollectionEnabled)
            {
                return null;
            }

            var endEvent = new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext);
            this.SendEvent(endEvent);

            var result = this.attachmentManager.GetAttachments(endEvent.Context);

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
            if (this.RunDataCollectors.Count == 0)
            {
                // No TestCase level events are needed if data collection is disabled or no data collectors are loaded.
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
        /// Helper method that gets the Type from type name string specified.
        /// </summary>
        /// <param name="collectorTypeName">
        /// Type name of the collector
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
                    EqtTrace.Error("DataCollectionManager.GetCollectorType: Failed to get type for Collector '{0}': {1}", collectorTypeName, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// The get data collector information from binary.
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
        /// The load assembly from path.
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
        /// Creates an instance of collector plugin of given type.
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
        /// The load and initialize.
        /// </summary>
        /// <param name="dataCollectorSettings">
        /// The data collector settings.
        /// </param>
        private void LoadAndInitialize(DataCollectorSettings dataCollectorSettings)
        {
            var codebase = dataCollectorSettings.CodeBase;

            var collectorTypeName = dataCollectorSettings.AssemblyQualifiedName;
            var collectorDisplayName = string.IsNullOrWhiteSpace(dataCollectorSettings.FriendlyName) ? collectorTypeName : dataCollectorSettings.FriendlyName;
            TestPlatformDataCollector testplatformDataCollector;
            DataCollectorConfig dataCollectorConfig;

            try
            {
                dataCollectorConfig = GetDataCollectorConfig(collectorTypeName, codebase);
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

            lock (this.RunDataCollectors)
            {
                if (this.RunDataCollectors.ContainsKey(dataCollectorConfig.DataCollectorType))
                {
                    // Collector is already loaded (may be configured twice). Ignore duplicates and return.
                    return;
                }
            }

            Debug.Assert(dataCollectorConfig.DataCollectorType != null, string.Format(CultureInfo.CurrentCulture, "Could not find collector type '{0}'", collectorTypeName));

            try
            {
                var dataCollector = CreateDataCollector(dataCollectorConfig.DataCollectorType);

                // Attempt to get the data collector information verifying that all of the required metadata for the collector is available.
                testplatformDataCollector = dataCollectorConfig == null
                                                ? null
                                                : new TestPlatformDataCollector(
                                                    dataCollector,
                                                    dataCollectorSettings.Configuration,
                                                    dataCollectorConfig,
                                                    this.dataCollectionEnvironmentContext,
                                                    this.attachmentManager,
                                                    this.Events,
                                                    this.MessageSink);

                if (testplatformDataCollector == null || !testplatformDataCollector.DataCollectorConfig.TypeUri.Equals(dataCollectorSettings.Uri))
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
                    EqtTrace.Error("DataCollectionManager.LoadAndInitDataCollectors: exception while creating data collector {0}: " + ex, collectorTypeName);
                }

                // No data collector info, so send the error with no direct association to the collector.
                this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resources.Resources.DataCollectorInitializationError, collectorTypeName, ex.Message));
                return;
            }

            try
            {
                testplatformDataCollector.InitializeDataCollector();
                lock (this.RunDataCollectors)
                {
                    // Add data collectors to run cache.
                    this.RunDataCollectors[dataCollectorConfig.DataCollectorType] = testplatformDataCollector;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error(ex.Message);
                // todo : add logging.
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
            // todo: implement this functionality
        }

        /// <summary>
        /// Sends the event to all data collectors and fires a callback on the sender, letting it
        /// know when all plugins have completed processing the event
        /// </summary>
        /// <param name="args">The context information for the event</param>
        private void SendEvent(DataCollectionEventArgs args)
        {
            Debug.Assert(args != null, "'args' is null");

            if (!this.IsDataCollectionEnabled)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("RaiseEvent called when no collection is enabled.");
                }

                Debug.Assert(false, "RaiseEvent called when no collection is enabled.");
                return;
            }

            foreach (var dataCollectorInfo in this.GetDataCollectorsSnapshot())
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionManger:SendEvent: Raising event {0} to collector {1}", args.GetType(), dataCollectorInfo.DataCollectorConfig.FriendlyName);
                }

                dataCollectorInfo.Events.RaiseEvent(args);
            }
        }

        /// <summary>
        /// Gets a snapshot of current data collectors.
        /// </summary>
        /// <returns>
        /// Collection of TestPlatformDataCollectorInfo.
        /// </returns>
        private List<TestPlatformDataCollector> GetDataCollectorsSnapshot()
        {
            var datacollectorInfoList = new List<TestPlatformDataCollector>();
            lock (this.RunDataCollectors)
            {
                foreach (var dataCollectorInfo in this.RunDataCollectors.Values)
                {
                    if (dataCollectorInfo != null)
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose(
                                "DataCollectionManager.GetDataCollectorsSnapshot: DataCollector:{0}",
                                dataCollectorInfo.DataCollectorConfig.FriendlyName);
                        }

                        datacollectorInfoList.Add(dataCollectorInfo);
                    }
                    else
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("DataCollectionManager.GetDataCollectorsSnapshot: got null data collector info from the data collector info collection (ignored).");
                        }
                    }
                }
            }

            return datacollectorInfoList;
        }
    }
}
