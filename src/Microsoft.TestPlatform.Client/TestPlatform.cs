// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Client.Discovery;
    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using ClientResources = Resources.Resources;

    /// <summary>
    /// Implementation for TestPlatform.
    /// </summary>
    internal class TestPlatform : ITestPlatform
    {
        private readonly TestRuntimeProviderManager testHostProviderManager;

        private readonly IFileHelper fileHelper;

        static TestPlatform()
        {
            // TODO: This is not the right way to force initialization of default extensions.
            // Test runtime providers require this today. They're getting initialized even before
            // test adapter paths are provided, which is incorrect.
            AddExtensionAssembliesFromExtensionDirectory();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatform"/> class.
        /// </summary>
        public TestPlatform()
            : this(
                  new TestEngine(),
                  new FileHelper(),
                  TestRuntimeProviderManager.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatform"/> class.
        /// </summary>
        ///
        /// <param name="testEngine">The test engine.</param>
        /// <param name="filehelper">The file helper.</param>
        /// <param name="testHostProviderManager">The data.</param>
        protected TestPlatform(
            ITestEngine testEngine,
            IFileHelper filehelper,
            TestRuntimeProviderManager testHostProviderManager)
        {
            this.TestEngine = testEngine;
            this.fileHelper = filehelper;
            this.testHostProviderManager = testHostProviderManager;
        }

        /// <summary>
        /// Gets or sets the test engine instance.
        /// </summary>
        private ITestEngine TestEngine { get; set; }

        /// <inheritdoc/>
        public IDiscoveryRequest CreateDiscoveryRequest(
            IRequestData requestData,
            DiscoveryCriteria discoveryCriteria,
            TestPlatformOptions options)
        {
            if (discoveryCriteria == null)
            {
                throw new ArgumentNullException(nameof(discoveryCriteria));
            }

            PopulateExtensions(discoveryCriteria.RunSettings, discoveryCriteria.Sources);

            // Initialize loggers.
            var loggerManager = this.TestEngine.GetLoggerManager(requestData);
            loggerManager.Initialize(discoveryCriteria.RunSettings);

            var testHostManager = this.testHostProviderManager.GetTestHostManagerByRunConfiguration(discoveryCriteria.RunSettings);
            ThrowExceptionIfTestHostManagerIsNull(testHostManager, discoveryCriteria.RunSettings);

            testHostManager.Initialize(TestSessionMessageLogger.Instance, discoveryCriteria.RunSettings);

            var discoveryManager = this.TestEngine.GetDiscoveryManager(requestData, testHostManager, discoveryCriteria);
            discoveryManager.Initialize(options?.SkipDefaultAdapters ?? false);

            return new DiscoveryRequest(requestData, discoveryCriteria, discoveryManager, loggerManager);
        }

        /// <inheritdoc/>
        public ITestRunRequest CreateTestRunRequest(
            IRequestData requestData,
            TestRunCriteria testRunCriteria,
            TestPlatformOptions options)
        {
            if (testRunCriteria == null)
            {
                throw new ArgumentNullException(nameof(testRunCriteria));
            }

            var sources = GetSources(testRunCriteria);
            PopulateExtensions(testRunCriteria.TestRunSettings, sources);

            // Initialize loggers.
            var loggerManager = this.TestEngine.GetLoggerManager(requestData);
            loggerManager.Initialize(testRunCriteria.TestRunSettings);

            var testHostManager = this.testHostProviderManager.GetTestHostManagerByRunConfiguration(testRunCriteria.TestRunSettings);
            ThrowExceptionIfTestHostManagerIsNull(testHostManager, testRunCriteria.TestRunSettings);

            testHostManager.Initialize(TestSessionMessageLogger.Instance, testRunCriteria.TestRunSettings);

            // NOTE: The custom launcher should not be set when we have test session info available.
            if (testRunCriteria.TestHostLauncher != null)
            {
                testHostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
            }

            var executionManager = this.TestEngine.GetExecutionManager(requestData, testHostManager, testRunCriteria);
            executionManager.Initialize(options?.SkipDefaultAdapters ?? false);

            return new TestRunRequest(requestData, testRunCriteria, executionManager, loggerManager);
        }

        private void PopulateExtensions(string runSettings, IEnumerable<string> sources)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings);
            var strategy = runConfiguration.TestAdapterLoadingStrategy;

            // Update cache with Extension folder's files.
            this.AddExtensionAssemblies(runSettings, strategy);

            // Update extension assemblies from source when design mode is false.
            if (!runConfiguration.DesignMode)
            {
                this.AddExtensionAssembliesFromSource(sources, strategy);
            }
        }

        /// <inheritdoc/>
        public bool StartTestSession(
            IRequestData requestData,
            StartTestSessionCriteria testSessionCriteria,
            ITestSessionEventsHandler eventsHandler)
        {
            if (testSessionCriteria == null)
            {
                throw new ArgumentNullException(nameof(testSessionCriteria));
            }

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testSessionCriteria.RunSettings);
            var strategy = runConfiguration.TestAdapterLoadingStrategy;

            this.AddExtensionAssemblies(testSessionCriteria.RunSettings, strategy);

            if (!runConfiguration.DesignMode)
            {
                return false;
            }

            var testSessionManager = this.TestEngine.GetTestSessionManager(requestData, testSessionCriteria);
            if (testSessionManager == null)
            {
                // The test session manager is null because the combination of runsettings and
                // sources tells us we should run in-process (i.e. in vstest.console). Because
                // of this no session will be created because there's no testhost to be launched.
                // Expecting a subsequent call to execute tests with the same set of parameters.
                eventsHandler.HandleStartTestSessionComplete(null);
                return false;
            }

            return testSessionManager.StartSession(eventsHandler);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void UpdateExtensions(
            IEnumerable<string> pathToAdditionalExtensions,
            bool skipExtensionFilters)
        {
            this.TestEngine.GetExtensionManager().UseAdditionalExtensions(pathToAdditionalExtensions, skipExtensionFilters);
        }

        /// <inheritdoc/>
        public void ClearExtensions()
        {
            this.TestEngine.GetExtensionManager().ClearExtensions();
        }

        private void ThrowExceptionIfTestHostManagerIsNull(
            ITestRuntimeProvider testHostManager,
            string settingsXml)
        {
            if (testHostManager == null)
            {
                EqtTrace.Error("TestPlatform.CreateTestRunRequest: No suitable testHostProvider found for runsettings : {0}", settingsXml);
                throw new TestPlatformException(string.Format(CultureInfo.CurrentCulture, ClientResources.NoTestHostProviderFound));
            }
        }

        private void AddExtensionAssemblies(string runSettings, TestAdapterLoadingStrategy adapterLoadingStrategy)
        {
            IEnumerable<string> customTestAdaptersPaths = RunSettingsUtilities.GetTestAdaptersPaths(runSettings);

            if (customTestAdaptersPaths != null)
            {
                foreach (string customTestAdaptersPath in customTestAdaptersPaths)
                {
                    var extensionAssemblies = ExpandTestAdapterPaths(customTestAdaptersPath, this.fileHelper, adapterLoadingStrategy);

                    if (extensionAssemblies.Any())
                    {
                        this.UpdateExtensions(extensionAssemblies, skipExtensionFilters: false);
                    }

                }
            }
        }

        /// <summary>
        /// Updates the test logger paths from source directory.
        /// </summary>
        ///
        /// <param name="sources">The list of sources.</param>
        private void AddExtensionAssembliesFromSource(IEnumerable<string> sources, TestAdapterLoadingStrategy strategy)
        {
            // Skip discovery unless we're using the default behavior, or NextToSource is specified.
            if (strategy != TestAdapterLoadingStrategy.Default 
                && (strategy & TestAdapterLoadingStrategy.NextToSource) != TestAdapterLoadingStrategy.NextToSource)
            {
                return;
            }

            // Currently we support discovering loggers only from Source directory.
            var loggersToUpdate = new List<string>();

            foreach (var source in sources)
            {
                var sourceDirectory = Path.GetDirectoryName(source);
                if (!string.IsNullOrEmpty(sourceDirectory) && this.fileHelper.DirectoryExists(sourceDirectory))
                {
                    var searchOption = GetSearchOption(strategy, SearchOption.TopDirectoryOnly);

                    loggersToUpdate.AddRange(
                        this.fileHelper.EnumerateFiles(
                            sourceDirectory,
                            searchOption,
                            TestPlatformConstants.TestLoggerEndsWithPattern));
                }
            }

            if (loggersToUpdate.Count > 0)
            {
                this.UpdateExtensions(loggersToUpdate, skipExtensionFilters: false);
            }
        }

        /// <summary>
        /// Finds all test platform extensions from the `.\Extensions` directory. This is used to
        /// load the inbox extensions like TrxLogger and legacy test extensions like MSTest v1,
        /// MSTest C++, etc..
        /// </summary>
        private static void AddExtensionAssembliesFromExtensionDirectory()
        {
            // This method runs before adapter initialization path, ideally we should replace this mechanism
            // this is currently required because we need TestHostProvider to be able to resolve.
            var runSettings = RunSettingsManager.Instance.ActiveRunSettings.SettingsXml;
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings);
            var strategy = runConfiguration.TestAdapterLoadingStrategy;

            var fileHelper = new FileHelper();
            var defaultExtensionPaths = Enumerable.Empty<string>();

            // Explicit adapter loading
            if ((strategy & TestAdapterLoadingStrategy.Explicit) == TestAdapterLoadingStrategy.Explicit)
            {
                defaultExtensionPaths = RunSettingsUtilities.GetTestAdaptersPaths(runSettings)
                    .SelectMany(path => ExpandTestAdapterPaths(path, fileHelper, strategy))
                    .Union(defaultExtensionPaths);
            }

            var extensionsFolder = Path.Combine(Path.GetDirectoryName(typeof(TestPlatform).GetTypeInfo().Assembly.GetAssemblyLocation()), "Extensions");
            if (fileHelper.DirectoryExists(extensionsFolder))
            {
                // Load default runtime providers
                if ((strategy & TestAdapterLoadingStrategy.DefaultRuntimeProviders) == TestAdapterLoadingStrategy.DefaultRuntimeProviders)
                {
                    defaultExtensionPaths = fileHelper
                        .EnumerateFiles(extensionsFolder, SearchOption.TopDirectoryOnly, TestPlatformConstants.RunTimeEndsWithPattern)
                        .Union(defaultExtensionPaths);
                }

                // Default extension loader
                if (strategy == TestAdapterLoadingStrategy.Default
                    || (strategy & TestAdapterLoadingStrategy.ExtensionsDirectory) == TestAdapterLoadingStrategy.ExtensionsDirectory)
                {
                    defaultExtensionPaths = fileHelper
                        .EnumerateFiles(extensionsFolder, SearchOption.TopDirectoryOnly, ".dll", ".exe")
                        .Union(defaultExtensionPaths);
                }
            }

            TestPluginCache.Instance.DefaultExtensionPaths = defaultExtensionPaths.Distinct();
        }

        private static SearchOption GetSearchOption(TestAdapterLoadingStrategy strategy, SearchOption defaultStrategyOption) {
            if (strategy == TestAdapterLoadingStrategy.Default) {
                return defaultStrategyOption;
            }

            var searchOption = SearchOption.TopDirectoryOnly;
            if ((strategy & TestAdapterLoadingStrategy.Recursive) == TestAdapterLoadingStrategy.Recursive)
            {
                searchOption = SearchOption.AllDirectories;
            }

            return searchOption;     
        }

        private static IEnumerable<string> ExpandTestAdapterPaths(string path, IFileHelper fileHelper, TestAdapterLoadingStrategy strategy)
        {
            var adapterPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));

            // Default behavior is to only accept directories!
            if (strategy == TestAdapterLoadingStrategy.Default)
            {
                return ExpandAdaptersWithDefaultStrategy(adapterPath, fileHelper);
            }

            var adapters = ExpandAdaptersWithExplicitStrategy(adapterPath, fileHelper, strategy);

            return adapters.Distinct();
        }

        private static IEnumerable<string> ExpandAdaptersWithExplicitStrategy(string path, IFileHelper fileHelper, TestAdapterLoadingStrategy strategy)
        {
            if ((strategy & TestAdapterLoadingStrategy.Explicit) != TestAdapterLoadingStrategy.Explicit)
            {
                return Enumerable.Empty<string>();
            }

            if (fileHelper.Exists(path))
            {
                return new[] { path };
            }
            else if (fileHelper.DirectoryExists(path))
            {
                var searchOption = GetSearchOption(strategy, SearchOption.TopDirectoryOnly);

                var adapterPaths = fileHelper.EnumerateFiles(
                    path,
                    searchOption,
                    TestPlatformConstants.TestAdapterEndsWithPattern,
                    TestPlatformConstants.TestLoggerEndsWithPattern,
                    TestPlatformConstants.DataCollectorEndsWithPattern,
                    TestPlatformConstants.RunTimeEndsWithPattern);

                return adapterPaths;
            }

            EqtTrace.Warning(string.Format("AdapterPath Not Found:", path));
            return Enumerable.Empty<string>();
        }

        private static IEnumerable<string> ExpandAdaptersWithDefaultStrategy(string path, IFileHelper fileHelper)
        {
            if (!fileHelper.DirectoryExists(path))
            {
                EqtTrace.Warning(string.Format("AdapterPath Not Found:", path));

                return Enumerable.Empty<string>();
            }

            return fileHelper.EnumerateFiles(
                    path,
                    SearchOption.AllDirectories,
                    TestPlatformConstants.TestAdapterEndsWithPattern,
                    TestPlatformConstants.TestLoggerEndsWithPattern,
                    TestPlatformConstants.DataCollectorEndsWithPattern,
                    TestPlatformConstants.RunTimeEndsWithPattern);
        }

        private static IEnumerable<string> GetSources(TestRunCriteria testRunCriteria)
        {
            IEnumerable<string> sources = testRunCriteria.Sources;
            if (testRunCriteria.HasSpecificTests)
            {
                // If the test execution is with a test filter, group them by sources.
                sources = testRunCriteria.Tests.Select(tc => tc.Source).Distinct();
            }

            return sources;
        }
    }
}
