// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

/// <summary>
/// Internal pipeline hook that allows handlers to inspect or enrich a protocol message without
/// forcing additional deserialize / reserialize passes on every stage.
/// </summary>
internal interface IProtocolEnvelopeHandler
{
    void HandleProtocolMessage(ProtocolEnvelope protocolEnvelope);
}

internal static class ProtocolEnvelopeExtensions
{
    internal static void DispatchProtocolMessage(this ITestMessageEventHandler? handler, ProtocolEnvelope protocolEnvelope)
    {
        if (handler is null)
        {
            return;
        }

        if (handler is IProtocolEnvelopeHandler protocolEnvelopeHandler)
        {
            protocolEnvelopeHandler.HandleProtocolMessage(protocolEnvelope);
            return;
        }

        handler.HandleRawMessage(protocolEnvelope.RawMessage);
    }

    internal static void DispatchRawMessage(this ITestMessageEventHandler? handler, string rawMessage, IDataSerializer dataSerializer)
    {
        if (handler is null)
        {
            return;
        }

        if (handler is IProtocolEnvelopeHandler protocolEnvelopeHandler)
        {
            protocolEnvelopeHandler.HandleProtocolMessage(new ProtocolEnvelope(rawMessage, dataSerializer.DeserializeMessage(rawMessage), dataSerializer));
            return;
        }

        handler.HandleRawMessage(rawMessage);
    }
}
