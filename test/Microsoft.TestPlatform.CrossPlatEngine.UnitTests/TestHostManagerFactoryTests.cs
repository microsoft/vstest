// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestHostManagerFactoryTests
    {
        private TestHostManagerFactory testHostManagerFactory;
        private IRequestData requestData;

        public TestHostManagerFactoryTests()
        {
            this.requestData = new RequestData(new NoOpMetricsCollection());
            this.testHostManagerFactory = new TestHostManagerFactory(this.requestData);
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
            Assert.IsNotNull(this.testHostManagerFactory.GetExecutionManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheExecutionManagerInstance()
        {
            Assert.AreEqual(this.testHostManagerFactory.GetExecutionManager(), this.testHostManagerFactory.GetExecutionManager());
        }
    }
}
