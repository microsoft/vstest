// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <inheritdoc />
    /// <summary>
    /// This Will Aggregate all the VSTelemetry Data Points for one Test Run Request.
    ///  It will send all Data Points to Default Telemetry Service Provider.
    /// </summary>
    internal class UnitTestRunTelemetryCollector : IDisposable
    {
        internal Stopwatch stopwatch;

        #region TelemetryName

        private const string TestRunCompeleteEvent = "vs/unittest/testrunsession";

        #endregion

        private const string XpathOfMaxCpuCount = @"/RunSettings/RunConfiguration/MaxCpuCount";

        private ITestRunRequest testRunRequest;

        private IUnitTestTelemetryServiceProvider unitTestTelemetryServiceProvider;
     
        private int executorProcessId;

        internal UnitTestRunTelemetryCollector(ITestRunRequest testRunRequest)
        {
            this.testRunRequest = testRunRequest;
            this.unitTestTelemetryServiceProvider = TelemetryServiceProviderFactory.GetDefaultTelemetryServiceProvider();

            this.stopwatch = new Stopwatch();
        }

        public void Dispose()
        {            
            // this.unitTestTelemetryServiceProvider?.Dispose();
        }

        /// <summary>
        /// collect all the telemetry data and then post it.
        /// </summary>
        internal void CollectAndPostTelemetrydata()
        {
            if (!this.stopwatch.IsRunning)
            {
                return;
            }

            this.stopwatch.Stop();

            // Fill in the time taken to complete the run
            TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.TimeTakenInSecForRun, this.stopwatch.Elapsed.TotalSeconds.ToString());
            
            //Add Telemetry Data Points from TestHost
            TelemetryClient.AddTestRunCompleteMetrics();

            // Logging all Data Points collected to TelemetryEvent
            var telemetryDataPoints = TelemetryClient.GetMetrics();
            if (telemetryDataPoints != null && telemetryDataPoints.Count != 0)
            {
                foreach (var telemetryDataPoint in telemetryDataPoints)
                {
                    this.LogTelemetryData(telemetryDataPoint.Key, telemetryDataPoint.Value);
                }
            }

            // Post the event
            this.PostTelemetryData();
        }

        internal void Start()
        {
            Debug.Assert(!this.stopwatch.IsRunning,
                "Telemetry stopwatch can be started only if it is in 'Stopped' state");

            this.stopwatch.Start();
        }

        internal void AddDataPoints()
        {
            try
            {
                // Logging if Parallel is enabled.
                TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.ParallelEnabled, this.CheckIfTestIsRunningInParallel(this.testRunRequest).ToString());

                // Fill in the Peak Working Set of MSTestExecutor process
                uint peakWorkingSet;
                try
                {
                    peakWorkingSet = (uint)Process.GetProcessById(this.executorProcessId).PeakWorkingSet64;
                }
                catch (ArgumentException)
                {
                    // MSTestExecutor process has exited
                    peakWorkingSet = 0;
                }

                // Logging peak working set
                TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.PeakWorkingSetForRun, peakWorkingSet.ToString());

                // Fill in the collectors enabled
                var collectorSettings = this.GetEnabledDataCollectors();
                var dataCollectoresEnabled = (collectorSettings != null) ? Enumerable.Select<DataCollectorSettings, string>(collectorSettings, x => x.Uri.ToString()) : Enumerable.Empty<string>();

                // Logging Data Collectors enabled
                TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.DataCollectorsEnabled, string.Join(",", dataCollectoresEnabled.ToArray()));

                // TODO: Colelct IsAppContainer when available in TPV2

                var testRunCriteria = (TestRunCriteria)this.testRunRequest.TestRunConfiguration;

                // Identify total number of sources
                var numberOfSources = (uint)(testRunCriteria.Sources != null ? testRunCriteria.Sources.Count<string>() : 0);
                TelemetryClient.AddMetric(UnitTestTelemetryDataConstants.NumberOfSourcesSentForRun, numberOfSources.ToString());

                // TODO:  Fill in TargetDevice info for Phone : Emulator/Device
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "Error computing telemetry data for unit test session : {0}", ex.ToString()));
                }
            }
        }

        internal virtual void LogTelemetryData(string propertyName, string value)
        {
            this.unitTestTelemetryServiceProvider.LogEvent(TestRunCompeleteEvent, propertyName, value);
        }

        internal virtual void PostTelemetryData()
        {
            this.unitTestTelemetryServiceProvider?.PostEvent(TestRunCompeleteEvent);
        }

        internal void UpdateProcessId(int processId)
        {
            this.executorProcessId = processId;
        }

        internal bool CheckIfTestIsRunningInParallel(ITestRunRequest runRequest)
        {
            var settingXml = ((TestRunCriteria)runRequest.TestRunConfiguration).TestRunSettings;

            var runSettingsIsPathNavigable = new XmlDocument();
            using (var xmlReader = XmlReader.Create(new StringReader(settingXml), new XmlReaderSettings() { CloseInput = true }))
            {
                runSettingsIsPathNavigable.Load(xmlReader);
            }

            var navigator = runSettingsIsPathNavigable.CreateNavigator();
            var node = navigator.SelectSingleNode((string) XpathOfMaxCpuCount);

            return node != null;
        }

        internal List<DataCollectorSettings> GetEnabledDataCollectors()
        {
            var settingsXml = ((TestRunCriteria)this.testRunRequest.TestRunConfiguration).TestRunSettings;
            var runCollectionSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settingsXml);

            if (runCollectionSettings == null || !runCollectionSettings.IsCollectionEnabled)
            {
                return null;
            }

            var collectorSettings = new List<DataCollectorSettings>();
            foreach (var settings in runCollectionSettings.DataCollectorSettingsList)
            {
                if (!settings.IsEnabled)
                {
                    continue;
                }

                if (collectorSettings.Any(
                    dcSettings => dcSettings.Uri.Equals(settings.Uri) || string.Equals(
                                      dcSettings.AssemblyQualifiedName,
                                      settings.AssemblyQualifiedName,
                                      StringComparison.OrdinalIgnoreCase)))
                {
                    // If Uri or assembly qualified type name is repeated, consider data collector as duplicate and ignore it.
                    continue;
                }

                collectorSettings.Add(settings);
            }

            return collectorSettings;
        }
    }
}
