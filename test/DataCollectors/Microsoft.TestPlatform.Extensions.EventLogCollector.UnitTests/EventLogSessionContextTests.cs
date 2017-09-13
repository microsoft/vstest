// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EventLogSessionContextTests
    {
        private Dictionary<string, IEventLogContainer> eventLogContainersMap;

        private DummyEventLogContainer mockEventLogContainer;

        private EventLogSessionContext eventLogSessionContext;

        public EventLogSessionContextTests()
        {
            this.mockEventLogContainer = new DummyEventLogContainer(true);
            this.eventLogContainersMap = new Dictionary<string, IEventLogContainer>();
            this.eventLogContainersMap.Add("LogName", this.mockEventLogContainer);
        }

        [TestMethod]
        public void CreateEventLogContainerStartIndexMapShouldCreateStartIndexMap()
        {
            this.eventLogSessionContext = new EventLogSessionContext(this.eventLogContainersMap);
            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerStartIndexMap["LogName"] == 1);
        }

        [TestMethod]
        public void CreateEventLogContainerEndIndexMapShouldCreateEndIndexMap()
        {
            this.eventLogSessionContext = new EventLogSessionContext(this.eventLogContainersMap);
            this.eventLogSessionContext.CreateEventLogContainerEndIndexMap();
            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerEndIndexMap["LogName"] == 1);
        }

        [TestMethod]
        public void CreateEventLogContainerShouldNotAddIndexEntriesIfEventLogContainerMapsIsEmpty()
        {
            this.eventLogSessionContext = new EventLogSessionContext(new Dictionary<string, IEventLogContainer>());
            this.eventLogSessionContext.CreateEventLogContainerStartIndexMap();
            this.eventLogSessionContext.CreateEventLogContainerEndIndexMap();

            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerStartIndexMap.Count == 0);
            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerEndIndexMap.Count == 0);
        }

        [TestMethod]
        public void CreateEventLogContainerShouldCreateNegativeEndIndexIfLogEntriesAreEmpty()
        {
            var dict = new Dictionary<string, IEventLogContainer>();
            var dummyEventLogContainer = new DummyEventLogContainer(false);
            dict.Add("DummyEventLog", dummyEventLogContainer);

            this.eventLogSessionContext = new EventLogSessionContext(dict);
            this.eventLogSessionContext.CreateEventLogContainerStartIndexMap();
            this.eventLogSessionContext.CreateEventLogContainerEndIndexMap();

            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerStartIndexMap["DummyEventLog"] == 0);
            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerEndIndexMap["DummyEventLog"] == -1);
        }
    }

    public class DummyEventLogContainer : IEventLogContainer
    {
        public DummyEventLogContainer(bool initialize)
        {
            this.EventLogEntries = new List<EventLogEntry>(10);
            EventLog eventLog = new EventLog("Application");

            if (initialize)
            {
                int currentIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index - eventLog.Entries[0].Index;
                this.EventLogEntries.Add(eventLog.Entries[currentIndex]);
                this.EventLogEntries.Add(eventLog.Entries[currentIndex - 1]);
            }
        }

        public void Dispose()
        {
        }

        public EventLog EventLog { get; }

        public List<EventLogEntry> EventLogEntries { get; set; }

        public void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e)
        {
        }
    }
}
