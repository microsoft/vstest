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
        private readonly TestHostManagerFactory testHostManagerFactory;
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;

        public TestHostManagerFactoryTests()
        {
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData = new Mock<IRequestData>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            testHostManagerFactory = new TestHostManagerFactory(mockRequestData.Object);
        }

        [TestMethod]
        public void ConstructorShouldThrowIfRequestDataIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new TestHostManagerFactory(null));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnADiscoveryManagerInstance()
        {
            Assert.IsNotNull(testHostManagerFactory.GetDiscoveryManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheDiscoveryManagerInstance()
        {
            Assert.AreEqual(testHostManagerFactory.GetDiscoveryManager(), testHostManagerFactory.GetDiscoveryManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnAnExecutionManagerInstance()
        {
            Assert.IsNotNull(testHostManagerFactory.GetExecutionManager());
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldCacheTheExecutionManagerInstance()
        {
            Assert.AreEqual(testHostManagerFactory.GetExecutionManager(), testHostManagerFactory.GetExecutionManager());
        }
    }
}
