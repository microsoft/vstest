// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher
{
    using System.Threading.Tasks;

    /// <summary>
    /// Rerturns the Instance of <see cref="IMetricsPublisher"/> on basis of given parameters.
    /// </summary>
    public class MetricsPublisherFactory
    {
        /// <summary>
        /// Gets the Metrics Publisher
        /// </summary>
        /// <param name="isTelemetryOptedIn">Is Telemetry opted in or not</param>
        /// <param name="isDesignMode">Is Design Mode enabled or not</param>
        /// <returns>Returns Instance of Metrics Publisher</returns>
        public static async Task<IMetricsPublisher> GetMetricsPublisher(bool isTelemetryOptedIn, bool isDesignMode)
        {
            if (isTelemetryOptedIn && !isDesignMode)
            {
                return await Task.Run(() => new MetricsPublisher());
            }

            return await Task.Run(() => new NoOpMetricsPublisher());
        }
    }
}
