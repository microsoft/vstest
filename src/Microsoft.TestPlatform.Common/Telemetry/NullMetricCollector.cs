// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    /// <summary>
    /// If Telemtry is opted out, this class will be initialized and will do No operation.
    /// </summary>
    public class NullMetricCollector : IMetricsCollector
    {
        public void Add(string message, string value)
        {
            // No operation
        }

        public void Clear()
        {
            // No Operation
        }

        public IDictionary<string, string> Metrics()
        {
            return new Dictionary<string, string>();
        }
    }
}
