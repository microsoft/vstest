// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;

    public class DataCollectorConfig
    {
        public Type DataCollectorType { get; set; }

        public string DataCollectorConfiguration { get; set; }

        public Uri TypeUri { get; set; }

        public string FriendlyName { get; set; }


        public DataCollectorConfig(Type type, string dataCollectorConfig)
        {
            this.DataCollectorType = type;
            this.DataCollectorConfiguration = dataCollectorConfig;
        }
    }
}