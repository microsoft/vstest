// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    /// <summary>
    /// The Metrics collection factory.
    /// </summary>
    public class MetricsCollectionFactory
    {
        private bool telemetryOptedOut;

        /// <summary>
        /// Creates Instance of <see cref="MetricsCollectionFactory"/> class.
        /// </summary>
        /// <param name="isOptedOut">Telemetry opted out or not.</param>
        public MetricsCollectionFactory(bool isOptedOut)
        {
            this.telemetryOptedOut = isOptedOut;
        }

        /// <summary>
        /// Returns the Metrics Collection.
        /// </summary>
        /// <returns>Returns the MetricsCollection on basis of Telemetry opted in or not</returns>
        public IMetricsCollection GetMetricsCollection()
        {
            if (this.telemetryOptedOut)
            {
                return new NoOpMetricsCollection();
            }

            return new MetricsCollection();
        }
    }
}
