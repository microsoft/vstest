// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// If Telemtry is opted out, this class will be initialized and will do No operation.
    /// </summary>
    public class NoOpMetricsCollection : IMetricsCollection
    {
        /// <summary>
        /// Will do NO-OP
        /// </summary>
        /// <param name="message"></param>
        /// <param name="value"></param>
        public void Add(string message, object value)
        {
            // No operation
        }

        /// <summary>
        /// Will return empty list
        /// </summary>
        public IDictionary<string, object> Metrics => new Dictionary<string, object>();
    }
}
