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
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    /// <summary>
    /// Enumerates through all the discoverers.
    /// </summary>
    internal class DiscovererEnumerator
    {
        private DiscoveryResultCache discoveryResultCache;
        private ITestPlatformEventSource testPlatformEventSource;
        private IMetricsCollector metricsCollector;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscovererEnumerator"/> class.
        /// </summary>
        /// <param name="discoveryResultCache"> The discovery result cache. </param>
        /// <param name="metricsCollector">Metric Collector</param>
        public DiscovererEnumerator(DiscoveryResultCache discoveryResultCache, IMetricsCollector metricsCollector):this(discoveryResultCache, TestPlatformEventSource.Instance, metricsCollector)
        {
        }

        internal DiscovererEnumerator(
            DiscoveryResultCache discoveryResultCache,
            ITestPlatformEventSource testPlatformEventSource,
            IMetricsCollector metricsCollector)
        {
            this.discoveryResultCache = discoveryResultCache;
            this.testPlatformEventSource = testPlatformEventSource;
            this.metricsCollector = metricsCollector;
        }

        /// <summary>
        /// Discovers tests from the sources.
        /// </summary>
        /// <param name="testExtensionSourceMap"> The test extension source map. </param>
        /// <param name="settings"> The settings. </param>
        /// <param name="logger"> The logger. </param>
        internal void LoadTests(IDictionary<string, IEnumerable<string>> testExtensionSourceMap, IRunSettings settings, IMessageLogger logger)
        {
            this.testPlatformEventSource.DiscoveryStart();
            foreach (var kvp in testExtensionSourceMap)
            {
                this.LoadTestsFromAnExtension(kvp.Key, kvp.Value, settings, logger);
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
        /// <param name="logger"> The logger.  </param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This methods must invoke all possible discoverers and not fail or crash in any one.")]
        private void LoadTestsFromAnExtension(string extensionAssembly, IEnumerable<string> sources, IRunSettings settings, IMessageLogger logger)
        {  
            // Stopwatch to calculate how much time engine takes to load all adapter
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            var discovererToSourcesMap = GetDiscovererToSourcesMap(extensionAssembly, sources, logger);
            sw.Stop();

            // Collecting Data Point for TimeTaken to Load Adapters
            this.metricsCollector.Add(UnitTestTelemetryDataConstants.TimeTakenToLoadAdaptersInSec, (sw.Elapsed.TotalMilliseconds).ToString());

            // Warning is logged for in the inner function
            if (discovererToSourcesMap == null || discovererToSourcesMap.Count() == 0)
            {
                return;
            }


            //Collecting Total Number of Adapters Discovered in Machine
            this.metricsCollector.Add(UnitTestTelemetryDataConstants.NumberOfAdapterDiscoveredInTheMachine, (discovererToSourcesMap.Keys.Count()).ToString());

            var context = new DiscoveryContext { RunSettings = settings };

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
                    EqtTrace.Error(e);

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

                    sw.Reset();
                    sw.Start();

                    var currentTotalTests = this.discoveryResultCache.TotalDiscoveredTests;

                    this.testPlatformEventSource.AdapterDiscoveryStart(discoverer.Metadata.DefaultExecutorUri.AbsoluteUri);                    
                    discoverer.Value.DiscoverTests(discovererToSourcesMap[discoverer], context, logger, discoverySink);

                    sw.Stop();
                  
                    this.testPlatformEventSource.AdapterDiscoveryStop(this.discoveryResultCache.TotalDiscoveredTests - currentTotalTests);

                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "DiscoveryContext.LoadTests: Done loading tests for {0}",
                            discoverer.Value.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    var message = string.Format(
                        CultureInfo.CurrentUICulture,
                        CrossPlatEngineResources.ExceptionFromLoadTests,
                        discovererType.Name,
                        e.Message);

                    logger.SendMessage(TestMessageLevel.Error, message);
                    EqtTrace.Error(e);
                }

                // Collecting Data Point for Time Taken to Discover Tests by each Adapter
                this.metricsCollector.Add(string.Format("{0},{1}", UnitTestTelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, discoverer.Metadata.DefaultExecutorUri), (sw.Elapsed.TotalSeconds).ToString());
                totalTimeTakenByAdapters += sw.Elapsed.TotalSeconds;
            }

            //Collecting Total Time Taken by Adapters
            this.metricsCollector.Add(UnitTestTelemetryDataConstants.TimeTakenInSecByAllAdapters, totalTimeTakenByAdapters.ToString());
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
        internal static Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>> GetDiscovererToSourcesMap(string extensionAssembly, IEnumerable<string> sources, IMessageLogger logger)
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

            var result =
                new Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>>();
            var sourcesForWhichNoDiscovererIsAvailable = new List<string>(sources);

            foreach (var discoverer in allDiscoverers)
            {
                // Find the sources which this discoverer can look at. 
                // Based on whether it is registered for a matching file extension or no file extensions at all.
                var matchingSources = (from source in sources
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
