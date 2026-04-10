// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Mutable wrapper for an incoming protocol message that keeps the raw payload available while
/// allowing routing metadata and deserialized payloads to be reused across the internal pipeline.
/// </summary>
internal sealed class ProtocolEnvelope
{
    private readonly IDataSerializer _dataSerializer;
    private readonly Dictionary<Type, object?> _payloadCache = new();

    internal ProtocolEnvelope(string rawMessage, Message message, IDataSerializer dataSerializer)
    {
        RawMessage = rawMessage;
        Message = message;
        _dataSerializer = dataSerializer;
    }

    public string RawMessage { get; private set; }

    public Message Message { get; private set; }

    public string? MessageType => Message.MessageType;

    public int? Version => Message is VersionedMessage versionedMessage ? versionedMessage.Version : null;

    public T? GetPayload<T>()
    {
        if (!_payloadCache.TryGetValue(typeof(T), out object? payload))
        {
            payload = _dataSerializer.DeserializePayload<T>(Message);
            _payloadCache[typeof(T)] = payload;
        }

        return (T?)payload;
    }

    public void UpdateRawMessage(string rawMessage)
    {
        RawMessage = rawMessage;
        Message = _dataSerializer.DeserializeMessage(rawMessage);
        _payloadCache.Clear();
    }

    public void UpdatePayload<T>(string messageType, T? payload)
    {
        string rawMessage = Version is int version
            ? _dataSerializer.SerializePayload(messageType, payload, version)
            : _dataSerializer.SerializePayload(messageType, payload);

        UpdateRawMessage(rawMessage);
        _payloadCache[typeof(T)] = payload;
    }
}
