// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Converter used by v1 protocol serializer to serialize TestResult object to and from v1 json
    /// </summary>
    public class TestResultConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(TestResult) == objectType;
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JObject.Load(reader);

            var testCase = data["TestCase"].ToObject<TestCase>(serializer);
            var testResult = new TestResult(testCase);

            testResult.Attachments = data["Attachments"].ToObject<Collection<AttachmentSet>>(serializer);
            testResult.Messages = data["Messages"].ToObject<Collection<TestResultMessage>>(serializer);
            JToken properties = data["Properties"];
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
                        case "TestResult.DisplayName":
                            testResult.DisplayName = propertyData; break;
                        case "TestResult.ComputerName":
                            testResult.ComputerName = propertyData; break;
                        case "TestResult.Outcome":
                            testResult.Outcome = (TestOutcome)Enum.Parse(typeof(TestOutcome), propertyData); break;
                        case "TestResult.Duration":
                            testResult.Duration = TimeSpan.Parse(propertyData); break;
                        case "TestResult.StartTime":
                            testResult.StartTime = DateTimeOffset.Parse(propertyData); break;
                        case "TestResult.EndTime":
                            testResult.EndTime = DateTimeOffset.Parse(propertyData); break;
                        case "TestResult.ErrorMessage":
                            testResult.ErrorMessage = propertyData; break;
                        case "TestResult.ErrorStackTrace":
                            testResult.ErrorStackTrace = propertyData; break;
                        default:
                            testResult.SetPropertyValue(testProperty, propertyData);
                            break;
                    }
                }
            }

            return testResult;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // P2 to P1
            var testResult = value as TestResult;

            writer.WriteStartObject();
            writer.WritePropertyName("TestCase");
            serializer.Serialize(writer, testResult.TestCase);
            writer.WritePropertyName("Attachments");
            serializer.Serialize(writer, testResult.Attachments);
            writer.WritePropertyName("Messages");
            serializer.Serialize(writer, testResult.Messages);

            writer.WritePropertyName("Properties");
            writer.WriteStartArray();

            // Version Note: In 15.0.0, if some properties in TestResult were not set, they were not serialized.
            // Starting 15.1.0, test platform sends in default values for properties that were not set. This is not
            // a breaking change.

            // TestResult.Outcome
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.Outcome, serializer);
            writer.WriteValue((int)testResult.Outcome);
            writer.WriteEndObject();

            // TestResult.ErrorMessage
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.ErrorMessage, serializer);
            writer.WriteValue(testResult.ErrorMessage);
            writer.WriteEndObject();

            // TestResult.ErrorStackTrace
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.ErrorStackTrace, serializer);
            writer.WriteValue(testResult.ErrorStackTrace);
            writer.WriteEndObject();

            // TestResult.DisplayName
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.DisplayName, serializer);
            writer.WriteValue(testResult.DisplayName);
            writer.WriteEndObject();

            // TestResult.ComputerName
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.ComputerName, serializer);
            writer.WriteValue(testResult.ComputerName);
            writer.WriteEndObject();

            // TestResult.Duration
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.Duration, serializer);
            writer.WriteValue(testResult.Duration);
            writer.WriteEndObject();

            // TestResult.StartTime
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.StartTime, serializer);
            writer.WriteValue(testResult.StartTime);
            writer.WriteEndObject();

            // TestResult.EndTime
            writer.WriteStartObject();
            AddProperty(writer, TestResultProperties.EndTime, serializer);
            writer.WriteValue(testResult.EndTime);
            writer.WriteEndObject();

            foreach (var property in testResult.GetProperties())
            {
                serializer.Serialize(writer, property);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void AddProperty(JsonWriter writer, TestProperty property, JsonSerializer serializer)
        {
            writer.WritePropertyName("Key");
            serializer.Serialize(writer, property);
            writer.WritePropertyName("Value");
        }
    }
}
