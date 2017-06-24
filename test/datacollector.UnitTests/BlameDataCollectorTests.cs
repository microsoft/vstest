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
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;

    //using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;

    [TestClass]
    public class BlameDataCollectorTests
    {

        private DataCollectionEnvironmentContext context;
        private DataCollectionContext dataCollectionContext;
        private BlameDataCollector blameDataCollector;
        private Mock<DataCollectionLogger> mockLogger;
        private Mock<DataCollectionEvents> mockDataColectionEvents;
        private Mock<DataCollectionSink> mockDataCollectionSink;
        private Mock<IBlameFileManager> mockBlameFileManager;
        private XmlElement configurationElement;
        private TestCase testcase;

        public BlameDataCollectorTests()
        {
            this.mockLogger = new Mock<DataCollectionLogger>();
            this.mockDataColectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockBlameFileManager = new Mock<IBlameFileManager>();
            this.blameDataCollector = new BlameDataCollector(this.mockBlameFileManager.Object);

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
        public void TriggerSessionEndedHandlerShouldSaveToFile()
        {
            //Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    this.mockLogger.Object, context);
            var filename = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");


            //Raising Session End Event
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            this.mockBlameFileManager.Verify(x => x.SaveToFile(filename), Times.Once);
        }

    }
}
