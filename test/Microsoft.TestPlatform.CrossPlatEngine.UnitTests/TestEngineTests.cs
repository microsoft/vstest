// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class TestEngineTests
    {
        private ITestEngine testEngine;
        private Mock<IProcessHelper> mockProcessHelper;
        private ProtocolConfig protocolConfig = new ProtocolConfig { Version = 1 };
        private ITestRuntimeProvider testableTestRuntimeProvider;

        public TestEngineTests()
        {
            TestPluginCacheTests.SetupMockExtensions(new[] { typeof(TestEngineTests).GetTypeInfo().Assembly.Location }, () => { });
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.testableTestRuntimeProvider = new TestableRuntimeProvider(true);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockProcessHelper.Setup(o => o.GetCurrentProcessFileName()).Returns("vstest.console");
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object);
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnANonNullInstance()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsNewInstanceOfProxyDiscoveryManagerIfTestHostIsShared()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);

            Assert.AreNotSame(discoveryManager, this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig), typeof(ProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsParallelDiscoveryManagerIfTestHostIsNotShared()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            this.testableTestRuntimeProvider = new TestableRuntimeProvider(false);

            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig), typeof(ParallelProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldNotReturnsInProcessProxyDiscoveryManagerIfCurrentProcessIsDotnet()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            this.mockProcessHelper.Setup(o => o.GetCurrentProcessFileName()).Returns("dotnet.exe");

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
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

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
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

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
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

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
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

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
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

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
            Assert.IsNotNull(discoveryManager);
            Assert.IsNotInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsInProcessProxyDiscoveryManager()
        {
            string settingXml =
                @"<RunSettings>
                    <RunConfiguration>
                        <TargetPlatform>x86</TargetPlatform>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, settingXml);

            var discoveryManager = this.testEngine.GetDiscoveryManager(this.testableTestRuntimeProvider, discoveryCriteria, this.protocolConfig);
            Assert.IsNotNull(discoveryManager);
            Assert.IsInstanceOfType(discoveryManager, typeof(InProcessProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnANonNullInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnNewInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);
            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

            Assert.AreNotSame(executionManager, this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfHostIsNotShared()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            this.testableTestRuntimeProvider = new TestableRuntimeProvider(false);
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, null);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
        {
            var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);
            var result = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ProxyExecutionManagerWithDataCollection));
        }

        [TestMethod]
        public void GetExecutionManagerShouldNotReturnInProcessProxyexecutionManagerIfInIsolationIsTrue()
        {
            this.testEngine = new TestableTestEngine(this.mockProcessHelper.Object, true);
            string settingXml =
               @"<RunSettings>
                    <RunConfiguration>
                        <DisableAppDomain>false</DisableAppDomain>
                        <DesignMode>false</DesignMode>
                        <TargetFrameworkVersion>.NETFramework, Version=v4.5</TargetFrameworkVersion>
                    </RunConfiguration >
                 </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

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

            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

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

            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

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
                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='SimpleDataCollector.SimpleDataCollector, SimpleDataCollector, Version=15.5.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\Enlistments\vstest\test\TestAssets\SimpleDataCollector\bin\Debug\net451\SimpleDataCollector.dll'>
                                <Configuration>
                                    <Port>4312</Port>
                                </Configuration>
                            </InProcDataCollector>
                        </InProcDataCollectors>
                    </InProcDataCollectionRunSettings>
                 </RunSettings>";

            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

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

            var executionManager = this.testEngine.GetExecutionManager(this.testableTestRuntimeProvider, testRunCriteria, this.protocolConfig);

            Assert.IsNotNull(executionManager);
            Assert.IsInstanceOfType(executionManager, typeof(InProcessProxyExecutionManager));
        }

        [TestMethod]
        public void GetExtensionManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetExtensionManager());
        }
    }
}
