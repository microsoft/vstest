// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventLogSessionContextTests
    {
        private readonly Dictionary<string, IEventLogContainer> eventLogContainersMap;

        private readonly DummyEventLogContainer mockEventLogContainer;

        private EventLogSessionContext eventLogSessionContext;

        public EventLogSessionContextTests()
        {
            mockEventLogContainer = new DummyEventLogContainer(true);
            eventLogContainersMap = new Dictionary<string, IEventLogContainer>
            {
                { "LogName", mockEventLogContainer }
            };
        }

        [TestMethod]
        public void CreateEventLogContainerStartIndexMapShouldCreateStartIndexMap()
        {
            eventLogSessionContext = new EventLogSessionContext(eventLogContainersMap);
            Assert.IsTrue(eventLogSessionContext.EventLogContainerStartIndexMap["LogName"] == 2);
        }

        [TestMethod]
        public void CreateEventLogContainerEndIndexMapShouldCreateEndIndexMap()
        {
            eventLogSessionContext = new EventLogSessionContext(eventLogContainersMap);
            eventLogSessionContext.CreateEventLogContainerEndIndexMap();
            Assert.IsTrue(eventLogSessionContext.EventLogContainerEndIndexMap["LogName"] == 1);
        }

        [TestMethod]
        public void CreateEventLogContainerShouldNotAddIndexEntriesIfEventLogContainerMapsIsEmpty()
        {
            eventLogSessionContext = new EventLogSessionContext(new Dictionary<string, IEventLogContainer>());
            eventLogSessionContext.CreateEventLogContainerStartIndexMap();
            eventLogSessionContext.CreateEventLogContainerEndIndexMap();

            Assert.IsTrue(eventLogSessionContext.EventLogContainerStartIndexMap.Count == 0);
            Assert.IsTrue(eventLogSessionContext.EventLogContainerEndIndexMap.Count == 0);
        }

        [TestMethod]
        public void CreateEventLogContainerShouldCreateNegativeEndIndexIfLogEntriesAreEmpty()
        {
            var dict = new Dictionary<string, IEventLogContainer>();
            var dummyEventLogContainer = new DummyEventLogContainer(false);
            dict.Add("DummyEventLog", dummyEventLogContainer);

            eventLogSessionContext = new EventLogSessionContext(dict);
            eventLogSessionContext.CreateEventLogContainerStartIndexMap();
            eventLogSessionContext.CreateEventLogContainerEndIndexMap();

            Assert.IsTrue(eventLogSessionContext.EventLogContainerStartIndexMap["DummyEventLog"] == 0);
            Assert.IsTrue(eventLogSessionContext.EventLogContainerEndIndexMap["DummyEventLog"] == -1);
        }
    }

    public class DummyEventLogContainer : IEventLogContainer
    {
        public DummyEventLogContainer(bool initialize)
        {
            EventLogEntries = new List<EventLogEntry>(10);
            EventLog eventLog = new("Application");

            if (initialize)
            {
                int currentIndex = eventLog.Entries[eventLog.Entries.Count - 1].Index - eventLog.Entries[0].Index;
                EventLogEntries.Add(eventLog.Entries[currentIndex]);
                EventLogEntries.Add(eventLog.Entries[currentIndex - 1]);
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
