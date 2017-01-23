// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;

    /// <summary>
    /// Encapsulates datacollector object and other objects required to facilitate datacollection.
    /// </summary>
    internal class DataCollectorWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorWrapper"/> class.
        /// </summary>
        /// <param name="dataCollector">
        /// The data collector.
        /// </param>
        /// <param name="configurationElement">
        /// The configuration element.
        /// </param>
        /// <param name="dataCollectorConfig">
        /// The data collector config.
        /// </param>
        /// <param name="environmentContext">
        /// The environment Context.
        /// </param>
        /// <param name="attachmentManager">
        /// The attachment Manager.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="messageSink">
        /// The message Sink.
        /// </param>
        internal DataCollectorWrapper(DataCollector dataCollector, XmlElement configurationElement, DataCollectorConfig dataCollectorConfig, DataCollectionEnvironmentContext environmentContext, IDataCollectionAttachmentManager attachmentManager, TestPlatformDataCollectionEvents events, IMessageSink messageSink)
        {
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.DataCollectorConfig = dataCollectorConfig;
            this.Events = events;
            this.EnvironmentContext = environmentContext;
            this.DataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager, dataCollectorConfig);
            this.Logger = new TestPlatformDataCollectionLogger(messageSink, dataCollectorConfig);
        }

        /// <summary>
        /// Gets or sets the data collector.
        /// </summary>
        public DataCollector DataCollector { get; set; }

        /// <summary>
        /// Gets or sets the configuration element.
        /// </summary>
        public XmlElement ConfigurationElement { get; set; }

        /// <summary>
        /// Gets or sets the data collector config.
        /// </summary>
        public DataCollectorConfig DataCollectorConfig { get; set; }

        /// <summary>
        /// Gets the events object on which the collector registers for events
        /// </summary>
        public TestPlatformDataCollectionEvents Events { get; private set; }

        /// <summary>
        /// Gets the datacollection sink.
        /// </summary>
        public TestPlatformDataCollectionSink DataCollectionSink { get; private set; }

        /// <summary>
        /// Gets the data collection environment context.
        /// </summary>
        public DataCollectionEnvironmentContext EnvironmentContext { get; private set; }

        /// <summary>
        /// Gets the data collection logger
        /// </summary>
        public TestPlatformDataCollectionLogger Logger { get; private set; }

        /// <summary>
        /// Gets or sets environment variables supplied by the data collector.
        /// These are available after the collector has been initialized.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> TestExecutionEnvironmentVariables
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes datacollectors.
        /// </summary>
        internal void InitializeDataCollector()
        {
            this.DataCollector.Initialize(this.ConfigurationElement, this.Events, this.DataCollectionSink, this.Logger, this.EnvironmentContext);
            this.SetTestExecutionEnvironmentVariables();
        }

        /// <summary>
        /// Disposes datacollector.
        /// </summary>
        internal void DisposeDataCollector()
        {
            try
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectorWrapper.DisposeDataCollector: calling Dispose() on {0}", this.DataCollector.GetType());
                }

                this.DataCollector.Dispose();
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectorInfo.DisposeDataCollector: exception while calling Dispose() on {0}: " + ex, this.DataCollector.GetType());
                }
            }
        }

        /// <summary>
        /// The get test execution environment variables sync.
        /// </summary>
        private void SetTestExecutionEnvironmentVariables()
        {
            var testExecutionEnvironmentSpecifier = this.DataCollector as ITestExecutionEnvironmentSpecifier;
            if (testExecutionEnvironmentSpecifier != null)
            {
                // Get the environment variables the data collector wants set in the test execution environment
                this.TestExecutionEnvironmentVariables = testExecutionEnvironmentSpecifier.GetTestExecutionEnvironmentVariables();
            }
        }
    }
}