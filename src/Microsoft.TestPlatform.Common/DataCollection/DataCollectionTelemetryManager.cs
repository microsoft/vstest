// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
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

        private const string VanguardProfilerGuid = "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}";
        private const string ClrIeProfilerGuid = "{324F817A-7420-4E6D-B3C1-143FBED6D855}";
        private const string IntellitraceProfilerGuid = "{9317ae81-bcd8-47b7-aaa1-a28062e41c71}";

        private const string VanguardProfilerName = "vanguard";
        private const string ClrIeProfilerName = "clrie";
        private const string IntellitraceProfilerName = "intellitrace";
        private const string UnknownProfilerName = "unknown";
        private const string OverwrittenProfilerName = "overwritten";

        private const string CorProfilerTelemetryTemplate = "VS.TestPlatform.DataCollector.{0}.CorProfiler";
        private const string CoreClrProfilerTelemetryTemplate = "VS.TestPlatform.DataCollector.{0}.CoreClrProfiler";

        private readonly IRequestData requestData;

        internal DataCollectionTelemetryManager(IRequestData requestData)
        {
            this.requestData = requestData;
        }

        /// <inheritdoc/>
        public void OnEnvironmentVariableAdded(DataCollectorInformation dataCollectorInformation, string name, string value)
        {
            AddProfilerMetricForNewVariable(CorProfilerVariable, CorProfilerTelemetryTemplate, dataCollectorInformation, name, value);
            AddProfilerMetricForNewVariable(CoreClrProfilerVariable, CoreClrProfilerTelemetryTemplate, dataCollectorInformation, name, value);
        }

        /// <inheritdoc/>
        public void OnEnvironmentVariableConflict(DataCollectorInformation dataCollectorInformation, string name, string existingValue)
        {
            AddProfilerMetricForConflictedVariable(CorProfilerVariable, CorProfilerTelemetryTemplate, dataCollectorInformation, name, existingValue);
            AddProfilerMetricForConflictedVariable(CoreClrProfilerVariable, CoreClrProfilerTelemetryTemplate, dataCollectorInformation, name, existingValue);
        }

        private void AddProfilerMetricForNewVariable(string profilerVariable, string telemetryTemplateName, DataCollectorInformation dataCollectorInformation, string name, string value)
        {
            if (!string.Equals(profilerVariable, name, StringComparison.Ordinal))
            {
                return;
            }

            requestData.MetricsCollection.Add(string.Format(telemetryTemplateName, dataCollectorInformation.DataCollectorConfig?.TypeUri?.ToString()), GetProfilerName(value));
        }

        private void AddProfilerMetricForConflictedVariable(string profilerVariable, string telemetryTemplateName, DataCollectorInformation dataCollectorInformation, string name, string existingValue)
        {
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
                    requestData.MetricsCollection.Add(string.Format(telemetryTemplateName, dataCollectorInformation.DataCollectorConfig?.TypeUri?.ToString()), ClrIeProfilerName);
                    return;
                }
            }

            requestData.MetricsCollection.Add(string.Format(telemetryTemplateName, dataCollectorInformation.DataCollectorConfig?.TypeUri?.ToString()), OverwrittenProfilerName);
        }

        private static string GetProfilerName(string profilerGuid)
        {
            if (string.Equals(profilerGuid, VanguardProfilerGuid, StringComparison.OrdinalIgnoreCase))
            {
                return VanguardProfilerName;
            }
            else if (string.Equals(profilerGuid, ClrIeProfilerGuid, StringComparison.OrdinalIgnoreCase))
            {
                return ClrIeProfilerName;
            }
            else if (string.Equals(profilerGuid, IntellitraceProfilerGuid, StringComparison.OrdinalIgnoreCase))
            {
                return IntellitraceProfilerName;
            }

            return UnknownProfilerName;
        }
    }
}
