// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class TestEngineTests
    {
        private ITestEngine testEngine;
        private ProtocolConfig protocolConfig = new ProtocolConfig { Version = 1 };
        private Mock<ITestRuntimeProvider> mockTestHostManager;

        public TestEngineTests()
        {
            TestPluginCacheTests.SetupMockExtensions(new[] { typeof(TestEngineTests).GetTypeInfo().Assembly.Location }, () => { });
            this.testEngine = new TestEngine();
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            
            // Default setting for host manager
            this.mockTestHostManager.Setup(p => p.Shared).Returns(true);
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnANonNullInstance()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsNewInstanceOfProxyDiscoveryManagerIfTestHostIsShared()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            var discoveryManager = this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig);

            Assert.AreNotSame(discoveryManager, this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig), typeof(ProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsParallelDiscoveryManagerIfTestHostIsNotShared()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            this.mockTestHostManager.Setup(p => p.Shared).Returns(false);
            
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria, this.protocolConfig), typeof(ParallelProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnANonNullInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnNewInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);
            var executionManager = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig);

            Assert.AreNotSame(executionManager, this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfHostIsNotShared()
        {
            this.mockTestHostManager.Setup(p => p.Shared).Returns(false);
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, null);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
        {
            var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);
            var result = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria, this.protocolConfig);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ProxyExecutionManagerWithDataCollection));
        }

        [TestMethod]
        public void GetExtensionManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetExtensionManager());
        }
    }
}
