// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Castle.Components.DictionaryAdapter;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;

    [TestClass]
    public class EventLogContainerTests
    {
        private List<string> eventSources;

        private List<EventLogEntryType> entryTypes;

        private Mock<DataCollectionLogger> logger;

        private DataCollectionContext context;

        public EventLogContainerTests()
        {
            this.eventSources = new List<string>();
            this.entryTypes = new List<EventLogEntryType>();
            this.logger = new Mock<DataCollectionLogger>();
            this.context = new DataCollectionContext(new SessionId(Guid.NewGuid()));
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldAddLogs()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 1,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);

            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 234);
            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogCollectorContextData.EventLogEntries.Count;

            Assert.AreEqual(1, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldNotAddLogsIfNoNewEntryIsPresent()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 1,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);
            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogCollectorContextData.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldLogWarningIfNextEntryIndexToCollectIsGreaterThanMostRecentIndexLog()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 5,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);
            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            this.logger.Verify(x => x.LogWarning(It.IsAny<DataCollectionContext>(), string.Format(
                CultureInfo.InvariantCulture,
                Resource.Execution_Agent_DataCollectors_EventLog_EventsLostWarning,
               "Application")));
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulFilterLogsBasedOnEventTypeAndEventSource()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            this.entryTypes.Add(EventLogEntryType.Warning);
            this.eventSources.Add("Application");
            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 1,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);

            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogCollectorContextData.EventLogEntries.Count;

            Assert.AreEqual(1, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventSourceIsDifferent()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            this.eventSources.Add("Application1");
            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 1,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);

            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogCollectorContextData.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventTypeIsDifferent()
        {
            EventLog eventLog = new EventLog("Application");
            EntryWrittenEventArgs entryWrittenEventArgs = new EntryWrittenEventArgs();

            this.entryTypes.Add(EventLogEntryType.FailureAudit);
            EventLogCollectorContextData eventLogCollectorContextData = new EventLogCollectorContextData(5);
            EventLogContainer container = new EventLogContainer(
                eventLog,
                eventLog.Entries[eventLog.Entries.Count - 1].Index + 1,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.context,
                eventLogCollectorContextData);

            eventLogCollectorContextData.EventLogContainers.Add("Application", container);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            container.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogCollectorContextData.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }
    }
}
