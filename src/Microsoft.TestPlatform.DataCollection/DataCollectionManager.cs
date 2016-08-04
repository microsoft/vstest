// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.DataCollection.Implementations;
    using Microsoft.VisualStudio.TestPlatform.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.Common;

    using CollectorDataEntry = Microsoft.VisualStudio.TestPlatform.ObjectModel.AttachmentSet;
    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using DataCollectionEnvironmentContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionEnvironmentContext;
    using DataCollectionEventArgs = Microsoft.VisualStudio.TestTools.Execution.DataCollectionEventArgs;
    using DataCollector = Microsoft.VisualStudio.TestTools.Execution.DataCollector;
    using DataCollectorInformation = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInformation;
    using DataCollectorInvocationError = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInvocationError;
    using SessionEndEventArgs = Microsoft.VisualStudio.TestTools.Execution.SessionEndEventArgs;
    using SessionId = Microsoft.VisualStudio.TestTools.Common.SessionId;
    using UriDataAttachment = Microsoft.VisualStudio.TestPlatform.ObjectModel.UriDataAttachment;

    /// <summary>
    /// The data collection plugin manager.
    /// </summary>
    internal class DataCollectionManager : IDataCollectionManager, IDisposable
    {
        /// <summary>
        /// Cache of data collectors associated with the run.
        /// </summary>
        private Dictionary<Type, TestPlatformDataCollectorInfo> runDataCollectors;

        /// <summary>
        /// Is data collection currently enabled.
        /// </summary>
        private bool collectionEnabled;

        /// <summary>
        /// Data collection environment context.
        /// </summary>
        private DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        /// <summary>
        /// Data collection and log messages are sent through message sink
        /// </summary>
        private IMessageSink messageSink;

        /// <summary>
        /// Abort user work item factory, needed for DataCollectionEvents TestTool interface.
        /// </summary>
        private SafeAbortableUserWorkItemFactory userWorkItemFactory;

        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Directory in which data collector collection files are copied.
        /// </summary>
        private string collectionOutputDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        public DataCollectionManager() : this(default(IMessageSink))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        /// <param name="messageSink">
        /// The message sink.
        /// </param>
        internal DataCollectionManager(IMessageSink messageSink)
        {
            this.collectionEnabled = false;
            this.runDataCollectors = new Dictionary<Type, TestPlatformDataCollectorInfo>();

            this.messageSink = messageSink;
            this.userWorkItemFactory = new SafeAbortableUserWorkItemFactory();
            this.ConfigureNewSession();
        }

        /// <summary>
        /// Raises TestCaseStart event to all data collectors configured for run.
        /// </summary>
        /// <param name="testCaseStartEventArgs">TestCaseStart event.</param>
        public void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raises TestCaseEnd event to all data collectors configured for run.
        /// </summary>
        /// <param name="testCase">Test case which is complete.</param>
        /// <param name="testOutcome">Outcome of the test case.</param>
        /// <returns>Collection of  testCase attachmentSet.</returns>
        public Collection<CollectorDataEntry> TestCaseEnded(TestCase testCase, ObjectModel.TestOutcome testOutcome)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raises SessionStart event to all data collectors configured for run.
        /// </summary>
        /// <returns>Are test case level events required.</returns>
        public bool SessionStarted()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raises SessionEnd event to all data collectors configured for run.
        /// </summary>
        /// <param name="isCancelled">Specified whether the run is cancelled or not.</param>
        /// <returns>Collection of session attachmentSet.</returns>
        public Collection<CollectorDataEntry> SessionEnded(bool isCancelled)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads and initializes data collector plugins.
        /// </summary>
        /// <param name="testRunSettings">Settings for test run.</param>
        /// <returns>Environment variables requested by data collectors</returns>
        public IDictionary<string, string> LoadDataCollectors(RunSettings testRunSettings)
        {
            ValidateArg.NotNull<RunSettings>(testRunSettings, "testRunSettings");
            IDictionary<string, string> executionEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var dataCollectorSettingsProvider =
                (IDataCollectorsSettingsProvider)testRunSettings.GetSettings(Constants.DataCollectionRunSettingsName);
            if (null == dataCollectorSettingsProvider)
            {
                // If config file didn't specify any settings than settings provider will not be loaded. No data collection enabled.
                return executionEnvironmentVariables;
            }

            var runCollectionSettings = dataCollectorSettingsProvider.Settings;
            if (null == runCollectionSettings || !runCollectionSettings.IsCollectionEnabled)
            {
                return executionEnvironmentVariables;
            }

            var runConfigurationSettingsProvider =
                (IRunConfigurationSettingsProvider)testRunSettings.GetSettings(Constants.RunConfigurationSettingsName);
            RunConfiguration runConfiguration = null;
            if (null != runConfigurationSettingsProvider)
            {
                runConfiguration = runConfigurationSettingsProvider.Settings;
            }

            this.collectionOutputDirectory = null;
            if (null != runConfiguration)
            {
                this.collectionOutputDirectory = runConfiguration.ResultsDirectory;
            }

            this.collectionEnabled = runCollectionSettings.IsCollectionEnabled;

            if (this.collectionEnabled)
            {
                executionEnvironmentVariables = this.LoadAndInitDataCollectors(runCollectionSettings);
            }

            return executionEnvironmentVariables;
        }

        /// <summary>
        /// Dispose event object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
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
            catch (SystemException ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.CreateDataCollector: Could not create instance of type: " + dataCollectorType.ToString() + "  Exception: " + ex.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Helper method that gets the Type from typename string specified.
        /// </summary>
        /// <param name="collectorTypeName">
        /// Type name of the collector
        /// </param>
        /// <param name="dataCollectorInformation">
        /// The data Collector Information.
        /// </param>
        /// <returns>
        /// Type of the collector type name
        /// </returns>
        private static Type GetCollectorType(string collectorTypeName, out DataCollectorInformation dataCollectorInformation)
        {
            try
            {
                dataCollectorInformation = null;
                var pluginDirectoryPath = DataCollectorDiscoveryHelper.DataCollectorsDirectory;

                var basePath = Path.Combine(pluginDirectoryPath, collectorTypeName.Split(',')[1].Trim());

                Assembly assembly = null;

                Type dcType = null;
                dcType = GetDataCollectorInformationFromBinary(basePath, collectorTypeName, out assembly);

                // Not able to locate data collector binary.
                if (dcType == null)
                {
                    return dcType;
                }

                var configuration = DataCollectorDiscoveryHelper.GetConfigurationForAssembly(assembly);

                dataCollectorInformation = new DataCollectorInformation(dcType, configuration);

                return dcType;
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
        /// <param name="collectorTypeName">
        /// The collector type name.
        /// </param>
        /// <param name="assembly">
        /// The assembly.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        private static Type GetDataCollectorInformationFromBinary(string binaryPath, string collectorTypeName, out Assembly assembly)
        {
            assembly = null;
            Type dcType = null;
            var dllPath = string.Concat(binaryPath, ".dll");
            if (File.Exists(dllPath))
            {
                assembly = Assembly.LoadFrom(dllPath);
                dcType = GetDataCollectorInformationFromAssembly(assembly, collectorTypeName);
            }

            if (dcType == null)
            {
                var exePath = string.Concat(binaryPath, ".exe");
                if (File.Exists(exePath))
                {
                    assembly = Assembly.LoadFrom(exePath);
                    dcType = GetDataCollectorInformationFromAssembly(assembly, collectorTypeName);
                }
            }

            return dcType;
        }

        /// <summary>
        /// The get data collector information from assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly.
        /// </param>
        /// <param name="collectorTypeName">
        /// The collector type name.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        private static Type GetDataCollectorInformationFromAssembly(Assembly assembly, string collectorTypeName)
        {
            Type dcType = null;
            if (assembly != null)
            {
                var types = assembly.GetTypes();
                dcType = types.Where((type) => type.AssemblyQualifiedName.Equals(collectorTypeName)).FirstOrDefault();
            }

            return dcType;
        }

        /// <summary>
        /// Configures new session by
        ///  a. creating new session id
        /// </summary>
        private void ConfigureNewSession()
        {
            // todo : add stuff here required to configure new session
            var dataCollectionContext = new DataCollectionContext(new SessionId(Guid.NewGuid()));
            this.dataCollectionEnvironmentContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(dataCollectionContext);
        }

        /// <summary>
        /// Loads the data collector plugins and initializes them.
        /// </summary>
        /// <param name="dataCollectionSettings">data collection settings.</param>
        /// <returns>Environment variables that needs to be set in test process for data collection.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all exception type to send  as data collection error to client.")]
        private IDictionary<string, string> LoadAndInitDataCollectors(DataCollectionRunSettings dataCollectionSettings)
        {
            IDictionary<string, string> variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var enabledCollectors = this.GetDataCollectorsEnabledForRun(dataCollectionSettings);
            if (enabledCollectors.Count == 0)
            {
                return variables;
            }

            foreach (var collectorSettings in enabledCollectors)
            {
                this.LoadAndInitDataCollector(collectorSettings);
            }

            // Once all data collectors have been initialized, query for environment variables
            bool unloadedAnyCollector;
            var dataCollectorEnvironmentVariables = this.GetEnvironmentVariables(out unloadedAnyCollector);

            foreach (var variable in dataCollectorEnvironmentVariables.Values)
            {
                variables.Add(variable.Name, variable.Value);
            }

            return variables;
        }

        /// <summary>
        /// Finds data collector enabled for the run in data collection settings.
        /// </summary>
        /// <param name="dataCollectionSettings">data collection settings</param>
        /// <returns>List of enabled data collectors</returns>
        private List<DataCollectorSettings> GetDataCollectorsEnabledForRun(DataCollectionRunSettings dataCollectionSettings)
        {
            List<DataCollectorSettings> runEnabledDataCollectors = new List<DataCollectorSettings>();
            foreach (DataCollectorSettings settings in dataCollectionSettings.DataCollectorSettingsList)
            {
                if (settings.IsEnabled)
                {
                    if (runEnabledDataCollectors.Any(dcSettings => dcSettings.Uri.Equals(settings.Uri)
                        || string.Equals(dcSettings.AssemblyQualifiedName, settings.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                        this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resource.IgnoredDuplicateConfiguration, settings.AssemblyQualifiedName, settings.Uri));
                        continue;
                    }

                    runEnabledDataCollectors.Add(settings);
                }
            }

            return runEnabledDataCollectors;
        }

        /// <summary>
        /// The get environment variables.
        /// </summary>
        /// <param name="unloadedAnyCollector">
        /// The unloaded any collector.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary"/>.
        /// </returns>
        private Dictionary<string, CollectorRequestedEnvironmentVariable> GetEnvironmentVariables(out bool unloadedAnyCollector)
        {
            var failedCollectors = new List<TestPlatformDataCollectorInfo>();
            unloadedAnyCollector = false;
            var dataCollectorEnvironmentVariables = new Dictionary<string, CollectorRequestedEnvironmentVariable>(StringComparer.OrdinalIgnoreCase);
            foreach (var dataCollectorInfo in this.GetDataCollectorsSnapshot())
            {
                dataCollectorInfo.GetTestExecutionEnvironmentVariables();
                try
                {
                    this.AddCollectorEnvironmentVariables(dataCollectorInfo, dataCollectorEnvironmentVariables);
                }
                catch (Exception ex)
                {
                    unloadedAnyCollector = true;

                    Type dataCollectorType = dataCollectorInfo.DataCollector.GetType();
                    failedCollectors.Add(dataCollectorInfo);
                    dataCollectorInfo.Logger.LogError(
                        this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.DataCollectorErrorOnGetVariable,
                            dataCollectorType,
                            ex.ToString()));

                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("DataCollectionManager.GetEnvironmentVariables: Failed to get variable for Collector '{0}': {1}", dataCollectorType, ex);
                    }
                }
            }

            this.RemoveDataCollectors(failedCollectors);
            return dataCollectorEnvironmentVariables;
        }

        /// <summary>
        /// Gets a snapshot of current data collectors.
        /// </summary>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        private List<TestPlatformDataCollectorInfo> GetDataCollectorsSnapshot()
        {
            var datacollectorInfoList = new List<TestPlatformDataCollectorInfo>();
            lock (this.runDataCollectors)
            {
                foreach (var dataCollectorInfo in this.runDataCollectors.Values)
                {
                    if (dataCollectorInfo != null)
                    {
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            EqtTrace.Verbose(
                                "DataCollectionManager.GetDataCollectorsSnapshot: DataCollector:{0}",
                                dataCollectorInfo.DataCollectorInformation.FriendlyName);
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

        /// <summary>
        /// Collects environment variable to be set in test process by avoiding duplicates
        /// and detecting override of variable value by multiple adapters.
        /// </summary>
        /// <param name="dataCollectorInfo">Data collector information for newly loaded plugin.</param>
        /// <param name="dataCollectorEnvironmentVariables">Environment variables required for already loaded plugin.</param>
        private void AddCollectorEnvironmentVariables(
            TestPlatformDataCollectorInfo dataCollectorInfo,
            Dictionary<string, CollectorRequestedEnvironmentVariable> dataCollectorEnvironmentVariables)
        {
            if (null != dataCollectorInfo.TestExecutionEnvironmentVariables)
            {
                var collectorFriendlyName = dataCollectorInfo.DataCollectorInformation.FriendlyName;
                foreach (var namevaluepair in dataCollectorInfo.TestExecutionEnvironmentVariables)
                {
                    CollectorRequestedEnvironmentVariable alreadyRequestedVariable;
                    if (dataCollectorEnvironmentVariables.TryGetValue(namevaluepair.Key, out alreadyRequestedVariable))
                    {
                        if (string.Equals(namevaluepair.Value, alreadyRequestedVariable.Value, StringComparison.Ordinal))
                        {
                            alreadyRequestedVariable.AddRequestingDataCollector(collectorFriendlyName);
                        }
                        else
                        {
                            // Data collector is overriding an already requested variable, possibly an error.                            
                            dataCollectorInfo.Logger.LogError(
                                this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                                string.Format(
                                    CultureInfo.CurrentUICulture,
                                    Resource.DataCollectorRequestedDuplicateEnvironmentVariable,
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
                            // New variable, add to the list.
                            EqtTrace.Verbose("DataCollectionManager.AddCollectionEnvironmentVariables: Adding Environment variable '{0}' value '{1}'", namevaluepair.Key, namevaluepair.Value);
                        }

                        dataCollectorEnvironmentVariables.Add(
                            namevaluepair.Key,
                            new CollectorRequestedEnvironmentVariable(namevaluepair, collectorFriendlyName));
                    }
                }
            }
        }

        /// <summary>
        /// Initializes data collector plugin.
        /// </summary>
        /// <param name="collectorSettings">
        /// data collector settings.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all exception type to send  as data collection error to client.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Data collector object is disposed when plugins are cleaned up when test run ends.")]
        private bool LoadAndInitDataCollector(DataCollectorSettings collectorSettings)
        {
            var codeBase = collectorSettings.CodeBase;
            var collectorTypeName = collectorSettings.AssemblyQualifiedName;
            var collectorDisplayName = string.IsNullOrWhiteSpace(collectorSettings.FriendlyName) ? collectorTypeName : collectorSettings.FriendlyName;
            Type collectorType = null;
            DataCollectorInformation dcInfo = null;
            TestPlatformDataCollectorInfo dataCollectorInfo = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(codeBase))
                {
                    var fullyQualifiedAssemblyName = this.GetFullyQualifiedAssemblyNameFromFullTypeName(collectorSettings.AssemblyQualifiedName); // assemblyQualifiedname will have type name also. Get assemblyname only
                    var name = new AssemblyName(fullyQualifiedAssemblyName) { CodeBase = codeBase };

                    // Eg codebase="file://c:/TestImpact/Microsoft.VisualStudio.TraceCollector.dll"

                    // Check if file is there. There can be a case data collector was loaded from some other path. If user has given a codebase we should ensure it is there
                    if (!File.Exists(new Uri(codeBase).LocalPath))
                    {
                        throw new FileNotFoundException(codeBase);
                    }

                    Assembly.Load(name); // This will do check for publicKeyToken etc
                }

                collectorType = GetCollectorType(collectorTypeName, out dcInfo);
            }
            catch (FileNotFoundException)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorAssemblyNotFound, collectorDisplayName));
                return false;
            }
            catch (Exception ex)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorTypeNotFound, collectorDisplayName, ex.Message));
                return false;
            }

            lock (this.runDataCollectors)
            {
                if (this.runDataCollectors.ContainsKey(collectorType))
                {
                    // Collector is already loaded (may be configured twice). Ignore duplicates and return.
                    return true;
                }
            }

            Debug.Assert(null != collectorType, string.Format(CultureInfo.CurrentCulture, "Could not find collector type '{0}'", collectorTypeName));

            try
            {
                var dataCollector = CreateDataCollector(collectorType);

                // Attempt to get the data collector information verifying that all of the required metadata for the collector is available.
                dataCollectorInfo = dcInfo == null ? null : new TestPlatformDataCollectorInfo(
                dataCollector,
                collectorSettings.Configuration,
                this.messageSink,
                dcInfo,
                this.userWorkItemFactory);

                if (dataCollectorInfo == null || !dataCollectorInfo.DataCollectorInformation.TypeUri.Equals(collectorSettings.Uri))
                {
                    // If the data collector was not found, send an error.
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorNotFound, collectorType.FullName, collectorSettings.Uri));
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.LoadAndInitDataCollectors: exception while creating data collector {0}: " + ex, collectorTypeName);
                }

                // No data collector info, so send the error with no direct association to the collector.
                this.LogWarning(string.Format(CultureInfo.CurrentUICulture, Resource.DataCollectorInitializationError, collectorTypeName, ex.Message));
                return false;
            }

            try
            {
                dataCollectorInfo.InitializeDataCollector(this.dataCollectionEnvironmentContext);
                lock (this.runDataCollectors)
                {
                    // Add data collectors to run cache.
                    this.runDataCollectors[collectorType] = dataCollectorInfo;
                }
            }
            catch (Exception ex)
            {
                // data collector failed to initialize. Dispose it and mark it failed.
                dataCollectorInfo.Logger.LogError(
                    this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.DataCollectorInitializationError,
                        dataCollectorInfo.DataCollectorInformation.FriendlyName,
                        ex.Message));
                this.DisposeDataCollector(dataCollectorInfo);
                return false;
            }

            return true;
        }

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
        /// Given assemblyQualifiedName of type get the fully qualified name of assembly.
        /// </summary>
        /// <param name="assemblyQualifiedName">The assembly qualified name.</param>
        /// <returns>The fully qualified assembly name.</returns>
        private string GetFullyQualifiedAssemblyNameFromFullTypeName(string assemblyQualifiedName)
        {
            // Below is assemblyQualifiedName
            // Microsoft.VisualStudio.TraceCollector.TestImpactDataCollector, Microsoft.VisualStudio.TraceCollector, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            // FullyQualifiedAssemblyName will be
            // Microsoft.VisualStudio.TraceCollector, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            var firstIndex = assemblyQualifiedName.IndexOf(",", StringComparison.Ordinal);
            return assemblyQualifiedName.Substring(firstIndex + 1);
        }

        private void RemoveDataCollectors(IEnumerable<TestPlatformDataCollectorInfo> dataCollectorsToRemove)
        {
            if (null == dataCollectorsToRemove || dataCollectorsToRemove.Count() == 0)
            {
                return;
            }

            lock (this.runDataCollectors)
            {
                foreach (var dataCollectorToRemove in dataCollectorsToRemove)
                {
                    this.DisposeDataCollector(dataCollectorToRemove);
                    this.runDataCollectors.Remove(dataCollectorToRemove.DataCollector.GetType());
                }
                if (this.runDataCollectors.Count == 0)
                {
                    this.collectionEnabled = false;
                }
            }
        }

        private void DisposeDataCollector(TestPlatformDataCollectorInfo dataCollectorInfo)
        {
            Type dataCollectorType = dataCollectorInfo.DataCollector.GetType();
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionManager.DisposeDataCollector: calling Dispose() on {0}", dataCollectorType);
                }
                dataCollectorInfo.DisposeDataCollector();
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionManager.DisposeDataCollector: exception while calling Dispose() on {0}: " + ex, dataCollectorType);
                }

                dataCollectorInfo.Logger.LogError(
                    this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.DataCollectorDisposeError,
                        dataCollectorType,
                        ex.ToString()));
            }
        }
    }
}