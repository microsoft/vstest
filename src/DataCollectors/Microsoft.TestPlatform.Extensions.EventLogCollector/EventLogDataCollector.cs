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
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using Resource = Resources.Resources;

    /// <summary>
    /// A data collector that collects event log data
    /// </summary>
    [DataCollectorTypeUri(DefaultUri)]
    [DataCollectorFriendlyName("Event Log")]
    public class EventLogDataCollector : DataCollector
    {
        #region Constants

        /// <summary>
        /// The event log file name.
        /// </summary>
        private const string EventLogFileName = "Event Log";

        /// <summary>
        /// DataCollector URI.
        /// </summary>
        private const string DefaultUri = @"datacollector://Microsoft/EventLog/2.0";

        #endregion

        #region Private fields

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

        /// <summary>
        /// The event log directories.
        /// </summary>
        private readonly List<string> eventLogDirectories;

        /// <summary>
        /// Object containing the execution events the data collector registers for
        /// </summary>
        private DataCollectionEvents events;

        /// <summary>
        /// The sink used by the data collector to send its data
        /// </summary>
        private DataCollectionSink dataSink;

        /// <summary>
        /// The data collector context.
        /// </summary>
        private DataCollectionContext dataCollectorContext;

        /// <summary>
        /// Used by the data collector to send warnings, errors, or other messages
        /// </summary>
        private DataCollectionLogger logger;

        /// <summary>
        /// The event log names.
        /// </summary>
        private ISet<string> eventLogNames;

        /// <summary>
        /// The event sources.
        /// </summary>
        private ISet<string> eventSources;

        /// <summary>
        /// The entry types.
        /// </summary>
        private ISet<EventLogEntryType> entryTypes;

        /// <summary>
        /// The max entries.
        /// </summary>
        private int maxEntries;

        /// <summary>
        /// The file helper.
        /// </summary>
        private IFileHelper fileHelper;

        /// <summary>
        /// The event log map.
        /// </summary>
        private IDictionary<string, IEventLogContainer> eventLogContainerMap = new Dictionary<string, IEventLogContainer>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogDataCollector"/> class.
        /// </summary>
        public EventLogDataCollector()
            : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogDataCollector"/> class.
        /// </summary>
        /// <param name="fileHelper">
        /// File Helper.
        /// </param>
        internal EventLogDataCollector(IFileHelper fileHelper)
        {
            this.sessionStartEventHandler = this.OnSessionStart;
            this.sessionEndEventHandler = this.OnSessionEnd;
            this.testCaseStartEventHandler = this.OnTestCaseStart;
            this.testCaseEndEventHandler = this.OnTestCaseEnd;

            this.eventLogDirectories = new List<string>();
            this.ContextMap = new Dictionary<DataCollectionContext, EventLogSessionContext>();
            this.fileHelper = fileHelper;
        }

        #endregion

        #region Internal Fields

        internal int MaxEntries
        {
            get
            {
                return this.maxEntries;
            }
        }

        internal ISet<string> EventSources
        {
            get
            {
                return this.eventSources;
            }
        }

        internal ISet<EventLogEntryType> EntryTypes
        {
            get
            {
                return this.entryTypes;
            }
        }

        internal ISet<string> EventLogNames
        {
            get
            {
                return this.eventLogNames;
            }
        }

        /// <summary>
        /// Gets the context data.
        /// </summary>
        internal Dictionary<DataCollectionContext, EventLogSessionContext> ContextMap { get; private set; }

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
            ValidateArg.NotNull(events, nameof(events));
            ValidateArg.NotNull(dataSink, nameof(dataSink));
            ValidateArg.NotNull(logger, nameof(logger));

            this.events = events;
            this.dataSink = dataSink;
            this.logger = logger;
            this.dataCollectorContext = dataCollectionEnvironmentContext.SessionDataCollectionContext;

            // Load the configuration
            CollectorNameValueConfigurationManager nameValueSettings =
                new CollectorNameValueConfigurationManager(configurationElement);

            // Apply the configuration
            this.ConfigureEventSources(nameValueSettings);
            this.ConfigureEntryTypes(nameValueSettings);
            this.ConfigureMaxEntries(nameValueSettings);
            this.ConfigureEventLogNames(nameValueSettings);

            // Register for events
            events.SessionStart += this.sessionStartEventHandler;
            events.SessionEnd += this.sessionEndEventHandler;
            events.TestCaseStart += this.testCaseStartEventHandler;
            events.TestCaseEnd += this.testCaseEndEventHandler;
        }

        #endregion

        #region Internal

        /// <summary>
        /// The write event logs.
        /// </summary>
        /// <param name="eventLogEntries">
        /// The event log entries.
        /// </param>
        /// <param name="maxLogEntries">
        /// Max Log Entries.
        /// </param>
        /// <param name="dataCollectionContext">
        /// The data collection context.
        /// </param>
        /// <param name="requestedDuration">
        /// The requested duration.
        /// </param>
        /// <param name="timeRequestReceived">
        /// The time request received.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        internal string WriteEventLogs(List<EventLogEntry> eventLogEntries, int maxLogEntries, DataCollectionContext dataCollectionContext, TimeSpan requestedDuration, DateTime timeRequestReceived)
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

            string eventLogBasePath = Path.Combine(eventLogDirPath, EventLogFileName);
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

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
                eventLogPath,
                eventLogEntries.Where(
                    entry => entry.TimeGenerated > minDate && entry.TimeGenerated < DateTime.MaxValue).OrderBy(x => x.TimeGenerated).ToList().Take(maxLogEntries).ToList(),
                this.fileHelper);

            stopwatch.Stop();

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "EventLogDataContainer: Wrote {0} event log entries to file '{1}' in {2} seconds",
                        eventLogEntries.Count,
                        eventLogPath,
                        stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)));
            }

            // Write the event log file
            FileTransferInformation fileTransferInformation =
                new FileTransferInformation(dataCollectionContext, eventLogPath, true, this.fileHelper);
            this.dataSink.SendFileAsync(fileTransferInformation);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    "EventLogDataContainer: Event log successfully sent for data collection context '{0}'.",
                    dataCollectionContext.ToString());
            }

            return eventLogPath;
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Cleans up resources allocated by the data collector
        /// </summary>
        /// <param name="disposing">Not used since this class does not have a finaliser.</param>
        protected override void Dispose(bool disposing)
        {
            // Unregister events
            this.events.SessionStart -= this.sessionStartEventHandler;
            this.events.SessionEnd -= this.sessionEndEventHandler;
            this.events.TestCaseStart -= this.testCaseStartEventHandler;
            this.events.TestCaseEnd -= this.testCaseEndEventHandler;

            // Unregister EventLogEntry Written.
            foreach (var eventLogContainer in this.eventLogContainerMap.Values)
            {
                eventLogContainer.Dispose();
            }

            // Delete all the temp event log directories
            this.RemoveTempEventLogDirs(this.eventLogDirectories);
            GC.SuppressFinalize(this);
        }

        #endregion

        private static ISet<string> ParseCommaSeparatedList(string commaSeparatedList)
        {
            ISet<string> strings = new HashSet<string>();
            string[] items = commaSeparatedList.Split(new char[] { ',' });
            foreach (string item in items)
            {
                strings.Add(item.Trim());
            }

            return strings;
        }

        #region Event Handlers

        private void OnSessionStart(object sender, SessionStartEventArgs e)
        {
            ValidateArg.NotNull(e, "SessionStartEventArgs");
            ValidateArg.NotNull(e.Context, "SessionStartEventArgs.Context");

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("EventLogDataCollector: SessionStart received");
            }

            this.StartCollectionForContext(e.Context, true);
        }

        private void OnSessionEnd(object sender, SessionEndEventArgs e)
        {
            ValidateArg.NotNull(e, "SessionEndEventArgs");
            ValidateArg.NotNull(e.Context, "SessionEndEventArgs.Context");

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("EventLogDataCollector: SessionEnd received");
            }

            this.WriteCollectedEventLogEntries(e.Context, true, TimeSpan.MaxValue, DateTime.Now);
        }

        private void OnTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            ValidateArg.NotNull(e, "TestCaseStartEventArgs");
            ValidateArg.NotNull(e.Context, "TestCaseStartEventArgs.Context");

            if (!e.Context.HasTestCase)
            {
                Debug.Fail("Context is not for a test case");
                throw new ArgumentNullException("TestCaseStartEventArgs.Context.HasTestCase");
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("EventLogDataCollector: TestCaseStart received for test '{0}'.", e.TestCaseName);
            }

            this.StartCollectionForContext(e.Context, false);
        }

        private void OnTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            ValidateArg.NotNull(e, "TestCaseEndEventArgs");

            Debug.Assert(e.Context != null, "Context is null");
            Debug.Assert(e.Context.HasTestCase, "Context is not for a test case");

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose(
                    "EventLogDataCollector: TestCaseEnd received for test '{0}' with Test Outcome: {1}.",
                    e.TestCaseName,
                    e.TestOutcome);
            }

            this.WriteCollectedEventLogEntries(e.Context, false, TimeSpan.MaxValue, DateTime.Now);
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
                    this.fileHelper.DeleteEmptyDirectroy(dir);
                }
            }
        }

        private void StartCollectionForContext(DataCollectionContext dataCollectionContext, bool isSessionContext)
        {
            EventLogSessionContext eventLogSessionContext = null;
            lock (this.ContextMap)
            {
                eventLogSessionContext =
                    new EventLogSessionContext(this.eventLogContainerMap);
                this.ContextMap.Add(dataCollectionContext, eventLogSessionContext);
            }
        }

        private void WriteCollectedEventLogEntries(
            DataCollectionContext dataCollectionContext,
            bool isSessionEnd,
            TimeSpan requestedDuration,
            DateTime timeRequestReceived)
        {
            var context = this.GetEventLogSessionContext(dataCollectionContext);
            context.CreateEventLogContainerEndIndexMap();

            List<EventLogEntry> eventLogEntries = new List<EventLogEntry>();
            foreach (KeyValuePair<string, IEventLogContainer> kvp in this.eventLogContainerMap)
            {
                try
                {
                    if (isSessionEnd)
                    {
                        kvp.Value.EventLog.EnableRaisingEvents = false;
                    }

                    for (int i = context.EventLogContainerStartIndexMap[kvp.Key]; i <= context.EventLogContainerEndIndexMap[kvp.Key]; i++)
                    {
                        eventLogEntries.Add(kvp.Value.EventLogEntries[i]);
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogWarning(
                        dataCollectionContext,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.CleanupException,
                            kvp.Value.EventLog,
                            e.ToString()));
                }
            }

            var fileName = this.WriteEventLogs(eventLogEntries, isSessionEnd ? int.MaxValue : this.maxEntries, dataCollectionContext, requestedDuration, timeRequestReceived);

            // Add the directory to the list
            this.eventLogDirectories.Add(Path.GetDirectoryName(fileName));

            lock (this.ContextMap)
            {
                this.ContextMap.Remove(dataCollectionContext);
            }
        }

        private void ConfigureEventLogNames(CollectorNameValueConfigurationManager collectorNameValueConfigurationManager)
        {
            this.eventLogNames = new HashSet<string>();
            string eventLogs = collectorNameValueConfigurationManager[EventLogConstants.SettingEventLogs];
            if (eventLogs != null)
            {
                this.eventLogNames = ParseCommaSeparatedList(eventLogs);
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "EventLogDataCollector configuration: " + EventLogConstants.SettingEventLogs + "=" + eventLogs);
                }
            }
            else
            {
                // Default to collecting these standard logs
                this.eventLogNames.Add("System");
                this.eventLogNames.Add("Application");
            }

            foreach (string eventLogName in this.eventLogNames)
            {
                try
                {
                    // Create an EventLog object and add it to the eventLogContext if one does not already exist
                    if (!this.eventLogContainerMap.ContainsKey(eventLogName))
                    {
                        IEventLogContainer eventLogContainer = new EventLogContainer(
                            eventLogName,
                            this.eventSources,
                            this.entryTypes,
                            int.MaxValue,
                            this.logger,
                            this.dataCollectorContext);
                        this.eventLogContainerMap.Add(eventLogName, eventLogContainer);
                    }

                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(string.Format(
                            CultureInfo.InvariantCulture,
                            "EventLogDataCollector: Created EventSource '{0}'",
                            eventLogName));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        null,
                        new EventLogCollectorException(string.Format(CultureInfo.InvariantCulture, Resource.ReadError, eventLogName, Environment.MachineName), ex));
                }
            }
        }

        private void ConfigureEventSources(CollectorNameValueConfigurationManager collectorNameValueConfigurationManager)
        {
            string eventSourcesStr = collectorNameValueConfigurationManager[EventLogConstants.SettingEventSources];
            if (!string.IsNullOrEmpty(eventSourcesStr))
            {
                this.eventSources = ParseCommaSeparatedList(eventSourcesStr);
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "EventLogDataCollector configuration: " + EventLogConstants.SettingEventSources + "="
                        + this.eventSources);
                }
            }
        }

        private void ConfigureEntryTypes(CollectorNameValueConfigurationManager collectorNameValueConfigurationManager)
        {
            this.entryTypes = new HashSet<EventLogEntryType>();
            string entryTypesStr = collectorNameValueConfigurationManager[EventLogConstants.SettingEntryTypes];
            if (entryTypesStr != null)
            {
                foreach (string entryTypestring in ParseCommaSeparatedList(entryTypesStr))
                {
                    this.entryTypes.Add(
                        (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), entryTypestring, true));
                }

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "EventLogDataCollector configuration: " + EventLogConstants.SettingEntryTypes + "="
                        + this.entryTypes);
                }
            }
            else
            {
                this.entryTypes.Add(EventLogEntryType.Error);
                this.entryTypes.Add(EventLogEntryType.Warning);
                this.entryTypes.Add(EventLogEntryType.FailureAudit);
            }
        }

        private void ConfigureMaxEntries(CollectorNameValueConfigurationManager collectorNameValueConfigurationManager)
        {
            string maxEntriesstring = collectorNameValueConfigurationManager[EventLogConstants.SettingMaxEntries];
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
                catch (FormatException)
                {
                    this.maxEntries = EventLogConstants.DefaultMaxEntries;
                }

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose(
                        "EventLogDataCollector configuration: " + EventLogConstants.SettingMaxEntries + "="
                        + this.maxEntries);
                }
            }
            else
            {
                this.maxEntries = EventLogConstants.DefaultMaxEntries;
            }
        }

        private EventLogSessionContext GetEventLogSessionContext(DataCollectionContext dataCollectionContext)
        {
            EventLogSessionContext eventLogSessionContext;
            bool eventLogContainerFound;
            lock (this.ContextMap)
            {
                eventLogContainerFound = this.ContextMap.TryGetValue(dataCollectionContext, out eventLogSessionContext);
            }

            if (!eventLogContainerFound)
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    Resource.ContextNotFoundException,
                    dataCollectionContext.ToString());
                throw new EventLogCollectorException(msg, null);
            }

            return eventLogSessionContext;
        }

        #endregion
    }
}
