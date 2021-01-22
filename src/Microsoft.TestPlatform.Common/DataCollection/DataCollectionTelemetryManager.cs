// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System;
using System.Linq;

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    /// <summary>
    /// Stores and provides telemetry information for data collection.
    /// </summary>
    internal class DataCollectionTelemetryManager : IDataCollectionTelemetryManager
    {
        private const string CorProfilerVariable = "COR_PROFILER";
        private const string CoreClrProfilerVariable = "CORECLR_PROFILER";
        private const string ClrIeInstrumentationMethodConfigurationPrefix32Variable = "MicrosoftInstrumentationEngine_ConfigPath32_";
        private const string ClrIeInstrumentationMethodConfigurationPrefix64Variable = "MicrosoftInstrumentationEngine_ConfigPath64_";

        private const string VanguardProfilerGuid = "{e5f256dc-7959-4dd6-8e4f-c11150ab28e0}";
        private const string ClrIeProfilerGuid = "{324f817a-7420-4e6d-b3c1-143fbed6d855}";
        private const string IntellitraceProfilerGuid = "{9317ae81-bcd8-47b7-aaa1-a28062e41c71}";

        private const string VanguardProfilerName = "vanguard";
        private const string ClrIeProfilerName = "clrie";
        private const string IntellitraceProfilerName = "intellitrace";
        private const string UnknownProfilerName = "unknown";
        private const string OverwrittenProfilerName = "overwritten";

        private readonly IRequestData requestData;

        internal DataCollectionTelemetryManager(IRequestData requestData)
        {
            this.requestData = requestData;
        }

        /// <inheritdoc/>
        public void RecordEnvironmentVariableAddition(DataCollectorInformation dataCollectorInformation, string name, string value)
        {
            RecordProfilerMetricForNewVariable(CorProfilerVariable, TelemetryDataConstants.DataCollectorsCorProfiler, dataCollectorInformation, name, value);
            RecordProfilerMetricForNewVariable(CoreClrProfilerVariable, TelemetryDataConstants.DataCollectorsCoreClrProfiler, dataCollectorInformation, name, value);
        }

        /// <inheritdoc/>
        public void RecordEnvironmentVariableConflict(DataCollectorInformation dataCollectorInformation, string name, string value, string existingValue)
        {
            RecordProfilerMetricForConflictedVariable(CorProfilerVariable, TelemetryDataConstants.DataCollectorsCorProfiler, dataCollectorInformation, name, value, existingValue);
            RecordProfilerMetricForConflictedVariable(CoreClrProfilerVariable, TelemetryDataConstants.DataCollectorsCoreClrProfiler, dataCollectorInformation, name, value, existingValue);
        }

        private void RecordProfilerMetricForNewVariable(string profilerVariable, string telemetryPrefix, DataCollectorInformation dataCollectorInformation, string name, string value)
        {
            if (!string.Equals(profilerVariable, name, StringComparison.Ordinal))
            {
                return;
            }

            requestData.MetricsCollection.Add(GetTelemetryKey(telemetryPrefix, dataCollectorInformation), GetProfilerName(value));
        }

        private void RecordProfilerMetricForConflictedVariable(string profilerVariable, string telemetryPrefix, DataCollectorInformation dataCollectorInformation, string name, string value, string existingValue)
        {
            // If data collector is requesting same profiler record it same as new
            if (string.Equals(value, existingValue, StringComparison.OrdinalIgnoreCase))
            {
                RecordProfilerMetricForNewVariable(profilerVariable, telemetryPrefix, dataCollectorInformation, name, value);
                return;
            }

            if (!string.Equals(profilerVariable, name, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(existingValue, ClrIeProfilerGuid, StringComparison.OrdinalIgnoreCase))
            {
                if (dataCollectorInformation.TestExecutionEnvironmentVariables != null &&
                    dataCollectorInformation.TestExecutionEnvironmentVariables.Any(pair => pair.Key.StartsWith(ClrIeInstrumentationMethodConfigurationPrefix32Variable)) &&
                    dataCollectorInformation.TestExecutionEnvironmentVariables.Any(pair => pair.Key.StartsWith(ClrIeInstrumentationMethodConfigurationPrefix64Variable)))
                {
                    requestData.MetricsCollection.Add(GetTelemetryKey(telemetryPrefix, dataCollectorInformation), ClrIeProfilerName);
                    return;
                }
            }

            requestData.MetricsCollection.Add(GetTelemetryKey(telemetryPrefix, dataCollectorInformation), $"{OverwrittenProfilerName}({GetProfilerName(value)})");
        }

        private static string GetProfilerName(string profilerGuid)
        {
            return profilerGuid.ToLower() switch
            {
                VanguardProfilerGuid => VanguardProfilerName,
                ClrIeProfilerGuid => ClrIeProfilerName,
                IntellitraceProfilerGuid => IntellitraceProfilerName,
                _ => UnknownProfilerName
            };
        }

        private static string GetTelemetryKey(string telemetryPrefix, DataCollectorInformation dataCollectorInformation)
        {
            return string.Format("{0}.{1}", telemetryPrefix, dataCollectorInformation.DataCollectorConfig?.TypeUri?.ToString());
        }
    }
}
