// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Newtonsoft.Json;

    /// <inheritdoc/>
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
            serializer.Populate(reader, testCase);

            testCase.Id = testCase.GetPropertyValue<Guid>(TestCaseProperties.Id, Guid.Empty);
            testCase.FullyQualifiedName = testCase.GetPropertyValue<string>(TestCaseProperties.FullyQualifiedName, null);
            testCase.DisplayName = testCase.GetPropertyValue<string>(TestCaseProperties.DisplayName, null);
            testCase.Source = testCase.GetPropertyValue<string>(TestCaseProperties.Source, null);
            testCase.ExecutorUri = testCase.GetPropertyValue<Uri>(TestCaseProperties.ExecutorUri, null);
            testCase.CodeFilePath = testCase.GetPropertyValue<string>(TestCaseProperties.CodeFilePath, null);
            testCase.LineNumber = testCase.GetPropertyValue<int>(TestCaseProperties.LineNumber, -1);
            return testCase;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // P2 to P1
            var testCase = value as TestCase;

            testCase.SetPropertyValue<string>(TestCaseProperties.FullyQualifiedName, testCase.FullyQualifiedName);
            testCase.SetPropertyValue<Uri>(TestCaseProperties.ExecutorUri, testCase.ExecutorUri);
            testCase.SetPropertyValue<string>(TestCaseProperties.Source, testCase.Source);
            testCase.SetPropertyValue<Guid>(TestCaseProperties.Id, testCase.Id);
            if (!testCase.DisplayName.Equals(testCase.FullyQualifiedName))
            {
                testCase.SetPropertyValue<string>(TestCaseProperties.DisplayName, testCase.DisplayName);
            }

            if (!string.IsNullOrEmpty(testCase.CodeFilePath))
            {
                testCase.SetPropertyValue<string>(TestCaseProperties.CodeFilePath, testCase.CodeFilePath);
            }

            if (testCase.LineNumber >= 0)
            {
                testCase.SetPropertyValue<int>(TestCaseProperties.LineNumber, testCase.LineNumber);
            }

            var properties = testCase.GetProperties();

            writer.WriteStartObject();
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
