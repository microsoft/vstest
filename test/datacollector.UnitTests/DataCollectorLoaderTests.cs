// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectorLoaderTests
    {
        private readonly DataCollectorLoader dataCollectionLoader;

        private readonly string dummyDataCollectorLocation;

        private readonly string dummyDataCollectorTypeName;

        public DataCollectorLoaderTests()
        {
            this.dataCollectionLoader = new DataCollectorLoader();
            this.dummyDataCollectorLocation = typeof(DummyDataCollector1).GetTypeInfo().Assembly.Location;
            this.dummyDataCollectorTypeName = typeof(DummyDataCollector1).AssemblyQualifiedName;
        }

        [TestMethod]
        public void LoadShouldLoadTheDataCollectorIfTypeIsAvailable()
        {
            var collector = this.dataCollectionLoader.Load(
                this.dummyDataCollectorLocation,
                this.dummyDataCollectorTypeName);

            Assert.IsNotNull(collector);
            Assert.IsInstanceOfType(collector, typeof(DataCollector));
        }

        [TestMethod]
        public void LoadShouldReturnNullDataCollectorIfTypeIsNotAvailable()
        {
            var collector = this.dataCollectionLoader.Load(
                this.dummyDataCollectorLocation,
                this.dummyDataCollectorTypeName.Replace("DataCollector1", "DataCollectorNotExist"));

            Assert.IsNull(collector);
        }

        [TestMethod]
        public void LoadShouldReturnNullDataCollectorIfTypeIsNotInstanceOfDataCollector()
        {
            var collector = this.dataCollectionLoader.Load(
                this.dummyDataCollectorLocation.Replace("Tests", "UnitTests"),
                this.dummyDataCollectorTypeName.Replace("DataCollector1", "DataCollector2"));

            Assert.IsNull(collector);
        }

        public void LoadShouldReturnNullDataCollectorIfAssemblyIsNotPresent()
        {
            var collector = this.dataCollectionLoader.Load(
                this.dummyDataCollectorLocation,
                this.dummyDataCollectorTypeName.Replace("DataCollector1", "DataCollector2"));

            Assert.IsNull(collector);
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

        [DataCollectorFriendlyName("mymockeddatacollector")]
        [DataCollectorTypeUri("my://mocked/datacollector")]
        public class DummyDataCollector2
        {
        }
    }
}