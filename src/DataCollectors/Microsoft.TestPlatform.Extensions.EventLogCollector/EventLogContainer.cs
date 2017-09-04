// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;

    /// <summary>
    /// The event log container.
    /// </summary>
    internal class EventLogContainer : IEventLogContainer
    {
        private List<string> eventSources;

        private List<EventLogEntryType> entryTypes;

        private DataCollectionLogger logger;

        private DataCollectionContext context;

        private int nextEntryIndexToCollect;

        private EventLogCollectorContextData contextData;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogContainer"/> class.
        /// </summary>
        /// <param name="eventLog">
        /// The event log.
        /// </param>
        /// <param name="nextEntryIndexToCollect">
        /// The next entry index to collect.
        /// </param>
        /// <param name="eventSources">
        /// The event Sources.
        /// </param>
        /// <param name="entryTypes">
        /// The entry Types.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="contextData">
        /// The context data.
        /// </param>
        public EventLogContainer(
            EventLog eventLog,
            int nextEntryIndexToCollect,
            List<string> eventSources,
            List<EventLogEntryType> entryTypes,
            DataCollectionLogger logger,
            DataCollectionContext context,
            EventLogCollectorContextData contextData)
        {
            this.EventLog = eventLog;
            this.nextEntryIndexToCollect = nextEntryIndexToCollect;
            this.contextData = contextData;
            this.eventSources = eventSources;
            this.entryTypes = entryTypes;
            this.context = context;
            this.logger = logger;
        }

        /// <inheritdoc />
        public EventLog EventLog { get; set; }

        /// <summary>
        /// This is the event handler for the EntryWritten event of the System.Diagnostics.EventLog class.
        /// Note that the documentation for the EntryWritten event includes these remarks:
        ///     "The system responds to WriteEntry only if the last write event occurred at least five seconds previously.
        ///      This implies you will only receive one EntryWritten event notification within a five-second interval, even if more
        ///      than one event log change occurs. If you insert a sufficiently long sleep interval (around 10 seconds) between calls
        ///      to WriteEntry, no events will be lost. However, if write events occur more frequently, the most recent write events
        ///      could be lost."
        /// This complicates this data collector because we don't want to sleep to wait for all events or lose the most recent events.
        /// To workaround, the implementation does several things:
        /// 1. We get the EventLog entries to collect from the EventLog.Entries collection and ignore the EntryWrittenEventArgs.
        /// 2. When event log collection ends for a data collection context, this method is called explicitly by the EventLogDataCollector
        ///    passing null for EntryWrittenEventArgs (which is fine since the argument is ignored.
        /// 3. We keep track of which EventLogEntry object in the EventLog.Entries we still need to collect.  We do this by inspecting
        ///    the value of the EventLogEntry.Index property.  The value of this property is an integer that is incremented for each entry
        ///    that is written to the event log, but is reset to 0 if the entire event log is cleared.
        /// Another behavior of event logs that we need to account for is that if the event log reaches a size limit, older events are
        /// automatically deleted.  In this case the collection EventLog.Entries contains only the entries remaining in the log,
        /// and the value of the EventLog.Entries[0].Index will not be 0; it will be the index of the oldest entry still in the log.
        /// For example, if the first 1000 entries written to an event log (since it was last completely cleared) are deleted because
        /// of the size limitation, then EventLog.Entries[0].Index would have a value of 1000 (this value is saved in the local variable
        /// "firstIndexInLog" in the method implementation.  Similarly "mostRecentIndexInLog" is the index of the last entry written
        /// to the log at the time we examine it.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="e">The System.Diagnostics.EntryWrittenEventArgs object describing the entry that was written.</param>
        public void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e)
        {
            while (this.contextData.ProcessEvents)
            {
                int currentCount = this.EventLog.Entries.Count;
                if (currentCount == 0)
                {
                    break;
                }

                int firstIndexInLog = this.EventLog.Entries[0].Index;
                int mostRecentIndexInLog = this.EventLog.Entries[currentCount - 1].Index;

                if (mostRecentIndexInLog == this.nextEntryIndexToCollect - 1)
                {
                    // We've already collected the most recent entry in the log
                    break;
                }

                if (mostRecentIndexInLog < this.nextEntryIndexToCollect - 1)
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "EventLogDataCollector: OnEventLogEntryWritten: Handling clearing of log (mostRecentIndexInLog < eventLogContainer.NextEntryIndex): firstIndexInLog: {0}:, mostRecentIndexInLog: {1}, NextEntryIndex: {2}",
                                firstIndexInLog,
                                mostRecentIndexInLog,
                                this.nextEntryIndexToCollect));
                    }

                    // Send warning; event log must have been cleared.
                    this.logger.LogWarning(
                        this.context,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.DataCollectors_EventLog_EventsLostWarning,
                            this.EventLog.Log));

                    this.nextEntryIndexToCollect = 0;
                    firstIndexInLog = 0;
                }

                for (; this.nextEntryIndexToCollect <= mostRecentIndexInLog; ++this.nextEntryIndexToCollect)
                {
                    int nextEntryIndexInCurrentLog = this.nextEntryIndexToCollect - firstIndexInLog;
                    EventLogEntry nextEntry = this.EventLog.Entries[nextEntryIndexInCurrentLog];

                    // If an explicit list of event sources was provided, only report log entries from those sources
                    if (this.eventSources != null && this.eventSources.Count > 0)
                    {
                        bool eventSourceFound = false;
                        foreach (string eventSource in this.eventSources)
                        {
                            if (string.Equals(nextEntry.Source, eventSource, StringComparison.OrdinalIgnoreCase))
                            {
                                eventSourceFound = true;
                                break;
                            }
                        }

                        if (!eventSourceFound)
                        {
                            continue;
                        }
                    }

                    if (this.entryTypes != null && this.entryTypes.Count > 0)
                    {
                        bool eventTypeFound = false;
                        foreach (EventLogEntryType entryType in this.entryTypes)
                        {
                            if (nextEntry.EntryType == entryType)
                            {
                                eventTypeFound = true;
                                break;
                            }
                        }

                        if (!eventTypeFound)
                        {
                            continue;
                        }
                    }

                    lock (this.contextData.EventLogEntries)
                    {
                        if (this.contextData.EventLogEntries.Count < this.contextData.MaxLogEntries)
                        {
                            this.contextData.EventLogEntries.Add(nextEntry);

                            if (EqtTrace.IsVerboseEnabled)
                            {
                                EqtTrace.Verbose(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        "EventLogDataCollector.OnEventLogEntryWritten() add event with Id {0} from position {1} in the current {2} log",
                                        nextEntry.Index,
                                        nextEntryIndexInCurrentLog,
                                        this.EventLog.Log));
                            }
                        }
                        else
                        {
                            this.contextData.LimitReached = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
