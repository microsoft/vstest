// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

#if NET451
    using Microsoft.VisualStudio.Telemetry;
#endif

    /// <summary>
    /// The metrics publisher.
    /// </summary>
    public class MetricsPublisher : IMetricsPublisher
    {
#if NET451
        private TelemetrySession session;
#endif
        public MetricsPublisher()
        {
#if NET451
            try
            {
                this.session = new TelemetrySession(TelemetryService.DefaultSession.SerializeSettings());
                this.session.IsOptedIn = true;
                this.session.Start();
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "TelemetrySession: Error in starting Telemetry session : {0}", e.Message));
                }
            }
#endif
        }

            /// <summary>
            /// Publishes the metrics
            /// </summary>
            /// <param name="eventName"></param>
            /// <param name="metrics"></param>
            public void PublishMetrics(string eventName, IDictionary<string, string> metrics)
        {
#if NET451
            if (metrics == null || metrics.Count == 0)
            {
                return;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("TelemetrySession: Sending the telemetry data to the server.");
            }

            try
            {
                var finalMetrics = RemoveInvalidCharactersFromProperties(metrics);

                TelemetryEvent telemetryEvent = new TelemetryEvent(eventName);

                foreach (var metric in finalMetrics)
                {
                    telemetryEvent.Properties[metric.Key] = metric.Value;
                }

                this.session.PostEvent(telemetryEvent);
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "TelemetrySession: Error in Posting Event: {0}", e.Message));
                }
            }
#endif
        }

        /// <summary>
        /// Dispose the Telemetry Session
        /// </summary>
        public void Dispose()
        {
#if NET451
            try
            {
                this.session.Dispose();
            }
            catch (Exception e)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "TelemetrySession: Error in Disposing Event: {0}", e.Message));
                }
            }
#endif
        }

        /// <summary>
        /// Removes the invalid characters from the properties which are not supported by VsTelemetryAPI's
        /// </summary>
        /// <param name="metrics">
        /// The metrics.
        /// </param>
        /// <returns>
        /// Removes the invalid keys from the Keys
        /// </returns>
        internal IDictionary<string, string> RemoveInvalidCharactersFromProperties(IDictionary<string, string> metrics)
        {
            if (metrics == null)
            {
                return new Dictionary<string, string>();
            }

            var finalMetrics = new Dictionary<string, string>();
            foreach (var metric in metrics)
            {
                if (metric.Key.Contains(":"))
                {
                    var invalidKey = metric.Key;
                    var validKey = invalidKey.Replace(":", string.Empty);
                    finalMetrics.Add(validKey, metric.Value);
                }
                else
                {
                    finalMetrics.Add(metric.Key, metric.Value);
                }
            }

            return finalMetrics;
        }
    }
}