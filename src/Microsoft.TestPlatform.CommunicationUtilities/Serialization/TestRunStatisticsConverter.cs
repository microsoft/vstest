// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

/// <summary>
/// JSON converter for converting ITestRunStatistics to TestRunStatistics
/// </summary>
public class TestRunStatisticsConverter : JsonConverter<ITestRunStatistics>
{
    /// <inheritdoc/>
    public override ITestRunStatistics? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TestRunStatistics>(ref reader, options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ITestRunStatistics value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
