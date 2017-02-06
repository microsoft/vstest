// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        public DataCollectionEnvironmentContext dataCollectionEnvironmentContext { get; set; }

        public DataCollectionSink dataSink { get; set; }

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return default(IEnumerable<KeyValuePair<string, string>>);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    public class CustomDataCollectorWithoutUri : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }
    }

    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollectorWithoutFriendlyName : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }
    }

    [DataCollectorFriendlyName("")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollectorWithEmptyFriendlyName : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }
    }
}