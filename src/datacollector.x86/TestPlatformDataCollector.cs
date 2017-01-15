// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using System.Xml;

    public class TestPlatformDataCollector
    {
        public DataCollector DataCollector { get; set; }
        public XmlElement ConfigurationElement { get; set; }
        public IMessageSink MessageSink { get; set; }
        public DataCollectorConfig DataCollectorConfig { get; set; }

        /// <summary>
        /// Gets the logger
        /// </summary>
        public  DataCollectionLogger Logger
        {
            get;
            private set;
        }

        public TestPlatformDataCollector(DataCollector dataCollector, XmlElement configurationElement, IMessageSink messageSink, DataCollectorConfig dataCollectorConfig)
        {
            // todo : validate arguments.
            this.DataCollector = dataCollector;
            this.ConfigurationElement = configurationElement;
            this.MessageSink = messageSink;
            this.DataCollectorConfig = dataCollectorConfig;
        }
    }
}