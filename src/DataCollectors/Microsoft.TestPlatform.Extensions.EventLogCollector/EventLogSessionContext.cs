// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores the start and end index for EventLogEntries correspoinding to a data collection session.
    /// </summary>
    internal class EventLogSessionContext
    {
        private IDictionary<string, IEventLogContainer> eventLogContainerMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogSessionContext"/> class.
        /// </summary>
        /// <param name="eventLogContainerMap">
        /// Event Log container map.
        /// </param>
        public EventLogSessionContext(IDictionary<string, IEventLogContainer> eventLogContainerMap)
        {
            this.eventLogContainerMap = eventLogContainerMap;
            this.CreateEventLogContainerStartIndexMap();
        }

        /// <summary>
        /// Gets the start index for EventLogs Entries.
        /// </summary>
        internal Dictionary<string, int> EventLogContainerStartIndexMap { get; private set; }

        /// <summary>
        /// Gets the end index for EventLogs Entries
        /// </summary>
        internal Dictionary<string, int> EventLogContainerEndIndexMap { get; private set; }

        /// <summary>
        /// Creates the end index map for EventLogs Entries
        /// </summary>
        public void CreateEventLogContainerEndIndexMap()
        {
            this.EventLogContainerEndIndexMap = new Dictionary<string, int>(this.eventLogContainerMap.Count);

            foreach (KeyValuePair<string, IEventLogContainer> kvp in this.eventLogContainerMap)
            {
                kvp.Value.OnEventLogEntryWritten(kvp.Value.EventLog, null);

                this.EventLogContainerEndIndexMap.Add(kvp.Key, kvp.Value.EventLogEntries.Count == 0 ? 0 : kvp.Value.EventLogEntries.Count - 1);
            }
        }

        /// <summary>
        /// Creates the start index map for EventLogs Entries
        /// </summary>
        public void CreateEventLogContainerStartIndexMap()
        {
            this.EventLogContainerStartIndexMap = new Dictionary<string, int>(this.eventLogContainerMap.Count);

            foreach (KeyValuePair<string, IEventLogContainer> kvp in this.eventLogContainerMap)
            {
                this.EventLogContainerStartIndexMap.Add(kvp.Key, kvp.Value.EventLogEntries.Count == 0 ? 0 : kvp.Value.EventLogEntries.Count - 1);
            }
        }
    }
}