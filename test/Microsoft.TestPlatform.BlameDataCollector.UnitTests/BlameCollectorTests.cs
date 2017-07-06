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
        private Mock<IBlameReaderWriter> mockBlameReaderWriter;
        private XmlElement configurationElement;

        public BlameCollectorTests()
        {
            // Initializing mocks
            this.mockLogger = new Mock<DataCollectionLogger>();
            this.mockDataColectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockBlameReaderWriter = new Mock<IBlameReaderWriter>();
            this.blameDataCollector = new BlameCollector(this.mockBlameReaderWriter.Object);

            // Initializing members
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
        public void TriggerTestCaseStartedHandlerShouldIncreaseTestStartCount()
        {
            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                this.mockLogger.Object, this.context);

            // Setup and Raise Session End Event
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));

            // Assert
            Assert.AreEqual(1, this.blameDataCollector.TestStartCount);
        }

        [TestMethod]
        public void TriggerTestCaseEndedHandlerShouldIncreaseTestEndCount()
        {
            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                this.mockLogger.Object, this.context);

            // Setup and Raise Session End Event
            this.mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));

            // Assert
            Assert.AreEqual(1, this.blameDataCollector.TestEndCount);
        }

        [TestMethod]
        public void TriggerSessionEndedHandlerShouldWriteToFileIfTestStartCountIsGreater()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                    this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                    this.mockLogger.Object, this.context);
            var filepath = Path.Combine(AppContext.BaseDirectory, "Sequence");
            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<String>(), It.IsAny<bool>()));
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), filepath), Times.Once);
        }

        [TestMethod]
        public void TriggerSessionEndedHandlerShouldNotWriteToTestStartCountIsSameAsTestEndCountFile()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(this.configurationElement,
                this.mockDataColectionEvents.Object, this.mockDataCollectionSink.Object,
                this.mockLogger.Object, this.context);
            var filepath = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");
            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<String>(), It.IsAny<bool>()));
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), filepath), Times.Never);
        }
    }
}
