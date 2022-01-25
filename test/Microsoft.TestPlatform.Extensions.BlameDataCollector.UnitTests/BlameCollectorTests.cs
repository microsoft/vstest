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
    [TestCategory("Windows")]
    public class BlameCollectorTests
    {
        private readonly DataCollectionEnvironmentContext context;
        private readonly DataCollectionContext dataCollectionContext;
        private BlameCollector blameDataCollector;
        private readonly Mock<DataCollectionLogger> mockLogger;
        private readonly Mock<DataCollectionEvents> mockDataColectionEvents;
        private readonly Mock<DataCollectionSink> mockDataCollectionSink;
        private readonly Mock<IBlameReaderWriter> mockBlameReaderWriter;
        private readonly Mock<IProcessDumpUtility> mockProcessDumpUtility;
        private readonly Mock<IInactivityTimer> mockInactivityTimer;
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly XmlElement configurationElement;
        private readonly string filepath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameCollectorTests"/> class.
        /// </summary>
        public BlameCollectorTests()
        {
            // Initializing mocks
            mockLogger = new Mock<DataCollectionLogger>();
            mockDataColectionEvents = new Mock<DataCollectionEvents>();
            mockDataCollectionSink = new Mock<DataCollectionSink>();
            mockBlameReaderWriter = new Mock<IBlameReaderWriter>();
            mockProcessDumpUtility = new Mock<IProcessDumpUtility>();
            mockInactivityTimer = new Mock<IInactivityTimer>();
            mockFileHelper = new Mock<IFileHelper>();
            blameDataCollector = new TestableBlameCollector(
                mockBlameReaderWriter.Object,
                mockProcessDumpUtility.Object,
                mockInactivityTimer.Object,
                mockFileHelper.Object);

            // Initializing members
            TestCase testcase = new() { Id = Guid.NewGuid() };
            dataCollectionContext = new DataCollectionContext(testcase);
            configurationElement = null;
            context = new DataCollectionEnvironmentContext(dataCollectionContext);

            filepath = Path.Combine(Path.GetTempPath(), "Test");
            FileStream stream = File.Create(filepath);
            stream.Dispose();
        }

        /// <summary>
        /// The initialize should throw exception if data collection logger is null.
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfDataCollectionLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => blameDataCollector.Initialize(
                    configurationElement,
                    mockDataColectionEvents.Object,
                    mockDataCollectionSink.Object,
                    null,
                    null));
        }

        /// <summary>
        /// Initializing with collect dump for hang disabled should ensure InativityTimer is never initialized or reset
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangDisabledShouldNotInitializeInactivityTimerOrCallReset()
        {
            int resetCalledCount = 0;

            mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMinutes == 1.0))).Callback(() => resetCalledCount++);

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, false),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

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

            mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMilliseconds == 1.0))).Callback(() => resetCalledCount++);

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, true, 1),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

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

            mockInactivityTimer.Setup(x => x.ResetTimer(It.Is<TimeSpan>(y => y.TotalMilliseconds == 1.0))).Callback(() => resetCalledCount++);

            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(filepath);

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, true, 1),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            TestCase testcase = new("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            Assert.AreEqual(3, resetCalledCount, "Should have called InactivityTimer.Reset exactly 3 times");
        }

        /// <summary>
        /// Initializing with collect dump for hang should capture a dump on timeout
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureADumpOnTimeout()
        {
            blameDataCollector = new TestableBlameCollector(
                mockBlameReaderWriter.Object,
                mockProcessDumpUtility.Object,
                null,
                mockFileHelper.Object);

            var dumpFile = "abc_hang.dmp";
            var hangBasedDumpcollected = new ManualResetEventSlim();

            mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()));
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(true, false)).Returns(new[] { dumpFile });
            mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<FileTransferInformation>())).Callback(() => hangBasedDumpcollected.Set());

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, true, 0),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            hangBasedDumpcollected.Wait(1000);
            mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Once);
            mockProcessDumpUtility.Verify(x => x.GetDumpFiles(true, false), Times.Once);
            mockDataCollectionSink.Verify(x => x.SendFileAsync(It.Is<FileTransferInformation>(y => y.Path == dumpFile)), Times.Once);
        }

        /// <summary>
        /// Initializing with collect dump for hang should kill test host process even if an error
        /// occurs during capturing the dump. Basically it should not throw.
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureKillTestHostOnTimeoutEvenIfGetDumpFileFails()
        {
            blameDataCollector = new TestableBlameCollector(
                mockBlameReaderWriter.Object,
                mockProcessDumpUtility.Object,
                null,
                mockFileHelper.Object);

            var hangBasedDumpcollected = new ManualResetEventSlim();

            mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()));
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(true, false)).Callback(() => hangBasedDumpcollected.Set()).Throws(new Exception("Some exception"));

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, true, 0),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            hangBasedDumpcollected.Wait(1000);
            mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Once);
            mockProcessDumpUtility.Verify(x => x.GetDumpFiles(true, false), Times.Once);
        }

        /// <summary>
        /// Initializing with collect dump for hang should kill test host process even if an error
        /// occurs during attaching it as a datacollector attachment. Basically it should not throw.
        /// </summary>
        [TestMethod]
        public void InitializeWithDumpForHangShouldCaptureKillTestHostOnTimeoutEvenIfAttachingDumpFails()
        {
            blameDataCollector = new TestableBlameCollector(
                mockBlameReaderWriter.Object,
                mockProcessDumpUtility.Object,
                null,
                mockFileHelper.Object);

            var dumpFile = "abc_hang.dmp";
            var hangBasedDumpcollected = new ManualResetEventSlim();

            mockFileHelper.Setup(x => x.Exists(It.Is<string>(y => y == "abc_hang.dmp"))).Returns(true);
            mockFileHelper.Setup(x => x.GetFullPath(It.Is<string>(y => y == "abc_hang.dmp"))).Returns("abc_hang.dmp");
            mockProcessDumpUtility.Setup(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()));
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(true, false)).Returns(new[] { dumpFile });
            mockDataCollectionSink.Setup(x => x.SendFileAsync(It.IsAny<FileTransferInformation>())).Callback(() => hangBasedDumpcollected.Set()).Throws(new Exception("Some other exception"));

            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, false, true, 0),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            hangBasedDumpcollected.Wait(1000);
            mockProcessDumpUtility.Verify(x => x.StartHangBasedProcessDump(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Once);
            mockProcessDumpUtility.Verify(x => x.GetDumpFiles(true, false), Times.Once);
            mockDataCollectionSink.Verify(x => x.SendFileAsync(It.Is<FileTransferInformation>(y => y.Path == dumpFile)), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should write to file if test start count is greater.
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldWriteToFileIfTestHostCrash()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                configurationElement,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            TestCase testcase = new("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(filepath);

            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify WriteTestSequence Call
            mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should not write to file if test start count is same as test end count.
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldNotWriteToFileIfNoTestHostCrash()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                configurationElement,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            TestCase testcase = new("TestProject.UnitTest.TestMethod", new Uri("test:/abc"), "abc.dll");

            // Setup and Raise TestCaseStart and Session End Event
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>())).Returns(filepath);
            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase));
            mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase, TestOutcome.Passed));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify WriteTestSequence Call
            mockBlameReaderWriter.Verify(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), filepath), Times.Never);
        }

        /// <summary>
        /// The event handlers should generate correct test sequence and testObjectDictionary for both completed and not completed tests
        /// </summary>
        [TestMethod]
        public void EventHandlersShouldGenerateCorrectTestSequenceAndTestObjectDictionaryForBothCompletedAndNotCompletedTests()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                configurationElement,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            TestCase testcase1 = new("TestProject.UnitTest.TestMethod1", new Uri("test:/abc"), "abc.dll");
            TestCase testcase2 = new("TestProject.UnitTest.TestMethod2", new Uri("test:/abc"), "abc.dll");
            testcase1.DisplayName = "TestMethod1";
            testcase2.DisplayName = "TestMethod2";
            var blameTestObject1 = new BlameTestObject(testcase1);
            var blameTestObject2 = new BlameTestObject(testcase2);

            // Setup and Raise TestCaseStart and Session End Event
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>())).Returns(filepath);
            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase1));
            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(testcase2));
            mockDataColectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(testcase1, TestOutcome.Passed));
            blameTestObject1.IsCompleted = true;
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify call to mockBlameReaderWriter
            mockBlameReaderWriter.Verify(
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
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Setup
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(It.IsAny<bool>(), It.IsAny<bool>())).Returns(new[] { filepath });
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(filepath);

            // Raise
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));
            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new TestCase()));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify GetDumpFiles Call
            mockProcessDumpUtility.Verify(x => x.GetDumpFiles(It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should ensure proc dump process is terminated when no crash is detected
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldEnsureProcDumpProcessIsTerminated()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Mock proc dump utility terminate process call
            mockProcessDumpUtility.Setup(x => x.DetachFromTargetProcess(It.IsAny<int>()));

            // Raise
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify GetDumpFiles Call
            mockProcessDumpUtility.Verify(x => x.DetachFromTargetProcess(It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should get dump files if collect dump on exit was enabled irrespective of completed test case count
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldGetDumpFileIfCollectDumpOnExitIsEnabled()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(collectDumpOnExit: true),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Setup
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(true, false)).Returns(new[] { filepath });
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(filepath);

            // Raise
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify GetDumpFiles Call
            mockProcessDumpUtility.Verify(x => x.GetDumpFiles(true, false), Times.Once);
        }

        /// <summary>
        /// The trigger session ended handler should log exception if GetDumpfile throws FileNotFound Exception
        /// </summary>
        [TestMethod]
        public void TriggerSessionEndedHandlerShouldLogWarningIfGetDumpFileThrowsFileNotFound()
        {
            // Initializing Blame Data Collector
            // force it to collect dump on exit, which won't happen and we should see a warning
            // but we should not see warning if we tell it to create dump and there is no crash
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(false, collectDumpOnExit: true),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Setup and raise events
            mockBlameReaderWriter.Setup(x => x.WriteTestSequence(It.IsAny<List<Guid>>(), It.IsAny<Dictionary<Guid, BlameTestObject>>(), It.IsAny<string>()))
                .Returns(filepath);
            mockProcessDumpUtility.Setup(x => x.GetDumpFiles(true, It.IsAny<bool>())).Throws(new FileNotFoundException());
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));
            mockDataColectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new TestCase()));
            mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionContext));

            // Verify GetDumpFiles Call
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility if proc dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityIfProcDumpEnabled()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), false, It.IsAny<string>(), false));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityForFullDumpIfFullDumpEnabled()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(isFullDump: true),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), true, It.IsAny<string>(), false));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldStartProcDumpUtilityForFullDumpIfFullDumpEnabledCaseSensitivity()
        {
            var dumpConfig = GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DuMpType");
            dumpTypeAttribute.Value = "FuLl";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);
            var dumpOnExitAttribute = dumpConfig.OwnerDocument.CreateAttribute("CollEctAlways");
            dumpOnExitAttribute.Value = "FaLSe";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpOnExitAttribute);

            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                dumpConfig,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify StartProcessDumpCall
            mockProcessDumpUtility.Verify(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), true, It.IsAny<string>(), false));
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForWrongCollectDumpKey()
        {
            var dumpConfig = GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("Xyz");
            dumpTypeAttribute.Value = "FuLl";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                dumpConfig,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterKeyIncorrect, "Xyz"))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForWrongDumpType()
        {
            var dumpConfig = GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DumpType");
            dumpTypeAttribute.Value = "random";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                dumpConfig,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, "DumpType", BlameDataCollector.Constants.FullConfigurationValue, BlameDataCollector.Constants.MiniConfigurationValue))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogWarningForNonBooleanCollectAlwaysValue()
        {
            var dumpConfig = GetDumpConfigurationElement();
            var dumpTypeAttribute = dumpConfig.OwnerDocument.CreateAttribute("DumpType");
            dumpTypeAttribute.Value = "random";
            dumpConfig[BlameDataCollector.Constants.DumpModeKey].Attributes.Append(dumpTypeAttribute);

            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                dumpConfig,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.BlameParameterValueIncorrect, "DumpType", BlameDataCollector.Constants.FullConfigurationValue, BlameDataCollector.Constants.MiniConfigurationValue))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should start process dump utility for full dump if full dump was enabled
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldLogNoWarningWhenDumpTypeIsUsedWithHangDumpBecauseEitherHangDumpTypeOrDumpTypeCanBeSpecified()
        {
            var dumpConfig = GetDumpConfigurationElement(isFullDump: true, false, colectDumpOnHang: true, 1800000);

            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                dumpConfig,
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// The trigger test host launched handler should not break if start process dump throws TestPlatFormExceptions and log error message
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldCatchTestPlatFormExceptionsAndReportMessage()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Make StartProcessDump throw exception
            var tpex = new TestPlatformException("env var exception");
            mockProcessDumpUtility.Setup(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), false, It.IsAny<string>(), false))
                                       .Throws(tpex);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, tpex.Message))), Times.Once);
        }

        /// <summary>
        /// The trigger test host launched handler should not break if start process dump throws unknown exceptions and report message with stack trace
        /// </summary>
        [TestMethod]
        public void TriggerTestHostLaunchedHandlerShouldCatchAllUnexpectedExceptionsAndReportMessageWithStackTrace()
        {
            // Initializing Blame Data Collector
            blameDataCollector.Initialize(
                GetDumpConfigurationElement(),
                mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                mockLogger.Object,
                context);

            // Make StartProcessDump throw exception
            var ex = new Exception("start process failed");
            mockProcessDumpUtility.Setup(x => x.StartTriggerBasedProcessDump(1234, It.IsAny<string>(), false, It.IsAny<string>(), false))
                                       .Throws(ex);

            // Raise TestHostLaunched
            mockDataColectionEvents.Raise(x => x.TestHostLaunched += null, new TestHostLaunchedEventArgs(dataCollectionContext, 1234));

            // Verify
            mockLogger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), It.Is<string>(str => str == string.Format(CultureInfo.CurrentUICulture, Resources.Resources.ProcDumpCouldNotStart, ex.ToString()))), Times.Once);
        }

        [TestCleanup]
        public void CleanUp()
        {
            File.Delete(filepath);
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
                var collectDumpOnExitAttribute = xmldoc.CreateAttribute(BlameDataCollector.Constants.CollectDumpAlwaysKey);
                collectDumpOnExitAttribute.Value = "true";
                node.Attributes.Append(collectDumpOnExitAttribute);
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
