// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectorLoaderTests
    {
        private readonly DataCollectorLoader dataCollectionLoader;


        private readonly string dummyDataCollectorTypeName;

        public DataCollectorLoaderTests()
        {
            this.dataCollectionLoader = new DataCollectorLoader();
        }

        [TestMethod]
        public void CreateInstanceShouldCreateInstanceIfValidTypeIsPassed()
        {
            var collector = this.dataCollectionLoader.CreateInstance(typeof(DummyDataCollector1));

            Assert.IsNotNull(collector);
            Assert.IsInstanceOfType(collector, typeof(DataCollector));
        }

        [TestMethod]
        public void CreateInstanceShouldReturnNullIfTypeIsNull()
        {
            var collector = this.dataCollectionLoader.CreateInstance(null);

            Assert.IsNull(collector);
        }

        [TestMethod]
        public void CreateInstanceShouldReturnNullIfTypeIsNotInstanceOfDataCollector()
        {
            var collector = this.dataCollectionLoader.CreateInstance(typeof(DummyDataCollector2));

            Assert.IsNull(collector);
        }

        [TestMethod]
        public void FindDataCollectorsShouldReturnDataCollectors()
        {
            var result = this.dataCollectionLoader.FindDataCollectors(typeof(DataCollectorLoaderTests).GetTypeInfo().Assembly.Location);

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void FindDataCollectorsShouldReturnEmptyListIfPathIsNull()
        {
            var result = this.dataCollectionLoader.FindDataCollectors(null);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetTypeUriShouldReturnUri()
        {
            var uri = this.dataCollectionLoader.GetTypeUri(typeof(DummyDataCollector1));

            Assert.AreEqual("my://mocked/datacollector", uri.ToString());
        }

        [TestMethod]
        public void GetTypeUriShouldReturnThrowIfTypeIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var uri = this.dataCollectionLoader.GetTypeUri(null);
                });
        }

        [TestMethod]
        public void GetTypeUriShouldThrowIfUriIsNotAssociatedWithDataCollector()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                this.dataCollectionLoader.GetTypeUri(typeof(DummyDataCollector3));
            });
        }

        [TestMethod]
        public void GetFriendlyNameUriShouldReturnUri()
        {
            var friendlyName = this.dataCollectionLoader.GetFriendlyName(typeof(DummyDataCollector1));

            Assert.AreEqual("mymockeddatacollector", friendlyName.ToString());
        }

        [TestMethod]
        public void GetFriendlyNameShouldReturnThrowIfTypeIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var uri = this.dataCollectionLoader.GetFriendlyName(null);
                });
        }

        [TestMethod]
        public void GetFriendlyNameShouldThrowIfUriIsNotAssociatedWithDataCollector()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                this.dataCollectionLoader.GetFriendlyName(typeof(DummyDataCollector4));
            });
        }
    }

    [DataCollectorFriendlyName("mymockeddatacollector")]
    [DataCollectorTypeUri("my://mocked/datacollector")]
    public class DummyDataCollector1 : DataCollector
    {
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            throw new NotImplementedException();
        }
    }

    [DataCollectorFriendlyName("mymockeddatacollector2")]
    [DataCollectorTypeUri("my://mocked/datacollector2")]
    public class DummyDataCollector2
    {
    }

    [DataCollectorFriendlyName("mymockeddatacollector3")]
    public class DummyDataCollector3 : DataCollector
    {
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            throw new NotImplementedException();
        }
    }

    [DataCollectorTypeUri("my://mocked/datacollector4")]
    public class DummyDataCollector4 : DataCollector
    {
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            throw new NotImplementedException();
        }
    }
}