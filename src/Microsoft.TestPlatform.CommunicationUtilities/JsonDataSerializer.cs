// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// JsonDataSerializes serializes and deserializes data using Json format
    /// </summary>
    public class JsonDataSerializer : IDataSerializer
    {
        private static JsonDataSerializer instance;

        private static JsonSerializer payloadSerializer;
        private static JsonSerializer payloadSerializer2;

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

            payloadSerializer = JsonSerializer.Create(jsonSettings);
            payloadSerializer2 = JsonSerializer.Create(jsonSettings);

            payloadSerializer.ContractResolver = new TestPlatformContractResolver1();
            payloadSerializer2.ContractResolver = new DefaultTestPlatformContractResolver();

#if DEBUG
            // MemoryTraceWriter can help diagnose serialization issues. Enable it for
            // debug builds only.
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
            // Convert to VersionedMessage
            // Message can be deserialized to VersionedMessage where version will be 0
            return JsonConvert.DeserializeObject<VersionedMessage>(rawMessage);
        }

        /// <summary>
        /// Deserialize the <see cref="Message.Payload"/> for a message.
        /// </summary>
        /// <param name="message">A <see cref="Message"/> object.</param>
        /// <typeparam name="T">Payload type.</typeparam>
        /// <returns>The deserialized payload.</returns>
        public T DeserializePayload<T>(Message message)
        {
            T retValue = default(T);

            var versionedMessage = message as VersionedMessage;
            var serializer = this.GetPayloadSerializer(versionedMessage?.Version);

            retValue = message.Payload.ToObject<T>(serializer);
            return retValue;
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
            var serializer = this.GetPayloadSerializer(version);

            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        /// <summary>
        /// Serialize an empty message.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializeMessage(string messageType)
        {
            return JsonConvert.SerializeObject(new Message { MessageType = messageType });
        }

        /// <summary>
        /// Serialize a message with payload.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="payload">Payload for the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializePayload(string messageType, object payload)
        {
            var serializedPayload = JToken.FromObject(payload, payloadSerializer);

            return JsonConvert.SerializeObject(new Message { MessageType = messageType, Payload = serializedPayload });
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
            var serializer = this.GetPayloadSerializer(version);
            var serializedPayload = JToken.FromObject(payload, serializer);

            var message = version > 1 ?
            new VersionedMessage { MessageType = messageType, Version = version, Payload = serializedPayload } :
            new Message { MessageType = messageType, Payload = serializedPayload };

            return JsonConvert.SerializeObject(message);
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
            var serializer = this.GetPayloadSerializer(version);

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, data);

                return stringWriter.ToString();
            }
        }

        private JsonSerializer GetPayloadSerializer(int? version)
        {
            return version == 2 ? payloadSerializer2 : payloadSerializer;
        }
    }
}
