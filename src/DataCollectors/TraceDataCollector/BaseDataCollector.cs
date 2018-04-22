// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// Implements the base event hooking logic for data collectors.
    /// </summary>
    /// <remarks>
    /// This class is used so that we can wrap the concrete objects given to us from the collector architecture with interfaces.
    /// This allows us to mock up collectors for unit tests.
    /// </remarks>
    public abstract class BaseDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        static List<BaseDataCollector> _collectors = new List<BaseDataCollector>();

        /// <summary>
        /// The setting name for coverage file name
        /// </summary>
        private const string LogFile = "LogFile";

        /// <summary>
        /// Internal constructor to prevent outside construction.
        /// </summary>
        internal BaseDataCollector()
        {
            EqtTrace.Info("BaseDataCollector.ctor: adding datacollector: {0}", this);
            _collectors.Add(this);
        }

        internal IDataCollectionEvents Events { get; private set; }
        internal IDataCollectionLogger Logger { get; private set; }
        internal IDataCollectionSink DataSink { get; private set; }
        internal IDataCollectionAgentContext AgentContext { get; private set; }

        static protected ReadOnlyCollection<BaseDataCollector> Collectors
        {
            get
            {
                return _collectors.AsReadOnly();
            }
        }

        #region Test entry point
        internal void Initialize(XmlElement configurationElement, IDataCollectionEvents events, IDataCollectionSink dataSink, IDataCollectionLogger logger, IDataCollectionAgentContext agentContext)
        {
            InternalConstruct(configurationElement, events, dataSink, logger, agentContext);
        }

        internal IEnumerable<KeyValuePair<string, string>> RequestEnvironmentVariables()
        {
            return GetEnvironmentVariables();
        }

        #endregion

        #region Interface entry points
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            InternalConstruct(configurationElement, new DataCollectionEventsWrapper(events), new DataCollectionSinkWrapper(dataSink), new DataCollectionLoggerWrapper(logger), new DataCollectionEnvironmentContextWrapper(environmentContext));
        }

        IEnumerable<KeyValuePair<string, string>> ITestExecutionEnvironmentSpecifier.GetTestExecutionEnvironmentVariables()
        {
            return GetEnvironmentVariables();
        }
        #endregion

        void InternalConstruct(XmlElement configurationElement, IDataCollectionEvents events, IDataCollectionSink dataSink, IDataCollectionLogger logger, IDataCollectionAgentContext agentContext)
        {
            EqtTrace.Info("BaseDataCollector.InternalConstruct: Enabling datacollector with configuration: {0}", configurationElement?.InnerXml);
            Events = events;
            DataSink = dataSink;
            Logger = logger;
            AgentContext = agentContext;

            // Add to the SendFileCompleted event here since the data sink will persist for all derived classes.
            if (DataSink != null)
            {
                DataSink.SendFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(OnSendFileCompleted);
            }

            OnInitialize(configurationElement);

            SubscribeToEvents();
        }

        protected abstract void OnInitialize(XmlElement configurationElement);

        ///Provide required environment variables for test execution through this method.
        protected abstract IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables();

        internal abstract void SetCollectionPerProcess(Dictionary<string, XmlElement> processCPMap);

        protected void SubscribeToEvents()
        {
            if (Events != null)
            {
                Events.SessionStart += new EventHandler<SessionStartEventArgs>(OnSessionStart);
                Events.SessionEnd += new EventHandler<SessionEndEventArgs>(OnSessionEnd);

                SubscribeToTestCaseEvents();
            }
        }

        protected void UnsubscribeFromEvents()
        {
            if (Events != null)
            {
                Events.SessionStart -= new EventHandler<SessionStartEventArgs>(OnSessionStart);
                Events.SessionEnd -= new EventHandler<SessionEndEventArgs>(OnSessionEnd);

                UnsubscribeFromTestCaseEvents();

                Events = null;
            }
        }

        /// <summary>
        /// Unsubscribe to test case events.
        /// If for active set of data collectors, test case events is not required, calling this
        /// method will unsubscribe BaseDataCollector from TestCase events
        /// If only Dynamic Code Coverage Collector is enabled, this method is called from
        /// SessionStart of DynamicCodeCoverageDataCollector
        /// </summary>
        protected void UnsubscribeFromTestCaseEvents()
        {
            if (Events != null)
            {
                Events.TestCaseStart -= new EventHandler<TestCaseStartEventArgs>(OnTestCaseStart);
                Events.TestCaseEnd -= new EventHandler<TestCaseEndEventArgs>(OnTestCaseEnd);
            }
        }

        /// <summary>
        /// Subscribe to testcase events.
        /// </summary>
        protected void SubscribeToTestCaseEvents()
        {
            if (Events != null)
            {
                Events.TestCaseStart += new EventHandler<TestCaseStartEventArgs>(OnTestCaseStart);
                Events.TestCaseEnd += new EventHandler<TestCaseEndEventArgs>(OnTestCaseEnd);
            }
        }

        protected virtual void OnSendFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) { AssertNotDisposed(); }
        protected virtual void OnSessionEnd(object sender, SessionEndEventArgs e) { AssertNotDisposed(); }
        protected virtual void OnSessionStart(object sender, SessionStartEventArgs e) { AssertNotDisposed(); }
        protected virtual void OnTestCaseStart(object sender, TestCaseStartEventArgs e) { AssertNotDisposed(); }
        protected virtual void OnTestCaseEnd(object sender, TestCaseEndEventArgs e) { AssertNotDisposed(); }

        ~BaseDataCollector()
        {
            Dispose(false);
        }

        /// <summary>
        /// Route the request to write a file to the correct collector.
        /// </summary>
        protected void SendFileAsync(DataCollectionContext context, string displayName, string logFilePath, bool sinkOwnsFile, object userToken, bool writeTraceLog)
        {
// TestImpactDataCollector specific code in BaseDataCollector. Currently TestImpactDataCollector not required for netstandard.
/*#if !NETSTANDARD
            foreach (BaseDataCollector collector in _collectors)
            {
                CommonDataCollector cdc = null;
                cdc = collector as TestImpactDataCollector;

                if (cdc != null)
                {
                    cdc.WriteFile(context, displayName, logFilePath, sinkOwnsFile, userToken);
                }
            }
#endif*/
        }

        /// <summary>
        /// Called when a file needs to be written to this data collector's data sink.
        /// </summary>
        protected void WriteFile(DataCollectionContext context, string description, string logFilePath, bool deleteFile, object userToken)
        {
            var fileInfo = new FileTransferInformation(context, logFilePath, deleteFile);
            fileInfo.Description = description;
            fileInfo.UserToken = userToken;

            DataSink.SendFileAsync(fileInfo);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                if (DataSink != null)
                {
                    DataSink.SendFileCompleted -= new System.ComponentModel.AsyncCompletedEventHandler(OnSendFileCompleted);
                }

                _collectors.Remove(this);

                UnsubscribeFromEvents();
                IsDisposed = true;
            }
        }

        protected bool IsDisposed { get; private set; }

        private void AssertNotDisposed() { if (IsDisposed) throw new ObjectDisposedException(this.GetType().ToString()); }
    }
}
