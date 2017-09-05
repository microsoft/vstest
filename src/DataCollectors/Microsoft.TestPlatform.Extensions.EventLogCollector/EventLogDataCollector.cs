// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
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
        private Dictionary<string, bool> eventLogNames;

        /// <summary>
        /// The event sources.
        /// </summary>
        private Dictionary<string, bool> eventSources;

        /// <summary>
        /// The entry types.
        /// </summary>
        private Dictionary<EventLogEntryType, bool> entryTypes;

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
        private Dictionary<string, EventLog> eventLogMap = new Dictionary<string, EventLog>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogDataCollector"/> class.
        /// </summary>
        public EventLogDataCollector()
        {
            this.sessionStartEventHandler = this.OnSessionStart;
            this.sessionEndEventHandler = this.OnSessionEnd;
            this.testCaseStartEventHandler = this.OnTestCaseStart;
            this.testCaseEndEventHandler = this.OnTestCaseEnd;

            this.eventLogDirectories = new List<string>();
            this.ContextData = new Dictionary<DataCollectionContext, IEventLogContainer>();
            this.fileHelper = new FileHelper();
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

        internal Dictionary<string, bool> EventSources
        {
            get
            {
                return this.eventSources;
            }
        }

        internal Dictionary<EventLogEntryType, bool> EntryTypes
        {
            get
            {
                return this.entryTypes;
            }
        }

        internal Dictionary<string, bool> EventLogNames
        {
            get
            {
                return this.eventLogNames;
            }
        }

        /// <summary>
        /// Gets the context data.
        /// </summary>
        internal Dictionary<DataCollectionContext, IEventLogContainer> ContextData { get; private set; }

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
            this.ConfigureEventLogNames(nameValueSettings);
            this.ConfigureEventSources(nameValueSettings);
            this.ConfigureEntryTypes(nameValueSettings);
            this.ConfigureMaxEntries(nameValueSettings);

            // Register for events
            events.SessionStart += this.sessionStartEventHandler;
            events.SessionEnd += this.sessionEndEventHandler;
            events.TestCaseStart += this.testCaseStartEventHandler;
            events.TestCaseEnd += this.testCaseEndEventHandler;
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

            // Delete all the temp event log directories
            this.RemoveTempEventLogDirs(this.eventLogDirectories);
            GC.SuppressFinalize(this);
        }

        #endregion

        private static Dictionary<string, bool> ParseCommaSeparatedList(string commaSeparatedList)
        {
            Dictionary<string, bool> strings = new Dictionary<string, bool>();
            string[] items = commaSeparatedList.Split(new char[] { ',' });
            foreach (string item in items)
            {
                strings.Add(item.Trim(), false);
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
                EqtTrace.Verbose("EventLogDataCollector: TestCaseStart received for test '{1}'.", e.TestCaseName);
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
                    "EventLogDataCollector: TestCaseEnd received for test '{1}' with Test Outcome: {2}.",
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

        private void StartCollectionForContext(DataCollectionContext dataCollectionContext, bool isSessionContext)
        {
            IEventLogContainer eventLogContainer = null;
            lock (this.ContextData)
            {
                if (this.ContextData.TryGetValue(dataCollectionContext, out eventLogContainer))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(string.Format(
                            CultureInfo.InvariantCulture,
                            "EventLogDataCollector: Context data already in dictionary"));
                    }
                }
                else
                {
                    eventLogContainer = new EventLogContainer(this.eventLogMap, this.eventSources, this.entryTypes, this.logger, this.dataCollectorContext, this.dataSink, isSessionContext ? int.MaxValue : this.maxEntries);

                    this.ContextData.Add(dataCollectionContext, eventLogContainer);
                }
            }
        }

        private void WriteCollectedEventLogEntries(
            DataCollectionContext dataCollectionContext,
            bool disposeEventLogs,
            TimeSpan requestedDuration,
            DateTime timeRequestReceived)
        {
            IEventLogContainer eventLogContainer = this.GetEventLogContainer(dataCollectionContext);

            foreach (var eventLog in this.eventLogMap.Values)
            {
                try
                {
                    eventLog.EntryWritten -= eventLogContainer.OnEventLogEntryWritten;

                    eventLogContainer.OnEventLogEntryWritten(eventLog, null);

                    if (disposeEventLogs)
                    {
                        eventLog.EnableRaisingEvents = false;
                        eventLog.Dispose();
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogWarning(
                        dataCollectionContext,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.EventLog_CleanupException,
                            eventLog,
                            e.ToString()));
                }
            }

            var fileName = eventLogContainer.WriteEventLogs(dataCollectionContext, requestedDuration, timeRequestReceived);

            // Add the directory to the list
            this.eventLogDirectories.Add(Path.GetDirectoryName(fileName));

            lock (this.ContextData)
            {
                this.ContextData.Remove(dataCollectionContext);
            }
        }

        private IEventLogContainer GetEventLogContainer(DataCollectionContext dataCollectionContext)
        {
            IEventLogContainer eventLogContext;
            bool eventLogContainerFound;
            lock (this.ContextData)
            {
                eventLogContainerFound = this.ContextData.TryGetValue(dataCollectionContext, out eventLogContext);
            }

            if (!eventLogContainerFound)
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    Resource.EventLog_ContextNotFoundException,
                    dataCollectionContext.ToString());
                throw new EventLogCollectorException(msg, null);
            }

            return eventLogContext;
        }

        private void ConfigureEventLogNames(CollectorNameValueConfigurationManager collectorNameValueConfigurationManager)
        {
            this.eventLogNames = new Dictionary<string, bool>();
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
                this.eventLogNames.Add("System", false);
                this.eventLogNames.Add("Security", false);
                this.eventLogNames.Add("Application", false);
            }

            foreach (string eventLogName in this.eventLogNames.Keys)
            {
                try
                {
                    // Create an EventLog object and add it to the eventLogContext if one does not already exist
                    if (!this.eventLogMap.ContainsKey(eventLogName))
                    {
                        EventLog eventLog = new EventLog(eventLogName);
                        this.eventLogMap.Add(eventLogName, eventLog);
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
                        new EventLogCollectorException(string.Format(CultureInfo.InvariantCulture, Resource.EventLog_ReadError, eventLogName, Environment.MachineName), ex));
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
            this.entryTypes = new Dictionary<EventLogEntryType, bool>();
            string entryTypesStr = collectorNameValueConfigurationManager[EventLogConstants.SettingEntryTypes];
            if (entryTypesStr != null)
            {
                foreach (string entryTypestring in ParseCommaSeparatedList(entryTypesStr).Keys)
                {
                    try
                    {
                        this.entryTypes.Add(
                            (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), entryTypestring, true), false);
                    }
                    catch (ArgumentException e)
                    {
                        throw new EventLogCollectorException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resource.EventLog_InvalidEntryTypeInConfig,
                                entryTypesStr),
                            e);
                    }
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
                this.entryTypes.Add(EventLogEntryType.Error, false);
                this.entryTypes.Add(EventLogEntryType.Warning, false);
                this.entryTypes.Add(EventLogEntryType.FailureAudit, false);
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
                catch (FormatException e)
                {
                    throw new EventLogCollectorException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.EventLog_InvalidMaxEntriesInConfig,
                            maxEntriesstring),
                        e);
                }

                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogConstants.SettingMaxEntries + "="
                    + maxEntriesstring);
            }
            else
            {
                this.maxEntries = EventLogConstants.DefaultMaxEntries;
            }
        }

        #endregion
    }
}
