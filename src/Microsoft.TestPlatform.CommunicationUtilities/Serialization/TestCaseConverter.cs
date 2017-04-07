// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Converter used by v1 protocol serializer to serialize TestCase object to and from v1 json
    /// </summary>
    public class TestCaseConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(TestCase) == objectType;
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var testCase = new TestCase();

            var data = JObject.Load(reader);
            var properties = data["Properties"];

            if (properties != null && properties.HasValues)
            {
                // Every class that inherits from TestObject uses a properties store for <Property, Object>
                // key value pairs.
                foreach (var property in properties.Values<JToken>())
                {
                    var testProperty = property["Key"].ToObject<TestProperty>();

                    // Let the null values be passed in as null data
                    var token = property["Value"];
                    string propertyData = null;
                    if (token.Type != JTokenType.Null)
                    {
                        // If the property is already a string. No need to convert again.
                        if (token.Type == JTokenType.String)
                        {
                            propertyData = token.ToObject<string>();
                        }
                        else
                        {
                            // On deserialization, the value for each TestProperty is always a string. It is up
                            // to the consumer to deserialize it further as appropriate.
                            propertyData = token.ToString(Formatting.None).Trim('"');
                        }
                    }

                    switch (testProperty.Id)
                    {
                        case "TestCase.Id":
                            testCase.Id = Guid.Parse(propertyData); break;
                        case "TestCase.ExecutorUri":
                            testCase.ExecutorUri = new Uri(propertyData); break;
                        case "TestCase.FullyQualifiedName":
                            testCase.FullyQualifiedName = propertyData; break;
                        case "TestCase.DisplayName":
                            testCase.DisplayName = propertyData; break;
                        case "TestCase.Source":
                            testCase.Source = propertyData; break;
                        case "TestCase.CodeFilePath":
                            testCase.CodeFilePath = propertyData; break;
                        case "TestCase.LineNumber":
                            testCase.LineNumber = int.Parse(propertyData); break;
                        default:
                            testCase.SetPropertyValue(testProperty, propertyData);
                            break;
                    }
                }
            }

            return testCase;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // P2 to P1
            var testCase = value as TestCase;

            writer.WriteStartObject();
            writer.WritePropertyName("Properties");
            writer.WriteStartArray();

            this.AddProperty(writer, TestCaseProperties.FullyQualifiedName, testCase.FullyQualifiedName, serializer);
            this.AddProperty(writer, TestCaseProperties.ExecutorUri, testCase.ExecutorUri, serializer);
            this.AddProperty(writer, TestCaseProperties.Source, testCase.Source, serializer);
            this.AddProperty(writer, TestCaseProperties.CodeFilePath, testCase.CodeFilePath, serializer);
            this.AddProperty(writer, TestCaseProperties.DisplayName, testCase.DisplayName, serializer);
            this.AddProperty(writer, TestCaseProperties.Id, testCase.Id, serializer);
            this.AddProperty(writer, TestCaseProperties.LineNumber, testCase.LineNumber, serializer);

            var properties = testCase.GetProperties();
            foreach (var property in properties)
            {
                serializer.Serialize(writer, property);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void AddProperty(JsonWriter writer, TestProperty property, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            serializer.Serialize(writer, property);
            writer.WritePropertyName("Value");
            if (value is int)
            {
                writer.WriteValue((int)value);
            }
            else
            {
                writer.WriteValue(value?.ToString());
            }

            writer.WriteEndObject();
        }
    }
}