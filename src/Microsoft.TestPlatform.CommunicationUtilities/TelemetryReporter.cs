// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

internal class TelemetryReporter : ITelemetryReporter
{
    private readonly ICommunicationManager _communicationManager;
    private readonly IDataSerializer _dataSerializer;

    public TelemetryReporter(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
    {
        _communicationManager = communicationManager;
        _dataSerializer = dataSerializer;
    }

    public void Report(TelemetryEvent telemetryEvent)
    {
        string message = _dataSerializer.SerializePayload(MessageType.TelemetryEventMessage, telemetryEvent);
        _communicationManager.SendRawMessage(message);
    }
}
