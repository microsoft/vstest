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
            this.mockEventLogContainer = new DummyEventLogContainer();
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
        public void CreateEventLogContainerEndIndexMapShouldEndIndexCreateMap()
        {
            this.eventLogSessionContext = new EventLogSessionContext(this.eventLogContainersMap);
            this.eventLogSessionContext.CreateEventLogContainerEndIndexMap();
            Assert.IsTrue(this.eventLogSessionContext.EventLogContainerEndIndexMap["LogName"] == 1);
        }
    }

    public class DummyEventLogContainer : IEventLogContainer
    {
        public DummyEventLogContainer()
        {
            this.EventLogEntries = new List<EventLogEntry>(10);
            EventLog eventLog = new EventLog("Application");
            int currentIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index - eventLog.Entries[0].Index;
            this.EventLogEntries.Add(eventLog.Entries[currentIndex]);
            this.EventLogEntries.Add(eventLog.Entries[currentIndex - 1]);
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
