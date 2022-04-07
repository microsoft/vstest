// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;
using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace TestPlatform.CrossPlatEngine.UnitTests;

[TestClass]
public class TestEngineTests
{
    private readonly ITestEngine _testEngine;
    private readonly Mock<IProcessHelper> _mockProcessHelper;
    private readonly ProtocolConfig _protocolConfig = new() { Version = 1 };
    private readonly Mock<IRequestData> _mockRequestData;
    private readonly Mock<IMetricsCollection> _mockMetricsCollection;

    private ITestRuntimeProvider _testableTestRuntimeProvider;

    public TestEngineTests()
    {
        TestPluginCacheHelper.SetupMockExtensions(new[] { typeof(TestEngineTests).GetTypeInfo().Assembly.Location }, () => { });
        _mockProcessHelper = new Mock<IProcessHelper>();
        _testableTestRuntimeProvider = new TestableRuntimeProvider(true);
        _mockRequestData = new Mock<IRequestData>();
        _mockMetricsCollection = new Mock<IMetricsCollection>();
        _mockRequestData.Setup(rd => rd.MetricsCollection).Returns(_mockMetricsCollection.Object);
        _mockRequestData.Setup(rd => rd.ProtocolConfig).Returns(_protocolConfig);
        _mockProcessHelper.Setup(o => o.GetCurrentProcessFileName()).Returns("vstest.console");
        _testEngine = new TestableTestEngine(_mockProcessHelper.Object);
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldReturnANonNullInstance()
    {
        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
        Assert.IsNotNull(_testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldReturnsNewInstanceOfProxyDiscoveryManagerIfTestHostIsShared()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                 </RunSettings>";
        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);
        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);

        Assert.AreNotSame(discoveryManager, _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria));
        Assert.IsInstanceOfType(_testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria), typeof(ProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldReturnsParallelDiscoveryManagerIfTestHostIsNotShared()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                 </RunSettings>";
        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);
        _testableTestRuntimeProvider = new TestableRuntimeProvider(false);

        Assert.IsNotNull(_testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria));
        Assert.IsInstanceOfType(_testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria), typeof(ParallelProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagerIfCurrentProcessIsDotnet()
    {
        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
        _mockProcessHelper.Setup(o => o.GetCurrentProcessFileName()).Returns("dotnet.exe");

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagerIfDisableAppDomainIsSet()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>true</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagerIfDesignModeIsTrue()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>true</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagereIfTargetFrameworkIsNetcoreApp()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETCoreApp, Version=v1.1</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagereIfTargetFrameworkIsNetStandard()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETStandard, Version=v1.4</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagereIfTargetPlatformIsX64()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x64</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETStandard, Version=v1.4</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagereIfrunsettingsHasTestSettingsInIt()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration>
                    <MSTest>
                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                    </MSTest>
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetDiscoveryManagerShouldReturnsInProcessProxyDiscoveryManager()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x64</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration>
                 </RunSettings>";

        var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

        var discoveryManager = _testEngine.GetDiscoveryManager(_mockRequestData.Object, _testableTestRuntimeProvider, discoveryCriteria);
        Assert.IsNotNull(discoveryManager);
        Assert.IsInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldReturnANonNullInstance()
    {
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);

        Assert.IsNotNull(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
    }

    [TestMethod]
    public void GetExecutionManagerShouldReturnNewInstance()
    {
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);
        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.AreNotSame(executionManager, _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
    }

    [TestMethod]
    public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
    {
        string settingXml = @"<RunSettings><RunConfiguration><InIsolation>true</InIsolation></RunConfiguration></RunSettings>";
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

        Assert.IsNotNull(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
        Assert.IsInstanceOfType(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria), typeof(ProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <MaxCpuCount>2</MaxCpuCount>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                </RunSettings>";
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

        Assert.IsNotNull(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
        Assert.IsInstanceOfType(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria), typeof(ProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
    {
        string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        Assert.IsNotNull(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
        Assert.IsInstanceOfType(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria), typeof(ParallelProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldReturnParallelExecutionManagerIfHostIsNotShared()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                </RunSettings>";
        _testableTestRuntimeProvider = new TestableRuntimeProvider(false);
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        Assert.IsNotNull(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria));
        Assert.IsInstanceOfType(_testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria), typeof(ParallelProxyExecutionManager));
    }

    [TestMethod]
    public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
    {
        var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);
        var result = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ProxyExecutionManagerWithDataCollection));
    }

    [TestMethod]
    public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfInIsolationIsTrue()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsNotInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfParallelEnabled()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>2</MaxCpuCount>
                    </RunConfiguration >
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsNotInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfDataCollectorIsEnabled()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>1</MaxCpuCount>
                    </RunConfiguration >
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsNotInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfInProcDataCollectorIsEnabled()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>1</MaxCpuCount>
                    </RunConfiguration >
                    <InProcDataCollectionRunSettings>
                        <InProcDataCollectors>
                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='SimpleDataCollector.SimpleDataCollector, SimpleDataCollector, Version=15.6.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\Enlistments\vstest\test\TestAssets\SimpleDataCollector\bin\Debug\net451\SimpleDataCollector.dll'>
                                <Configuration>
                                    <Port>4312</Port>
                                </Configuration>
                            </InProcDataCollector>
                        </InProcDataCollectors>
                    </InProcDataCollectionRunSettings>
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsNotInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }

    [TestMethod]
    public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfrunsettingsHasTestSettingsInIt()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>1</MaxCpuCount>
                    </RunConfiguration >
                    <MSTest>
                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                    </MSTest>
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsNotInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }


    [TestMethod]
    public void GetExecutionManagerShouldReturnInProcessProxyexecutionManager()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>1</MaxCpuCount>
                    </RunConfiguration>
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsNotNull(executionManager);
        Assert.IsInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
    }

    [TestMethod]
    public void GetExtensionManagerShouldReturnANonNullInstance()
    {
        Assert.IsNotNull(_testEngine.GetExtensionManager());
    }

    [TestMethod]
    public void GetExtensionManagerShouldCollectMetrics()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                        <MaxCpuCount>1</MaxCpuCount>
                    </RunConfiguration>
                 </RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        _mockMetricsCollection.Verify(mc => mc.Add(TelemetryDataConstants.ParallelEnabledDuringExecution, It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ProxyDataCollectionManagerShouldBeInitialzedWithCorrectTestSourcesWhenTestRunCriteriaContainsSourceList()
    {
        var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";

        var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsTrue(((ProxyExecutionManagerWithDataCollection)executionManager).ProxyDataCollectionManager.Sources.Contains("1.dll"));
    }

    [TestMethod]
    public void ProxyDataCollectionManagerShouldBeInitialzedWithCorrectTestSourcesWhenTestRunCriteriaContainsTestCaseList()
    {
        var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";

        var testCaseList = new List<TestCase> { new TestCase("x.y.z", new Uri("uri://dummy"), "x.dll") };
        var testRunCriteria = new TestRunCriteria(testCaseList, 100, false, settingXml);

        var executionManager = _testEngine.GetExecutionManager(_mockRequestData.Object, _testableTestRuntimeProvider, testRunCriteria);

        Assert.IsTrue(((ProxyExecutionManagerWithDataCollection)executionManager).ProxyDataCollectionManager.Sources.Contains("x.dll"));
    }

    /// <summary>
    /// GetLoggerManager should return a non null instance.
    /// </summary>
    [TestMethod]
    public void GetLoggerManagerShouldReturnNonNullInstance()
    {
        Assert.IsNotNull(_testEngine.GetLoggerManager(_mockRequestData.Object));
    }

    /// <summary>
    /// GetLoggerManager should always return new instance of logger manager.
    /// </summary>
    [TestMethod]
    public void GetLoggerManagerShouldAlwaysReturnNewInstance()
    {
        Assert.AreNotSame(_testEngine.GetLoggerManager(_mockRequestData.Object), _testEngine.GetLoggerManager(_mockRequestData.Object));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnAValidInstance()
    {
        var settingXml = @"<RunSettings><RunConfiguration><InIsolation>true</InIsolation></RunConfiguration></RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        Assert.IsNotNull(_testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNewInstance()
    {
        var settingXml = @"<RunSettings><RunConfiguration><InIsolation>true</InIsolation></RunConfiguration></RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager1 = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.AreNotSame(
            _testEngine.GetTestSessionManager(
                _mockRequestData.Object,
                testSessionCriteria),
            testSessionManager1);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnDefaultTestSessionManagerIfParallelDisabled()
    {
        var settingXml = @"<RunSettings><RunConfiguration><InIsolation>true</InIsolation></RunConfiguration></RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
        Assert.IsInstanceOfType(testSessionManager, typeof(ProxyTestSessionManager));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnDefaultTestSessionManagerEvenIfParallelEnabled()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <MaxCpuCount>2</MaxCpuCount>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                </RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
        Assert.IsInstanceOfType(testSessionManager, typeof(ProxyTestSessionManager));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnDefaultTestSessionManagerIfParallelEnabled()
    {
        string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll", "2.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
        Assert.IsInstanceOfType(testSessionManager, typeof(ProxyTestSessionManager));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnDefaultTestSessionManagerIfHostIsNotShared()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <InIsolation>true</InIsolation>
                    </RunConfiguration >
                </RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll", "2.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
        Assert.IsInstanceOfType(testSessionManager, typeof(ProxyTestSessionManager));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnDefaultTestSessionManagerIfDataCollectionIsEnabled()
    {
        var settingXml =
            @"<RunSettings>
                    <DataCollectionRunSettings>
                        <DataCollectors>
                            <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
                            </DataCollector>
                        </DataCollectors>
                    </DataCollectionRunSettings>
                </RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
        Assert.IsInstanceOfType(testSessionManager, typeof(ProxyTestSessionManager));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNullWhenTargetFrameworkIsNetFramework()
    {
        var settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration>
                 </RunSettings>";
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        Assert.IsNull(_testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria));
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfCurrentProcessIsDotnet()
    {
        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = null
        };
        _mockProcessHelper.Setup(ph => ph.GetCurrentProcessFileName()).Returns("dotnet.exe");

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfDisableAppDomainIsSet()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>true</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfDesignModeIsTrue()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>true</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfTargetFrameworkIsNetcoreApp()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETCoreApp, Version=v1.1</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfTargetFrameworkIsNetStandard()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETStandard, Version=v1.4</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfTargetPlatformIsX64()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x64</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETStandard, Version=v1.4</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }

    [TestMethod]
    public void GetTestSessionManagerShouldReturnNotNullIfRunSettingsHasTestSettingsInIt()
    {
        string settingXml =
            @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration>
                    <MSTest>
                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                    </MSTest>
                 </RunSettings>";

        var testSessionCriteria = new StartTestSessionCriteria()
        {
            Sources = new List<string> { "1.dll" },
            RunSettings = settingXml
        };

        var testSessionManager = _testEngine.GetTestSessionManager(
            _mockRequestData.Object,
            testSessionCriteria);

        Assert.IsNotNull(testSessionManager);
    }
}
