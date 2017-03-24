// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
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

        private static JsonSerializer serializer;

        /// <summary>
        /// Prevents a default instance of the <see cref="JsonDataSerializer"/> class from being created.
        /// </summary>
        private JsonDataSerializer()
        {
            serializer = JsonSerializer.Create(
                            new JsonSerializerSettings
                                {
                                    ContractResolver = new TestPlatformContractResolver(),
                                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                                    DateParseHandling = DateParseHandling.DateTimeOffset,
                                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                    TypeNameHandling = TypeNameHandling.None
                                });
#if DEBUG
            // MemoryTraceWriter can help diagnose serialization issues. Enable it for
            // debug builds only.
            serializer.TraceWriter = new MemoryTraceWriter();
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
            return JsonConvert.DeserializeObject<Message>(rawMessage);
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

            // TODO: Currently we use json serializer auto only for non-testmessage types
            // CHECK: Can't we just use auto for everything
            if (MessageType.TestMessage.Equals(message.MessageType))
            {
                retValue = message.Payload.ToObject<T>();
            }
            else
            {
                retValue = message.Payload.ToObject<T>(serializer);
            }

            return retValue;
        }

        /// <summary>
        /// Deserialize raw JSON to an object using the default serializer.
        /// </summary>
        /// <param name="json">JSON string.</param>
        /// <typeparam name="T">Target type to deserialize.</typeparam>
        /// <returns>An instance of <see cref="T"/>.</returns>
        public T Deserialize<T>(string json)
        {
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
            JToken serializedPayload = null;

            // TODO: Currently we use json serializer auto only for non-testmessage types
            // CHECK: Can't we just use auto for everything
            if (MessageType.TestMessage.Equals(messageType))
            {
                serializedPayload = JToken.FromObject(payload);
            }
            else
            {
                serializedPayload = JToken.FromObject(payload, serializer);
            }

            return JsonConvert.SerializeObject(new Message { MessageType = messageType, Payload = serializedPayload });
        }

        /// <summary>
        /// Serialize an object to JSON using default serialization settings.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="data">Instance of the object to serialize.</param>
        /// <returns>JSON string.</returns>
        public string Serialize<T>(T data)
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, data);

                return stringWriter.ToString();
            }
        }
    }
}
