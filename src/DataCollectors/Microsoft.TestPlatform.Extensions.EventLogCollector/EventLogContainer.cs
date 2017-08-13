// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using MSResources = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;


    internal class EventLogContainer : IEventLogContainer
    {
        public EventLog EventLog { get; set; }

        public int NextEntryIndexToCollect { get; set; }

        public EventLogDataCollector DataCollector { get; set; }

        public EventLogCollectorContextData ContextData { get; set; }

        public EventLogContainer(
            EventLog eventLog,
            int nextEntryIndexToCollect,
            EventLogDataCollector dataCollector,
            EventLogCollectorContextData contextData)
        {
            this.EventLog = eventLog;
            this.NextEntryIndexToCollect = nextEntryIndexToCollect;
            this.DataCollector = dataCollector;
            this.ContextData = contextData;
        }

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
        /// <param name="source"></param>
        /// <param name="e">The System.Diagnostics.EntryWrittenEventArgs object describing the entry that was written.</param>
        public void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e)
        {
            while (this.ContextData.ProcessEvents)
            {
                int currentCount = this.EventLog.Entries.Count;
                if (currentCount == 0)
                {
                    break;
                }

                int firstIndexInLog = this.EventLog.Entries[0].Index;
                int mostRecentIndexInLog = this.EventLog.Entries[currentCount - 1].Index;

                if (mostRecentIndexInLog == this.NextEntryIndexToCollect - 1)
                {
                    // We've already collected the most recent entry in the log
                    break;
                }

                if (mostRecentIndexInLog < this.NextEntryIndexToCollect - 1)
                {
                    /* Uncomment for debugging
                    EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture,
                        "EventLogDataCollector: OnEventLogEntryWritten: Handling clearing of log (mostRecentIndexInLog < eventLogContainer.NextEntryIndex): firstIndexInLog: {0}:, mostRecentIndexInLog: {1}, NextEntryIndex: {2}",
                        firstIndexInLog, mostRecentIndexInLog, NextEntryIndexToCollect));
                    */

                    // Send warning; event log must have been cleared.
                    foreach (DataCollectionContext collectionContext in this.DataCollector.ContextData.Keys)
                    {
                        this.DataCollector.Logger.LogWarning(
                            collectionContext,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                MSResources.Execution_Agent_DataCollectors_EventLog_EventsLostWarning,
                                this.EventLog.Log));
                        break;
                    }

                    this.NextEntryIndexToCollect = 0;
                    firstIndexInLog = 0;
                }

                for (; this.NextEntryIndexToCollect <= mostRecentIndexInLog; ++this.NextEntryIndexToCollect)
                {
                    int nextEntryIndexInCurrentLog = this.NextEntryIndexToCollect - firstIndexInLog;
                    EventLogEntry nextEntry = this.EventLog.Entries[nextEntryIndexInCurrentLog];

                    // BILLBAR_TODO: Event sources can no longer be configured in the Test Settings Config UI (only by XML editor)
                    //     Drop this feature, add to config UI, or leave as only configurable via XML editor?

                    // If an explicit list of event sources was provided, only report log entries from those sources
                    if (this.DataCollector.EventSources != null && this.DataCollector.EventSources.Count > 0)
                    {
                        bool eventSourceFound = false;
                        foreach (string eventSource in this.DataCollector.EventSources)
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

                    if (this.DataCollector.EntryTypes != null && this.DataCollector.EntryTypes.Count > 0)
                    {
                        bool eventTypeFound = false;
                        foreach (EventLogEntryType entryType in this.DataCollector.EntryTypes)
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

                    lock (this.ContextData.EventLogEntries)
                    {
                        if (this.ContextData.EventLogEntries.Count < this.ContextData.MaxLogEntries)
                        {
                            this.ContextData.EventLogEntries.Add(nextEntry);
                            /* Uncomment for debugging
                            EqtTrace.Verbose(string.Format(CultureInfo.InvariantCulture,
                                "EventLogDataCollector.OnEventLogEntryWritten() add event with Id {0} from position {1} in the current {2} log",
                                nextEntry.Index, nextEntryIndexInCurrentLog, EventLog.Log));
                            */
                        }
                        else
                        {
                            this.ContextData.LimitReached = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
