// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        private static List<BaseDataCollector> collectors = new List<BaseDataCollector>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollector"/> class.
        /// Internal constructor to prevent outside construction.
        /// </summary>
        internal BaseDataCollector()
        {
            EqtTrace.Info("BaseDataCollector.ctor: adding datacollector: {0}", this);
            collectors.Add(this);
        }

        ~BaseDataCollector()
        {
            this.Dispose(false);
        }

        internal IDataCollectionEvents Events { get; private set; }

        internal IDataCollectionLogger Logger { get; private set; }

        internal IDataCollectionSink DataSink { get; private set; }

        internal IDataCollectionAgentContext AgentContext { get; private set; }

        protected static ReadOnlyCollection<BaseDataCollector> Collectors
        {
            get { return collectors.AsReadOnly(); }
        }

        protected bool IsDisposed { get; private set; }

        #region Interface entry points

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            this.InternalConstruct(
                configurationElement,
                new DataCollectionEventsWrapper(events),
                new DataCollectionSinkWrapper(dataSink),
                new DataCollectionLoggerWrapper(logger),
                new DataCollectionEnvironmentContextWrapper(environmentContext));
        }

        IEnumerable<KeyValuePair<string, string>> ITestExecutionEnvironmentSpecifier.
            GetTestExecutionEnvironmentVariables()
        {
            return this.GetEnvironmentVariables();
        }

        #endregion

        #region Test entry point

        internal void Initialize(
            XmlElement configurationElement,
            IDataCollectionEvents events,
            IDataCollectionSink dataSink,
            IDataCollectionLogger logger,
            IDataCollectionAgentContext agentContext)
        {
            this.InternalConstruct(configurationElement, events, dataSink, logger, agentContext);
        }

        internal IEnumerable<KeyValuePair<string, string>> RequestEnvironmentVariables()
        {
            return this.GetEnvironmentVariables();
        }

        #endregion

        internal abstract void SetCollectionPerProcess(Dictionary<string, XmlElement> processCPMap);

        protected abstract void OnInitialize(XmlElement configurationElement);

        // Provide required environment variables for test execution through this method.
        protected abstract IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables();

        protected void SubscribeToEvents()
        {
            if (this.Events != null)
            {
                this.Events.SessionStart += new EventHandler<SessionStartEventArgs>(this.OnSessionStart);
                this.Events.SessionEnd += new EventHandler<SessionEndEventArgs>(this.OnSessionEnd);

                this.SubscribeToTestCaseEvents();
            }
        }

        protected void UnsubscribeFromEvents()
        {
            if (this.Events != null)
            {
                this.Events.SessionStart -= new EventHandler<SessionStartEventArgs>(this.OnSessionStart);
                this.Events.SessionEnd -= new EventHandler<SessionEndEventArgs>(this.OnSessionEnd);

                this.UnsubscribeFromTestCaseEvents();

                this.Events = null;
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
            if (this.Events != null)
            {
                this.Events.TestCaseStart -= new EventHandler<TestCaseStartEventArgs>(this.OnTestCaseStart);
                this.Events.TestCaseEnd -= new EventHandler<TestCaseEndEventArgs>(this.OnTestCaseEnd);
            }
        }

        /// <summary>
        /// Subscribe to testcase events.
        /// </summary>
        protected void SubscribeToTestCaseEvents()
        {
            if (this.Events != null)
            {
                this.Events.TestCaseStart += new EventHandler<TestCaseStartEventArgs>(this.OnTestCaseStart);
                this.Events.TestCaseEnd += new EventHandler<TestCaseEndEventArgs>(this.OnTestCaseEnd);
            }
        }

        protected virtual void OnSendFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.AssertNotDisposed();
        }

        protected virtual void OnSessionEnd(object sender, SessionEndEventArgs e)
        {
            this.AssertNotDisposed();
        }

        protected virtual void OnSessionStart(object sender, SessionStartEventArgs e)
        {
            this.AssertNotDisposed();
        }

        protected virtual void OnTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.AssertNotDisposed();
        }

        protected virtual void OnTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            this.AssertNotDisposed();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this.IsDisposed)
            {
                if (this.DataSink != null)
                {
                    this.DataSink.SendFileCompleted -=
                        new System.ComponentModel.AsyncCompletedEventHandler(this.OnSendFileCompleted);
                }

                collectors.Remove(this);

                this.UnsubscribeFromEvents();
                this.IsDisposed = true;
            }
        }

        private void AssertNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        private void InternalConstruct(
            XmlElement configurationElement,
            IDataCollectionEvents events,
            IDataCollectionSink dataSink,
            IDataCollectionLogger logger,
            IDataCollectionAgentContext agentContext)
        {
            EqtTrace.Info(
                "BaseDataCollector.InternalConstruct: Enabling datacollector with configuration: {0}",
                configurationElement?.InnerXml);
            this.Events = events;
            this.DataSink = dataSink;
            this.Logger = logger;
            this.AgentContext = agentContext;

            // Add to the SendFileCompleted event here since the data sink will persist for all derived classes.
            if (this.DataSink != null)
            {
                this.DataSink.SendFileCompleted +=
                    new System.ComponentModel.AsyncCompletedEventHandler(this.OnSendFileCompleted);
            }

            this.OnInitialize(configurationElement);

            this.SubscribeToEvents();
        }
    }
}