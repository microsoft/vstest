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

        private Mock<DataCollectionEvents> mockDataCollectionEvents;

        private TestableDataCollectionSink mockDataCollectionSink;

        private Mock<DataCollectionLogger> mockDataCollectionLogger;

        private DataCollectionEnvironmentContext dataCollectionEnvironmentContext;

        private EventLogDataCollector eventLogDataCollector;

        private Mock<IFileHelper> mockFileHelper;

        public EventLogDataCollectorTests()
        {
            this.mockDataCollectionEvents = new Mock<DataCollectionEvents>();
            this.mockDataCollectionSink = new TestableDataCollectionSink();
            this.mockFileHelper = new Mock<IFileHelper>();
            TestCase tc = new TestCase();
            DataCollectionContext dataCollectionContext =
                new DataCollectionContext(new SessionId(Guid.NewGuid()));
            this.dataCollectionEnvironmentContext = new DataCollectionEnvironmentContext(dataCollectionContext);
            this.mockDataCollectionLogger = new Mock<DataCollectionLogger>();
            this.eventLogDataCollector = new EventLogDataCollector(this.mockFileHelper.Object);
        }

        [TestMethod]
        public void EventLoggerLogsErrorForInvalidEventSources()
        {
            string configurationString =
            @"<Configuration><Setting name=""EventLogs"" value=""MyEventName"" /></Configuration>";
            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            var mockCollector = new Mock<DataCollectionLogger>();
            mockCollector.Setup(m => m.LogError(It.IsAny<DataCollectionContext>(), It.Is<string>(s => s.Contains(@"The event log 'MyEventName' on computer '.' does not exist.")), It.IsAny<Exception>()));

            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, mockCollector.Object, this.dataCollectionEnvironmentContext);

            mockCollector.Verify(m => m.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            null,
                            this.mockDataCollectionSink,
                            this.mockDataCollectionLogger.Object,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfCollectionSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            this.mockDataCollectionEvents.Object,
                            null,
                            this.mockDataCollectionLogger.Object,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.eventLogDataCollector.Initialize(
                            null,
                            this.mockDataCollectionEvents.Object,
                            this.mockDataCollectionSink,
                            null,
                            this.dataCollectionEnvironmentContext);
                    });
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultEventLogNames()
        {
            List<string> eventLogNames = new List<string>();
            eventLogNames.Add("System");
            eventLogNames.Add("Application");

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeCustomEventLogNamesIfSpecifiedInConfiguration()
        {
            string configurationString =
            @"<Configuration><Setting name=""EventLogs"" value=""MyEventName,MyEventName2"" /></Configuration>";

            List<string> eventLogNames = new List<string>();
            eventLogNames.Add("MyEventName");
            eventLogNames.Add("MyEventName2");

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);

            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventLogNames, this.eventLogDataCollector.EventLogNames.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultLogEntryTypes()
        {
            List<EventLogEntryType> entryTypes = new List<EventLogEntryType>();
            entryTypes.Add(EventLogEntryType.Error);
            entryTypes.Add(EventLogEntryType.Warning);
            entryTypes.Add(EventLogEntryType.FailureAudit);

            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeEntryTypesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EntryTypes"" value=""Error"" /></Configuration>";

            List<EventLogEntryType> entryTypes = new List<EventLogEntryType>();
            entryTypes.Add(EventLogEntryType.Error);

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(entryTypes, this.eventLogDataCollector.EntryTypes.ToList());
        }

        [TestMethod]
        public void InitializeShouldInitializeEventSourcesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""EventSources"" value=""MyEventSource"" /></Configuration>";

            List<string> eventSources = new List<string>();
            eventSources.Add("MyEventSource");

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            CollectionAssert.AreEqual(eventSources, this.eventLogDataCollector.EventSources.ToList());
        }

        [TestMethod]
        public void InitializeShouldNotInitializeEventSourcesByDefault()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.IsNull(this.eventLogDataCollector.EventSources);
        }

        [TestMethod]
        public void InitializeShouldInitializeMaxEntriesIfSpecifiedInConfiguration()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(20, this.eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSetDefaultMaxEntries()
        {
            this.eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            Assert.AreEqual(50000, this.eventLogDataCollector.MaxEntries);
        }

        [TestMethod]
        public void InitializeShouldSubscribeToDataCollectionEvents()
        {
            var testableDataCollectionEvents = new TestableDataCollectionEvents();
            this.eventLogDataCollector.Initialize(null, testableDataCollectionEvents, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
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
            Assert.AreEqual(eventLogDataCollector.ContextMap.Count, 0);
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            this.mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs());
            Assert.AreEqual(eventLogDataCollector.ContextMap.Count, 1);
        }

        [TestMethod]
        public void TestCaseStartEventShouldCreateEventLogContainer()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            Assert.AreEqual(eventLogDataCollector.ContextMap.Count, 0);

            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid())), new TestCase()));
            Assert.AreEqual(eventLogDataCollector.ContextMap.Count, 1);
        }

        [TestMethod]

        public void TestCaseEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        public void TestCaseEndEventShouldInvokeSendFileAsync()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseStart += null, new TestCaseStartEventArgs(context, tc));
            this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        [TestMethod]
        public void TestCaseEndEventShouldThrowIfTestCaseStartIsNotInvoked()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();
            var context = new DataCollectionContext(new SessionId(Guid.NewGuid()), new TestExecId(Guid.NewGuid()));

            Assert.ThrowsException<EventLogCollectorException>(() =>
            {
                this.mockDataCollectionEvents.Raise(x => x.TestCaseEnd += null, new TestCaseEndEventArgs(context, tc, TestOutcome.Passed));
            });
        }

        public void SessionEndEventShouldThrowIfSessionStartEventtIsNotInvoked()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var tc = new TestCase();

            Assert.ThrowsException<EventLogCollectorException>(() =>
                {
                    this.mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));
                });
        }

        [TestMethod]
        public void TestSessionEndEventShouldWriteEventLogEntriesAndSendFile()
        {
            var eventLogDataCollector = new EventLogDataCollector();
            eventLogDataCollector.Initialize(null, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            var testcase = new TestCase() { Id = Guid.NewGuid() };
            this.mockDataCollectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext, new Dictionary<string, object>()));
            this.mockDataCollectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs(this.dataCollectionEnvironmentContext.SessionDataCollectionContext));
            Assert.IsTrue(this.mockDataCollectionSink.IsSendFileAsyncInvoked);
        }

        [TestMethod]
        public void WriteEventLogsShouldCreateXmlFile()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);

            this.mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);
            this.eventLogDataCollector.WriteEventLogs(
                new List<EventLogEntry>(),
                20,
                this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            this.mockFileHelper.Verify(x => x.WriteAllTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void WriteEventLogsShouldThrowExceptionIfThrownByFileHelper()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Throws<Exception>();
            Assert.ThrowsException<Exception>(
                () =>
                    {
                        this.eventLogDataCollector.WriteEventLogs(
                            new List<EventLogEntry>(),
                            20,
                            this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                            TimeSpan.MaxValue,
                            DateTime.Now);
                    });
        }

        [TestMethod]
        public void WriteEventLogsShouldFilterTestsBasedOnTimeAndMaxValue()
        {
            string configurationString =
                @"<Configuration><Setting name=""MaxEventLogEntriesToCollect"" value=""20"" /><Setting name=""EventLog"" value=""Application"" /><Setting name=""EntryTypes"" value=""Warning"" /></Configuration>";

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);

            this.mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            var entries = new List<EventLogEntry>();

            var eventLog = new EventLog("Application");
            int endIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index;
            int firstIndexInLog = eventLog.Entries[0].Index;
            for (int i = endIndex; i > endIndex - 10; i--)
            {
                entries.Add(eventLog.Entries[i - firstIndexInLog]);
            }

            var filteredEntries = entries.Where(entry => entry.TimeGenerated > DateTime.MinValue && entry.TimeGenerated < DateTime.MaxValue)
                .OrderBy(x => x.TimeGenerated).ToList().Take(5).ToList();

            this.eventLogDataCollector.WriteEventLogs(
                entries,
                5,
                this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            this.mockFileHelper.Verify(
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

            XmlDocument expectedXmlDoc = new XmlDocument();
            expectedXmlDoc.LoadXml(configurationString);

            this.mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            this.eventLogDataCollector.Initialize(expectedXmlDoc.DocumentElement, this.mockDataCollectionEvents.Object, this.mockDataCollectionSink, this.mockDataCollectionLogger.Object, this.dataCollectionEnvironmentContext);

            var entries = new List<EventLogEntry>();

            var eventLog = new EventLog("Application");
            int endIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index;
            int firstIndexInLog = eventLog.Entries[0].Index;
            for (int i = endIndex; i > endIndex - 5; i--)
            {
                entries.Add(eventLog.Entries[i - firstIndexInLog]);
            }

            var filteredEntries = entries.Where(entry => entry.TimeGenerated > DateTime.MinValue && entry.TimeGenerated < DateTime.MaxValue)
                .OrderBy(x => x.TimeGenerated).ToList().Take(10).ToList();

            this.eventLogDataCollector.WriteEventLogs(
                entries,
                5,
                this.dataCollectionEnvironmentContext.SessionDataCollectionContext,
                TimeSpan.MaxValue,
                DateTime.Now);

            this.mockFileHelper.Verify(
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
            return this.TestHostLaunched.GetInvocationList();
        }

        public Delegate[] GetTestCaseStartInvocationList()
        {
            return this.TestCaseStart.GetInvocationList();
        }

        public Delegate[] GetTestCaseEndInvocationList()
        {
            return this.TestCaseEnd.GetInvocationList();
        }

        public Delegate[] GetTestSessionStartInvocationList()
        {
            return this.SessionStart.GetInvocationList();
        }

        public Delegate[] GetTestSessionEndInvocationList()
        {
            return this.SessionEnd.GetInvocationList();
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
            this.IsSendFileAsyncInvoked = true;
            if (this.SendFileCompleted == null)
            {
                return;
            }
        }
    }
}
