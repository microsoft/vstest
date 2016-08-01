// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestHostManagerFactoryTests
    {
        private TestHostManagerFactory testHostManagerFactory;

        [TestInitialize]
        public void TestInit()
        {
            this.testHostManagerFactory = new TestHostManagerFactory();
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
