// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Protocol
{
    using System.Text.Json;

    /// <summary>
    /// JsonDataSerializer serializes and deserializes data using Json format
    /// </summary>
    public class JsonDataSerializer
    {
        private static JsonDataSerializer instance;
        private static JsonSerializerOptions payloadSerializerOptions;
        private static JsonSerializerOptions defaultSerializerOptions;

        /// <summary>
        /// Prevents a default instance of the <see cref="JsonDataSerializer"/> class from being created.
        /// </summary>
        private JsonDataSerializer()
        {
            defaultSerializerOptions = new JsonSerializerOptions();
            payloadSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            };
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
            return JsonSerializer.Deserialize<Message>(rawMessage);
        }

        /// <summary>
        /// Deserialize the <see cref="Message.Payload"/> for a message.
        /// </summary>
        /// <param name="message">A <see cref="Message"/> object.</param>
        /// <typeparam name="T">Payload type.</typeparam>
        /// <returns>The deserialized payload.</returns>
        public T DeserializePayload<T>(Message message)
        {
            var options = MessageType.TestMessage.Equals(message.MessageType) ?
                defaultSerializerOptions : payloadSerializerOptions;

            return message.Payload.HasValue
                ? message.Payload.Value.Deserialize<T>(options)
                : default;
        }

        /// <summary>
        /// Deserialize raw JSON to an object using the default serializer.
        /// </summary>
        /// <param name="json">JSON string.</param>
        /// <typeparam name="T">Target type to deserialize.</typeparam>
        /// <returns>An instance of <see cref="T"/>.</returns>
        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, payloadSerializerOptions);
        }

        /// <summary>
        /// Serialize an empty message.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializeMessage(string messageType)
        {
            return JsonSerializer.Serialize(new Message { MessageType = messageType });
        }

        /// <summary>
        /// Serialize a message with payload.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="payload">Payload for the message.</param>
        /// <returns>Serialized message.</returns>
        public string SerializePayload(string messageType, object payload)
        {
            var options = MessageType.TestMessage.Equals(messageType) ?
                defaultSerializerOptions : payloadSerializerOptions;

            JsonElement serializedPayload = JsonSerializer.SerializeToElement(payload, options);

            return JsonSerializer.Serialize(new Message { MessageType = messageType, Payload = serializedPayload });
        }

        /// <summary>
        /// Serialize an object to JSON using default serialization settings.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="data">Instance of the object to serialize.</param>
        /// <returns>JSON string.</returns>
        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, payloadSerializerOptions);
        }
    }
}
