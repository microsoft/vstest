// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestTools.DataCollection.EventLog
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.EventLogCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.TestPlatform.Extensions.EventLogCollector.Resources;

    /// <summary>
    /// A data collector that collects event log data
    /// </summary>
    [DataCollectorTypeUri(DEFAULT_URI)]
    [DataCollectorFriendlyName("Event Log")]
    public class EventLogDataCollector : DataCollector
    {
        #region Constants

        /// <summary>
        /// DataCollector URI.
        /// </summary>
        private const string DEFAULT_URI = @"datacollector://Microsoft/EventLog/1.0";

        #endregion

        #region Private fields

        /// <summary>
        /// The event log file name.
        /// </summary>
        private static string eventLogFileName = "Event Log";

        /// <summary>
        /// The event log directories.
        /// </summary>
        private List<string> eventLogDirectories;

        /// <summary>
        /// Object containing the execution events the data collector registers for
        /// </summary>
        private DataCollectionEvents events;

        /// <summary>
        /// The sink used by the data collector to send its data
        /// </summary>
        private DataCollectionSink dataSink;

        /// <summary>
        /// Used by the data collector to send warnings, errors, or other messages
        /// </summary>
        private DataCollectionLogger logger;

        /// <summary>
        /// Event handler delegate for the SessionStart event
        /// </summary>
        private readonly EventHandler<SessionStartEventArgs> sessionStartEventHandler;

        /// <summary>
        /// Event handler delegate for the SessionEnd event
        /// </summary>
        private readonly EventHandler<SessionEndEventArgs> sessionEndEventHandler;

        /// <summary>
        /// Event handler delegate for the TestCaseStart event
        /// </summary>
        private readonly EventHandler<TestCaseStartEventArgs> testCaseStartEventHandler;

        /// <summary>
        /// Event handler delegate for the TestCaseEnd event
        /// </summary>
        private readonly EventHandler<TestCaseEndEventArgs> testCaseEndEventHandler;

        private List<string> eventLogNames;

        private List<string> eventSources;

        private List<EventLogEntryType> entryTypes;

        private int maxEntries;

        private bool collectForInnerTests;

        private Dictionary<DataCollectionContext, EventLogCollectorContextData> contextData =
            new Dictionary<DataCollectionContext, EventLogCollectorContextData>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogDataCollector"/> class. 
        /// </summary>
        public EventLogDataCollector()
        {
            this.sessionStartEventHandler = new EventHandler<SessionStartEventArgs>(this.OnSessionStart);
            this.sessionEndEventHandler = new EventHandler<SessionEndEventArgs>(this.OnSessionEnd);
            this.testCaseStartEventHandler = new EventHandler<TestCaseStartEventArgs>(this.OnTestCaseStart);
            this.testCaseEndEventHandler = new EventHandler<TestCaseEndEventArgs>(this.OnTestCaseEnd);

            // todo: dataRequestEventHandler = new EventHandler<DataRequestEventArgs>(OnDataRequest);
            this.eventLogDirectories = new List<string>();
        }

        #endregion

        #region DataCollector Members

        /// <summary>
        /// Initializes the data collector
        /// </summary>
        /// <param name="configurationElement">
        /// The XML element containing configuration information for the data collector. Currently,
        /// this data collector does not have any configuration, so we ignore this parameter.
        /// </param>
        /// <param name="events">
        /// Object containing the execution events the data collector registers for
        /// </param>
        /// <param name="dataSink">The sink used by the data collector to send its data</param>
        /// <param name="logger">
        /// Used by the data collector to send warnings, errors, or other messages
        /// </param>
        /// <param name="dataCollectionEnvironmentContext">Provides contextual information about the agent environment</param>
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext dataCollectionEnvironmentContext)
        {
            System.Diagnostics.Debugger.Launch();
            Debug.Assert(events != null, "'events' is null");
            Debug.Assert(dataSink != null, "'dataSink' is null");
            Debug.Assert(logger != null, "'logger' is null");

            this.events = events;
            this.dataSink = dataSink;
            this.logger = logger;

            // Load the configuration
            CollectorNameValueConfigurationManager nameValueSettings =
                new CollectorNameValueConfigurationManager(configurationElement);

            // Apply the configuration
            this.eventLogNames = new List<string>();
            string eventLogs = nameValueSettings[EventLogShared.SETTING_EVENT_LOGS];
            if (eventLogs != null)
            {
                this.eventLogNames = ParseCommaSeparatedList(eventLogs);
                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_EVENT_LOGS + "=" + eventLogs);
            }
            else
            {
                // Default to collecting these standard logs
                this.eventLogNames.Add("System");
                // this.eventLogNames.Add("Security");
                this.eventLogNames.Add("Application");
            }

            string eventSourcesStr = nameValueSettings[EventLogShared.SETTING_EVENT_SOURCES];
            if (!string.IsNullOrEmpty(eventSourcesStr))
            {
                this.eventSources = ParseCommaSeparatedList(eventSourcesStr);
                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_EVENT_SOURCES + "="
                    + this.eventSources);
            }

            this.entryTypes = new List<EventLogEntryType>();
            string entryTypesStr = nameValueSettings[EventLogShared.SETTING_ENTRY_TYPES];
            if (entryTypesStr != null)
            {
                foreach (string entryTypestring in ParseCommaSeparatedList(entryTypesStr))
                {
                    try
                    {
                        this.entryTypes.Add(
                            (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), entryTypestring, true));
                    }
                    catch (ArgumentException e)
                    {
                        throw new EventLogCollectorException(
                            "",
                            e);
                    }
                }

                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_ENTRY_TYPES + "=" + this.entryTypes);
            }
            else
            {
                this.entryTypes.Add(EventLogEntryType.Error);
                this.entryTypes.Add(EventLogEntryType.Warning);
                this.entryTypes.Add(EventLogEntryType.FailureAudit);
            }

            string maxEntriesstring = nameValueSettings[EventLogShared.SETTING_MAX_ENTRIES];
            if (maxEntriesstring != null)
            {
                try
                {
                    this.maxEntries = int.Parse(maxEntriesstring, CultureInfo.InvariantCulture);

                    // A negative or 0 value means no maximum
                    if (this.maxEntries <= 0)
                    {
                        this.maxEntries = int.MaxValue;
                    }
                }
                catch (FormatException e)
                {
                    throw new EventLogCollectorException(
                        "",
                        e);
                }

                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_MAX_ENTRIES + "="
                    + maxEntriesstring);
            }
            else
            {
                this.maxEntries = EventLogShared.DEFAULT_MAX_ENTRIES;
            }

            this.collectForInnerTests = GetBoolConfigSetting(
                nameValueSettings,
                EventLogShared.SETTING_COLLECT_FOR_INNER_TESTS,
                EventLogShared.DEFAULT_COLLECT_FOR_INNER_TESTS);

            // Register for events
            events.SessionStart += this.sessionStartEventHandler;
            events.SessionEnd += this.sessionEndEventHandler;
            events.TestCaseStart += this.testCaseStartEventHandler;
            events.TestCaseEnd += this.testCaseEndEventHandler;
        }

        private static bool GetBoolConfigSetting(
            CollectorNameValueConfigurationManager nameValueSettings,
            string settingName,
            bool defaultValue)
        {
            bool settingValue;
            string settingValuestring = nameValueSettings[settingName];
            if (settingValuestring != null)
            {
                try
                {
                    settingValue = bool.Parse(settingValuestring);
                }
                catch (FormatException ex)
                {
                    throw new EventLogCollectorException(
                        "",
                        ex);
                }

                EqtTrace.Verbose("EventLogDataCollector configuration: " + settingName + "=" + settingValuestring);
            }
            else
            {
                settingValue = defaultValue;
            }

            return settingValue;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Cleans up resources allocated by the data collector
        /// </summary>
        /// <param name="disposing">Not used since this class does not have a finalizer.</param>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "The real Dispose method is in the base class and FxCop doesn't seem to find it.")]
        protected override void Dispose(bool disposing)
        {
            // Unregister events
            this.events.SessionStart -= this.sessionStartEventHandler;
            this.events.SessionEnd -= this.sessionEndEventHandler;
            this.events.TestCaseStart -= this.testCaseStartEventHandler;
            this.events.TestCaseEnd -= this.testCaseEndEventHandler;

            // Delete all the temp event log directories
            this.RemoveTempEventLogDirs(this.eventLogDirectories);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Event Handlers

        private void OnSessionStart(object sender, SessionStartEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            EqtTrace.Verbose("EventLogDataCollector: SessionStart received");
            this.StartCollectionForContext(e.Context, true);
        }

        private void OnSessionEnd(object sender, SessionEndEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            EqtTrace.Verbose("EventLogDataCollector: SessionEnd received");
            this.WriteCollectedEventLogEntries(e.Context, true, TimeSpan.MaxValue, DateTime.Now);
        }

        private void OnTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            if (!e.Context.HasTestCase)
            {
                Debug.Fail("Context is not for a test case");
                throw new ArgumentNullException("e");
            }

            EqtTrace.Verbose(
                "EventLogDataCollector: TestCaseStart received for {0} test '{1}'.",
                e.IsChildTestCase ? "child" : "parent",
                e.TestCaseName);

            if (this.collectForInnerTests || !e.IsChildTestCase)
            {
                this.StartCollectionForContext(e.Context, false);
            }
        }

        private void OnDataRequest(object sender, DataRequestEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            Debug.Assert(e.Context != null, "Context is null");

            EqtTrace.Verbose("EventLogDataCollector: DataRequest received");

            this.WriteCollectedEventLogEntries(e.Context, false, e.RequestedDuration, DateTime.Now);
        }

        private void OnTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            Debug.Assert(e.Context != null, "Context is null");
            Debug.Assert(e.Context.HasTestCase, "Context is not for a test case");

            EqtTrace.Verbose(
                "EventLogDataCollector: TestCaseEnd received for {0} test '{1}' with Test Outcome: {2}.",
                e.IsChildTestCase ? "child" : "parent",
                e.TestCaseName,
                e.TestOutcome);

            if (this.collectForInnerTests || !e.IsChildTestCase)
            {
                this.WriteCollectedEventLogEntries(e.Context, true, TimeSpan.MaxValue, DateTime.Now);
            }
        }

        #endregion

        #region Private methods

        private void RemoveTempEventLogDirs(List<string> tempDirs)
        {
            if (tempDirs != null)
            {
                foreach (string dir in tempDirs)
                {
                    // Delete only if the directory is empty
                    this.DeleteEmptyDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Helper for deleting a directory. It deletes the directory only if its empty.
        /// </summary>
        /// <param name="dirPath">Path of the directory to be deleted</param>
        private void DeleteEmptyDirectory(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath) && Directory.GetFiles(dirPath).Length == 0
                    && Directory.GetDirectories(dirPath).Length == 0)
                {
                    Directory.Delete(dirPath, true);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Warning(
                    "Error occurred while trying to delete the temporary event log directory {0} :{1}",
                    dirPath,
                    ex);
            }
        }

        private static List<string> ParseCommaSeparatedList(string commaSeparatedList)
        {
            List<string> strings = new List<string>();
            string[] items = commaSeparatedList.Split(new char[] { ',' });
            foreach (string item in items)
            {
                strings.Add(item.Trim());
            }

            return strings;
        }

        private void StartCollectionForContext(DataCollectionContext dataCollectionContext, bool isSessionContext)
        {
            EventLogCollectorContextData eventLogContext;
            lock (this.contextData)
            {
                if (this.contextData.TryGetValue(dataCollectionContext, out eventLogContext))
                {
                    Debug.Fail("Context data already in dictionary");
                }
                else
                {
                    eventLogContext =
                        new EventLogCollectorContextData(isSessionContext ? int.MaxValue : this.maxEntries);
                    this.contextData.Add(dataCollectionContext, eventLogContext);
                }
            }

            foreach (string eventLogName in this.eventLogNames)
            {
                try
                {
                    // Create an EventLog object and add it to the eventLogContext if one does not already exist
                    if (!eventLogContext.EventLogContainers.ContainsKey(eventLogName))
                    {
                        // Specifying machine name remaps a local path to a network share path format in 
                        // System.Diagnostics.EventLogInternal.FormatMessageWrapper for the parameter passed to LoadLibraryEx().
                        // In ARM machines, the network restrictions causes the access to the path to be blocked and  
                        // fails after a delay of 8-9 seconds. This causes perf issues in the CreateBug scenario for Image ActionLog.
                        EventLog eventLog = new EventLog(eventLogName);

                        int currentCount = eventLog.Entries.Count;
                        int nextEntryIndexToCollect =
                            (currentCount == 0) ? 0 : eventLog.Entries[currentCount - 1].Index + 1;
                        EventLogContainer eventLogContainer =
                            new EventLogContainer(eventLog, nextEntryIndexToCollect, this, eventLogContext);

                        eventLog.EntryWritten += new EntryWrittenEventHandler(eventLogContainer.OnEventLogEntryWritten);
                        eventLog.EnableRaisingEvents = true;

                        eventLogContext.EventLogContainers.Add(eventLogName, eventLogContainer);
                        EqtTrace.Verbose(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "EventLogDataCollector: Enabling collection of '{0}' events for data collection context '{1}'",
                                eventLogName,
                                dataCollectionContext.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        dataCollectionContext,
                        new EventLogCollectorException(
                            "",
                            ex));
                }
            }
        }

        private void WriteCollectedEventLogEntries(
            DataCollectionContext dataCollectionContext,
            bool terminateCollectionForContext,
            TimeSpan requestedDuration,
            DateTime timeRequestRecieved)
        {
            DateTime minDate = DateTime.MinValue;
            EventLogCollectorContextData eventLogContext = this.GetEventLogContext(dataCollectionContext);

            if (terminateCollectionForContext)
            {
                foreach (EventLogContainer eventLogContainer in eventLogContext.EventLogContainers.Values)
                {
                    try
                    {
                        eventLogContainer.EventLog.EntryWritten -=
                            new EntryWrittenEventHandler(eventLogContainer.OnEventLogEntryWritten);
                        eventLogContainer.EventLog.EnableRaisingEvents = false;
                        eventLogContainer.OnEventLogEntryWritten(eventLogContainer.EventLog, null);
                        eventLogContainer.EventLog.Dispose();
                    }
                    catch (Exception e)
                    {
                        this.logger.LogWarning(
                            dataCollectionContext,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Execution_Agent_DataCollectors_EventLog_CleanupException,
                                eventLogContainer.EventLog,
                                e.ToString()));
                    }
                }
            }

            // Generate a unique but friendly Directory name in the temp directory
            string eventLogDirName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2:yyyy}{2:MM}{2:dd}-{2:HH}{2:mm}{2:ss}.{2:fff}",
                Resources.Execution_Agent_DataCollectors_EventLog_FriendlyName,
                Environment.MachineName,
                DateTime.Now);

            string eventLogFilename = EventLogFileName;
            string eventLogDirPath = Path.Combine(Path.GetTempPath(), eventLogDirName);

            // Create the directory            
            Directory.CreateDirectory(eventLogDirPath);

            // Add the directory to the list 
            this.eventLogDirectories.Add(eventLogDirPath);

            string eventLogBasePath = Path.Combine(eventLogDirPath, eventLogFilename);
            bool unusedFilenameFound = false;

            string eventLogPath = eventLogBasePath + ".xml";

            if (File.Exists(eventLogPath))
            {
                for (int i = 1; !unusedFilenameFound; i++)
                {
                    eventLogPath = eventLogBasePath + "-" + i.ToString(CultureInfo.InvariantCulture) + ".xml";

                    if (!File.Exists(eventLogPath))
                    {
                        unusedFilenameFound = true;
                    }
                }
            }

            // Limit entries to a certain time range if requested
            if (requestedDuration < TimeSpan.MaxValue)
            {
                try
                {
                    minDate = timeRequestRecieved - requestedDuration;
                }
                catch (ArgumentOutOfRangeException)
                {
                    minDate = DateTime.MinValue;
                }
            }

            // The lock here and in OnEventLogEntryWritten() ensure that all of the events have been processed 
            // and added to eventLogContext.EventLogEntries before we try to write them.
            lock (eventLogContext.EventLogEntries)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
                    eventLogPath,
                    eventLogContext.EventLogEntries,
                    minDate,
                    DateTime.MaxValue);

                stopwatch.Stop();
                EqtTrace.Verbose(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "EventLogDataCollector: Wrote {0} event log entries to file '{1}' in {2} seconds",
                        eventLogContext.EventLogEntries.Count,
                        eventLogPath,
                        stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)));
            }

            // Write the event log file
            this.dataSink.SendFileAsync(dataCollectionContext, eventLogPath, true);

            EqtTrace.Verbose(
                "EventLogDataCollector: Event log successfully sent for data collection context '{0}'.",
                dataCollectionContext.ToString());

            if (terminateCollectionForContext)
            {
                lock (this.contextData)
                {
                    this.contextData.Remove(dataCollectionContext);
                }
            }
        }

        private EventLogCollectorContextData GetEventLogContext(DataCollectionContext dataCollectionContext)
        {
            EventLogCollectorContextData eventLogContext;
            bool eventLogContextFound;
            lock (this.contextData)
            {
                eventLogContextFound = this.contextData.TryGetValue(dataCollectionContext, out eventLogContext);
            }

            if (!eventLogContextFound)
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Execution_Agent_DataCollectors_EventLog_ContextNotFoundException,
                    dataCollectionContext.ToString());
                throw new EventLogCollectorException(msg, null);
            }

            return eventLogContext;
        }

        #endregion       

        #region Internal Fields

        internal static string EventLogFileName
        {
            get
            {
                return eventLogFileName;
            }
        }

        internal static string FriendlyName
        {
            get
            {
                return Resources.Execution_Agent_DataCollectors_EventLog_FriendlyName;
            }
        }

        internal static string Uri
        {
            get
            {
                return DEFAULT_URI;
            }
        }

        internal DataCollectionLogger Logger
        {
            get
            {
                return this.logger;
            }
        }

        internal int MaxEntries
        {
            get
            {
                return this.maxEntries;
            }
        }

        internal List<string> EventSources
        {
            get
            {
                return this.eventSources;
            }
        }

        internal List<EventLogEntryType> EntryTypes
        {
            get
            {
                return this.entryTypes;
            }
        }

        internal Dictionary<DataCollectionContext, EventLogCollectorContextData> ContextData
        {
            get
            {
                return this.contextData;
            }
        }

        #endregion

        internal class EventLogContainer
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
                                    Resources.Execution_Agent_DataCollectors_EventLog_EventsLostWarning,
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

    /// <summary>
    /// Private Exception class used for event log exceptions
    /// </summary>
    [Serializable]
    internal class EventLogCollectorException : Exception
    {
        /// <summary>
        /// Constructs a new EventLogCollectorException
        /// </summary>
        /// <param name="localizedMessage">the localized exception message</param>
        /// <param name="innerException">the inner exception</param>
        public EventLogCollectorException(string localizedMessage, Exception innerException)
            : base(localizedMessage, innerException)
        {
        }

        protected EventLogCollectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
