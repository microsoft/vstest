// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization.Legacy;

/// <summary>
/// Original Newtonsoft.Json TestRunStatistics converter for the legacy fallback serializer.
/// </summary>
internal class LegacyTestRunStatisticsConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanConvert(Type objectType)
    {
        return objectType.Equals(typeof(TestRunStatistics));
    }

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<TestRunStatistics>(reader);
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
