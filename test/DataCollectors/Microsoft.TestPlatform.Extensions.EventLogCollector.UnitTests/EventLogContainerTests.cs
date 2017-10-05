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

    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;
    using System.Globalization;

    [TestClass]
    public class EventLogContainerTests
    {
        private HashSet<string> eventSources;

        private HashSet<EventLogEntryType> entryTypes;

        private Mock<DataCollectionLogger> logger;

        private DataCollectionContext dataCollectionContext;

        private EventLog eventLog;

        private EventLogContainer eventLogContainer;

        private EntryWrittenEventArgs entryWrittenEventArgs;


        private string eventLogName = "Application";


        public EventLogContainerTests()
        {
            this.eventSources = new HashSet<string>();
            this.eventSources.Add("Application");
            this.entryTypes = new HashSet<EventLogEntryType>();
            this.entryTypes.Add(EventLogEntryType.Error);

            this.logger = new Mock<DataCollectionLogger>();
            this.eventLog = new EventLog("Application");
            this.entryWrittenEventArgs = new EntryWrittenEventArgs(this.eventLog.Entries[this.eventLog.Entries.Count - 1]);

            this.dataCollectionContext = new DataCollectionContext(new SessionId(Guid.NewGuid()));

            this.eventLogContainer = new EventLogContainer(
                this.eventLogName,
                this.eventSources,
                this.entryTypes,
                int.MaxValue,
                this.logger.Object,
                this.dataCollectionContext);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldAddLogs()
        {
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.IsTrue(newCount > 0);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldNotAddLogsIfNoNewEntryIsPresent()
        {
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulFilterLogsBasedOnEventTypeAndEventSource()
        {
            this.entryTypes.Add(EventLogEntryType.Warning);
            this.eventSources.Add("Application");

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(1, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventSourceIsDifferent()
        {
            this.eventSources.Clear();
            this.eventSources.Add("Application1");
            this.eventLogContainer = new EventLogContainer(
                this.eventLogName,
                this.eventSources,
                this.entryTypes,
                int.MaxValue,
                this.logger.Object,
                this.dataCollectionContext);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventTypeIsDifferent()
        {
            this.entryTypes.Clear();
            this.entryTypes.Add(EventLogEntryType.FailureAudit);

            this.eventSources.Add("Application1");
            this.eventLogContainer = new EventLogContainer(
                this.eventLogName,
                this.eventSources,
                this.entryTypes,
                int.MaxValue,
                this.logger.Object,
                this.dataCollectionContext);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }
    }
}
