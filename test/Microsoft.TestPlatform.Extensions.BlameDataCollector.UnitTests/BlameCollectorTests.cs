// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.BlameDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// The blame collector tests.
    /// </summary>
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
        private string filepath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollectorTests"/> class.
        /// </summary>
        public BlameCollectorTests()
        {
            // Initializing mocks
            this.mockLogger = new Mock<DataCollectionLogger>();
            this.mockDataColectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockBlameReaderWriter = new Mock<IBlameReaderWriter>();
            this.blameDataCollector = new TestableBlameCollector(this.mockBlameReaderWriter.Object);

            // Initializing members
            TestCase testcase = new TestCase { Id = Guid.NewGuid() };
            this.dataCollectionContext = new DataCollectionContext(testcase);
            this.configurationElement = null;
            this.context = new DataCollectionEnvironmentContext(this.dataCollectionContext);

            this.filepath = Path.Combine(Path.GetTempPath(), "Test");
            FileStream stream = File.Create(this.filepath);
            stream.Dispose();
        }

        /// <summary>
        /// The initialize should throw exception if data collection logger is null.
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfDataCollectionLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    this.blameDataCollector.Initialize(
                        this.configurationElement,
                        this.mockDataColectionEvents.Object,
                        this.mockDataCollectionSink.Object,
                        null,
                        null);
                });
        }

        /// <summary>
        /// The trigger session ended handler should write to file if test start count is greater.
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldWriteToFileIfTestHostCrash()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.configurationElement,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), It.IsAny<string>()))
                .Returns(this.filepath);

            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should not write to file if test start count is same as test end count.
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldNotWriteToFileIfNoTestHostCrash()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.configurationElement,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), It.IsAny<string>())).Returns(this.filepath);
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<TestCase>>(), this.filepath), Times.Never);
        }

        [TestCleanup]
        public void CleanUp()
        {
            File.Delete(this.filepath);
        }

        /// <summary>
        /// The testable blame collector.
        /// </summary>
        internal class TestableBlameCollector : BlameCollector
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableBlameCollector"/> class.
            /// </summary>
            /// <param name="blameReaderWriter">
            /// The blame reader writer.
            /// </param>
            internal TestableBlameCollector(IBlameReaderWriter blameReaderWriter)
                : base(blameReaderWriter)
            {
            }
        }
    }
}
