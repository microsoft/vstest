// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// JSON converter for converting ITestRunStatistics to TestRunStatistics
    /// </summary>
    public class TestRunStatisticsConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType.Equals(typeof(TestRunStatistics));
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TestRunStatistics>(reader);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}