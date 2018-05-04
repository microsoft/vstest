// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System.Collections.Generic;
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
        internal IDataCollectionEvents Events { get; private set; }

        internal IDataCollectionLogger Logger { get; private set; }

        internal IDataCollectionSink DataSink { get; private set; }

        internal IDataCollectionAgentContext AgentContext { get; private set; }

        #region Interface entry points

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            this.Initialize(
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

        internal void Initialize(
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

            this.OnInitialize(configurationElement);
        }

        protected abstract void OnInitialize(XmlElement configurationElement);

        // Provide required environment variables for test execution through this method.
        protected abstract IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables();
    }
}