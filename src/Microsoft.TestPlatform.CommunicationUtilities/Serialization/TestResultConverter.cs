// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Newtonsoft.Json;

    /// <inheritdoc/>
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
            var testResult = new TestResult();
            serializer.Populate(reader, testResult);

            testResult.Outcome = testResult.GetPropertyValue<TestOutcome>(TestResultProperties.Outcome, TestOutcome.None);
            testResult.ErrorMessage = testResult.GetPropertyValue<string>(TestResultProperties.ErrorMessage, null);
            testResult.ErrorStackTrace = testResult.GetPropertyValue<string>(TestResultProperties.ErrorStackTrace, null);
            testResult.DisplayName = testResult.GetPropertyValue<string>(TestResultProperties.DisplayName, null);
            testResult.ComputerName = testResult.GetPropertyValue<string>(TestResultProperties.ComputerName, null);
            testResult.Duration = testResult.GetPropertyValue<TimeSpan>(TestResultProperties.Duration, TimeSpan.Zero);
            testResult.StartTime = testResult.GetPropertyValue<DateTimeOffset>(TestResultProperties.StartTime, DateTimeOffset.Now);
            testResult.EndTime = testResult.GetPropertyValue<DateTimeOffset>(TestResultProperties.EndTime, DateTimeOffset.Now);

            return testResult;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // P2 to P1
            var testResult = value as TestResult;
            if (testResult.Outcome != TestOutcome.None)
            {
                testResult.SetPropertyValue<TestOutcome>(TestResultProperties.Outcome, testResult.Outcome);
            }

            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                testResult.SetPropertyValue<string>(TestResultProperties.ErrorMessage, testResult.ErrorMessage);
            }

            if (!string.IsNullOrEmpty(testResult.ErrorStackTrace))
            {
                testResult.SetPropertyValue<string>(TestResultProperties.ErrorStackTrace, testResult.ErrorStackTrace);
            }

            if (!string.IsNullOrEmpty(testResult.DisplayName))
            {
                testResult.SetPropertyValue<string>(TestResultProperties.DisplayName, testResult.DisplayName);
            }

            if (!string.IsNullOrEmpty(testResult.ComputerName))
            {
                testResult.SetPropertyValue<string>(TestResultProperties.ComputerName, testResult.ComputerName);
            }

            if (testResult.Duration != default(TimeSpan))
            {
                testResult.SetPropertyValue<TimeSpan>(TestResultProperties.Duration, testResult.Duration);
            }

            if (testResult.StartTime != default(DateTimeOffset))
            {
                testResult.SetPropertyValue<DateTimeOffset>(TestResultProperties.StartTime, testResult.StartTime);
            }

            if (testResult.EndTime != default(DateTimeOffset))
            {
                testResult.SetPropertyValue<DateTimeOffset>(TestResultProperties.EndTime, testResult.EndTime);
            }

            var properties = testResult.GetProperties();

            writer.WriteStartObject();
            writer.WritePropertyName("TestCase");
            serializer.Serialize(writer, testResult.TestCase);
            writer.WritePropertyName("Attachments");
            serializer.Serialize(writer, testResult.Attachments);
            writer.WritePropertyName("Messages");
            serializer.Serialize(writer, testResult.Messages);

            writer.WritePropertyName("Properties");
            writer.WriteStartArray();
            foreach (var property in properties)
            {
                serializer.Serialize(writer, property);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}