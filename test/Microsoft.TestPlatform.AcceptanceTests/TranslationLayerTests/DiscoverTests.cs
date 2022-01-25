﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TestUtilities;
using VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using VisualStudio.TestTools.UnitTesting;

using Moq;

[TestClass]
public class DiscoverTests : AcceptanceTestBase
{
    private readonly IVsTestConsoleWrapper _vstestConsoleWrapper;
    private readonly DiscoveryEventHandler _discoveryEventHandler;
    private readonly DiscoveryEventHandler2 _discoveryEventHandler2;

    public void Setup()
    {
        this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper(out _);
        this.discoveryEventHandler = new DiscoveryEventHandler();
        this.discoveryEventHandler2 = new DiscoveryEventHandler2();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingDiscoveryEventHandler1(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        Setup();

        _vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), _discoveryEventHandler);

        // Assert.
        Assert.AreEqual(6, _discoveryEventHandler.DiscoveredTestCases.Count);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedOut(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.DiscoverTests(
            GetTestAssemblies(),
            GetDefaultRunSettings(),
            new TestPlatformOptions() { CollectMetrics = false },
            _discoveryEventHandler2);

        // Assert.
        Assert.AreEqual(6, _discoveryEventHandler2.DiscoveredTestCases.Count);
        Assert.AreEqual(0, _discoveryEventHandler2.Metrics.Count);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedIn(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.DiscoverTests(GetTestAssemblies(), GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, _discoveryEventHandler2);

        // Assert.
        Assert.AreEqual(6, _discoveryEventHandler2.DiscoveredTestCases.Count);
        Assert.IsTrue(_discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
        Assert.IsTrue(_discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests));
        Assert.IsTrue(_discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecByAllAdapters));
        Assert.IsTrue(_discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForDiscovery));
        Assert.IsTrue(_discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.DiscoveryState));
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingEventHandler2AndBatchSize(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

        string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

        _vstestConsoleWrapper.DiscoverTests(
            GetTestAssemblies(),
            runSettingsXml,
            null,
            discoveryEventHandlerForBatchSize);

        // Assert.
        Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
        Assert.AreEqual(3, discoveryEventHandlerForBatchSize.BatchSize);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingEventHandler1AndBatchSize(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var discoveryEventHandlerForBatchSize = new DiscoveryEventHandlerForBatchSize();

        string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <BatchSize>3</BatchSize>
                                        </RunConfiguration>
                                    </RunSettings>";

        _vstestConsoleWrapper.DiscoverTests(
            GetTestAssemblies(),
            runSettingsXml,
            discoveryEventHandlerForBatchSize);

        // Assert.
        Assert.AreEqual(6, discoveryEventHandlerForBatchSize.DiscoveredTestCases.Count);
        Assert.AreEqual(3, discoveryEventHandlerForBatchSize.BatchSize);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void DiscoverTestsUsingSourceNavigation(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        _vstestConsoleWrapper.DiscoverTests(
            GetTestAssemblies(),
            GetDefaultRunSettings(),
            _discoveryEventHandler);

        // Assert.
        var testCase =
            _discoveryEventHandler.DiscoveredTestCases.Where(dt => dt.FullyQualifiedName.Equals("SampleUnitTestProject.UnitTest1.PassingTest"));

        // Release builds optimize code, hence line numbers are different.
        if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
        {
            Assert.AreEqual(25, testCase.FirstOrDefault().LineNumber);
        }
        else
        {
            Assert.AreEqual(24, testCase.FirstOrDefault().LineNumber);
        }
    }

    [TestMethod]
    // flaky on the desktop runner, desktop framework combo
    [NetFullTargetFrameworkDataSource(useDesktopRunner: false)]
    [NetCoreTargetFrameworkDataSource]
    public void CancelTestDiscovery(RunnerInfo runnerInfo)
    {
        // Setup
        var testAssemblies = new List<string>
        {
            GetAssetFullPath("DiscoveryTestProject.dll"),
            GetAssetFullPath("SimpleTestProject.dll"),
            GetAssetFullPath("SimpleTestProject2.dll")
        };

        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var discoveredTests = new List<TestCase>();
        var discoveryEvents = new Mock<ITestDiscoveryEventsHandler>();
        discoveryEvents.Setup((events) => events.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback
            ((IEnumerable<TestCase> testcases) => discoveredTests.AddRange(testcases));

        // Act
        var discoveryTask = Task.Run(() => _vstestConsoleWrapper.DiscoverTests(testAssemblies, GetDefaultRunSettings(), discoveryEvents.Object));

        Task.Delay(2000).Wait();
        _vstestConsoleWrapper.CancelDiscovery();
        discoveryTask.Wait();

        // Assert.
        int discoveredSources = discoveredTests.Select((testcase) => testcase.Source).Distinct().Count();
        Assert.AreNotEqual(testAssemblies.Count, discoveredSources, "All test assemblies discovered");
    }

    private IList<string> GetTestAssemblies()
    {
        var testAssemblies = new List<string>
        {
            GetAssetFullPath("SimpleTestProject.dll"),
            GetAssetFullPath("SimpleTestProject2.dll")
        };

        return testAssemblies;
    }
}