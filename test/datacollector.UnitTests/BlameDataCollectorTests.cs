// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.IO;
    using System.Xml;

    [TestClass]
    public class BlameDataCollectorTests
    {

        private DataCollectionEnvironmentContext context;
        private DataCollectionContext dataCollectionContext;
        private BlameDataCollector blameDataCollector;
        private Mock<DataCollectionLogger> mockLogger;
        private Mock<DataCollectionEvents> mockDataColectionEvents;
        private Mock<DataCollectionSink> mockDataCollectionSink;

        private Mock<IFileHelper> mockFileHelper;
        private XmlElement configurationElement;
        private TestCase testcase;

        public BlameDataCollectorTests()
        {
            this.mockLogger = new Mock<DataCollectionLogger>();
            this.mockDataColectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.blameDataCollector = new BlameDataCollector(mockFileHelper.Object);

            this.testcase = new TestCase();
            this.testcase.Id = Guid.NewGuid();
            this.dataCollectionContext = new DataCollectionContext(this.testcase);
            this.configurationElement = null;
            this.context = new DataCollectionEnvironmentContext();
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfDataCollectionLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    (DataCollectionLogger)null, this.context);
            });
        }

        [TestMethod]
        public void ValidateXmlWriterShouldReturnCorrectXmlDocument()
        {
            string fullyQualifiedName = "TestProject.UnitTest1.AcceptedTest1";
            string source = "C:\\users\\Test.dll";
            string xml = "<?xml version=\"1.0\"?><TestSequence><Test Name=\"TestProject.UnitTest1.AcceptedTest1\" Source=\"C:\\users\\Test.dll\" /></TestSequence>";

            this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    this.mockLogger.Object, context);

            var xmldoc = blameDataCollector.ValidateXmlWriter(fullyQualifiedName, source);
            Assert.AreEqual(xml, xmldoc.InnerXml);
        }

    }
}
