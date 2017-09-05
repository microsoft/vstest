// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// This Client will be used by Vstest.console process only to Handle Metrics.
    /// </summary>
    public static class MetricCollector
    {
        private static Dictionary<string, string> metricCollector = new Dictionary<string, string>();

        // Add Metric to concurrent Dictionary.
        public static void Add(string metric, string value)
        {
            ValidateArg.NotNull(metric, "metric");
            ValidateArg.NotNull(value, "value");

            if (!metricCollector.ContainsKey(metric))
            {
                metricCollector?.Add(metric, value);
            }
        }

        // Returns the Metrics
        public static IDictionary<string, string> Metrics()
        {
            if (metricCollector == null || metricCollector.Count == 0)
            {
                return  new ConcurrentDictionary<string, string>();
            }
            else
            {
                return metricCollector;
            }
        }

        public static void Dispose()
        {
            metricCollector?.Clear();
        }
    }
}
