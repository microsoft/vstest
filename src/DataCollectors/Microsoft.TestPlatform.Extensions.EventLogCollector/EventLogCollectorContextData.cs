// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// The event log collector context data.
    /// </summary>
    internal class EventLogCollectorContextData
    {
        private bool limitReached;

        private Dictionary<string, IEventLogContainer> eventLogContainers;

        private List<EventLogEntry> eventLogEntries;

        private int maxLogEntries;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogCollectorContextData"/> class.
        /// </summary>
        /// <param name="maxLogEntries">
        /// The max log entries.
        /// </param>
        public EventLogCollectorContextData(int maxLogEntries)
        {
            this.maxLogEntries = maxLogEntries;
            this.eventLogContainers = new Dictionary<string, IEventLogContainer>();
            this.eventLogEntries = new List<EventLogEntry>();
        }

        /// <summary>
        /// Gets a value indicating whether to process events or not.
        /// </summary>
        public bool ProcessEvents => !this.limitReached;

        /// <summary>
        /// Gets the event log containers.
        /// </summary>
        public Dictionary<string, IEventLogContainer> EventLogContainers => this.eventLogContainers;

        /// <summary>
        /// Gets the event log entries.
        /// </summary>
        public List<EventLogEntry> EventLogEntries => this.eventLogEntries;

        /// <summary>
        /// Gets the max log entries.
        /// </summary>
        public int MaxLogEntries => this.maxLogEntries;

        /// <summary>
        /// Gets or sets a value indicating whether limit reached.
        /// </summary>
        internal bool LimitReached
        {
            get => this.limitReached;

            set => this.limitReached = value;
        }
    }
}
