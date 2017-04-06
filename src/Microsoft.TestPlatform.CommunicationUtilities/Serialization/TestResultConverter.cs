﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Newtonsoft.Json;

    /// <summary>
    /// Converter used by v1 protocol serializer to serialize TestResult object to and from v1 json
    /// </summary>
    public class TestResultConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(TestResult) == objectType;
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // We do not need this as SetPropetyValue inside StoreKvpList will
            // set the properties as expected.
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // P2 to P1
            var testResult = value as TestResult;
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