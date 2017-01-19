// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The test platform data collector.
    /// </summary>
    internal class TestPlatformDataCollector
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollector"/> class.
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
        internal TestPlatformDataCollector(DataCollector dataCollector, XmlElement configurationElement, DataCollectorConfig dataCollectorConfig, DataCollectionEnvironmentContext environmentContext, IDataCollectionAttachmentManager attachmentManager, TestPlatformDataCollectionEvents events, IMessageSink messageSink)
        {
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.DataCollectorConfig = dataCollectorConfig;
            this.Events = events;
            this.EnvironmentContext = environmentContext;
            this.DataSink = new TestPlatformDataCollectionSink(attachmentManager, dataCollectorConfig);
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
        /// Gets the data sink
        /// </summary>
        public TestPlatformDataCollectionSink DataSink { get; private set; }

        /// <summary>
        /// Gets the data collection environment context.
        /// </summary>
        public DataCollectionEnvironmentContext EnvironmentContext { get; private set; }

        /// <summary>
        /// Gets the data collection logger
        /// </summary>
        public TestPlatformDataCollectionLogger Logger { get; private set; }

        /// <summary>
        /// Initializes data collectors.
        /// </summary>
        internal void InitializeDataCollector()
        {
            this.DataCollector.Initialize(this.ConfigurationElement, this.Events, this.DataSink, this.Logger, this.EnvironmentContext);
        }
    }
}