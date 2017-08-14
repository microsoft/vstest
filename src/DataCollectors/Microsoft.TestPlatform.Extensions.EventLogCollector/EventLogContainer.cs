// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using MSResources = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;


    internal class EventLogContainer : IEventLogContainer
    {
        /// <inheritdoc />
        public EventLog EventLog { get; set; }

        public int NextEntryIndexToCollect { get; set; }

        public EventLogDataCollector DataCollector { get; set; }

        public EventLogCollectorContextData ContextData { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogContainer"/> class.
        /// </summary>
        /// <param name="eventLog">
        /// The event log.
        /// </param>
        /// <param name="nextEntryIndexToCollect">
        /// The next entry index to collect.
        /// </param>
        /// <param name="dataCollector">
        /// The data collector.
        /// </param>
        /// <param name="contextData">
        /// The context data.
        /// </param>
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

        /// <inheritdoc />
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
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "EventLogDataCollector: OnEventLogEntryWritten: Handling clearing of log (mostRecentIndexInLog < eventLogContainer.NextEntryIndex): firstIndexInLog: {0}:, mostRecentIndexInLog: {1}, NextEntryIndex: {2}",
                                firstIndexInLog,
                                mostRecentIndexInLog,
                                this.NextEntryIndexToCollect));
                    }

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
                            this.ContextData.LimitReached = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
