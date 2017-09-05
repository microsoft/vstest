// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestHostManagerFactoryTests
    {
        private TestHostManagerFactory testHostManagerFactory;
        private IMetricsCollector metricsCollector;

        [TestInitialize]
        public void TestInit()
        {
            this.testHostManagerFactory = new TestHostManagerFactory();
            this.metricsCollector = new DummyMetricCollector();
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnADiscoveryManagerInstance()
        {
            Assert.IsNotNull(this.testHostManagerFactory.GetDiscoveryManager(this.metricsCollector));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheDiscoveryManagerInstance()
        {
            Assert.AreEqual(this.testHostManagerFactory.GetDiscoveryManager(this.metricsCollector), this.testHostManagerFactory.GetDiscoveryManager(this.metricsCollector));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnAnExecutionManagerInstance()
        {
            Assert.IsNotNull(this.testHostManagerFactory.GetExecutionManager(this.metricsCollector));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheExecutionManagerInstance()
        {
            Assert.AreEqual(this.testHostManagerFactory.GetExecutionManager(this.metricsCollector), this.testHostManagerFactory.GetExecutionManager(this.metricsCollector));
        }
    }
}
