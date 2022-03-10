// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Common;
using Common.Hosting;
using Common.Logging;
using Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using CommunicationUtilities;
using Client;
using Client.Parallel;
using DataCollection;
using Utilities;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using ObjectModel.Engine;
using ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using PlatformAbstractions;
using PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;

/// <summary>
/// Cross platform test engine entry point for the client.
/// </summary>
public class TestEngine : ITestEngine
{
    #region Private Fields

    private readonly TestRuntimeProviderManager _testHostProviderManager;
    private ITestExtensionManager _testExtensionManager;
    private readonly IProcessHelper _processHelper;

    #endregion

    public TestEngine() : this(TestRuntimeProviderManager.Instance, new ProcessHelper())
    {
    }

    protected TestEngine(
        TestRuntimeProviderManager testHostProviderManager,
        IProcessHelper processHelper)
    {
        _testHostProviderManager = testHostProviderManager;
        _processHelper = processHelper;
    }

    #region ITestEngine implementation

    /// <inheritdoc/>
    public IProxyDiscoveryManager GetDiscoveryManager(
        IRequestData requestData,
        ITestRuntimeProvider testHostManager,
        DiscoveryCriteria discoveryCriteria)
    {
        var parallelLevel = VerifyParallelSettingAndCalculateParallelLevel(
            discoveryCriteria.Sources.Count(),
            discoveryCriteria.RunSettings);

        // Collecting IsParallel enabled.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.ParallelEnabledDuringDiscovery,
            parallelLevel > 1 ? "True" : "False");
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionId,
            discoveryCriteria.TestSessionInfo?.Id.ToString() ?? string.Empty);

        if (ShouldRunInNoIsolation(discoveryCriteria.RunSettings, parallelLevel > 1, false))
        {
            var isTelemetryOptedIn = requestData.IsTelemetryOptedIn;
            var newRequestData = GetRequestData(isTelemetryOptedIn);
            return new InProcessProxyDiscoveryManager(
                testHostManager,
                new TestHostManagerFactory(newRequestData));
        }

        Func<IProxyDiscoveryManager> proxyDiscoveryManagerCreator = () =>
        {
            var hostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(discoveryCriteria.RunSettings);
            hostManager?.Initialize(TestSessionMessageLogger.Instance, discoveryCriteria.RunSettings);

            // This function is used to either take a pre-existing proxy operation manager from
            // the test pool or to create a new proxy operation manager on the spot.
            Func<string, ProxyDiscoveryManager, ProxyOperationManager>
                proxyOperationManagerCreator = (
                    string source,
                    ProxyDiscoveryManager proxyDiscoveryManager) =>
                {
                    // In case we have an active test session, we always prefer the already
                    // created proxies instead of the ones that need to be created on the spot.
                    var proxyOperationManager = TestSessionPool.Instance.TryTakeProxy(
                        discoveryCriteria.TestSessionInfo,
                        source,
                        discoveryCriteria.RunSettings);

                    if (proxyOperationManager == null)
                    {
                        // If the proxy creation process based on test session info failed, then
                        // we'll proceed with the normal creation process as if no test session
                        // info was passed in in the first place.
                        //
                        // WARNING: This should not normally happen and it raises questions
                        // regarding the test session pool operation and consistency.
                        EqtTrace.Warning("ProxyDiscoveryManager creation with test session failed.");

                        proxyOperationManager = new ProxyOperationManager(
                            requestData,
                            new TestRequestSender(requestData.ProtocolConfig, hostManager),
                            hostManager,
                            proxyDiscoveryManager);
                    }

                    return proxyOperationManager;
                };

            // In case we have an active test session, we always prefer the already
            // created proxies instead of the ones that need to be created on the spot.
            return (discoveryCriteria.TestSessionInfo != null)
                ? new ProxyDiscoveryManager(
                    discoveryCriteria.TestSessionInfo,
                    proxyOperationManagerCreator)
                : new ProxyDiscoveryManager(
                    requestData,
                    new TestRequestSender(requestData.ProtocolConfig, hostManager),
                    hostManager);
        };

        return (parallelLevel > 1 || !testHostManager.Shared)
            ? new ParallelProxyDiscoveryManager(
                requestData,
                proxyDiscoveryManagerCreator,
                parallelLevel,
                sharedHosts: testHostManager.Shared)
            : proxyDiscoveryManagerCreator();
    }

    /// <inheritdoc/>
    public IProxyExecutionManager GetExecutionManager(
        IRequestData requestData,
        ITestRuntimeProvider testHostManager,
        TestRunCriteria testRunCriteria)
    {
        var distinctSources = GetDistinctNumberOfSources(testRunCriteria);
        var parallelLevel = VerifyParallelSettingAndCalculateParallelLevel(
            distinctSources,
            testRunCriteria.TestRunSettings);

        // Collecting IsParallel enabled.
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.ParallelEnabledDuringExecution,
            parallelLevel > 1 ? "True" : "False");
        requestData.MetricsCollection.Add(
            TelemetryDataConstants.TestSessionId,
            testRunCriteria.TestSessionInfo?.Id.ToString() ?? string.Empty);

        var isDataCollectorEnabled = XmlRunSettingsUtilities.IsDataCollectionEnabled(testRunCriteria.TestRunSettings);
        var isInProcDataCollectorEnabled = XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(testRunCriteria.TestRunSettings);

        if (ShouldRunInNoIsolation(
                testRunCriteria.TestRunSettings,
                parallelLevel > 1,
                isDataCollectorEnabled || isInProcDataCollectorEnabled))
        {
            var isTelemetryOptedIn = requestData.IsTelemetryOptedIn;
            var newRequestData = GetRequestData(isTelemetryOptedIn);
            return new InProcessProxyExecutionManager(
                testHostManager,
                new TestHostManagerFactory(newRequestData));
        }

        // SetupChannel ProxyExecutionManager with data collection if data collectors are
        // specified in run settings.
        Func<IProxyExecutionManager> proxyExecutionManagerCreator = () =>
        {
            // Create a new host manager, to be associated with individual
            // ProxyExecutionManager(&POM)
            var hostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(testRunCriteria.TestRunSettings);
            hostManager?.Initialize(TestSessionMessageLogger.Instance, testRunCriteria.TestRunSettings);

            if (testRunCriteria.TestHostLauncher != null)
            {
                hostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
            }

            var requestSender = new TestRequestSender(requestData.ProtocolConfig, hostManager);

            if (testRunCriteria.TestSessionInfo != null)
            {
                // This function is used to either take a pre-existing proxy operation manager from
                // the test pool or to create a new proxy operation manager on the spot.
                Func<string, ProxyExecutionManager, ProxyOperationManager>
                    proxyOperationManagerCreator = (
                        string source,
                        ProxyExecutionManager proxyExecutionManager) =>
                    {
                        var proxyOperationManager = TestSessionPool.Instance.TryTakeProxy(
                            testRunCriteria.TestSessionInfo,
                            source,
                            testRunCriteria.TestRunSettings);

                        if (proxyOperationManager == null)
                        {
                            // If the proxy creation process based on test session info failed, then
                            // we'll proceed with the normal creation process as if no test session
                            // info was passed in in the first place.
                            //
                            // WARNING: This should not normally happen and it raises questions
                            // regarding the test session pool operation and consistency.
                            EqtTrace.Warning("ProxyExecutionManager creation with test session failed.");

                            proxyOperationManager = new ProxyOperationManager(
                                requestData,
                                requestSender,
                                hostManager,
                                proxyExecutionManager);
                        }

                        return proxyOperationManager;
                    };

                // In case we have an active test session, data collection needs were
                // already taken care of when first creating the session. As a consequence
                // we always return this proxy instead of choosing between the vanilla
                // execution proxy and the one with data collection enabled.
                return new ProxyExecutionManager(
                    testRunCriteria.TestSessionInfo,
                    proxyOperationManagerCreator,
                    testRunCriteria.DebugEnabledForTestSession);
            }

            return isDataCollectorEnabled
                ? new ProxyExecutionManagerWithDataCollection(
                    requestData,
                    requestSender,
                    hostManager,
                    new ProxyDataCollectionManager(
                        requestData,
                        testRunCriteria.TestRunSettings,
                        GetSourcesFromTestRunCriteria(testRunCriteria)))
                : new ProxyExecutionManager(
                    requestData,
                    requestSender,
                    hostManager);
        };

        // parallelLevel = 1 for desktop should go via else route.
        var executionManager = (parallelLevel > 1 || !testHostManager.Shared)
            ? new ParallelProxyExecutionManager(
                requestData,
                proxyExecutionManagerCreator,
                parallelLevel,
                sharedHosts: testHostManager.Shared)
            : proxyExecutionManagerCreator();

        EqtTrace.Verbose($"TestEngine.GetExecutionManager: Chosen execution manager '{executionManager.GetType().AssemblyQualifiedName}' ParallelLevel '{parallelLevel}' Shared host '{testHostManager.Shared}'");

        return executionManager;
    }

    /// <inheritdoc/>
    public IProxyTestSessionManager GetTestSessionManager(
        IRequestData requestData,
        StartTestSessionCriteria testSessionCriteria)
    {
        var parallelLevel = VerifyParallelSettingAndCalculateParallelLevel(
            testSessionCriteria.Sources.Count,
            testSessionCriteria.RunSettings);

        requestData.MetricsCollection.Add(
            TelemetryDataConstants.ParallelEnabledDuringStartTestSession,
            parallelLevel > 1 ? "True" : "False");

        var isDataCollectorEnabled = XmlRunSettingsUtilities.IsDataCollectionEnabled(testSessionCriteria.RunSettings);
        var isInProcDataCollectorEnabled = XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(testSessionCriteria.RunSettings);

        if (ShouldRunInNoIsolation(
                testSessionCriteria.RunSettings,
                parallelLevel > 1,
                isDataCollectorEnabled || isInProcDataCollectorEnabled))
        {
            // This condition is the equivalent of the in-process proxy execution manager case.
            // In this case all tests will be run in the vstest.console process, so there's no
            // test host to be started. As a consequence there'll be no session info.
            return null;
        }

        Func<ProxyOperationManager> proxyCreator = () =>
        {
            var hostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(testSessionCriteria.RunSettings);
            if (hostManager == null)
            {
                throw new TestPlatformException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.NoTestHostProviderFound));
            }

            hostManager.Initialize(TestSessionMessageLogger.Instance, testSessionCriteria.RunSettings);
            if (testSessionCriteria.TestHostLauncher != null)
            {
                hostManager.SetCustomLauncher(testSessionCriteria.TestHostLauncher);
            }

            var requestSender = new TestRequestSender(requestData.ProtocolConfig, hostManager)
            {
                CloseConnectionOnOperationComplete = false
            };

            // TODO (copoiena): For now we don't support data collection alongside test
            // sessions.
            //
            // The reason for this is that, in the case of Code Coverage for example, the
            // data collector needs to pass some environment variables to the testhost process
            // before the testhost process is started. This means that the data collector must
            // be running when the testhost process is spawned, however the testhost process
            // should be spawned during build, and it's problematic to have the data collector
            // running during build because it must instrument the .dll files that don't exist
            // yet.
            return isDataCollectorEnabled
                ? null
                // ? new ProxyOperationManagerWithDataCollection(
                //     requestData,
                //     requestSender,
                //     hostManager,
                //     new ProxyDataCollectionManager(
                //         requestData,
                //         testSessionCriteria.RunSettings,
                //         testSessionCriteria.Sources))
                //     {
                //         CloseRequestSenderChannelOnProxyClose = true
                //     }
                : new ProxyOperationManager(
                    requestData,
                    requestSender,
                    hostManager);
        };

        var testhostManager = _testHostProviderManager.GetTestHostManagerByRunConfiguration(testSessionCriteria.RunSettings);
        testhostManager.Initialize(TestSessionMessageLogger.Instance, testSessionCriteria.RunSettings);
        var testhostCount = (parallelLevel > 1 || !testhostManager.Shared)
            ? testSessionCriteria.Sources.Count
            : 1;

        return new ProxyTestSessionManager(testSessionCriteria, testhostCount, proxyCreator);
    }

    /// <inheritdoc/>
    public ITestExtensionManager GetExtensionManager() => _testExtensionManager ??= new TestExtensionManager();

    /// <inheritdoc/>
    public ITestLoggerManager GetLoggerManager(IRequestData requestData)
    {
        return new TestLoggerManager(
            requestData,
            TestSessionMessageLogger.Instance,
            new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
    }

    #endregion

    private static int GetDistinctNumberOfSources(TestRunCriteria testRunCriteria)
    {
        // No point in creating more processes if number of sources is less than what the user
        // configured for.
        int numSources = testRunCriteria.HasSpecificTests
            ? new HashSet<string>(
                testRunCriteria.Tests.Select(testCase => testCase.Source)).Count
            : testRunCriteria.Sources.Count();
        return numSources;
    }

    /// <summary>
    /// Verifies parallel setting and returns parallel level to use based on the run criteria.
    /// </summary>
    ///
    /// <param name="sourceCount">The source count.</param>
    /// <param name="runSettings">The run settings.</param>
    ///
    /// <returns>The parallel level to use.</returns>
    private int VerifyParallelSettingAndCalculateParallelLevel(
        int sourceCount,
        string runSettings)
    {
        // Default is 1.
        int parallelLevelToUse;
        try
        {
            // Check the user parallel setting.
            int userParallelSetting = RunSettingsUtilities.GetMaxCpuCount(runSettings);
            parallelLevelToUse = userParallelSetting == 0
                ? Environment.ProcessorCount
                : userParallelSetting;
            var enableParallel = parallelLevelToUse > 1;

            EqtTrace.Verbose(
                "TestEngine: Initializing Parallel Execution as MaxCpuCount is set to: {0}",
                parallelLevelToUse);

            // Verify if the number of sources is less than user setting of parallel.
            // We should use number of sources as the parallel level, if sources count is less
            // than parallel level.
            if (enableParallel)
            {
                parallelLevelToUse = Math.Min(sourceCount, parallelLevelToUse);

                // If only one source, no need to use parallel service client.
                enableParallel = parallelLevelToUse > 1;

                EqtTrace.Verbose(
                    "TestEngine: ParallelExecution set to '{0}' as the parallel level is adjusted to '{1}' based on number of sources",
                    enableParallel,
                    parallelLevelToUse);
            }
        }
        catch (Exception ex)
        {
            EqtTrace.Error(
                "TestEngine: Error occurred while initializing ParallelExecution: {0}",
                ex);
            EqtTrace.Warning("TestEngine: Defaulting to Sequential Execution");

            parallelLevelToUse = 1;
        }

        return parallelLevelToUse;
    }

    private bool ShouldRunInNoIsolation(
        string runsettings,
        bool isParallelEnabled,
        bool isDataCollectorEnabled)
    {
        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettings);

        if (runConfiguration.InIsolation)
        {
            EqtTrace.Info("TestEngine.ShouldRunInNoIsolation: running test in isolation");
            return false;
        }

        // Run tests in isolation if run is authored using testsettings.
        if (InferRunSettingsHelper.IsTestSettingsEnabled(runsettings))
        {
            return false;
        }

        var currentProcessPath = _processHelper.GetCurrentProcessFileName();

        // If running with the dotnet executable, then don't run in in process.
        if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
            || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Return true if
        // 1) Not running in parallel;
        // 2) Data collector is not enabled;
        // 3) Target framework is X64 or anyCpu;
        // 4) DisableAppDomain is false;
        // 5) Not running in design mode;
        // 6) target framework is NETFramework (Desktop test);
        if (!isParallelEnabled &&
            !isDataCollectorEnabled &&
            (runConfiguration.TargetPlatform == ObjectModel.Constants.DefaultPlatform || runConfiguration.TargetPlatform == Architecture.AnyCPU) &&
            !runConfiguration.DisableAppDomain &&
            !runConfiguration.DesignMode &&
            runConfiguration.TargetFramework.Name.IndexOf("netframework", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            EqtTrace.Info("TestEngine.ShouldRunInNoIsolation: running test in process(inside vstest.console.exe process)");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get request data on basis of telemetry opted in or not.
    /// </summary>
    ///
    /// <param name="isTelemetryOptedIn">A flag indicating if telemetry is opted in.</param>
    ///
    /// <returns>The request data.</returns>
    private IRequestData GetRequestData(bool isTelemetryOptedIn)
    {
        return new RequestData
        {
            MetricsCollection = isTelemetryOptedIn
                ? (IMetricsCollection)new MetricsCollection()
                : new NoOpMetricsCollection(),
            IsTelemetryOptedIn = isTelemetryOptedIn
        };
    }

    /// <summary>
    /// Gets test sources from test run criteria.
    /// </summary>
    ///
    /// <returns>The test sources.</returns>
    private IEnumerable<string> GetSourcesFromTestRunCriteria(TestRunCriteria testRunCriteria)
    {
        return testRunCriteria.HasSpecificTests
            ? TestSourcesUtility.GetSources(testRunCriteria.Tests)
            : testRunCriteria.Sources;
    }
}
