// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The test platform data collector.
    /// </summary>
    internal class TestPlatformDataCollector
    {
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
        /// Gets the logger
        /// </summary>
        public DataCollectionLogger Logger
        {
            get;
            private set;
        }

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
        public TestPlatformDataCollector(DataCollector dataCollector, XmlElement configurationElement, DataCollectorConfig dataCollectorConfig)
        {
            // todo : validate arguments.
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.DataCollectorConfig = dataCollectorConfig;
        }
    }
}