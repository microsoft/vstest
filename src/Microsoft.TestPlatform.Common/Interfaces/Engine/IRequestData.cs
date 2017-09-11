// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    /// <summary>
    /// Provide common services and data for a discovery/run request.
    /// </summary>
    public interface IRequestData
    {
        /// <summary>
        /// Gets an instance of <see cref="IMetricsCollector"/>.
        /// </summary>
        IMetricsCollector MetricsCollector { get; }
    }
}
