// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Discovery;
using Execution;
using Common;
using Common.ExtensionFramework;
using Common.Hosting;
using Common.Logging;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using CrossPlatEngine;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using ObjectModel.Engine;
using ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using PlatformAbstractions;
using Utilities.Helpers;
using Utilities.Helpers.Interfaces;

using ClientResources = Resources.Resources;

/// <summary>
/// Implementation for TestPlatform.
/// </summary>
internal class TestPlatform : ITestPlatform
{
    private readonly TestRuntimeProviderManager _testHostProviderManager;

    private readonly IFileHelper _fileHelper;

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
        TestEngine = testEngine;
        _fileHelper = filehelper;
        _testHostProviderManager = testHostProviderManager;
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

        // Update cache with Extension folder's files.
        AddExtensionAssemblies(discoveryCriteria.RunSettings);

        // Update extension assemblies from source when design mode is false.
        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(discoveryCriteria.RunSettings);
        if (!runConfiguration.DesignMode)
        {
            AddExtensionAssembliesFromSource(discoveryCriteria.Sources);
        }

        // Initialize loggers.
        var loggerManager = TestEngine.GetLoggerManager(requestData);
        loggerManager.Initialize(discoveryCriteria.RunSettings);

        var testHostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(discoveryCriteria.RunSettings);
        ThrowExceptionIfTestHostManagerIsNull(testHostManager, discoveryCriteria.RunSettings);

        testHostManager.Initialize(TestSessionMessageLogger.Instance, discoveryCriteria.RunSettings);

        var discoveryManager = TestEngine.GetDiscoveryManager(requestData, testHostManager, discoveryCriteria);
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

        AddExtensionAssemblies(testRunCriteria.TestRunSettings);

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testRunCriteria.TestRunSettings);

        // Update extension assemblies from source when design mode is false.
        if (!runConfiguration.DesignMode)
        {
            AddExtensionAssembliesFromSource(testRunCriteria);
        }

        // Initialize loggers.
        var loggerManager = TestEngine.GetLoggerManager(requestData);
        loggerManager.Initialize(testRunCriteria.TestRunSettings);

        var testHostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(testRunCriteria.TestRunSettings);
        ThrowExceptionIfTestHostManagerIsNull(testHostManager, testRunCriteria.TestRunSettings);

        testHostManager.Initialize(TestSessionMessageLogger.Instance, testRunCriteria.TestRunSettings);

        // NOTE: The custom launcher should not be set when we have test session info available.
        if (testRunCriteria.TestHostLauncher != null)
        {
            testHostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
        }

        var executionManager = TestEngine.GetExecutionManager(requestData, testHostManager, testRunCriteria);
        executionManager.Initialize(options?.SkipDefaultAdapters ?? false);

        return new TestRunRequest(requestData, testRunCriteria, executionManager, loggerManager);
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

        AddExtensionAssemblies(testSessionCriteria.RunSettings);

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testSessionCriteria.RunSettings);
        if (!runConfiguration.DesignMode)
        {
            return false;
        }

        var testSessionManager = TestEngine.GetTestSessionManager(requestData, testSessionCriteria);
        if (testSessionManager == null)
        {
            // The test session manager is null because the combination of runsettings and
            // sources tells us we should run in-process (i.e. in vstest.console). Because
            // of this no session will be created because there's no testhost to be launched.
            // Expecting a subsequent call to execute tests with the same set of parameters.
            eventsHandler.HandleStartTestSessionComplete(new());
            return false;
        }

        return testSessionManager.StartSession(eventsHandler, requestData);
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
        TestEngine.GetExtensionManager().UseAdditionalExtensions(pathToAdditionalExtensions, skipExtensionFilters);
    }

    /// <inheritdoc/>
    public void ClearExtensions()
    {
        TestEngine.GetExtensionManager().ClearExtensions();
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

    /// <summary>
    /// Updates the test adapter paths provided through run settings to be used by the test
    /// service.
    /// </summary>
    ///
    /// <param name="runSettings">The run settings.</param>
    private void AddExtensionAssemblies(string runSettings)
    {
        IEnumerable<string> customTestAdaptersPaths = RunSettingsUtilities.GetTestAdaptersPaths(runSettings);

        if (customTestAdaptersPaths != null)
        {
            foreach (string customTestAdaptersPath in customTestAdaptersPaths)
            {
                var adapterPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(customTestAdaptersPath));
                if (!Directory.Exists(adapterPath))
                {
                    EqtTrace.Warning($"AdapterPath Not Found: {adapterPath}");
                    continue;
                }

                var extensionAssemblies = new List<string>(
                    _fileHelper.EnumerateFiles(
                        adapterPath,
                        SearchOption.AllDirectories,
                        TestPlatformConstants.TestAdapterEndsWithPattern,
                        TestPlatformConstants.TestLoggerEndsWithPattern,
                        TestPlatformConstants.DataCollectorEndsWithPattern,
                        TestPlatformConstants.RunTimeEndsWithPattern));

                if (extensionAssemblies.Count > 0)
                {
                    UpdateExtensions(extensionAssemblies, skipExtensionFilters: false);
                }
            }
        }
    }

    /// <summary>
    /// Updates the extension assemblies from source directory.
    /// </summary>
    ///
    /// <param name="testRunCriteria">The test run criteria.</param>
    private void AddExtensionAssembliesFromSource(TestRunCriteria testRunCriteria)
    {
        IEnumerable<string> sources = testRunCriteria.Sources;
        if (testRunCriteria.HasSpecificTests)
        {
            // If the test execution is with a test filter, group them by sources.
            sources = testRunCriteria.Tests.Select(tc => tc.Source).Distinct();
        }

        AddExtensionAssembliesFromSource(sources);
    }

    /// <summary>
    /// Updates the test logger paths from source directory.
    /// </summary>
    ///
    /// <param name="sources">The list of sources.</param>
    private void AddExtensionAssembliesFromSource(IEnumerable<string> sources)
    {
        // Currently we support discovering loggers only from Source directory.
        var loggersToUpdate = new List<string>();

        foreach (var source in sources)
        {
            var sourceDirectory = Path.GetDirectoryName(source);
            if (!string.IsNullOrEmpty(sourceDirectory)
                && _fileHelper.DirectoryExists(sourceDirectory))
            {
                loggersToUpdate.AddRange(
                    _fileHelper.EnumerateFiles(
                        sourceDirectory,
                        SearchOption.TopDirectoryOnly,
                        TestPlatformConstants.TestLoggerEndsWithPattern));
            }
        }

        if (loggersToUpdate.Count > 0)
        {
            UpdateExtensions(loggersToUpdate, skipExtensionFilters: false);
        }
    }

    /// <summary>
    /// Finds all test platform extensions from the `.\Extensions` directory. This is used to
    /// load the inbox extensions like TrxLogger and legacy test extensions like MSTest v1,
    /// MSTest C++, etc..
    /// </summary>
    private static void AddExtensionAssembliesFromExtensionDirectory()
    {
        var fileHelper = new FileHelper();
        var extensionsFolder = Path.Combine(
            Path.GetDirectoryName(
                typeof(TestPlatform).GetTypeInfo().Assembly.GetAssemblyLocation()),
            "Extensions");

        if (fileHelper.DirectoryExists(extensionsFolder))
        {
            var defaultExtensionPaths = fileHelper.EnumerateFiles(
                extensionsFolder,
                SearchOption.TopDirectoryOnly,
                ".dll",
                ".exe");

            TestPluginCache.Instance.DefaultExtensionPaths = defaultExtensionPaths;
        }
    }
}
