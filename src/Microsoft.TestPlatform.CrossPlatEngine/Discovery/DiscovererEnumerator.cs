// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Enumerates through all the discoverers.
    /// </summary>
    internal class DiscovererEnumerator
    {
        private DiscoveryResultCache discoveryResultCache;
        private ITestPlatformEventSource testPlatformEventSource;
        private IRequestData requestData;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscovererEnumerator"/> class.
        /// </summary>
        /// <param name="requestData">The request data for providing discovery services and data.</param>
        /// <param name="discoveryResultCache"> The discovery result cache. </param>
        /// <param name="metricsCollector">Metric Collector</param>
        public DiscovererEnumerator(IRequestData requestData, DiscoveryResultCache discoveryResultCache) : this(requestData, discoveryResultCache, TestPlatformEventSource.Instance)
        {
        }

        internal DiscovererEnumerator(IRequestData requestData,
            DiscoveryResultCache discoveryResultCache,
            ITestPlatformEventSource testPlatformEventSource)
        {
            this.discoveryResultCache = discoveryResultCache;
            this.testPlatformEventSource = testPlatformEventSource;
            this.requestData = requestData;
        }

        /// <summary>
        /// Discovers tests from the sources.
        /// </summary>
        /// <param name="testExtensionSourceMap"> The test extension source map. </param>
        /// <param name="settings"> The settings. </param>
        /// <param name="testCaseFilter"> The test case filter. </param>
        /// <param name="logger"> The logger. </param>
        internal void LoadTests(IDictionary<string, IEnumerable<string>> testExtensionSourceMap, IRunSettings settings, string testCaseFilter, IMessageLogger logger)
        {
            this.testPlatformEventSource.DiscoveryStart();
            foreach (var kvp in testExtensionSourceMap)
            {
                this.LoadTestsFromAnExtension(kvp.Key, kvp.Value, settings, testCaseFilter, logger);
            }
            this.testPlatformEventSource.DiscoveryStop(this.discoveryResultCache.TotalDiscoveredTests);
        }

        /// <summary>
        /// Loads test cases from individual source. 
        /// Discovery extensions update progress through ITestCaseDiscoverySink.
        /// Discovery extensions sends discovery messages through TestRunMessageLoggerProxy
        /// </summary>
        /// <param name="extensionAssembly"> The extension Assembly. </param>
        /// <param name="sources"> The sources.   </param>
        /// <param name="settings"> The settings.   </param>
        /// <param name="settings"> The test case filter. </param>
        /// <param name="logger"> The logger.  </param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This methods must invoke all possible discoverers and not fail or crash in any one.")]
        private void LoadTestsFromAnExtension(string extensionAssembly, IEnumerable<string> sources, IRunSettings settings, string testCaseFilter, IMessageLogger logger)
        {
            double totalAdaptersUsed = 0;

            // Stopwatch to collect metrics
            var timeStart = DateTime.UtcNow;

            var discovererToSourcesMap = GetDiscovererToSourcesMap(extensionAssembly, sources, logger);
            var totalAdapterLoadTIme = DateTime.UtcNow - timeStart;

            // Collecting Data Point for TimeTaken to Load Adapters
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, totalAdapterLoadTIme.TotalSeconds);

            // Warning is logged for in the inner function
            if (discovererToSourcesMap == null || !discovererToSourcesMap.Any())
            {
                return;
            }

            // Collecting Total Number of Adapters Discovered in Machine
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, discovererToSourcesMap.Keys.Count());

            var context = new DiscoveryContext { RunSettings = settings };
            context.FilterExpressionWrapper = !string.IsNullOrEmpty(testCaseFilter) ? new FilterExpressionWrapper(testCaseFilter) : null;

            // Set on the logger the TreatAdapterErrorAsWarning setting from runsettings.
            this.SetAdapterLoggingSettings(logger, settings);

            var discoverySink = new TestCaseDiscoverySink(this.discoveryResultCache);
            double totalTimeTakenByAdapters = 0;

            foreach (var discoverer in discovererToSourcesMap.Keys)
            {
                Type discovererType = null;

                // See if discoverer can be instantiated successfully else move next.
                try
                {
                    discovererType = discoverer.Value.GetType();
                }
                catch (Exception e)
                {
                    var mesage = string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatEngineResources.DiscovererInstantiationException,
                        e.Message);
                    logger.SendMessage(TestMessageLevel.Warning, mesage);
                    EqtTrace.Error("DiscovererEnumerator.LoadTestsFromAnExtension: {0} ", e);

                    continue;
                }

                // if instantiated successfully, get tests
                try
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "DiscoveryContext.LoadTests: Loading tests for {0}",
                            discoverer.Value.GetType().FullName);
                    }

                    var currentTotalTests = this.discoveryResultCache.TotalDiscoveredTests;
                    var newTimeStart = DateTime.UtcNow;

                    this.testPlatformEventSource.AdapterDiscoveryStart(discoverer.Metadata.DefaultExecutorUri.AbsoluteUri);
                    discoverer.Value.DiscoverTests(discovererToSourcesMap[discoverer], context, logger, discoverySink);

                    var totalAdapterRunTime = DateTime.UtcNow - newTimeStart;

                    this.testPlatformEventSource.AdapterDiscoveryStop(this.discoveryResultCache.TotalDiscoveredTests - currentTotalTests);

                    // Collecting Total Tests Discovered By each Adapter.
                    if (this.discoveryResultCache.TotalDiscoveredTests > currentTotalTests)
                    {
                        var totalDiscoveredTests = this.discoveryResultCache.TotalDiscoveredTests - currentTotalTests;
                        this.requestData.MetricsCollection.Add(string.Format("{0}.{1}", TelemetryDataConstants.TotalTestsByAdapter, discoverer.Metadata.DefaultExecutorUri), totalDiscoveredTests);
                        totalAdaptersUsed++;
                    }

                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "DiscoveryContext.LoadTests: Done loading tests for {0}",
                            discoverer.Value.GetType().FullName);
                    }

                    // Collecting Data Point for Time Taken to Discover Tests by each Adapter
                    this.requestData.MetricsCollection.Add(string.Format("{0}.{1}", TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, discoverer.Metadata.DefaultExecutorUri), totalAdapterRunTime.TotalSeconds);
                    totalTimeTakenByAdapters += totalAdapterRunTime.TotalSeconds;
                }
                catch (Exception e)
                {
                    var message = string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatEngineResources.ExceptionFromLoadTests,
                        discovererType.Name,
                        e.Message);

                    logger.SendMessage(TestMessageLevel.Error, message);
                    EqtTrace.Error("DiscovererEnumerator.LoadTestsFromAnExtension: {0} ", e);
                }
            }

            // Collecting Total Time Taken by Adapters
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenInSecByAllAdapters, totalTimeTakenByAdapters);

            // Collecting Total Adapters Used to Discover tests
            this.requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, totalAdaptersUsed);
        }

        private void SetAdapterLoggingSettings(IMessageLogger messageLogger, IRunSettings runSettings)
        {
            var discoveryMessageLogger = messageLogger as TestSessionMessageLogger;
            if (discoveryMessageLogger != null && runSettings != null)
            {
#if Todo
                // Todo: Enable this when RunSettings is enabled.
                IRunConfigurationSettingsProvider runConfigurationSettingsProvider =
                        (IRunConfigurationSettingsProvider)runSettings.GetSettings(ObjectModel.Constants.RunConfigurationSettingsName);
                if (runConfigurationSettingsProvider != null
                        && runConfigurationSettingsProvider.Settings != null)
                {
                    discoveryMessageLogger.TreatTestAdapterErrorsAsWarnings
                                = runConfigurationSettingsProvider.Settings.TreatTestAdapterErrorsAsWarnings;
                }
#endif
            }
        }

        /// <summary>
        /// Get the discoverers matching with the parameter sources
        /// </summary>
        /// <param name="extensionAssembly"> The extension assembly. </param>
        /// <param name="sources"> The sources. </param>
        /// <param name="logger"> The logger instance. </param>
        /// <returns> The map between an extension type and a source. </returns>
        internal static Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>> GetDiscovererToSourcesMap(
            string extensionAssembly,
            IEnumerable<string> sources,
            IMessageLogger logger)
        {
            var allDiscoverers = GetDiscoverers(extensionAssembly, throwOnError: true);

            if (allDiscoverers == null || !allDiscoverers.Any())
            {
                var sourcesString = string.Join(" ", sources);
                // No discoverer available, log a warning
                logger.SendMessage(
                    TestMessageLevel.Warning,
                    string.Format(CultureInfo.CurrentCulture, CrossPlatEngineResources.TestRunFailed_NoDiscovererFound_NoTestsAreAvailableInTheSources, sourcesString));

                return null;
            }

            IDictionary<AssemblyType, IEnumerable<string>> assemblyTypeToSoucesMap = null;
            var result = new Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>>();
            var sourcesForWhichNoDiscovererIsAvailable = new List<string>(sources);

            foreach (var discoverer in allDiscoverers)
            {
                var sourcesToCheck = sources;

                if (discoverer.Metadata.AssemblyType == AssemblyType.Native ||
                    discoverer.Metadata.AssemblyType == AssemblyType.Managed)
                {
                    assemblyTypeToSoucesMap = assemblyTypeToSoucesMap ?? GetAssemblyTypeToSoucesMap(sources);
                    sourcesToCheck = assemblyTypeToSoucesMap[AssemblyType.None].Concat(assemblyTypeToSoucesMap[discoverer.Metadata.AssemblyType]);
                }

                // Find the sources which this discoverer can look at. 
                // Based on whether it is registered for a matching file extension or no file extensions at all.
                var matchingSources = (from source in sourcesToCheck
                                       where
                                           (discoverer.Metadata.FileExtension == null
                                            || discoverer.Metadata.FileExtension.Contains(
                                                Path.GetExtension(source),
                                                StringComparer.OrdinalIgnoreCase))
                                       select source).ToList(); // ToList is required to actually execute the query 


                // Update the source list for which no matching source is available.
                if (matchingSources.Any())
                {
                    sourcesForWhichNoDiscovererIsAvailable =
                        sourcesForWhichNoDiscovererIsAvailable.Except(matchingSources, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    result.Add(discoverer, matchingSources);
                }
            }

            if (EqtTrace.IsWarningEnabled && sourcesForWhichNoDiscovererIsAvailable != null)
            {
                foreach (var source in sourcesForWhichNoDiscovererIsAvailable)
                {
                    // Log a warning to logfile, not to the "default logger for discovery time messages".
                    EqtTrace.Warning(
                        "No test discoverer is registered to perform discovery for the type of test source '{0}'. Register a test discoverer for this source type and try again.",
                        source);
                }
            }

            return result;
        }

        /// <summary>
        /// Get assembly type to sources map.
        /// </summary>
        /// <param name="sources">Sources.</param>
        /// <param name="assemblyType">Assembly type.</param>
        /// <returns>Sources with mathcing assembly type.</returns>
        private static IDictionary<AssemblyType, IEnumerable<string>> GetAssemblyTypeToSoucesMap(IEnumerable<string> sources)
        {
            var assemblyTypeToSoucesMap = new Dictionary<AssemblyType, IEnumerable<string>>()
            {
                { AssemblyType.Native, new List<string>()},
                { AssemblyType.Managed, new List<string>()},
                { AssemblyType.None, new List<string>()}
            };

            if (sources != null && sources.Any())
            {
                foreach (string source in sources)
                {
                    var sourcesList = IsAssembly(source) ?
                        assemblyTypeToSoucesMap[new PEReaderHelper().GetAssemblyType(source)] :
                        assemblyTypeToSoucesMap[AssemblyType.None];

                    ((List<string>)sourcesList).Add(source);
                }
            }

            return assemblyTypeToSoucesMap;
        }

        /// <summary>
        /// Finds if a file is an assembly or not.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>True if file is an assembly.</returns>
        private static bool IsAssembly(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath);

            return ".dll".Equals(fileExtension, StringComparison.OrdinalIgnoreCase) ||
                ".exe".Equals(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        private static IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> GetDiscoverers(
            string extensionAssembly,
            bool throwOnError)
        {
            try
            {
                if (string.IsNullOrEmpty(extensionAssembly) || string.Equals(extensionAssembly, ObjectModel.Constants.UnspecifiedAdapterPath))
                {
                    // full discovery.
                    return TestDiscoveryExtensionManager.Create().Discoverers;
                }
                else
                {
                    return TestDiscoveryExtensionManager.GetDiscoveryExtensionManager(extensionAssembly).Discoverers;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "TestDiscoveryManager: LoadExtensions: Exception occured while loading extensions {0}",
                    ex);

                if (throwOnError)
                {
                    throw;
                }

                return null;
            }
        }

    }
}
