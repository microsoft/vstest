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
    using System.Linq;
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
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray(), null);

            // assert
            Assert.AreEqual(3, dataCollectorAttachmentsProcessors.Length);

            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count(x => x.FriendlyName == "Sample") == 1);
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count(x => x.FriendlyName == "SampleData3") == 1);
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Count(x => x.FriendlyName == "Code Coverage") == 1);

            Assert.AreEqual(typeof(DataCollectorAttachmentProcessor).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[0].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
            Assert.AreEqual(typeof(DataCollectorAttachmentProcessor2).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[1].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[2].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Create_EmptyOrNullInvokedDataCollector_ShouldReturnCodeCoverageDataAttachmentsHandler(bool empty)
        {
            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(empty ? new InvokedDataCollector[0] : null, null);

            //assert
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Length);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[0].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
        }

        [TestMethod]
        public void Create_ShouldNotFailIfWrongDataCollectorAttachmentProcessor()
        {
            // arrange
            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>();
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://SampleData4"), typeof(SampleData4Collector).AssemblyQualifiedName, typeof(SampleData4Collector).Assembly.Location, true));

            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray(), null);

            // assert
            Assert.AreEqual(1, dataCollectorAttachmentsProcessors.Length);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[0].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
        }

        [TestMethod]
        public void Create_ShouldAddTwoTimeCodeCoverageDataAttachmentsHandler()
        {
            // arrange
            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>();
            invokedDataCollectors.Add(new InvokedDataCollector(new Uri("datacollector://microsoft/CodeCoverage/2.0"), typeof(SampleData5Collector).AssemblyQualifiedName, typeof(SampleData5Collector).Assembly.Location, true));

            // act
            var dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollectors.ToArray(), null);

            // assert
            Assert.AreEqual(2, dataCollectorAttachmentsProcessors.Length);
            Assert.AreEqual(typeof(DataCollectorAttachmentProcessorCodeCoverage).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[0].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
            Assert.AreEqual(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, dataCollectorAttachmentsProcessors[1].DataCollectorAttachmentProcessorInstance.GetType().AssemblyQualifiedName);
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

    [DataCollectorFriendlyName("Code Coverage")]
    [DataCollectorTypeUri("datacollector://microsoft/CodeCoverage/2.0")]
    [DataCollectorAttachmentProcessor(typeof(DataCollectorAttachmentProcessorCodeCoverage))]
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

    public class DataCollectorAttachmentProcessorCodeCoverage : IDataCollectorAttachmentProcessor
    {
        public bool SupportsIncrementalProcessing => true;

        public IEnumerable<Uri> GetExtensionUris()
        {
            yield return new Uri("datacollector://microsoft/CodeCoverage/2.0");
        }

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            return Task.FromResult(attachments);
        }
    }

    public class DataCollectorAttachmentProcessor : IDataCollectorAttachmentProcessor
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

    public class DataCollectorAttachmentProcessor2 : IDataCollectorAttachmentProcessor
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
