// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestHostManagerFactoryTests
    {
        private TestHostManagerFactory testHostManagerFactory;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;

        public TestHostManagerFactoryTests()
        {
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.testHostManagerFactory = new TestHostManagerFactory(this.mockRequestData.Object);
        }

        [TestMethod]
        public void ConstructorShouldThrowIfRequestDataIsNull()
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
            Assert.IsNotNull(this.testHostManagerFactory.GetExecutionManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheExecutionManagerInstance()
        {
            Assert.AreEqual(this.testHostManagerFactory.GetExecutionManager(), this.testHostManagerFactory.GetExecutionManager());
        }
    }
}
