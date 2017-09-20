// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher
{
    using System.Collections.Generic;

    /// <summary>
    /// This class will be initialized if Telemetry is opted out.
    /// </summary>
    public class NoOpMetricsPublisher : IMetricsPublisher
    {
        /// <summary>
        /// Will do NO-OP.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="metrics"></param>
        public void PublishMetrics(string eventName, IDictionary<string, string> metrics)
        {
            // No Opertation
        }

        /// <summary>
        /// Will do NO-OP
        /// </summary>
        public void Dispose()
        {
            // No operation
        }
    }
}
