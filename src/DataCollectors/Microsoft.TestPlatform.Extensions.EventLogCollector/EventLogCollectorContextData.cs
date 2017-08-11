// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class EventLogCollectorContextData
    {
        private bool limitReached;

        private Dictionary<string, EventLogContainer> eventLogContainers;

        private List<EventLogEntry> eventLogEntries;

        private int maxLogEntries;

        public bool ProcessEvents
        {
            get
            {
                return !this.limitReached;
            }
        }

        public Dictionary<string, EventLogContainer> EventLogContainers
        {
            get
            {
                return this.eventLogContainers;
            }
        }

        public List<EventLogEntry> EventLogEntries
        {
            get
            {
                return this.eventLogEntries;
            }
        }

        public int MaxLogEntries
        {
            get
            {
                return this.maxLogEntries;
            }
        }

        public EventLogCollectorContextData(int maxLogEntries)
        {
            this.maxLogEntries = maxLogEntries;
            this.eventLogContainers = new Dictionary<string, EventLogContainer>();
            this.eventLogEntries = new List<EventLogEntry>();
        }

        internal bool LimitReached
        {
            get
            {
                return this.limitReached;
            }

            set
            {
                this.limitReached = value;
            }
        }
    }
}
