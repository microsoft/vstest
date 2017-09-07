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
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;

    /// <summary>
    /// The event log container.
    /// </summary>
    internal class EventLogContainer : IEventLogContainer
    {
        private ISet<string> eventSources;

        private ISet<EventLogEntryType> entryTypes;

        private EventLog eventLog;

        private int nextEntryIndexToCollect;

        private int maxLogEntries;

        private DataCollectionLogger dataCollectionLogger;

        private DataCollectionContext dataCollectionContext;

        private bool limitReached;

        private List<EventLogEntry> eventLogEntries;

        /// <summary>
        /// Keeps track of if we are disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogContainer"/> class.
        /// </summary>
        /// <param name="eventLogName">
        /// Event Log Name for which logs has to be collected.
        /// </param>
        /// <param name="eventSources">
        /// The event Sources.
        /// </param>
        /// <param name="entryTypes">
        /// The entry Types.
        /// </param>
        /// <param name="maxLogEntries">
        /// Max entries to store
        /// </param>
        /// <param name="dataCollectionLogger">
        /// Data Collection Logger
        /// </param>
        /// <param name="dataCollectionContext">
        /// Data Collection Context
        /// </param>
        public EventLogContainer(string eventLogName, ISet<string> eventSources, ISet<EventLogEntryType> entryTypes, int maxLogEntries, DataCollectionLogger dataCollectionLogger, DataCollectionContext dataCollectionContext)
        {
            this.CreateEventLog(eventLogName);
            this.eventSources = eventSources;
            this.entryTypes = entryTypes;
            this.maxLogEntries = maxLogEntries;
            this.dataCollectionLogger = dataCollectionLogger;
            this.dataCollectionContext = dataCollectionContext;

            this.eventLogEntries = new List<EventLogEntry>();
        }

        /// <inheritdoc />
        public List<EventLogEntry> EventLogEntries => this.eventLogEntries;

        /// <inheritdoc />
        public EventLog EventLog => this.eventLog;

        internal int NextEntryIndexToCollect
        {
            get => this.nextEntryIndexToCollect;

            set => this.nextEntryIndexToCollect = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether limit reached.
        /// </summary>
        internal bool LimitReached
        {
            get => this.limitReached;

            set => this.limitReached = value;
        }

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
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
        /// <param name="source">Source</param>
        /// <param name="e">The System.Diagnostics.EntryWrittenEventArgs object describing the entry that was written.</param>
        public void OnEventLogEntryWritten(object source, EntryWrittenEventArgs e)
        {
            while (!this.limitReached)
            {
                try
                {
                    lock (this.eventLogEntries)
                    {
                        int currentCount = this.eventLog.Entries.Count;
                        if (currentCount == 0)
                        {
                            break;
                        }

                        int firstIndexInLog = this.eventLog.Entries[0].Index;
                        int mostRecentIndexInLog = this.eventLog.Entries[currentCount - 1].Index;

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
                                        "EventLogDataContainer: OnEventLogEntryWritten: Handling clearing of log (mostRecentIndexInLog < eventLogContainer.NextEntryIndex): firstIndexInLog: {0}:, mostRecentIndexInLog: {1}, NextEntryIndex: {2}",
                                        firstIndexInLog,
                                        mostRecentIndexInLog,
                                        this.nextEntryIndexToCollect));
                            }

                            // Send warning; event log must have been cleared.
                            this.dataCollectionLogger.LogWarning(
                                this.dataCollectionContext,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    Resource.EventsLostWarning,
                                    this.eventLog.Log));

                            this.nextEntryIndexToCollect = 0;
                            firstIndexInLog = 0;
                        }

                        for (;
                            this.nextEntryIndexToCollect <= mostRecentIndexInLog;
                            this.nextEntryIndexToCollect = this.nextEntryIndexToCollect + 1)
                        {
                            int nextEntryIndexInCurrentLog = this.nextEntryIndexToCollect - firstIndexInLog;
                            EventLogEntry nextEntry = this.eventLog.Entries[nextEntryIndexInCurrentLog];

                            // If an explicit list of event sources was provided, only report log entries from those sources
                            if (this.eventSources != null && this.eventSources.Count > 0)
                            {
                                if (!this.eventSources.Contains(nextEntry.Source))
                                {
                                    continue;
                                }
                            }

                            if (!this.entryTypes.Contains(nextEntry.EntryType))
                            {
                                continue;
                            }

                            if (this.eventLogEntries.Count < this.maxLogEntries)
                            {
                                this.eventLogEntries.Add(nextEntry);

                                if (EqtTrace.IsVerboseEnabled)
                                {
                                    EqtTrace.Verbose(
                                        string.Format(
                                            CultureInfo.InvariantCulture,
                                            "EventLogDataContainer.OnEventLogEntryWritten() add event with Id {0} from position {1} in the current {2} log",
                                            nextEntry.Index,
                                            nextEntryIndexInCurrentLog,
                                            this.eventLog.Log));
                                }
                            }
                            else
                            {
                                this.LimitReached = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // todo : log exception
                }
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.eventLog.EnableRaisingEvents = false;
                    this.eventLog.EntryWritten -= this.OnEventLogEntryWritten;
                    this.eventLog.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private void CreateEventLog(string eventLogName)
        {
            this.eventLog = new EventLog(eventLogName);
            this.eventLog.EnableRaisingEvents = true;
            this.eventLog.EntryWritten += this.OnEventLogEntryWritten;
            int currentCount = this.eventLog.Entries.Count;
            this.nextEntryIndexToCollect =
                (currentCount == 0) ? 0 : this.eventLog.Entries[currentCount - 1].Index + 1;
        }
    }
}
