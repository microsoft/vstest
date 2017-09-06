// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using Resource = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;

    /// <summary>
    /// The event log container.
    /// </summary>
    internal class EventLogContainer : IEventLogContainer
    {
        /// <summary>
        /// The event log file name.
        /// </summary>
        private static string eventLogFileName = "Event Log";

        private ISet<string> eventSources;

        private ISet<EventLogEntryType> entryTypes;

        private DataCollectionLogger logger;

        private DataCollectionContext dataCollectionContext;

        private DataCollectionSink dataCollectionSink;

        private bool limitReached;

        private int maxLogEntries;

        private Dictionary<string, EventLog> eventLogMap;

        private IFileHelper fileHelper;

        public EventLogContainer(
            Dictionary<string, EventLog> eventLogMap,
            ISet<string> eventSources,
            ISet<EventLogEntryType> entryTypes,
            DataCollectionLogger logger,
            DataCollectionContext context,
            DataCollectionSink dataCollectionSink,
            int maxLogEntries)
            : this(eventLogMap, eventSources, entryTypes, logger, context, dataCollectionSink, maxLogEntries, new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogContainer"/> class.
        /// </summary>
        /// <param name="eventLogMap">
        /// Event Log Map
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
        /// <param name="dataCollectionSink">
        /// Data Collection Sink for attaching file with Test Results.
        /// </param>
        /// <param name="maxLogEntries">
        /// Max Log Entries
        /// </param>
        /// <param name="fileHelper">
        /// File Helper
        /// </param>
        public EventLogContainer(
            Dictionary<string, EventLog> eventLogMap,
            ISet<string> eventSources,
            ISet<EventLogEntryType> entryTypes,
            DataCollectionLogger logger,
            DataCollectionContext context,
            DataCollectionSink dataCollectionSink,
            int maxLogEntries,
            IFileHelper fileHelper)
        {
            this.eventLogMap = eventLogMap;
            this.eventSources = eventSources;
            this.entryTypes = entryTypes;
            this.dataCollectionContext = context;
            this.dataCollectionSink = dataCollectionSink;
            this.logger = logger;
            this.maxLogEntries = maxLogEntries;
            this.fileHelper = fileHelper;

            this.EventLogIndexMap = new Dictionary<string, int>();
            this.SetCurrentIndexForEventLogs(this.eventLogMap);

            this.EventLogEntries = new List<EventLogEntry>();
        }

        /// <summary>
        /// Gets the max log entries.
        /// </summary>
        public int MaxLogEntries => this.maxLogEntries;

        /// <summary>
        /// Gets or sets the event log entries.
        /// </summary>
        internal List<EventLogEntry> EventLogEntries { get; set; }

        /// <summary>
        /// Gets or sets the event log index map.
        /// </summary>
        internal Dictionary<string, int> EventLogIndexMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether limit reached.
        /// </summary>
        internal bool LimitReached
        {
            get => this.limitReached;

            set => this.limitReached = value;
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
                EventLog eventLog = (EventLog)source;
                string eventLogName = eventLog != null ? eventLog.Log : e.Entry.Source;

                int nextEntryIndexToCollect = this.EventLogIndexMap[eventLogName];

                int currentCount = eventLog.Entries.Count;
                if (currentCount == 0)
                {
                    break;
                }

                int firstIndexInLog = eventLog.Entries[0].Index;
                int mostRecentIndexInLog = eventLog.Entries[currentCount - 1].Index;

                if (mostRecentIndexInLog == nextEntryIndexToCollect - 1)
                {
                    // We've already collected the most recent entry in the log
                    break;
                }

                if (mostRecentIndexInLog < nextEntryIndexToCollect - 1)
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "EventLogDataContainer: OnEventLogEntryWritten: Handling clearing of log (mostRecentIndexInLog < eventLogContainer.NextEntryIndex): firstIndexInLog: {0}:, mostRecentIndexInLog: {1}, NextEntryIndex: {2}",
                                firstIndexInLog,
                                mostRecentIndexInLog,
                                nextEntryIndexToCollect));
                    }

                    // Send warning; event log must have been cleared.
                    this.logger.LogWarning(
                        this.dataCollectionContext,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.EventsLostWarning,
                            eventLog.Log));

                    nextEntryIndexToCollect = 0;
                    firstIndexInLog = 0;
                }

                for (; nextEntryIndexToCollect <= mostRecentIndexInLog; ++nextEntryIndexToCollect)
                {
                    int nextEntryIndexInCurrentLog = nextEntryIndexToCollect - firstIndexInLog;
                    EventLogEntry nextEntry = eventLog.Entries[nextEntryIndexInCurrentLog];

                    // If an explicit list of event sources was provided, only report log entries from those sources
                    if (this.eventSources != null && this.eventSources.Count > 0)
                    {
                        if (!this.eventSources.Contains(nextEntry.Source))
                        {
                            continue;
                        }
                    }

                    if (this.entryTypes != null && this.entryTypes.Count > 0)
                    {
                        if (!this.entryTypes.Contains(nextEntry.EntryType))
                        {
                            continue;
                        }
                    }

                    lock (this.EventLogEntries)
                    {
                        if (this.EventLogEntries.Count < this.maxLogEntries)
                        {
                            this.EventLogEntries.Add(nextEntry);

                            if (EqtTrace.IsVerboseEnabled)
                            {
                                EqtTrace.Verbose(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        "EventLogDataContainer.OnEventLogEntryWritten() add event with Id {0} from position {1} in the current {2} log",
                                        nextEntry.Index,
                                        nextEntryIndexInCurrentLog,
                                        eventLog.Log));
                            }
                        }
                        else
                        {
                            this.LimitReached = true;
                            break;
                        }
                    }
                }

                this.EventLogIndexMap[eventLogName] = nextEntryIndexToCollect;
            }
        }

        /// <inheritdoc />
        public string WriteEventLogs(DataCollectionContext dataCollectionContext, TimeSpan requestedDuration, DateTime timeRequestReceived)
        {
            // Generate a unique but friendly Directory name in the temp directory
            string eventLogDirName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2:yyyy}{2:MM}{2:dd}-{2:HH}{2:mm}{2:ss}.{2:fff}",
                "Event Log",
                Environment.MachineName,
                DateTime.Now);

            string eventLogDirPath = Path.Combine(Path.GetTempPath(), eventLogDirName);

            // Create the directory
            this.fileHelper.CreateDirectory(eventLogDirPath);

            string eventLogBasePath = Path.Combine(eventLogDirPath, eventLogFileName);
            bool unusedFilenameFound = false;

            string eventLogPath = eventLogBasePath + ".xml";

            if (this.fileHelper.Exists(eventLogPath))
            {
                for (int i = 1; !unusedFilenameFound; i++)
                {
                    eventLogPath = eventLogBasePath + "-" + i.ToString(CultureInfo.InvariantCulture) + ".xml";

                    if (!this.fileHelper.Exists(eventLogPath))
                    {
                        unusedFilenameFound = true;
                    }
                }
            }

            DateTime minDate = DateTime.MinValue;

            // Limit entries to a certain time range if requested
            if (requestedDuration < TimeSpan.MaxValue)
            {
                try
                {
                    minDate = timeRequestReceived - requestedDuration;
                }
                catch (ArgumentOutOfRangeException)
                {
                    minDate = DateTime.MinValue;
                }
            }

            // The lock here and in OnEventLogEntryWritten() ensure that all of the events have been processed
            // and added to eventLogContext.EventLogEntries before we try to write them.
            lock (this.EventLogEntries)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
                    eventLogPath,
                    this.EventLogEntries.Where(
                        entry => entry.TimeGenerated > minDate && entry.TimeGenerated < DateTime.MaxValue).ToList(),
                    this.fileHelper);

                stopwatch.Stop();

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "EventLogDataContainer: Wrote {0} event log entries to file '{1}' in {2} seconds",
                            this.EventLogEntries.Count,
                            eventLogPath,
                            stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)));
                }
            }

            // Write the event log file
            FileTransferInformation fileTransferInformation =
                new FileTransferInformation(dataCollectionContext, eventLogPath, true, this.fileHelper);
            this.dataCollectionSink.SendFileAsync(fileTransferInformation);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    "EventLogDataContainer: Event log successfully sent for data collection context '{0}'.",
                    dataCollectionContext.ToString());
            }

            return eventLogPath;
        }

        private void SetCurrentIndexForEventLogs(Dictionary<string, EventLog> eventLogMap)
        {
            foreach (KeyValuePair<string, EventLog> kvp in eventLogMap)
            {
                int currentCount = kvp.Value.Entries.Count;
                int nextEntryIndexToCollect =
                    (currentCount == 0) ? 0 : kvp.Value.Entries[currentCount - 1].Index + 1;
                this.EventLogIndexMap.Add(kvp.Key, nextEntryIndexToCollect);
            }
        }
    }
}
