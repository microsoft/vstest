// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization.Legacy;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Legacy Newtonsoft.Json-based serializer used as a fallback when
/// VSTEST_USE_NEWTONSOFT_JSON_SERIALIZER is set. This preserves the original
/// serialization behavior from before the System.Text.Json migration.
/// </summary>
internal class LegacyNewtonsoftJsonDataSerializer : IDataSerializer
{
    private static readonly JsonSerializer PayloadSerializerV1;
    private static readonly JsonSerializer PayloadSerializerV2;
    private static readonly JsonSerializer Serializer;

    static LegacyNewtonsoftJsonDataSerializer()
    {
        var jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            TypeNameHandling = TypeNameHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ConstructorHandling = ConstructorHandling.Default,
            MetadataPropertyHandling = MetadataPropertyHandling.Default,
            Formatting = Formatting.None,
            FloatParseHandling = FloatParseHandling.Double,
            FloatFormatHandling = FloatFormatHandling.String,
            StringEscapeHandling = StringEscapeHandling.Default,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Culture = CultureInfo.InvariantCulture,
            CheckAdditionalContent = false,
            DateFormatString = @"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK",
            MaxDepth = 64,
        };

        Serializer = JsonSerializer.Create();
        PayloadSerializerV1 = JsonSerializer.Create(jsonSettings);
        PayloadSerializerV2 = JsonSerializer.Create(jsonSettings);

        PayloadSerializerV1.ContractResolver = new LegacyTestPlatformContractResolver1();
        PayloadSerializerV2.ContractResolver = new LegacyDefaultTestPlatformContractResolver();
    }

    /// <inheritdoc/>
    public Message DeserializeMessage(string rawMessage)
    {
        var legacyMsg = Deserialize<LegacyVersionedMessage>(rawMessage);
        if (legacyMsg is null)
        {
            return new Message();
        }

        return new Message
        {
            Version = legacyMsg.Version,
            MessageType = legacyMsg.MessageType,
            RawMessage = rawMessage,
        };
    }

    /// <inheritdoc/>
    public T? DeserializePayload<T>(Message? message)
    {
        if (message is null || message.RawMessage is null)
        {
            return default;
        }

        var payloadSerializer = GetPayloadSerializer(message.Version);

        // Parse the raw JSON to extract the Payload field, then deserialize via JToken
        var legacyMsg = Deserialize<LegacyVersionedMessage>(message.RawMessage);
        if (legacyMsg?.Payload is null)
        {
            return default;
        }

        return legacyMsg.Payload.ToObject<T>(payloadSerializer);
    }

    /// <inheritdoc/>
    public string SerializeMessage(string? messageType)
    {
        return Serialize(Serializer, new LegacyMessage { MessageType = messageType });
    }

    /// <inheritdoc/>
    public string SerializePayload(string? messageType, object? payload)
    {
        return SerializePayload(messageType, payload, 1);
    }

    /// <inheritdoc/>
    public string SerializePayload(string? messageType, object? payload, int version)
    {
        var payloadSerializer = GetPayloadSerializer(version);

        if (payload is null)
        {
            return string.Empty;
        }

        var serializedPayload = JToken.FromObject(payload, payloadSerializer);

        return version > 1
            ? Serialize(Serializer, new LegacyVersionedMessage { MessageType = messageType, Version = version, Payload = serializedPayload })
            : Serialize(Serializer, new LegacyMessage { MessageType = messageType, Payload = serializedPayload });
    }

    /// <summary>
    /// Deserialize raw JSON to an object.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Mirrors JsonDataSerializer public API")]
    public T? Deserialize<T>(string json, int version = 1)
    {
        var payloadSerializer = GetPayloadSerializer(version);
        return Deserialize<T>(payloadSerializer, json);
    }

    /// <summary>
    /// Serialize an object to JSON.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Mirrors JsonDataSerializer public API")]
    public string Serialize<T>(T data, int version = 1)
    {
        var payloadSerializer = GetPayloadSerializer(version);
        return Serialize(payloadSerializer, data);
    }

    /// <inheritdoc/>
    [return: NotNullIfNotNull("obj")]
    public T? Clone<T>(T? obj)
    {
        if (obj is null)
        {
            return default;
        }

        var stringObj = Serialize(obj, 2);
        return Deserialize<T>(stringObj, 2)!;
    }

    private static string Serialize<T>(JsonSerializer serializer, T data)
    {
        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, data);
        return stringWriter.ToString();
    }

    private static T? Deserialize<T>(JsonSerializer serializer, string data)
    {
        using var stringReader = new StringReader(data);
        using var jsonReader = new JsonTextReader(stringReader);
        return serializer.Deserialize<T>(jsonReader);
    }

    private static T? Deserialize<T>(string data)
    {
        return Deserialize<T>(Serializer, data);
    }

    private static JsonSerializer GetPayloadSerializer(int? version)
    {
        version ??= 1;

        return version switch
        {
            0 or 1 or 3 => PayloadSerializerV1,
            2 or 4 or 5 or 6 or 7 => PayloadSerializerV2,

            _ => throw new NotSupportedException($"Protocol version {version} is not supported. "
                + "Ensure it is compatible with the latest serializer or add a new one."),
        };
    }

    /// <summary>
    /// Internal message type with JToken payload for Newtonsoft serialization.
    /// </summary>
    private class LegacyMessage
    {
        public string? MessageType { get; set; }
        public JToken? Payload { get; set; }
    }

    /// <summary>
    /// Internal versioned message type with JToken payload for Newtonsoft serialization.
    /// </summary>
    private class LegacyVersionedMessage : LegacyMessage
    {
        public int Version { get; set; }
    }
}
