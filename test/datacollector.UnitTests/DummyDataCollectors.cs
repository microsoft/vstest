// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests;

using System.Collections.Generic;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

[DataCollectorFriendlyName("CustomDataCollector")]
[DataCollectorTypeUri("my://custom/datacollector")]
public class CustomDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
{
    public DataCollectionEnvironmentContext DataCollectionEnvironmentContext { get; set; }

    public DataCollectionSink DataSink { get; set; }

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
        return default;
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
