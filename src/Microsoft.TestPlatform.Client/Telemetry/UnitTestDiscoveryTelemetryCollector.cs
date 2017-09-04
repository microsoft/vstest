// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.Telemetry
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    /// <inheritdoc />
    /// <summary>
    /// This Will Aggregate all the VSTelemetry Data Points for one Discovery Request.
    ///  It will send all Data Points to Default Telemetry Service Provider.
    /// </summary>
    internal class UnitTestDiscoveryTelemetryCollector : IDisposable
    {
        internal Stopwatch stopwatch;

        #region TelemetryName

        private const string TestDiscoveryCompleteEvent = "vs/unittest/testdiscoverysession";

        #endregion

        private IUnitTestTelemetryServiceProvider unitTestTelemetryServiceProvider;

        internal UnitTestDiscoveryTelemetryCollector()
        {
            this.unitTestTelemetryServiceProvider =
                TelemetryServiceProviderFactory.GetDefaultTelemetryServiceProvider();
            this.stopwatch = new Stopwatch();
        }

        internal void Start()
        {
            Debug.Assert(!this.stopwatch.IsRunning,
                "Telemetry stopwatch can be started only if it is in 'Stopped' state");
            this.stopwatch.Start();
        }

        internal void CollectAndPostTelemetrydata()
        {
            if (!this.stopwatch.IsRunning)
            {
                return;
            }

            this.stopwatch.Stop();

            // Logging total time taken for discovery
            TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.TimeTakenInSecForDiscovery,
                this.stopwatch.Elapsed.TotalSeconds.ToString());

            // Logging all Data Points collected to TelemetryEvent
            var telemetryDataPoints = TelemetryClient.GetMetrics();
            if (telemetryDataPoints != null && telemetryDataPoints.Count != 0)
            { 
                foreach (var telemetryDataPoint in telemetryDataPoints)
                {
                    this.LogTelemetryData(telemetryDataPoint.Key, telemetryDataPoint.Value);
                }
            }

            // Post the data
            this.PostTelemetryData();
        }

        public void Dispose()
        {
            // this.unitTestTelemetryServiceProvider?.Dispose();
        }

        internal virtual void PostTelemetryData()
        {
            this.unitTestTelemetryServiceProvider?.PostEvent(TestDiscoveryCompleteEvent);
        }

        internal virtual void LogTelemetryData(string propertyName, string value)
        {
            this.unitTestTelemetryServiceProvider.LogEvent(TestDiscoveryCompleteEvent, propertyName, value);
        }
    }
}
