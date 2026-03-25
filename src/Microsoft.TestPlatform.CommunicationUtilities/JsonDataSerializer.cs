// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

#if !NETFRAMEWORK
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
#else
using Jsonite;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
#endif

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// JsonDataSerializes serializes and deserializes data using Json format
/// </summary>
public class JsonDataSerializer : IDataSerializer
{
    private static JsonDataSerializer? s_instance;

    private static readonly bool UseNewtonsoftFallback = FeatureFlag.Instance.IsSet(FeatureFlag.VSTEST_USE_NEWTONSOFT_JSON_SERIALIZER);
    private static readonly LegacyNewtonsoftJsonDataSerializer? NewtonsoftFallback = UseNewtonsoftFallback
        ? new LegacyNewtonsoftJsonDataSerializer() : null;

#if !NETFRAMEWORK
    private static readonly bool DisableFastJson = FeatureFlag.Instance.IsSet(FeatureFlag.VSTEST_DISABLE_FASTER_JSON_SERIALIZATION);

    private static readonly JsonSerializerOptions PayloadOptionsV1; // payload options for version <= 1
    private static readonly JsonSerializerOptions PayloadOptionsV2; // payload options for version >= 2
    private static readonly JsonSerializerOptions FastOptions; // options for faster json
    private static readonly JsonSerializerOptions DefaultOptions; // generic options

    static JsonDataSerializer()
    {
        DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            MaxDepth = 64,
            WriteIndented = false,
            PropertyNamingPolicy = null, // PascalCase (same as Newtonsoft default)
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new TestPropertyConverter(),
                new ObjectConverter(),
                new AttachmentSetConverter(),
                new UriDataAttachmentConverter(),
                new TestExecutionContextConverter(),
                new TestRunCompleteEventArgsConverter(),
                new TestRunChangedEventArgsConverter(),
                new AfterTestRunEndResultConverter(),
                new TestProcessAttachDebuggerPayloadConverter(),
                new TestSessionInfoConverter(),
                new DiscoveryCriteriaConverter(),
            },
        };

        // V2 options: TestObjectConverter and TestRunStatisticsConverter only
        PayloadOptionsV2 = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            MaxDepth = 64,
            WriteIndented = false,
            PropertyNamingPolicy = null,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new TestPropertyConverter(),
                new ObjectConverter(),
                new TestCaseConverterV2(),
                new TestResultConverterV2(),
                new TestObjectConverter(),
                new TestRunStatisticsConverter(),
                new AttachmentSetConverter(),
                new UriDataAttachmentConverter(),
                new TestExecutionContextConverter(),
                new TestObjectBaseConverterFactory(),
                new TestRunCompleteEventArgsConverter(),
                new TestRunChangedEventArgsConverter(),
                new AfterTestRunEndResultConverter(),
                new TestProcessAttachDebuggerPayloadConverter(),
                new TestSessionInfoConverter(),
                new DiscoveryCriteriaConverter(),
            },
        };

        // V1 options: adds TestCaseConverter and TestResultConverter on top of V2 converters
        PayloadOptionsV1 = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            MaxDepth = 64,
            WriteIndented = false,
            PropertyNamingPolicy = null,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new TestPropertyConverter(),
                new ObjectConverter(),
                new TestCaseConverter(),
                new TestResultConverter(),
                new TestObjectConverter(),
                new TestRunStatisticsConverter(),
                new AttachmentSetConverter(),
                new UriDataAttachmentConverter(),
                new TestExecutionContextConverter(),
                new TestObjectBaseConverterFactory(),
                new TestRunCompleteEventArgsConverter(),
                new TestRunChangedEventArgsConverter(),
                new AfterTestRunEndResultConverter(),
                new TestProcessAttachDebuggerPayloadConverter(),
                new TestSessionInfoConverter(),
                new DiscoveryCriteriaConverter(),
            },
        };

        // Fast options: same as V2
        FastOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            MaxDepth = 64,
            WriteIndented = false,
            PropertyNamingPolicy = null,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new TestPropertyConverter(),
                new ObjectConverter(),
                new TestCaseConverterV2(),
                new TestResultConverterV2(),
                new TestObjectConverter(),
                new TestRunStatisticsConverter(),
                new AttachmentSetConverter(),
                new UriDataAttachmentConverter(),
                new TestExecutionContextConverter(),
                new TestObjectBaseConverterFactory(),
                new TestRunCompleteEventArgsConverter(),
                new TestRunChangedEventArgsConverter(),
                new AfterTestRunEndResultConverter(),
                new TestProcessAttachDebuggerPayloadConverter(),
                new TestSessionInfoConverter(),
                new DiscoveryCriteriaConverter(),
            },
        };
    }
#endif

    /// <summary>
    /// Prevents a default instance of the <see cref="JsonDataSerializer"/> class from being created.
    /// </summary>
    private JsonDataSerializer() { }

    /// <summary>
    /// Gets the JSON Serializer instance.
    /// </summary>
    public static JsonDataSerializer Instance => s_instance ??= new JsonDataSerializer();

    /// <summary>
    /// Deserialize a <see cref="Message"/> from raw JSON text.
    /// </summary>
    /// <param name="rawMessage">JSON string.</param>
    /// <returns>A <see cref="Message"/> instance.</returns>
    public Message DeserializeMessage(string rawMessage)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.DeserializeMessage(rawMessage);
        }

        // Try fast header parse first (string parsing, no JSON library)
        if (!FastHeaderParse(rawMessage, out int version, out string? messageType))
        {
            // Fallback: parse just the header fields from JSON
#if !NETFRAMEWORK
            using var doc = JsonDocument.Parse(rawMessage);
            var root = doc.RootElement;
            version = root.TryGetProperty("Version", out var vProp)
                ? (vProp.ValueKind == JsonValueKind.Number ? vProp.GetInt32() : int.TryParse(vProp.GetString(), out var v) ? v : 0)
                : 0;
            messageType = root.TryGetProperty("MessageType", out var mtProp) ? mtProp.GetString() : null;
#else
            var parsed = (JsonObject)Json.Deserialize(rawMessage);
            version = parsed.TryGetValue("Version", out var vObj) ? Convert.ToInt32(vObj, System.Globalization.CultureInfo.InvariantCulture) : 0;
            messageType = parsed.TryGetValue("MessageType", out var mtObj) ? (string?)mtObj : null;
#endif
        }

        return new Message
        {
            Version = version,
            MessageType = messageType,
            RawMessage = rawMessage,
        };
    }

    /// <summary>
    /// Deserialize the payload from a <see cref="Message"/>.
    /// </summary>
    /// <param name="message">A <see cref="Message"/> object.</param>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <returns>The deserialized payload.</returns>
    public T? DeserializePayload<T>(Message? message)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.DeserializePayload<T>(message);
        }

        if (message is null || message.RawMessage is null)
        {
            return default;
        }

#if !NETFRAMEWORK
        var payloadOptions = GetPayloadOptions(message.Version);

        if (payloadOptions == PayloadOptionsV2)
        {
            // Fast path: deserialize payload directly from raw message
            var messageWithPayload = DeserializeObjectFast<PayloadedMessage<T>>(message.RawMessage);
            return messageWithPayload is null ? default : messageWithPayload.Payload;
        }
        else
        {
            // V1 path: need to parse the Payload field as a separate step
            // because V1 converters (TestCaseConverter, TestResultConverter) need special handling
            using var doc = JsonDocument.Parse(message.RawMessage);
            if (doc.RootElement.TryGetProperty("Payload", out var payloadElement))
            {
                return JsonSerializer.Deserialize<T>(payloadElement.GetRawText(), payloadOptions);
            }
            return default;
        }
#else
        var parsed = (JsonObject)Json.Deserialize(message.RawMessage);
        if (!parsed.TryGetValue("Payload", out var payloadObj))
            return default;

        return JsoniteConvert.To<T>(payloadObj);
#endif
    }

#if !NETFRAMEWORK
    private static T? DeserializeObjectFast<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, FastOptions);
    }
#endif

    private static bool FastHeaderParse(string rawMessage, out int version, out string? messageType)
    {
        // PERF: This can be also done slightly better using ReadOnlySpan<char> but we don't have that available by default in .NET Framework
        // and the speed improvement does not warrant additional dependency. This is already taking just few ms for 10k messages.
        version = 0;
        messageType = null;

        try
        {
            // The incoming messages look like this, or like this:
            // {"Version":6,"MessageType":"TestExecution.GetTestRunnerProcessStartInfoForRunAll","Payload":{
            // {"MessageType":"TestExecution.GetTestRunnerProcessStartInfoForRunAll","Payload":{
            if (rawMessage.Length < 31)
            {
                // {"MessageType":"T","Payload":1} with length 31 is the smallest valid message we should be able to parse..
                return false;
            }

            // If the message is not versioned then the start quote of the message type string is at index 15 {"MessageType":"
            int messageTypeStartQuoteIndex = 15;
            int versionInt = 0;
            if (rawMessage[2] == 'V')
            {
                // This is a potential versioned message that looks like this:
                // {"Version":6,"MessageType":"TestExecution.GetTestRunnerProcessStartInfoForRunAll","Payload":{

                // Version ':' is on index 10, the number starts at the next index. Find wher the next ',' is and grab that as number.
                var versionColonIndex = 10;
                if (rawMessage[versionColonIndex] != ':')
                {
                    return false;
                }

                var firstVersionNumberIndex = 11;
                // The message is versioned, get the version and update the position of first quote that contains message type.
                if (!TryGetSubstringUntilDelimiter(rawMessage, firstVersionNumberIndex, ',', maxSearchLength: 4, out string? versionString, out int versionCommaIndex))
                {
                    return false;
                }

                // Message type delmiter is at at versionCommaIndex + the length of '"MessageType":"' which is 15 chars
                messageTypeStartQuoteIndex = versionCommaIndex + 15;

                if (!int.TryParse(versionString, out versionInt))
                {
                    return false;
                }
            }
            else if (rawMessage[2] != 'M' || rawMessage[12] != 'e')
            {
                // Message is not versioned message, and it is also not message that starts with MessageType
                return false;
            }

            if (rawMessage[messageTypeStartQuoteIndex] != '"')
            {
                return false;
            }

            int messageTypeStartIndex = messageTypeStartQuoteIndex + 1;
            // "TestExecution.LaunchAdapterProcessWithDebuggerAttachedCallback" is the longest message type we currently have with 62 chars
            if (!TryGetSubstringUntilDelimiter(rawMessage, messageTypeStartIndex, '"', maxSearchLength: 100, out string? messageTypeString, out _))
            {
                return false;
            }

            version = versionInt;
            messageType = messageTypeString;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///  Try getting substring until a given delimiter, but don't search more characters than maxSearchLength.
    /// </summary>
    private static bool TryGetSubstringUntilDelimiter(string rawMessage, int start, char character, int maxSearchLength, out string? substring, out int delimiterIndex)
    {
        var length = rawMessage.Length;
        var searchEnd = start + maxSearchLength;
        for (int i = start; i < length && i <= searchEnd; i++)
        {
            if (rawMessage[i] == character)
            {
                delimiterIndex = i;
                substring = rawMessage.Substring(start, i - start);
                return true;
            }
        }

        delimiterIndex = -1;
        substring = null;
        return false;
    }

    /// <summary>
    /// Deserialize raw JSON to an object using the default serializer.
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <param name="version">Version of serializer to be used.</param>
    /// <typeparam name="T">Target type to deserialize.</typeparam>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the public API")]
    public T? Deserialize<T>(string json, int version = 1)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.Deserialize<T>(json, version);
        }

#if !NETFRAMEWORK
        var options = GetPayloadOptions(version);
        return Deserialize<T>(options, json);
#else
        var obj = Json.Deserialize(json);
        return JsoniteConvert.To<T>(obj);
#endif
    }

    /// <summary>
    /// Serialize an empty message.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializeMessage(string? messageType)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.SerializeMessage(messageType);
        }

#if !NETFRAMEWORK
        return Serialize(DefaultOptions, new MessageEnvelope { MessageType = messageType });
#else
        var envelope = new JsonObject { ["MessageType"] = messageType! };
        return Json.Serialize(envelope);
#endif
    }

    /// <summary>
    /// Serialize a message with payload.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="payload">Payload for the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializePayload(string? messageType, object? payload)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.SerializePayload(messageType, payload);
        }

        return SerializePayload(messageType, payload, 1);
    }

    /// <summary>
    /// Serialize a message with payload.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="payload">Payload for the message.</param>
    /// <param name="version">Version for the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializePayload(string? messageType, object? payload, int version)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.SerializePayload(messageType, payload, version);
        }

#if !NETFRAMEWORK
        var payloadOptions = GetPayloadOptions(version);
        // Fast json is only equivalent to the serialization that is used for protocol version 2 and upwards (or more precisely for the paths that use PayloadOptionsV2)
        // so when we resolved the old options we should use non-fast path.
        if (DisableFastJson || payloadOptions == PayloadOptionsV1)
        {
            if (payload is null)
                return string.Empty;

            var serializedPayload = JsonSerializer.SerializeToElement(payload, payloadOptions);

            return version > 1 ?
                Serialize(DefaultOptions, new VersionedMessageEnvelope { MessageType = messageType, Version = version, Payload = serializedPayload }) :
                Serialize(DefaultOptions, new MessageEnvelope { MessageType = messageType, Payload = serializedPayload });
        }
        else
        {
            return Serialize(FastOptions, new VersionedMessageForSerialization { MessageType = messageType, Version = version, Payload = payload });
        }
#else
        if (payload is null)
            return string.Empty;

        var payloadValue = JsoniteConvert.ToJsonValue(payload, version);

        if (version > 1)
        {
            var envelope = new JsonObject
            {
                ["Version"] = version,
                ["MessageType"] = messageType!,
                ["Payload"] = payloadValue!,
            };
            return Json.Serialize(envelope);
        }
        else
        {
            var envelope = new JsonObject
            {
                ["MessageType"] = messageType!,
                ["Payload"] = payloadValue!,
            };
            return Json.Serialize(envelope);
        }
#endif
    }

    /// <summary>
    /// Serialize an object to JSON using default serialization settings.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <param name="data">Instance of the object to serialize.</param>
    /// <param name="version">Version to be stamped.</param>
    /// <returns>JSON string.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the public API")]
    public string Serialize<T>(T data, int version = 1)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.Serialize(data, version);
        }

#if !NETFRAMEWORK
        var options = GetPayloadOptions(version);
        return Serialize(options, data);
#else
        var jsonValue = JsoniteConvert.ToJsonValue(data, version);
        return Json.Serialize(jsonValue!);
#endif
    }

    /// <inheritdoc/>
    [return: NotNullIfNotNull("obj")]
    public T? Clone<T>(T? obj)
    {
        if (NewtonsoftFallback is not null)
        {
            return NewtonsoftFallback.Clone(obj);
        }

        if (obj is null)
        {
            return default;
        }

        var stringObj = Serialize(obj, 2);
        return Deserialize<T>(stringObj, 2)!;
    }

#if !NETFRAMEWORK
    /// <summary>
    /// Serialize data.
    /// </summary>
    /// <typeparam name="T">Type of data.</typeparam>
    /// <param name="options">Serializer options.</param>
    /// <param name="data">Data to be serialized.</param>
    /// <returns>Serialized data.</returns>
    private static string Serialize<T>(JsonSerializerOptions options, T data)
    {
        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Deserialize data.
    /// </summary>
    /// <typeparam name="T">Type of data.</typeparam>
    /// <param name="options">Serializer options.</param>
    /// <param name="data">Data to be deserialized.</param>
    /// <returns>Deserialized data.</returns>
    private static T? Deserialize<T>(JsonSerializerOptions options, string data)
    {
        return JsonSerializer.Deserialize<T>(data, options);
    }

    private static JsonSerializerOptions GetPayloadOptions(int? version)
    {
        version ??= 1;

        return version switch
        {
            // 0 is used during negotiation.
            // Protocol version 3 was accidentally used with serializer v1 and not
            // serializer v2, we downgrade to protocol 2 when 3 would be negotiated
            // unless this is disabled by VSTEST_DISABLE_PROTOCOL_3_VERSION_DOWNGRADE
            // env variable.
            0 or 1 or 3 => PayloadOptionsV1,
            2 or 4 or 5 or 6 or 7 => PayloadOptionsV2,

            _ => throw new NotSupportedException($"Protocol version {version} is not supported. "
                + "Ensure it is compatible with the latest serializer or add a new one."),
        };
    }

    /// <summary>
    /// This grabs payload from the message, we already know version and message type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class PayloadedMessage<T>
    {
        public T? Payload { get; set; }
    }

    /// <summary>
    /// Serialization-only DTO for building the JSON wire format (without Version).
    /// NOT a Message — this is never returned to callers.
    /// </summary>
    private class MessageEnvelope
    {
        public string? MessageType { get; set; }
        public object? Payload { get; set; }
    }

    /// <summary>
    /// Serialization-only DTO for building the JSON wire format (with Version).
    /// NOT a Message — this is never returned to callers.
    /// </summary>
    private class VersionedMessageEnvelope
    {
        public int Version { get; set; }
        public string? MessageType { get; set; }
        public object? Payload { get; set; }
    }

    /// <summary>
    /// For serialization directly into string, without first converting to JsonElement, and then from JsonElement to string.
    /// </summary>
    private class VersionedMessageForSerialization
    {
        /// <summary>
        /// Gets or sets the version of the message
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public string? MessageType { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        public object? Payload { get; set; }
    }
#endif
}
