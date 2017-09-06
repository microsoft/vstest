// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestHostManagerFactoryTests
    {
        private TestHostManagerFactory testHostManagerFactory;
        private IMetricsCollector metricsCollector;

        public TestHostManagerFactoryTests()
        {
            this.metricsCollector = new DummyMetricCollector();
            this.testHostManagerFactory = new TestHostManagerFactory(this.metricsCollector);
        }

        [TestMethod]
        public void ConstructorShouldThrowIfMetricsCollectorIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new TestHostManagerFactory(null));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnADiscoveryManagerInstance()
        {
            Assert.IsNotNull(this.testHostManagerFactory.GetDiscoveryManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheDiscoveryManagerInstance()
        {
            Assert.AreEqual(this.testHostManagerFactory.GetDiscoveryManager(), this.testHostManagerFactory.GetDiscoveryManager());
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
