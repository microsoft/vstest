// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;

    [TestClass]
    public class EventLogContainerTests
    {
        private HashSet<string> eventSources;

        private HashSet<EventLogEntryType> entryTypes;

        private Mock<DataCollectionLogger> logger;

        private DataCollectionContext dataCollectionContext;

        private Mock<DataCollectionSink> mockDataCollectionSink;

        private EventLog eventLog;

        private Dictionary<string, EventLog> eventLogMap;

        private EventLogContainer eventLogContainer;

        private EntryWrittenEventArgs entryWrittenEventArgs;

        private Mock<IFileHelper> mockFileHelper;


        public EventLogContainerTests()
        {
            this.eventSources = new HashSet<string>();
            this.entryTypes = new HashSet<EventLogEntryType>();

            this.logger = new Mock<DataCollectionLogger>();
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.eventLogMap = new Dictionary<string, EventLog>();

            this.eventLog = new EventLog("Application");
            this.eventLogMap.Add("Application", this.eventLog);
            this.entryWrittenEventArgs = new EntryWrittenEventArgs(this.eventLog.Entries[this.eventLog.Entries.Count - 1]);

            this.dataCollectionContext = new DataCollectionContext(new SessionId(Guid.NewGuid()));

            this.mockFileHelper = new Mock<IFileHelper>();

            this.eventLogContainer = new EventLogContainer(
                this.eventLogMap,
                this.eventSources,
                this.entryTypes,
                this.logger.Object,
                this.dataCollectionContext,
                this.mockDataCollectionSink.Object,
                5,
                this.mockFileHelper.Object);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldAddLogs()
        {
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(1, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldNotAddLogsIfNoNewEntryIsPresent()
        {
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShouldLogWarningIfNextEntryIndexToCollectIsGreaterThanMostRecentIndexLog()
        {
            this.eventLogContainer.EventLogIndexMap["Application"] =
                this.eventLogContainer.EventLogIndexMap["Application"] + 10;
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            this.logger.Verify(
                x => x.LogWarning(
                    It.IsAny<DataCollectionContext>(),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resource.EventsLostWarning,
               "Application")));
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
            this.eventSources.Add("Application1");

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void OnEventLogEntryWrittenShoulNotAddLogsIfEventTypeIsDifferent()
        {
            this.entryTypes.Add(EventLogEntryType.FailureAudit);

            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Warning, 234);
            this.eventLogContainer.OnEventLogEntryWritten(this.eventLog, this.entryWrittenEventArgs);
            var newCount = this.eventLogContainer.EventLogEntries.Count;

            Assert.AreEqual(0, newCount);
        }

        [TestMethod]
        public void WriteEventLogsShouldCreateXmlFile()
        {
            this.mockFileHelper.SetupSequence(x => x.Exists(It.IsAny<string>())).Returns(false).Returns(true);
            this.entryTypes.Add(EventLogEntryType.Warning);
            this.eventSources.Add("Application");
            this.eventLogContainer.WriteEventLogs(this.dataCollectionContext, TimeSpan.MaxValue, DateTime.Now);

            this.mockFileHelper.Verify(x => x.WriteAllTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void WriteEventLogsShouldThrowExceptionIfThrownByFileHelper()
        {
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Throws<Exception>();
            this.entryTypes.Add(EventLogEntryType.Warning);
            this.eventSources.Add("Application");
            Assert.ThrowsException<Exception>(
                () =>
                    {
                        this.eventLogContainer.WriteEventLogs(
                            this.dataCollectionContext,
                            TimeSpan.MaxValue,
                            DateTime.Now);
                    });
        }
    }
}
