// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Interface for extensions that choose to send telemetry events
/// </summary>
public interface ITelemetryEventsProvider
{
    /// <summary>
    /// Gets telemetry events that should be propagated by Test Platform
    /// </summary>
    /// <returns>Telemetry events that should be propagated by Test Platform</returns>
    IEnumerable<TelemetryEvent> GetTelemetryEvents();
}
