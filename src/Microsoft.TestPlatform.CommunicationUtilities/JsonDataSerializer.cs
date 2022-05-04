// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// JsonDataSerializes serializes and deserializes data using Json format
/// </summary>
public class JsonDataSerializer : IDataSerializer
{
    private static JsonDataSerializer s_instance;

    private static readonly bool DisableFastJson = FeatureFlag.Instance.IsSet(FeatureFlag.DISABLE_FASTER_JSON_SERIALIZATION);

    private static JsonSerializerSettings s_jsonSettings1; // serializer settings for v1
    private static JsonSerializerSettings s_fastJsonSettings; // serializer settings for v2 and faster json

    /// <summary>
    /// Prevents a default instance of the <see cref="JsonDataSerializer"/> class from being created.
    /// </summary>
    private JsonDataSerializer()
    {
        s_jsonSettings1 = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            TypeNameHandling = TypeNameHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

            ContractResolver = new TestPlatformContractResolver1(),
        };

        s_fastJsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = s_jsonSettings1.DateFormatHandling,
            DateParseHandling = s_jsonSettings1.DateParseHandling,
            DateTimeZoneHandling = s_jsonSettings1.DateTimeZoneHandling,
            TypeNameHandling = s_jsonSettings1.TypeNameHandling,
            ReferenceLoopHandling = s_jsonSettings1.ReferenceLoopHandling,
            // PERF: Null value handling has very small impact on serialization and deserialization. Enabling it does not warrant the risk we run
            // of changing how our consumers get their data.
            // NullValueHandling = NullValueHandling.Ignore,

            ContractResolver = new DefaultTestPlatformContractResolver(),
        };
    }

    /// <summary>
    /// Gets the JSON Serializer instance.
    /// </summary>
    public static JsonDataSerializer Instance => s_instance ??= new JsonDataSerializer();

    /// <summary>
    /// Deserialize a <see cref="RoutableMessage"/> from raw JSON text.
    /// </summary>
    /// <param name="rawMessage">JSON string.</param>
    /// <returns>A <see cref="RoutableMessage"/> instance.</returns>
    public RoutableMessage DeserializeMessage(string rawMessage)
    {
        // PERF: Try grabbing the version and message type from the string directly, we are pretty certain how the message is serialized
        // when the format does not match all we do is that we check if 6th character in the message is 'V'
        if (!FastHeaderParse(rawMessage, out int version, out string messageType))
        {
            // PERF: If the fast path fails, deserialize into header object that does not have any Payload. When the message type info
            // is at the start of the message, this is also pretty fast. Again, this won't touch the payload.
            MessageHeader header = DeserializeWithSettings<MessageHeader>(rawMessage, s_jsonSettings1);
            version = header.Version;
            messageType = header.MessageType;
        }

        var message = new RoutableMessage
        {
            Version = version,
            MessageType = messageType,
            RawMessage = rawMessage,
        };

        return message;
    }

    /// <summary>
    /// Deserialize the <see cref="RoutableMessage.Payload"/> for a message.
    /// </summary>
    /// <param name="message">A <see cref="RoutableMessage"/> object.</param>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <returns>The deserialized payload.</returns>
    public T DeserializePayload<T>(RoutableMessage message)
    {
        var settings = GetSerializerSettings(message.Version);
        var rawMessage = message.RawMessage;

        // PERF: Fast path is compatibile only with protocol versions that use serializer_2,
        // and this is faster than deserializing via deserializer_2.
        var messageWithPayload = DeserializeWithSettings<MessagePayload<T>>(rawMessage, settings);
        return messageWithPayload.Payload;
    }

    private bool FastHeaderParse(string rawMessage, out int version, out string messageType)
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
                if (!TryGetSubstringUntilDelimiter(rawMessage, firstVersionNumberIndex, ',', maxSearchLength: 4, out string versionString, out int versionCommaIndex))
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
            if (!TryGetSubstringUntilDelimiter(rawMessage, messageTypeStartIndex, '"', maxSearchLength: 100, out string messageTypeString, out _))
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
    private bool TryGetSubstringUntilDelimiter(string rawMessage, int start, char character, int maxSearchLength, out string substring, out int delimiterIndex)
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
    /// <returns>An instance of <see cref="T"/>.</returns>
    public T Deserialize<T>(string json, int version = 1)
    {
        var settings = GetSerializerSettings(version);
        return DeserializeWithSettings<T>(json, settings);
    }

    /// <summary>
    /// Serialize an empty message.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializeMessage(string messageType)
    {
        return Serialize(new RoutableMessage { MessageType = messageType }, 1);
    }

    /// <summary>
    /// Serialize a message with payload.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="payload">Payload for the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializePayload(string messageType, object payload)
    {
        return SerializePayload(messageType, payload, 1);
    }

    /// <summary>
    /// Serialize a message with payload.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="payload">Payload for the message.</param>
    /// <param name="version">Version for the message.</param>
    /// <returns>Serialized message.</returns>
    public string SerializePayload(string messageType, object payload, int version)
    {
        var settings = GetSerializerSettings(version);
        // REVIEW: possibly we can also serialize this without the version if settings end up being the legacy settings,
        // so the receiver gets the message without the Version field, keeping it more backwards compatible.
        return SerializeWithSettings(new SerializedVersionedMessage { MessageType = messageType, Version = version, Payload = payload }, settings);
    }

    /// <summary>
    /// Serialize an object to JSON using default serialization settings.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <param name="data">Instance of the object to serialize.</param>
    /// <param name="version">Version to be stamped.</param>
    /// <returns>JSON string.</returns>
    public string Serialize<T>(T data, int version = 1)
    {
        var settings = GetSerializerSettings(version);
        return SerializeWithSettings(data, settings);
    }

    /// <inheritdoc/>
    public T Clone<T>(T obj)
    {
        if (obj == null)
        {
            return default;
        }

        var stringObj = Serialize(obj, 2);
        return Deserialize<T>(stringObj, 2);
    }

    /// <summary>
    /// Serialize data.
    /// </summary>
    /// <typeparam name="T">Type of data.</typeparam>
    /// <param name="settings">Serializer.</param>
    /// <param name="data">Data to be serialized.</param>
    /// <returns>Serialized data.</returns>
    private string SerializeWithSettings<T>(T data, JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(data, settings);
    }

    /// <summary>
    /// Deserialize data.
    /// </summary>
    /// <typeparam name="T">Type of data.</typeparam>
    /// <param name="serializer">Serializer.</param>
    /// <param name="data">Data to be deserialized.</param>
    /// <returns>Deserialized data.</returns>
    private T DeserializeWithSettings<T>(string data, JsonSerializerSettings settings)
    {
        return JsonConvert.DeserializeObject<T>(data, settings);
    }

    private JsonSerializerSettings GetSerializerSettings(int? version)
    {
        if (version == null)
        {
            version = 1;
        }

        // 0: the original protocol with no versioning (Message). It is used during negotiation.
        // 1: new protocol with versioning (VersionedMessage).
        // 2: changed serialization because the serialization of properties in bag was too verbose,
        //    so common properties are considered built-in and serialized without type info.
        // 3: introduced because of changes to allow attaching debugger to external process.
        // 4: introduced because 3 did not update this table and ended up using the serializer for protocol v1,
        //    which is extremely slow. We negotiate 2 or 4, but never 3 unless the flag above is set.
        // 5: ???
        // 6: accepts abort and cancel with handlers that report the status.
        return version switch
        {
            // 0 is used during negotiation.
            // Protocol version 3 was accidentally used with serializer v1 and not
            // serializer v2, we downgrade to protocol 2 when 3 would be negotiated
            // unless this is disabled by VSTEST_DISABLE_PROTOCOL_3_VERSION_DOWNGRADE
            // env variable.
            0 or 1 or 3 => s_jsonSettings1,
            2 or 4 or 5 or 6 => s_fastJsonSettings,

            _ => throw new NotSupportedException($"Protocol version {version} is not supported. "
                + "Ensure it is compatible with the latest serializer or add a new one."),
        };
    }

    /// <summary>
    /// Just the header from versioned messages, to avoid touching the Payload when we deserialize message.
    /// </summary>
    private class MessageHeader
    {
        public int Version { get; set; }
        public string MessageType { get; set; }
    }

    /// <summary>
    /// This grabs payload from the message, we already know version and message type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class MessagePayload<T>
    {
        public T Payload { get; set; }
    }

    /// <summary>
    /// For serialization directly into string, without first converting to JToken, and then from JToken to string.
    /// </summary>
    private class SerializedVersionedMessage
    {
        /// <summary>
        /// Gets or sets the version of the message
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        public object Payload { get; set; }
    }
}
