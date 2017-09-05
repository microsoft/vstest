// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// This Client will be used by Vstest.console process only to Handle Metrics.
    /// </summary>
    public class TelemetryClient
    {
        private static ConcurrentDictionary<string, string> metricCollector;
        private static Dictionary<string, double> adapterUrisAndTestCount;
        private static Dictionary<string, double> adapterRunTestTime;
        private static double maxExecutionTime;

        public TelemetryClient()
        {
            metricCollector = new ConcurrentDictionary<string, string>();

            adapterUrisAndTestCount = new Dictionary<string, double>();
            adapterRunTestTime = new Dictionary<string, double>();
            maxExecutionTime = 0;
        }

        // Add Metric to concurrent Dictionary.
        public static void AddMetric(string message, string value)
        {
            ValidateArg.NotNull(message, "message");
            ValidateArg.NotNull(value, "value");

            metricCollector?.AddOrUpdate(message, value, (key, oldVal) => (value));
        }

        // Returns the Metrics
        public static IDictionary<string, string> GetMetrics()
        {
            if (metricCollector == null || metricCollector.IsEmpty)
            {
                return  new ConcurrentDictionary<string, string>();
            }
            else
            {
                return metricCollector;
            }
        }

        // Handle Test Run Complete Metrics. This will handle parallel case scenarios as well.
        public static void HandleTestRunCompleteMetrics(IDictionary<string, string> metrics)
        {
            if (metrics == null || metrics.Count == 0 || adapterRunTestTime == null || adapterUrisAndTestCount == null)
            {
                return; 
            }

            foreach (var metric in metrics)
            {
                if (metric.Key.Contains(UnitTestTelemetryDataConstants.TimeTakenToRunTestsByAnAdapter) || metric.Key.Contains(UnitTestTelemetryDataConstants.TimeTakenByAllAdaptersInSec) || (metric.Key.Contains(UnitTestTelemetryDataConstants.TotalTestsRun)))
                {
                    var newValue = Double.Parse(metric.Value);
                    Double oldValue;

                    if (adapterRunTestTime.TryGetValue(metric.Key, out oldValue))
                    {
                        adapterRunTestTime[metric.Key] = newValue + oldValue;
                    }

                    else
                    {
                        adapterRunTestTime.Add(metric.Key, newValue);
                    }
                }

                else if(metric.Key.Contains(UnitTestTelemetryDataConstants.RunState))
                {
                    AddMetric(UnitTestTelemetryDataConstants.RunState, metric.Value);
                }

                else if (metric.Key.Contains(UnitTestTelemetryDataConstants.TotalTestsRanByAdapter))
                {
                    var value = Double.Parse(metric.Value);
                    Double tests;

                    if (adapterUrisAndTestCount.TryGetValue(metric.Key, out tests))
                    {
                        adapterUrisAndTestCount[metric.Key] = value + tests;
                    }

                    else
                    {
                        adapterUrisAndTestCount.Add(metric.Key, value);
                    }
                }
            }
        }

        // Will Handle Discvoery Complete Metrics and Add to the Metric Collector.
        public static void HandleDiscoveryCompleteMetrics(IDictionary<string, string> metrics)
        {
            if (metrics == null || metrics.Count == 0)
            {
                return;
            }

            foreach (var metric in metrics)
            {
                AddMetric(metric.Key, metric.Value);
            }
        }

        // Will Add all Metrics from TestHost Process in Metric Dictioanry. This will handle all parallel case scenarios as well.
        public static void AddTestRunCompleteMetrics()
        {
            if (adapterRunTestTime != null && adapterRunTestTime.Count != 0)
            {
                foreach (var dataPoint in adapterRunTestTime)
                {
                    AddMetric(dataPoint.Key, dataPoint.Value.ToString());
                }
            }

            if (adapterUrisAndTestCount != null && adapterUrisAndTestCount.Count != 0)
            {
                foreach (var dataPoint in adapterUrisAndTestCount)
                {
                    AddMetric(dataPoint.Key, dataPoint.Value.ToString());
                }

                AddMetric(UnitTestTelemetryDataConstants.NumberOfAdapterUsedToRunTests, adapterUrisAndTestCount.Count.ToString());
            }         
        }

        // For Handling Parallel Case Scenario. In case of Parallel,it will be max Time taken.
        public static void HandleExecutionEngineTime(double value)
        {
            if(value > maxExecutionTime)
            {
                maxExecutionTime = value;
            }

            AddMetric(UnitTestTelemetryDataConstants.TimeTakenToStartExecutionEngineExe, maxExecutionTime.ToString());
        }

        public static void Dispose()
        {
            metricCollector?.Clear();
            adapterRunTestTime?.Clear();
            adapterUrisAndTestCount?.Clear();
            maxExecutionTime = 0;
        }
    }
}
