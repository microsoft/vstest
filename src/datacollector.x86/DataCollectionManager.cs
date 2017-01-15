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

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

#if !NET46
    using System.Runtime.Loader;
#endif

    /// <summary>
    /// The data collection manager.
    /// </summary>
    internal class DataCollectionManager : IDataCollectionManager
    {
        /// <summary>
        /// The default extensions folder.
        /// </summary>
        private const string DefaultExtensionsFolder = "Extensions";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        /// <param name="sources">
        /// The sources.
        /// </param>
        internal DataCollectionManager(IList<string> sources)
        {
            this.RunDataCollectors = new Dictionary<Type, TestPlatformDataCollector>();
            SourceDirectory = Path.GetDirectoryName(sources.First());
        }

        /// <summary>
        /// Gets or sets the source directory.
        /// </summary>
        internal static string SourceDirectory { get; set; }

        /// <summary>
        /// Gets cache of data collectors associated with the run.
        /// </summary>
        internal Dictionary<Type, TestPlatformDataCollector> RunDataCollectors { get; private set; }

        /// <inheritdoc/>
        public IDictionary<string, string> InitializeDataCollectors(string settingsXml)
        {
            ValidateArg.NotNull(settingsXml, "settingsXml");

            var executionEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settingsXml);

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
                LoadAndInitialize(dataCollectorSettings);
            }

            // todo : populate environment variables from loaded datacollectors.
            return executionEnvironmentVariables;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Collection<AttachmentSet> SessionEnded(bool isCancelled)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool SessionStarted()
        {
            throw new NotImplementedException();
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
        /// Helper method that gets the Type from type name string specified.
        /// </summary>
        /// <param name="collectorTypeName">
        /// Type name of the collector
        /// </param>
        /// <returns>
        /// Type of the collector type name
        /// </returns>
        private static DataCollectorConfig GetDataCollectorConfig(string collectorTypeName)
        {
            try
            {
                DataCollectorConfig dataCollectorConfig = null;
                var pluginDirectoryPath = Path.Combine(Path.GetDirectoryName(typeof(DataCollectionManager).GetTypeInfo().Assembly.Location), DefaultExtensionsFolder);
                var basePath = Path.Combine(pluginDirectoryPath, collectorTypeName.Split(',')[1].Trim());
                var dataCollectorType = GetDataCollectorType(basePath, collectorTypeName, out var assembly);

                // Not able to locate data collector binary in extensions folder
                if (dataCollectorType == null)
                {
                    // Try to find the data collector in the source directory
                    basePath = Path.Combine(SourceDirectory, collectorTypeName.Split(',')[1].Trim());
                    dataCollectorType = GetDataCollectorType(basePath, collectorTypeName, out assembly);
                    if (dataCollectorType == null)
                    {
                        return null;
                    }
                }

                // todo : get the config file.
                // var configuration = DataCollectorDiscoveryHelper.GetConfigurationForAssembly(assembly);
                var configuration = string.Empty;

                dataCollectorConfig = new DataCollectorConfig(dataCollectorType, configuration);

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
            var dllPath = string.Concat(binaryPath, ".dll");
            if (File.Exists(dllPath))
            {
                assembly = LoadAssemblyFromPath(dllPath);
            }

            if (assembly == null)
            {
                var exePath = string.Concat(binaryPath, ".exe");
                if (File.Exists(exePath))
                {
                    assembly = LoadAssemblyFromPath(exePath);
                }
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
            var collectorTypeName = dataCollectorSettings.AssemblyQualifiedName;
            var collectorDisplayName = string.IsNullOrWhiteSpace(dataCollectorSettings.FriendlyName) ? collectorTypeName : dataCollectorSettings.FriendlyName;
            TestPlatformDataCollector testplatformDataCollector;
            DataCollectorConfig dataCollectorConfig;

            try
            {
                dataCollectorConfig = GetDataCollectorConfig(collectorTypeName);
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
                testplatformDataCollector = dataCollectorConfig == null ? null : new TestPlatformDataCollector(
                dataCollector,
                dataCollectorSettings.Configuration,
                null,
                dataCollectorConfig);

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
                lock (this.RunDataCollectors)
                {
                    // Add data collectors to run cache.
                    this.RunDataCollectors[dataCollectorConfig.DataCollectorType] = testplatformDataCollector;
                }
            }
            catch (Exception)
            {
                // data collector failed to initialize. Dispose it and mark it failed.
                //dataCollectorInfo.Logger.LogError(
                //    this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                //    string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorInitializationError, dataCollectorInfo.DataCollectorConfig.FriendlyName, ex.Message));
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
    }
}
