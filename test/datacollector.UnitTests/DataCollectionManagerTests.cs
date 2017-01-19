// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectorManager;

        [TestInitialize]
        public void Init()
        {
            this.dataCollectorManager = new DataCollectionManager(new List<string>() { typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldThrowExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectorManager.InitializeDataCollectors(null);
            });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnEmptyDictionaryIfDataCollectorsAreNotConfigured()
        {
            const string RunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollector()
        {
            const string RunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors><DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" /></DataCollectors>  </DataCollectionRunSettings>\r\n</RunSettings>";

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);

            // todo : update this when call to initialize is implemented
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfUriIsNotCorrect()
        {
            const string RunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors><DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom1/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" /></DataCollectors>  </DataCollectionRunSettings>\r\n</RunSettings>";

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, this.dataCollectorManager.RunDataCollectors.Count);

            // todo : update this when call to initialize is implemented
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        public void InitializeDataCollectorShouldNotAddSameDataCollectorMoreThanOnce()
        {
            const string RunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors><DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" /><DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" /></DataCollectors>  </DataCollectionRunSettings>\r\n</RunSettings>";

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);

            // todo : update this when call to initialize is implemented
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldNotAddDataCollectorIfUriIsNotSpecifiedByDataCollector()
        {
            const string RunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors><DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollectorWithoutUri, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" /></DataCollectors>  </DataCollectionRunSettings>\r\n</RunSettings>";

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, this.dataCollectorManager.RunDataCollectors.Count);

            // todo : update this when call to initialize is implemented
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        // todo : add tests for verifying logger after implementing logger functionality.
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollector : DataCollector
    {
        public static bool IsInitialized = false;

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            IsInitialized = true;
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    public class CustomDataCollectorWithoutUri : DataCollector
    {
        public static bool IsInitialized = false;

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            IsInitialized = true;
        }
    }
}
