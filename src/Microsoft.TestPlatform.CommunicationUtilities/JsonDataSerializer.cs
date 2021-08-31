// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JsonDataSerializes serializes and deserializes data using Json format
    /// </summary>
    public class JsonDataSerializer : IDataSerializer
    {
        private static JsonDataSerializer instance;

        private static JsonSerializer payloadSerializer; // payload serializer for version <= 1
        private static JsonSerializer payloadSerializer2; // payload serializer for version >= 2
        private static JsonSerializer serializer; // generic serializer

        /// <summary>
        /// Prevents a default instance of the <see cref="JsonDataSerializer"/> class from being created.
        /// </summary>
        private JsonDataSerializer()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            serializer = JsonSerializer.Create();
            payloadSerializer = JsonSerializer.Create(jsonSettings);
            payloadSerializer2 = JsonSerializer.Create(jsonSettings);

            payloadSerializer.ContractResolver = new TestPlatformContractResolver1();
            payloadSerializer2.ContractResolver = new DefaultTestPlatformContractResolver();

#if TRACE_JSON_SERIALIZATION
            // MemoryTraceWriter can help diagnose serialization issues. Enable it for
            // debug builds only.
            // Note that MemoryTraceWriter is not thread safe, please don't use it in parallel
            // test runs. See https://github.com/JamesNK/Newtonsoft.Json/issues/1279
            payloadSerializer.TraceWriter = new MemoryTraceWriter();
            payloadSerializer2.TraceWriter = new MemoryTraceWriter();
#endif
        }

        /// <summary>
        /// Gets the JSON Serializer instance.
        /// </summary>
        public static JsonDataSerializer Instance
        {
            get
            {
                return instance ?? (instance = new JsonDataSerializer());
            }
        }

        /// <summary>
        /// Deserialize a <see cref="Message"/> from raw JSON text.
        /// </summary>
        /// <param name="rawMessage">JSON string.</param>
        /// <returns>A <see cref="Message"/> instance.</returns>
        public Message DeserializeMessage(string rawMessage)
        {
            return this.Deserialize<VersionedMessage>(serializer, rawMessage);
        }

        /// <summary>
        /// Deserialize the <see cref="Message.Payload"/> for a message.
        /// </summary>
        /// <param name="message">A <see cref="Message"/> object.</param>
        /// <typeparam name="T">Payload type.</typeparam>
        /// <returns>The deserialized payload.</returns>
        public T DeserializePayload<T>(Message message)
        {
            var versionedMessage = message as VersionedMessage;
            var payloadSerializer = this.GetPayloadSerializer(versionedMessage?.Version);
            return this.Deserialize<T>(payloadSerializer, message.Payload);
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
            var payloadSerializer = this.GetPayloadSerializer(version);
            return this.Deserialize<T>(payloadSerializer, json);
        }

        /// <summary>
        /// Serialize an empty message.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializeMessage(string messageType)
        {
            return this.Serialize(serializer, new Message { MessageType = messageType });
        }

        /// <summary>
        /// Serialize a message with payload.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="payload">Payload for the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializePayload(string messageType, object payload)
        {
            return this.SerializePayload(messageType, payload, 1);
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
            var payloadSerializer = this.GetPayloadSerializer(version);
            var serializedPayload = JToken.FromObject(payload, payloadSerializer);

            return version > 1 ?
                this.Serialize(serializer, new VersionedMessage { MessageType = messageType, Version = version, Payload = serializedPayload }) :
                this.Serialize(serializer, new Message { MessageType = messageType, Payload = serializedPayload });
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
            var payloadSerializer = this.GetPayloadSerializer(version);
            return this.Serialize(payloadSerializer, data);
        }

        /// <inheritdoc/>
        public T Clone<T>(T obj)
        {
            if (obj == null)
            {
                return default;
            }

            var stringObj = this.Serialize<T>(obj, 2);
            return this.Deserialize<T>(stringObj, 2);
        }

        /// <summary>
        /// Serialize data.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="serializer">Serializer.</param>
        /// <param name="data">Data to be serialized.</param>
        /// <returns>Serialized data.</returns>
        private string Serialize<T>(JsonSerializer serializer, T data)
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, data);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Deserialize data.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="serializer">Serializer.</param>
        /// <param name="data">Data to be deserialized.</param>
        /// <returns>Deserialized data.</returns>
        private T Deserialize<T>(JsonSerializer serializer, string data)
        {
            using (var stringReader = new StringReader(data))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        /// <summary>
        /// Deserialize JToken object to T object.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="serializer">Serializer.</param>
        /// <param name="jToken">JToken to be deserialized.</param>
        /// <returns>Deserialized data.</returns>
        private T Deserialize<T>(JsonSerializer serializer, JToken jToken)
        {
            return jToken.ToObject<T>(serializer);
        }

        private JsonSerializer GetPayloadSerializer(int? version)
        {
            if (version == null)
            {
                version = 1;
            }

            switch (version)
            {
                // 0 is used during negotiation
                case 0:
                case 1:
                // Protocol version 3 was accidentally used with serializer v1 and not
                // serializer v2, we downgrade to protocol 2 when 3 would be negotiated
                // unless this is disabled by VSTEST_DISABLE_PROTOCOL_3_VERSION_DOWNGRADE
                // env variable.
                case 3:
                    return payloadSerializer;
                case 2:
                case 4:
                case 5:
                    return payloadSerializer2;
                default:
                    throw new NotSupportedException($"Protocol version {version} is not supported. " +
                        "Ensure it is compatible with the latest serializer or add a new one.");
            }
        }
    }
}
