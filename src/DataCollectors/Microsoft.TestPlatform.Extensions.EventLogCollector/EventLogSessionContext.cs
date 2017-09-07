// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Stores the start and end index for EventLogEntries correspoinding to a data collection session.
    /// </summary>
    internal class EventLogSessionContext
    {
        private Dictionary<string, IEventLogContainer> eventLogContainerMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogSessionContext"/> class.
        /// </summary>
        /// <param name="eventLogContainerMap">
        /// Event Log container map.
        /// </param>
        public EventLogSessionContext(Dictionary<string, IEventLogContainer> eventLogContainerMap)
        {
            this.eventLogContainerMap = eventLogContainerMap;
            this.CreateEventLogContainerStartIndexMap();
        }

        /// <summary>
        /// Gets the event log container index map.
        /// </summary>
        internal Dictionary<string, int> EventLogContainerStartIndexMap { get; private set; }

        /// <summary>
        /// Gets the event log container map end index map.
        /// </summary>
        internal Dictionary<string, int> EventLogContainerMapEndIndexMap { get; private set; }

        /// <summary>
        /// The create event log container map end index map.
        /// </summary>
        public void CreateEventLogContainerMapEndIndexMap()
        {
            this.EventLogContainerMapEndIndexMap = new Dictionary<string, int>(this.eventLogContainerMap.Count);

            foreach (KeyValuePair<string, IEventLogContainer> kvp in this.eventLogContainerMap)
            {
                kvp.Value.OnEventLogEntryWritten(kvp.Value.EventLog, null);

                this.EventLogContainerMapEndIndexMap.Add(kvp.Key, kvp.Value.EventLogEntries.Count == 0 ? 0 : kvp.Value.EventLogEntries.Count - 1);
            }
        }

        private void CreateEventLogContainerStartIndexMap()
        {
            this.EventLogContainerStartIndexMap = new Dictionary<string, int>(this.eventLogContainerMap.Count);

            foreach (KeyValuePair<string, IEventLogContainer> kvp in this.eventLogContainerMap)
            {
                this.EventLogContainerStartIndexMap.Add(kvp.Key, kvp.Value.EventLogEntries.Count);
            }
        }
    }
}