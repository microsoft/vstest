// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using System.Collections.ObjectModel;
    using DCResources = Microsoft.VisualStudio.TestPlatform.DataCollector.Resources.Resources;
    using System.Globalization;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using System.Linq;
#if !NET46
    using System.Runtime.Loader;
#endif

    internal class DataCollectionManager : IDataCollectionManager
    {
        /// <summary>
        /// Gets cache of data collectors associated with the run.
        /// </summary>
        internal Dictionary<Type, TestPlatformDataCollector> RunDataCollectors { get; private set; }

        internal DataCollectionManager()
        {
            this.RunDataCollectors = new Dictionary<Type, TestPlatformDataCollector>();
        }

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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Collection<AttachmentSet> SessionEnded(bool isCancelled)
        {
            throw new NotImplementedException();
        }

        public bool SessionStarted()
        {
            throw new NotImplementedException();
        }

        public Collection<AttachmentSet> TestCaseEnded(TestCase testCase, TestOutcome testOutcome)
        {
            throw new NotImplementedException();
        }

        public void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs)
        {
            throw new NotImplementedException();
        }

        private void LoadAndInitialize(DataCollectorSettings dataCollectorSettings)
        {
            // Supporting codebase enables the user to specify the DataCollector in case DataCollector is not present under Extensions folder.
            var codeBase = dataCollectorSettings.CodeBase;
            var collectorTypeName = dataCollectorSettings.AssemblyQualifiedName;
            var collectorDisplayName = string.IsNullOrWhiteSpace(dataCollectorSettings.FriendlyName) ? collectorTypeName : dataCollectorSettings.FriendlyName;
            TestPlatformDataCollector testplatformDataCollector;
            DataCollectorConfig dataCollectorConfig;

            try
            {
                if (!string.IsNullOrWhiteSpace(codeBase))
                {
                    var fullyQualifiedAssemblyName = this.GetFullyQualifiedAssemblyNameFromFullTypeName(dataCollectorSettings.AssemblyQualifiedName); // assemblyQualifiedname will have type name also. Get assemblyname only
                    var name = new AssemblyName(fullyQualifiedAssemblyName);

                    // Eg codebase="file://c:/TestImpact/Microsoft.VisualStudio.TraceCollector.dll"

                    // Check if file is there. There can be a case data collector was loaded from some other path. If user has given a codebase we should ensure it is there
                    if (!File.Exists(new Uri(codeBase).LocalPath))
                    {
                        throw new FileNotFoundException(codeBase);
                    }

                    Assembly.Load(name); // This will do check for publicKeyToken etc
                }

                dataCollectorConfig = GetDataCollectorConfig(collectorTypeName);
            }
            catch (FileNotFoundException)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, DCResources.DataCollectorAssemblyNotFound, collectorDisplayName));
                return;
            }
            catch (Exception ex)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, DCResources.DataCollectorTypeNotFound, collectorDisplayName, ex.Message));
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

            Debug.Assert(null != dataCollectorConfig.DataCollectorType, string.Format(CultureInfo.CurrentCulture, "Could not find collector type '{0}'", collectorTypeName));

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
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, DCResources.DataCollectorNotFound, dataCollectorConfig.DataCollectorType.FullName, dataCollectorSettings.Uri));
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
                this.LogWarning(string.Format(CultureInfo.CurrentUICulture, DCResources.DataCollectorInitializationError, collectorTypeName, ex.Message));
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
                //    string.Format(CultureInfo.CurrentCulture, DCResources.DataCollectorInitializationError, dataCollectorInfo.DataCollectorConfig.FriendlyName, ex.Message));
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
            foreach (DataCollectorSettings settings in dataCollectionSettings.DataCollectorSettingsList)
            {
                if (settings.IsEnabled)
                {
                    if (runEnabledDataCollectors.Any(dcSettings => dcSettings.Uri.Equals(settings.Uri)
                        || string.Equals(dcSettings.AssemblyQualifiedName, settings.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                        this.LogWarning(string.Format(CultureInfo.CurrentUICulture, DCResources.IgnoredDuplicateConfiguration, settings.AssemblyQualifiedName, settings.Uri));
                        continue;
                    }

                    runEnabledDataCollectors.Add(settings);
                }
            }

            return runEnabledDataCollectors;
        }

        /// <summary>
        /// Helper method that gets the Type from type name string specified.
        /// </summary>
        /// <param name="collectorTypeName">
        /// Type name of the collector
        /// </param>
        /// <param name="dataCollectorConfig">
        /// The data Collector Information.
        /// </param>
        /// <returns>
        /// Type of the collector type name
        /// </returns>
        private static DataCollectorConfig GetDataCollectorConfig(string collectorTypeName)
        {
            try
            {
                DataCollectorConfig dataCollectorConfig = null;
                //var pluginDirectoryPath = DataCollectorDiscoveryHelper.DataCollectorsDirectory;
                var pluginDirectoryPath = Directory.GetCurrentDirectory();

                var basePath = Path.Combine(pluginDirectoryPath, collectorTypeName.Split(',')[1].Trim());

                Assembly assembly;

                Type dataCollectorType = GetDataCollectorType(basePath, collectorTypeName, out assembly);

                // Not able to locate data collector binary.
                if (dataCollectorType == null)
                {
                    return null;
                }

                //todo : get the config file.
                //var configuration = DataCollectorDiscoveryHelper.GetConfigurationForAssembly(assembly);
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

        private static Assembly LoadAssemblyFromPath(string assemblyPath)
        {
            Assembly assembly;
#if NET46
            return assembly = Assembly.LoadFrom(assemblyPath);
#else
            return assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
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
    }
}
