namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JSON converter for the <see cref="TestObject"/> and derived entities.
    /// </summary>
    public class TestObjectConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(List<KeyValuePair<TestProperty, object>>))
            {
                // Support only deserialization of KeyValuePair list
                throw new ArgumentException(nameof(objectType));
            }

            var propertyList = new List<KeyValuePair<TestProperty, object>>();

            if (reader.TokenType == JsonToken.StartArray)
            {
                var properties = JArray.Load(reader);
                if (properties != null && properties.HasValues)
                {
                    // Every class that inherits from TestObject uses a properties store for <Property, Object>
                    // key value pairs.
                    foreach (var property in properties)
                    {
                        var testProperty = property["Key"].ToObject<TestProperty>();

                        // Let the null values be passed in as null data
                        object propertyData = null;
                        if (property["Value"].Type != JTokenType.Null)
                        {
                            // On deserialization, the value for each TestProperty is always a string. It is up
                            // to the consumer to deserialize it further as appropriate.
                            propertyData = property["Value"].ToString(Formatting.None).Trim('"');
                        }

                        propertyList.Add(new KeyValuePair<TestProperty, object>(testProperty, propertyData));
                    }
                }
            }

            return propertyList;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Create an array of <Property, Value> dictionary
        }
    }
}