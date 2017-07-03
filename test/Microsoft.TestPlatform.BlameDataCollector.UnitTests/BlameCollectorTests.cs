// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.BlameDataCollector.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Moq;
    using Microsoft.TestPlatform.BlameDataCollector;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.IO;
    using System.Collections.Generic;

    [TestClass]
    public class BlameCollectorTests
    {
        private DataCollectionEnvironmentContext context;
        private DataCollectionContext dataCollectionContext;
        private BlameCollector blameDataCollector;
        private Mock<DataCollectionLogger> mockLogger;
        private Mock<DataCollectionEvents> mockDataColectionEvents;
        private Mock<DataCollectionSink> mockDataCollectionSink;
        private Mock<IBlameFileManager> mockBlameFileManager;
        private XmlElement configurationElement;

        public BlameCollectorTests()
        {

            // Initaializing mocks
            this.mockLogger = new Mock<DataCollectionLogger>();
            this.mockDataColectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockBlameFileManager = new Mock<IBlameFileManager>();
            this.blameDataCollector = new BlameCollector(this.mockBlameFileManager.Object);


            TestCase testcase = new TestCase();
            testcase.Id = Guid.NewGuid();
            this.dataCollectionContext = new DataCollectionContext(testcase);
            this.configurationElement = null;
            this.context = new DataCollectionEnvironmentContext(this.dataCollectionContext);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfDataCollectionLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    (DataCollectionLogger)null, null);
            });
        }

        [TestMethod]
        public void TriggerSessionEndedHandlerShouldSaveToFile()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    this.mockLogger.Object, this.context);
            var filepath = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");

            // Raising Session End Event
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<String>(), It.IsAny<bool>()));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify Add Tests to Format
            this.mockBlameFileManager.Verify(x => x.AddTestsToFormat(It.IsAny<List<object>>(),filepath), Times.Once);
        }
    }
}
