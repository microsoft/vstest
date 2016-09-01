// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

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
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;

    /// <summary>
    /// Enumerates through all the discoverers.
    /// </summary>
    internal class DiscovererEnumerator
    {
        private DiscoveryResultCache discoveryResultCache;
        private TestPlatformEventSource testPlatformEventSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscovererEnumerator"/> class.
        /// </summary>
        /// <param name="discoveryResultCache"> The discovery result cache. </param>
        /// <param name="testPlatformEventSource1"></param>
        public DiscovererEnumerator(DiscoveryResultCache discoveryResultCache, TestPlatformEventSource testPlatformEventSource)
        {
            this.discoveryResultCache = discoveryResultCache;
            this.testPlatformEventSource = testPlatformEventSource;
        }

        /// <summary>
        /// Discovers tests from the sources.
        /// </summary>
        /// <param name="testExtensionSourceMap"> The test extension source map. </param>
        /// <param name="settings"> The settings. </param>
        /// <param name="logger"> The logger. </param>
        internal void LoadTests(IDictionary<string, IEnumerable<string>> testExtensionSourceMap, IRunSettings settings, IMessageLogger logger)
        {
            foreach (var kvp in testExtensionSourceMap)
            {
                this.LoadTestsFromAnExtension(kvp.Key, kvp.Value, settings, logger);
            }
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
            var discovererToSourcesMap = GetDiscovererToSourcesMap(extensionAssembly, sources, logger);

            // Warning is logged for in the inner function
            if (discovererToSourcesMap == null || discovererToSourcesMap.Count() == 0)
            {
                return;
            }

            var context = new DiscoveryContext { RunSettings = settings };

            // Set on the logger the TreatAdapterErrorAsWarning setting from runsettings.
            this.SetAdapterLoggingSettings(logger, settings);

            var discoverySink = new TestCaseDiscoverySink(this.discoveryResultCache);


            this.testPlatformEventSource?.Discovery();
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
                        CrossPlatEngine.Resources.DiscovererInstantiationException,
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

                    discoverer.Value.DiscoverTests(discovererToSourcesMap[discoverer], context, logger, discoverySink);

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
                        CrossPlatEngine.Resources.ExceptionFromLoadTests,
                        discovererType.Name,
                        e.Message);

                    logger.SendMessage(TestMessageLevel.Error, message);
                    EqtTrace.Error(e);
                }
                this.testPlatformEventSource?.DiscoveryEnd();
            }
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
        internal static Dictionary<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>, IEnumerable<string>>
            GetDiscovererToSourcesMap(string extensionAssembly, IEnumerable<string> sources, IMessageLogger logger)
        {
            var allDiscoverers = GetDiscoverers(extensionAssembly, throwOnError: true);

            if (allDiscoverers == null || !allDiscoverers.Any())
            {
                // No discoverer available, log a warning
                logger.SendMessage(
                    TestMessageLevel.Warning,
                    String.Format(CultureInfo.CurrentCulture, CrossPlatEngine.Resources.NoDiscovererRegistered));

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
