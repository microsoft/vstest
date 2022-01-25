// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Resource = Resources.Resources;
    using System.Globalization;

    [TestClass]
    public class EventLogContainerTests
    {
        private readonly HashSet<string> eventSources;

        private readonly HashSet<EventLogEntryType> entryTypes;

        private readonly Mock<DataCollectionLogger> logger;

        private readonly DataCollectionContext dataCollectionContext;

        private readonly EventLog eventLog;

        private EventLogContainer eventLogContainer;

        private readonly EntryWrittenEventArgs entryWrittenEventArgs;


        private readonly string eventLogName = "Application";


        public EventLogContainerTests()
        {
            eventSources = new HashSet<string>
            {
                "Application"
            };
            entryTypes = new HashSet<EventLogEntryType>
            {
                EventLogEntryType.Error
            };

            logger = new Mock<DataCollectionLogger>();
            eventLog = new EventLog("Application");
            entryWrittenEventArgs = new EntryWrittenEventArgs(eventLog.Entries[eventLog.Entries.Count - 1]);

            dataCollectionContext = new DataCollectionContext(new SessionId(Guid.NewGuid()));

            eventLogContainer = new EventLogContainer(
                eventLogName,
                eventSources,
                entryTypes,
                int.MaxValue,
                logger.Object,
                dataCollectionContext);
        }

        [TestMethod]
        [Ignore]
        public void OnEventLogEntryWrittenShouldAddLogs()
        {
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 234);
            eventLogContainer.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogContainer.EventLogEntries.Count;

            Assert.IsTrue(newCount > 0);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldNotAddLogsIfNoNewEntryIsPresent()
        {
            eventLogContainer.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulFilterLogsBasedOnEventTypeAndEventSource()
        {
            entryTypes.Add(EventLogEntryType.Warning);
            eventSources.Add("Application");

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            eventLogContainer.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(1, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventSourceIsDifferent()
        {
            eventSources.Clear();
            eventSources.Add("Application1");
            eventLogContainer = new EventLogContainer(
                eventLogName,
                eventSources,
                entryTypes,
                int.MaxValue,
                logger.Object,
                dataCollectionContext);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            eventLogContainer.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventTypeIsDifferent()
        {
            entryTypes.Clear();
            entryTypes.Add(EventLogEntryType.FailureAudit);

            eventSources.Add("Application1");
            eventLogContainer = new EventLogContainer(
                eventLogName,
                eventSources,
                entryTypes,
                int.MaxValue,
                logger.Object,
                dataCollectionContext);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            eventLogContainer.OnEventLogEntryWritten(eventLog, entryWrittenEventArgs);
            var newCount = eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }
    }
}
