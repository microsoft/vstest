// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
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
        private Mock<IInactivityTimer> mockInactivityTimer;
        private Mock<IFileHelper> mockFileHelper;
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
            this.mockInactivityTimer = new Mock<IInactivityTimer>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.blameDataCollector = new TestableBlameCollector(
                this.mockBlameReaderWriter.Object,
                this.mockProcessDumpUtility.Object,
                this.mockInactivityTimer.Object,
                this.mockFileHelper.Object);

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
        /// Initializing with collect dump for hang disabled should ensure InativityTimer is never initialized or reset
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangDisabledShouldNotInitializeInactivityTimerOrCallReset()
        {
            int resetCalledCount = 0;

            this.mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMinutes == 1.0))).Callback(() => { resetCalledCount++; });

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, false),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            Assert.AreEqual(0, resetCalledCount, "Should not have called InactivityTimer.Reset since no collect dump on hang is disabled.");
        }

        /// <summary>
        /// Initializing with collect dump for hang should configure the timer with the right values and should
        /// not call the reset method if no events are received.
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldInitializeInactivityTimerAndCallResetOnce()
        {
            int resetCalledCount = 0;

            this.mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMilliseconds == 1.0))).Callback(() => { resetCalledCount++; });

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, true, 1),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            Assert.AreEqual(1, resetCalledCount, "Should have called InactivityTimer.Reset exactly once since no events were received");
        }

        /// <summary>
        /// Initializing with collect dump for hang should configure the timer with the right values and should
        /// reset for each event received
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldInitializeInactivityTimerAndResetForEachEventReceived()
        {
            int resetCalledCount = 0;

            this.mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMilliseconds == 1.0))).Callback(() => { resetCalledCount++; });

            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, true, 1),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            TestCase testcase = new TestCase("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            Assert.AreEqual(3, resetCalledCount, "Should have called InactivityTimer.Reset exactly 3 times");
        }

        /// <summary>
        /// Initializing with collect dump for hang should capture a dump on timeout
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureADumpOnTimeout()
        {
            this.blameDataCollector = new TestableBlameCollector(
                this.mockBlameReaderWriter.Object,
                this.mockProcessDumpUtility.Object,
                null,
                this.mockFileHelper.Object);

            var dumpFile = "abc_hang.dmp";
            var hangBasedDumpcollected = new ManualResetEventSlim();

            this.mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            this.mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            this.mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()));
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Returns(dumpFile);
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<FileTransferInformation>())).Callback(() => hangBasedDumpcollected.Set());

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, true, 0),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            hangBasedDumpcollected.Wait(1000);
            this.mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Once);
            this.mockDataCollectionSink.Verify(x => x.SendFileAsync(It.Is<FileTransferInformation>(y => y.Path == dumpFile)), Times.Once);
        }

        /// <summary>
        /// Initializing with collect dump for hang should kill test host process even if an error
        /// occurs during capturing the dump. Basically it should not throw.
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureKillTestHostOnTimeoutEvenIfGetDumpFileFails()
        {
            this.blameDataCollector = new TestableBlameCollector(
                this.mockBlameReaderWriter.Object,
                this.mockProcessDumpUtility.Object,
                null,
                this.mockFileHelper.Object);

            var hangBasedDumpcollected = new ManualResetEventSlim();

            this.mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            this.mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            this.mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()));
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Callback(() => hangBasedDumpcollected.Set()).Throws(new Exception("Some exception"));

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, true, 0),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            hangBasedDumpcollected.Wait(1000);
            this.mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Once);
        }

        /// <summary>
        /// Initializing with collect dump for hang should kill test host process even if an error
        /// occurs during attaching it as a datacollector attachment. Basically it should not throw.
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureKillTestHostOnTimeoutEvenIfAttachingDumpFails()
        {
            this.blameDataCollector = new TestableBlameCollector(
                this.mockBlameReaderWriter.Object,
                this.mockProcessDumpUtility.Object,
                null,
                this.mockFileHelper.Object);

            var dumpFile = "abc_hang.dmp";
            var hangBasedDumpcollected = new ManualResetEventSlim();

            this.mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            this.mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            this.mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()));
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Returns(dumpFile);
            this.mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<FileTransferInformation>())).Callback(() => hangBasedDumpcollected.Set()).Throws(new Exception("Some other exception"));

            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(false, false, true, 0),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            hangBasedDumpcollected.Wait(1000);
            this.mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Once);
            this.mockDataCollectionSink.Verify(x => x.SendFileAsync(It.Is<FileTransferInformation>(y => y.Path == dumpFile)), Times.Once);
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
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);

            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()), Times.Once);
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
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>())).Returns(this.filepath);
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            this.mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify WriteTestSequence Call
            this.mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), this.filepath), Times.Never);
        }

        /// <summary>
        /// The event handlers should generate correct test sequence and testObjectDictionary for both completed and not completed tests
        /// </summary>
        [TestMethod]
        public void EventHandlersShouldGenerateCorrectTestSequenceAndTestObjectDictionaryForBothCompletedAndNotCompletedTests()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.configurationElement,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            TestCase testcase1 = new TestCase("TestProject.UnitTest.TestMethod1", new Uri("test:/abc"), "abc.dll");
            TestCase testcase2 = new TestCase("TestProject.UnitTest.TestMethod2", new Uri("test:/abc"), "abc.dll");
            testcase1.DisplayName = "TestMethod1";
            testcase2.DisplayName = "TestMethod2";
            var blameTestObject1 = new BlameTestObject(testcase1);
            var blameTestObject2 = new BlameTestObject(testcase2);

            // Setup and Raise TestCaseStart and Session End Event
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>())).Returns(this.filepath);
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase1));
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase2));
            this.mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase1, TestOutcome.Passed));
            blameTestObject1.IsCompleted = true;
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify call to mockBlameReaderWriter
            this.mockBlameReaderWriter.Verify(
                x => x.WriteTestSequence(
                It.Is<List<Guid>>(y => y.Count == 2 && y.First() == blameTestObject1.Id && y.Last() == blameTestObject2.Id),
                It.Is<Dictionary<Guid, BlameTestObject>>(
                    y => y.Count == 2 &&
                    y[blameTestObject1.Id].IsCompleted == true && y[blameTestObject2.Id].IsCompleted == false &&
                    y[blameTestObject1.Id].FullyQualifiedName == "TestProject.UnitTest.TestMethod1" && y[blameTestObject2.Id].FullyQualifiedName == "TestProject.UnitTest.TestMethod2" &&
                    y[blameTestObject1.Id].Source == "abc.dll" && y[blameTestObject2.Id].Source == "abc.dll" &&
                    y[blameTestObject1.Id].DisplayName == "TestMethod1" && y[blameTestObject2.Id].DisplayName == "TestMethod2"),
                It.IsAny<string>()),
                Times.Once);
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
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);

            // Raise
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new TestCase()));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should ensure proc dump process is terminated when no crash is detected
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldEnsureProcDumpProcessIsTerminated()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Mock proc dump utility terminate process call
            this.mockProcessDumpUtility.Setup(x => x.DetachFromTargetProcess(It.IsAny<int>()));

            // Raise
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockProcessDumpUtility.Verify(x => x.DetachFromTargetProcess(It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should not dump files if proc dump was enabled and test host did not crash
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldNotGetDumpFileIfNoCrash()
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
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);

            // Raise
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockProcessDumpUtility.Verify(x => x.GetDumpFile(), Times.Never);
        }

        /// <summary>
        /// The trigger session ended handler should get dump files if collect dump on exit was enabled irrespective of completed test case count
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldGetDumpFileIfCollectDumpOnExitIsEnabled()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(collectDumpOnExit: true),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Setup
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Returns(this.filepath);
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);

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
        public void TriggerSessionEndedHandlerShouldLogWarningIfGetDumpFileThrowsFileNotFound()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Setup and raise events
            this.mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(this.filepath);
            this.mockProcessDumpUtility.Setup(x => x.GetDumpFile()).Throws(new FileNotFoundException());
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));
            this.mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new TestCase()));
            this.mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionContext));

            // Verify GetDumpFiles Call
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.IsAny<string>()), Times.Once);
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
            this.mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<string>()));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityForFullDumpIfFullDumpEnabled()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(isFullDump: true),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            this.mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), It.IsAny<string>(), true,  It.IsAny<string>()));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityForFullDumpIfFullDumpEnabledCaseSensitivity()
        {
            var dumpConfig = this.GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DuMpType");
            dumpTypeAttribute.Value = "FuLl";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);
            var dumpOnExitAttribute = dumpConfig.OwnerDocument.CreateAttribute("CollEctAlways");
            dumpOnExitAttribute.Value = "FaLSe";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpOnExitAttribute);

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                dumpConfig,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            this.mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<string>()));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForWrongCollectDumpKey()
        {
            var dumpConfig = this.GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("Xyz");
            dumpTypeAttribute.Value = "FuLl";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                dumpConfig,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, "Xyz"))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForWrongDumpType()
        {
            var dumpConfig = this.GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DumpType");
            dumpTypeAttribute.Value = "random";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                dumpConfig,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, "DumpType", BlameDataCollector.Constants.FullConfigurationValue, BlameDataCollector.Constants.MiniConfigurationValue))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForNonBooleanCollectAlwaysValue()
        {
            var dumpConfig = this.GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DumpType");
            dumpTypeAttribute.Value = "random";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                dumpConfig,
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, "DumpType", BlameDataCollector.Constants.FullConfigurationValue, BlameDataCollector.Constants.MiniConfigurationValue))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should not break if start process dump throws TestPlatFormExceptions and log error message
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldCatchTestPlatFormExceptionsAndReportMessage()
        {
            // Initializing Blame Data Collector
            this.blameDataCollector.Initialize(
                this.GetDumpConfigurationElement(),
                this.mockDataColectionEvents.Object,
                this.mockDataCollectionSink.Object,
                this.mockLogger.Object,
                this.context);

            // Make StartProcessDump throw exception
            var tpex = new TestPlatformException("env var exception");
            this.mockProcessDumpUtility.Setup(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<string>()))
                                       .Throws(tpex);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, tpex.Message))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should not break if start process dump throws unknown exceptions and report message with stack trace
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldCatchAllUnexpectedExceptionsAndReportMessageWithStackTrace()
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
            this.mockProcessDumpUtility.Setup(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<string>()))
                                       .Throws(ex);

            // Raise TestHostLaunched
            this.mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(this.dataCollectionContext, 1234));

            // Verify
            this.mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, ex.ToString()))), Times.Once);
        }

        [TestCleanup]
        public void CleanUp()
        {
            File.Delete(this.filepath);
        }

        private XmlElement GetDumpConfigurationElement(
            bool isFullDump = false,
            bool collectDumpOnExit = false,
            bool colectDumpOnHang = false,
            int inactivityTimeInMilliseconds = 0)
        {
            var xmldoc = new XmlDocument();
            var outernode = xmldoc.CreateElement("Configuration");
            var node = xmldoc.CreateElement(BlameDataCollector.Constants.DumpModeKey);
            outernode.AppendChild(node);
            node.InnerText = "Text";

            if (isFullDump)
            {
                var fulldumpAttribute = xmldoc.CreateAttribute(BlameDataCollector.Constants.DumpTypeKey);
                fulldumpAttribute.Value = "full";
                node.Attributes.Append(fulldumpAttribute);
            }

            if (collectDumpOnExit)
            {
                var fulldumpAttribute = xmldoc.CreateAttribute(BlameDataCollector.Constants.CollectDumpAlwaysKey);
                fulldumpAttribute.Value = "true";
                node.Attributes.Append(fulldumpAttribute);
            }

            if (colectDumpOnHang)
            {
                var hangDumpNode = xmldoc.CreateElement(BlameDataCollector.Constants.CollectDumpOnTestSessionHang);
                outernode.AppendChild(hangDumpNode);

                var inactivityTimeAttribute = xmldoc.CreateAttribute(BlameDataCollector.Constants.TestTimeout);
                inactivityTimeAttribute.Value = $"{inactivityTimeInMilliseconds}";
                hangDumpNode.Attributes.Append(inactivityTimeAttribute);

                if (isFullDump)
                {
                    var fulldumpAttribute = xmldoc.CreateAttribute(BlameDataCollector.Constants.DumpTypeKey);
                    fulldumpAttribute.Value = "full";
                    hangDumpNode.Attributes.Append(fulldumpAttribute);
                }
            }

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
            /// <param name="inactivityTimer">
            /// InactivityTimer instance.
            /// </param>
            /// <param name="mockFileHelper">
            /// MockFileHelper instance.
            /// </param>
            internal TestableBlameCollector(IBlameReaderWriter blameReaderWriter, IProcessDumpUtility processDumpUtility, IInactivityTimer inactivityTimer, IFileHelper mockFileHelper)
                : base(blameReaderWriter, processDumpUtility, inactivityTimer, mockFileHelper)
            {
            }
        }
    }
}
