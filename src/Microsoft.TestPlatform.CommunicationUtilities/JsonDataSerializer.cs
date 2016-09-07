// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
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

        private JsonDataSerializer()
        {
            ITraceWriter traceWriter = new MemoryTraceWriter();
            serializer = JsonSerializer.Create(
                            new JsonSerializerSettings
                                {
                                    TraceWriter = traceWriter,
                                    ContractResolver = new TestPlatformContractResolver(),
                                    TypeNameHandling = TypeNameHandling.None
                                });
        }

        public static JsonDataSerializer Instance
        {
            get
            {
                return instance ?? (instance = new JsonDataSerializer());
            }
        }

        public Message DeserializeMessage(string rawMessage)
        {
            return JsonConvert.DeserializeObject<Message>(rawMessage);
        }

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

        public string SerializeObject(string messageType)
        {
            return JsonConvert.SerializeObject(new Message { MessageType = messageType });
        }

        public string SerializeObject(string messageType, object payload)
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
    }
}
