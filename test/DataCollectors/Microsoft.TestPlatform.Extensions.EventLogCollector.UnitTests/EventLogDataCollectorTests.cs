// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EventLogDataCollectorTests
    {
        private const string ConfigurationString =
            @"<Configuration><Setting name=""key"" value=""value"" /></Configuration>";

        private readonly Mock<DataCollectionEvents> mockDataCollectionEvents;

        private readonly TestableDataCollectionSink mockDataCollectionSink;

        private readonly Mock<DataCollectionLogger> mockDataCollectionLogger;

        private readonly DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        private readonly EventLogDataCollector eventLogDataCollector;

        private readonly Mock<IFileHelper> mockFileHelper;

        public EventLogDataCollectorTests()
        {
            mockDataCollectionEvents = new Mock<DataCollectionEvents>();
            mockDataCollectionSink = new TestableDataCollectionSink();
            mockFileHelper = new Mock<IFileHelper>();
            _ = new();
            DataCollectionContext dataCollectionContext =
                new(new SessionId(Guid.NewGuid()));
            dataCollectionEnvironmentContext = new DataCollectionEnvironmentContext(dataCollectionContext);
            mockDataCollectionLogger = new Mock<DataCollectionLogger>();
            eventLogDataCollector = new EventLogDataCollector(mockFileHelper.Object);
        }

        [TestMethod]
        public void EventLoggerLogsErrorForInvalidEventSources()
        {
            string configurationString =
            @"<Configuration><Setting name=""EventLogs"" value=""MyEventName"" /></Configuration>";
            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);
            var mockCollector = new Mock<DataCollectionLogger>();
            mockCollector.Setup(m => m.LogError(It.IsAny<DataCollectionContext>(), It.Is<string>(s => s.Contains(@"The event log 'MyEventName' on computer '.' does not exist.")), It.IsAny<Exception>()));

            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockCollector.Object, dataCollectionEnvironmentContext);

            mockCollector.Verify(m => m.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => eventLogDataCollector.Initialize(
                            null,
                            null,
                            mockDataCollectionSink,
                            mockDataCollectionLogger.Object,
                            dataCollectionEnvironmentContext));
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfCollectionSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => eventLogDataCollector.Initialize(
                            null,
                            mockDataCollectionEvents.Object,
                            null,
                            mockDataCollectionLogger.Object,
                            dataCollectionEnvironmentContext));
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => eventLogDataCollector.Initialize(
                            null,
                            mockDataCollectionEvents.Object,
                            mockDataCollectionSink,
                            null,
                            dataCollectionEnvironmentContext));
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultEventLogNames()
        {
            List<string> eventLogNames = new()
            {
                "System",
                "Application"
            };

            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, eventLogDataCollector.EventLogNames.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeCustomEventLogNamesIfSpecifiedInConfiguration()
        {
            string configurationString =
            @"<Configuration><Setting name=""EventLogs"" value=""MyEventName,MyEventName2"" /></Configuration>";

            List<string> eventLogNames = new()
            {
                "MyEventName",
                "MyEventName2"
            };

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);

            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, eventLogDataCollector.EventLogNames.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultLogEntryTypes()
        {
            List<EventLogEntryType> entryTypes = new()
            {
                EventLogEntryType.Error,
                EventLogEntryType.Warning,
                EventLogEntryType.FailureAudit
            };

            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, eventLogDataCollector.EntryTypes.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeEntryTypesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EntryTypes"" value=""Error"" /></Configuration>";

            List<EventLogEntryType> entryTypes = new()
            {
                EventLogEntryType.Error
            };

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, eventLogDataCollector.EntryTypes.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeEventSourcesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EventSources"" value=""MyEventSource"" /></Configuration>";

            List<string> eventSources = new()
            {
                "MyEventSource"
            };

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventSources, eventLogDataCollector.EventSources.ToList());
        }

        [TestMethod]
        public void InitializeShouldNotInitializeEventSourcesByDefault()
        {
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            Assert.IsNull(eventLogDataCollector.EventSources);
        }

        [TestMethod]
        public void InitializeShouldInitializeMaxEntriesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /></Configuration>";

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            Assert.AreEqual(20, eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSetDefaultMaxEntries()
        {
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            Assert.AreEqual(50000, eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSubscribeToDataCollectionEvents()
        {
            var testableDataCollectionEvents = new TestableDataCollectionEvents();
            eventLogDataCollector.Initialize(null, testableDataCollectionEvents, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestHostLaunchedInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestCaseStartInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestCaseEndInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestSessionEndInvocationList().Length);
            Assert.AreEqual(1, testableDataCollectionEvents.GetTestSessionStartInvocationList().Length);
        }

        [TestMethod]
        public void TestSessionStartEventShouldCreateEventLogContainer()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            Assert.AreEqual(0, eventLogDataCollector.ContextMap.Count);
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs());
            Assert.AreEqual(1, eventLogDataCollector.ContextMap.Count);
        }

        [TestMethod]
        public void TestCaseStartEventShouldCreateEventLogContainer()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            Assert.AreEqual(0, eventLogDataCollector.ContextMap.Count);

            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid())), new TestCase()));
            Assert.AreEqual(1, eventLogDataCollector.ContextMap.Count);
        }

        [TestMethod]

        public void TestCaseEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        public void TestCaseEndEventShouldInvokeSendFileAsync()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        [TestMethod]
        public void TestCaseEndEventShouldThrowIfTestCaseStartIsNotInvoked()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));

            Assert.ThrowsException<EventLogCollectorException>(() => mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed)));
        }

        public void SessionEndEventShouldThrowIfSessionStartEventtIsNotInvoked()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            var tc = new TestCase();

            Assert.ThrowsException<EventLogCollectorException>(() => mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionEnvironmentContext.SessionDataCollectionContext)));
        }

        [TestMethod]
        public void TestSessionEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            var testcase = new TestCase() { Id = Guid.NewGuid() };
            mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(dataCollectionEnvironmentContext.SessionDataCollectionContext, new Dictionary<string, object>()));
            mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(dataCollectionEnvironmentContext.SessionDataCollectionContext));
            Assert.IsTrue(mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        [TestMethod]
        public void WriteEventLogsShouldCreateXmlFile()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);

            mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);
            eventLogDataCollector.WriteEventLogs(
                new List<EventLogEntry>(),
                20,
                dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            mockFileHelper.Verify(x => x.WriteAllTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void WriteEventLogsShouldThrowExceptionIfThrownByFileHelper()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);
            mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Throws<Exception>();
            Assert.ThrowsException<Exception>(
                () => eventLogDataCollector.WriteEventLogs(
                            new List<EventLogEntry>(),
                            20,
                            dataCollectionEnvironmentContext.SessionDataCollectionContext,
                            TimeSpan.MaxValue,
                            DateTime.Now));
        }

        [TestMethod]
        public void WriteEventLogsShouldFilterTestsBasedOnTimeAndMaxValue()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);

            mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            var entries = new List<EventLogEntry>();

            var eventLog = new EventLog("Application");
            int endIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index;
            int firstIndexInLog = eventLog.Entries[0].Index;
            for (int i = endIndex; i > endIndex - 10; i--)
            {
                entries.Add(eventLog.Entries[i - firstIndexInLog]);
            }

            var filteredEntries = entries.Where(entry => entry.TimeGenerated > DateTime.MinValue && entry.TimeGenerated < DateTime.MaxValue)
                .OrderBy(x => x.TimeGenerated).Take(5).ToList();

            eventLogDataCollector.WriteEventLogs(
                entries,
                5,
                dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            mockFileHelper.Verify(
                x => x.WriteAllTextToFile(
                    It.IsAny<string>(),
                    It.Is<string>(
                        str => str.Contains(filteredEntries[0].InstanceId.ToString())
                               && str.Contains(filteredEntries[1].InstanceId.ToString())
                               && str.Contains(filteredEntries[2].InstanceId.ToString())
                               && str.Contains(filteredEntries[3].InstanceId.ToString())
                               && str.Contains(filteredEntries[4].InstanceId.ToString()))));
        }

        [TestMethod]
        public void WriteEventLogsShouldFilterTestIfMaxValueExceedsEntries()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new();
            expectedXmlDoc.LoadXml(configurationString);

            mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, mockDataCollectionEvents.Object, mockDataCollectionSink, mockDataCollectionLogger.Object, dataCollectionEnvironmentContext);

            var entries = new List<EventLogEntry>();

            var eventLog = new EventLog("Application");
            int endIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index;
            int firstIndexInLog = eventLog.Entries[0].Index;
            for (int i = endIndex; i > endIndex - 5; i--)
            {
                entries.Add(eventLog.Entries[i - firstIndexInLog]);
            }

            var filteredEntries = entries.Where(entry => entry.TimeGenerated > DateTime.MinValue && entry.TimeGenerated < DateTime.MaxValue)
                .OrderBy(x => x.TimeGenerated).Take(10).ToList();

            eventLogDataCollector.WriteEventLogs(
                entries,
                5,
                dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            mockFileHelper.Verify(
                x => x.WriteAllTextToFile(
                    It.IsAny<string>(),
                    It.Is<string>(
                        str => str.Contains(filteredEntries[0].InstanceId.ToString())
                               && str.Contains(filteredEntries[1].InstanceId.ToString())
                               && str.Contains(filteredEntries[2].InstanceId.ToString())
                               && str.Contains(filteredEntries[3].InstanceId.ToString())
                               && str.Contains(filteredEntries[4].InstanceId.ToString()))));
        }
    }

    /// <summary>
    /// The testable data collection events.
    /// </summary>
    public class TestableDataCollectionEvents : DataCollectionEvents
    {
        public override event EventHandler<TestHostLaunchedEventArgs> TestHostLaunched;

        public override event EventHandler<SessionStartEventArgs> SessionStart;

        public override event EventHandler<SessionEndEventArgs> SessionEnd;

        public override event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        public override event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        public Delegate[] GetTestHostLaunchedInvocationList()
        {
            return TestHostLaunched.GetInvocationList();
        }

        public Delegate[] GetTestCaseStartInvocationList()
        {
            return TestCaseStart.GetInvocationList();
        }

        public Delegate[] GetTestCaseEndInvocationList()
        {
            return TestCaseEnd.GetInvocationList();
        }

        public Delegate[] GetTestSessionStartInvocationList()
        {
            return SessionStart.GetInvocationList();
        }

        public Delegate[] GetTestSessionEndInvocationList()
        {
            return SessionEnd.GetInvocationList();
        }
    }

    /// <summary>
    /// The testable data collection sink.
    /// </summary>
    public class TestableDataCollectionSink : DataCollectionSink
    {
        /// <summary>
        /// The send file completed.
        /// </summary>
        public override event AsyncCompletedEventHandler SendFileCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether is send file async invoked.
        /// </summary>
        public bool IsSendFileAsyncInvoked { get; set; }

        public override void SendFileAsync(FileTransferInformation fileTransferInformation)
        {
            IsSendFileAsyncInvoked = true;
            if (SendFileCompleted == null)
            {
                return;
            }
        }
    }
}
