// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <inheritdoc />
    /// <summary>
    /// Provide common services and data for a discovery/run request.
    /// </summary>
    public class RequestData : IRequestData
    {
        /// <summary>
        /// The metrics collection.
        /// </summary>
        private IMetricsCollection metricsCollection;

        /// <summary>
        /// The protocol config.
        /// </summary>
        private ProtocolConfig protocolConfig;

        /// <summary>
        /// Gets or sets the metrics collection.
        /// </summary>
        public IMetricsCollection MetricsCollection
        {
            get => this.metricsCollection;
            set => this.metricsCollection = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the protocol config.
        /// </summary>
        public ProtocolConfig ProtocolConfig
        {
            get => this.protocolConfig;
            set => this.protocolConfig = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether is telemetry opted in.
        /// </summary>
        public bool IsTelemetryOptedIn { get; set; }
    }
}
