using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.BlameDataCollector.UnitTests
{
    [TestClass]
    class BlameDataCollectorTests
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
            var filepath = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");
            //Raising Session End Event
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<String>(), It.IsAny<bool>()));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            this.mockBlameFileManager.Verify(x => x.SaveToFile(filepath), Times.Once);
        }
    }
}
