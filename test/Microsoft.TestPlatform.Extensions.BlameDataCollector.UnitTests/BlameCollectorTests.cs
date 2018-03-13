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
        private Mock<IProcessDumpUtility> mockProcessDumpUtility;
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
            this.mockProcessDumpUtility = new Mock<IProcessDumpUtility>();
            this.blameDataCollector = new TestableBlameCollector(this.mockBlameReaderWriter.Object, this.mockProcessDumpUtility.Object);

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

        /// <summary>
        /// The trigger session ended handler should get dump files if proc dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldGetDumpFileIfProcDumpEnabled()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Setup
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Returns(this.filepath);

            // Raise
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should log exception if GetDumpfile throws FileNotFound Exception
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldLogErrorIfGetDumpFileThrowsFileNotFound()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Setup and raise events
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Throws(new FileNotFoundException());
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockLogger.Verify(x => x.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility if proc dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityIfProcDumpEnabled()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            this.mockProcessDumpUtility.Verify(x => x.StartProcessDump(1234, It.IsAny<string>(), It.IsAny<string>()));
        }

        /// <summary>
        /// The trigger test host launcehd handler should not break if start process dump throws
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldCatchAllExceptions()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Make StartProcessDump throw exception
            var ex = new Exception("start process failed");
            var tpex = new TestPlatformException("env var exception");
            this.mockProcessDumpUtility.Setup(x => x.StartProcessDump(1, It.IsAny<string>(), It.IsAny<string>()))
                                       .Throws(ex);
            this.mockProcessDumpUtility.Setup(x => x.StartProcessDump(2, It.IsAny<string>(), It.IsAny<string>()))
                                       .Throws(tpex);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1));
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 2));

            // Verify
            this.mockLogger.Verify(x => x.LogError(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == ex.ToString())), Times.Once);
            this.mockLogger.Verify(x => x.LogError(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == tpex.Message)), Times.Once);
        }

        [TestCleanup]
        public void CleanUp()
        {
            File.Delete(this.filepath);
        }

        private XmlElement GetDumpConfigurationElement()
        {
            var xmldoc = new XmlDocument();
            var outernode = xmldoc.CreateElement("Configuration");
            var node = xmldoc.CreateElement(BlameDataCollector.Constants.DumpModeKey);
            outernode.AppendChild(node);
            node.InnerText = "Text";

            return outernode;
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
            /// <param name="processDumpUtility">
            /// ProcessDumpUtility instance.
            /// </param>
            internal TestableBlameCollector(IBlameReaderWriter blameReaderWriter, IProcessDumpUtility processDumpUtility)
                : base(blameReaderWriter, processDumpUtility)
            {
            }
        }
    }
}
