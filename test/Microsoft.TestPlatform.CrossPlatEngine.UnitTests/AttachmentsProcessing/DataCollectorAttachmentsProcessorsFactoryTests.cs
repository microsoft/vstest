// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollectorAttachmentsProcessorsFactoryTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    [TestClass]
    public class DataCollectorAttachmentsProcessorsFactoryTests
    {
        private readonly DataCollectorAttachmentsProcessorsFactory dataCollectorAttachmentsProcessorsFactory = new DataCollectorAttachmentsProcessorsFactory();

        [TestInitialize]
        public void Init()
        {
            TestPluginCacheHelper.SetupMockExtensions(typeof(DataCollectorAttachmentsProcessorsFactoryTests));
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestPluginCacheHelper.ResetExtensionsCache();
        }

        [TestMethod]
        public void Create_ShouldReturnListOfAttachmentProcessors()
        {
            // arrange
            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>();
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://Sample"), typeof(SampleDataCollector).AssemblyQualifiedName, typeof(SampleDataCollector).Assembly.Location, true));
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://SampleData2"), typeof(SampleData2Collector).AssemblyQualifiedName, typeof(SampleData2Collector).Assembly.Location, true));
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://SampleData3"), typeof(SampleData3Collector).AssemblyQualifiedName, typeof(SampleData3Collector).Assembly.Location, true));
            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray());

            // assert
            Assert.AreEqual(3, dataCollectorAttachmentsProcessors.Count);

            Assert.IsTrue(dataCollectorAttachmentsProcessors.ContainsKey("Sample"));
            Assert.IsTrue(dataCollectorAttachmentsProcessors.ContainsKey("SampleData3"));
            Assert.IsTrue(dataCollectorAttachmentsProcessors.ContainsKey("Code Coverage"));

            Assert.AreEqual(typeof(DataCollectorAttachmentProcessor).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["Sample"].GetType().AssemblyQualifiedName);
            Assert.AreEqual(typeof(DataCollectorAttachmentProcessor2).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["SampleData3"].GetType().AssemblyQualifiedName);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["Code Coverage"].GetType().AssemblyQualifiedName);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Create_EmptyOrNullInvokedDataCollector_ShouldReturnCodeCoverageDataAttachmentsHandler(bool empty)
        {
            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(empty ? new InvokedDataCollector[0] : null);

            //assert
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["Code Coverage"].GetType().AssemblyQualifiedName);
        }

        [TestMethod]
        public void Create_ShouldNotFailIfWrongDataCollectorAttachmentProcessor()
        {
            // arrange
            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>();
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://SampleData4"), typeof(SampleData4Collector).AssemblyQualifiedName, typeof(SampleData4Collector).Assembly.Location, true));

            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray());

            // assert
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["Code Coverage"].GetType().AssemblyQualifiedName);
        }

        [TestMethod]
        public void Create_ShouldNotAddTwoTimeCodeCoverageDataAttachmentsHandler()
        {
            // arrange
            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>();
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://microsoft/CodeCoverage/2.0"), typeof(SampleData5Collector).AssemblyQualifiedName, typeof(SampleData5Collector).Assembly.Location, true));

            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray());

            // assert
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors["SampleData5"].GetType().AssemblyQualifiedName);
        }
    }

    [DataCollectorFriendlyName("Sample")]
    [DataCollectorTypeUri("datacollector://Sample")]
    [DataCollectorAttachmentProcessor(typeof(DataCollectorAttachmentProcessor))]
    public class SampleDataCollector : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

        }
    }

    [DataCollectorFriendlyName("SampleData2")]
    [DataCollectorTypeUri("datacollector://SampleData2")]
    [DataCollectorAttachmentProcessor(typeof(DataCollectorAttachmentProcessor))]
    public class SampleData2Collector : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

        }
    }

    [DataCollectorFriendlyName("SampleData3")]
    [DataCollectorTypeUri("datacollector://SampleData3")]
    [DataCollectorAttachmentProcessor(typeof(DataCollectorAttachmentProcessor2))]
    public class SampleData3Collector : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

        }
    }

    [DataCollectorFriendlyName("SampleData4")]
    [DataCollectorTypeUri("datacollector://SampleData4")]
    [DataCollectorAttachmentProcessor(typeof(string))]
    public class SampleData4Collector : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

        }
    }

    [DataCollectorFriendlyName("SampleData5")]
    [DataCollectorTypeUri("datacollector://microsoft/CodeCoverage/2.0")]
    [DataCollectorAttachmentProcessor(typeof(CodeCoverageDataAttachmentsHandler))]
    public class SampleData5Collector : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

        }
    }

    public class DataCollectorAttachmentProcessor : IConfigurableDataCollectorAttachmentProcessor
    {
        public bool SupportsIncrementalProcessing => throw new NotImplementedException();

        public IEnumerable<Uri> GetExtensionUris()
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class DataCollectorAttachmentProcessor2 : IConfigurableDataCollectorAttachmentProcessor
    {
        public bool SupportsIncrementalProcessing => throw new NotImplementedException();

        public IEnumerable<Uri> GetExtensionUris()
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
