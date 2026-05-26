// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

/// <summary>
/// JSON converter for <see cref="Exception"/> that handles the read-only properties
/// (<c>Message</c>, <c>StackTrace</c>) which STJ's source-generated metadata cannot populate
/// via the parameterless constructor. Uses the <c>Exception(string, Exception)</c> constructor
/// to preserve Message and InnerException during deserialization.
/// <para>
/// Note: the original exception type is erased during deserialization — all exceptions are
/// materialized as <see cref="Exception"/>. <c>StackTrace</c> is not round-trippable because
/// it is computed, not settable. <c>HResult</c> and <c>Source</c> are restored where present.
/// </para>
/// </summary>
internal class ExceptionConverter : JsonConverter<Exception>
{
    /// <inheritdoc/>
    public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        string? message = root.TryGetProperty("Message", out var msgProp) && msgProp.ValueKind != JsonValueKind.Null
            ? msgProp.GetString()
            : null;

        Exception? innerException = null;
        if (root.TryGetProperty("InnerException", out var innerProp) && innerProp.ValueKind != JsonValueKind.Null)
        {
            innerException = StjSafe.Deserialize<Exception>(innerProp.GetRawText(), options);
        }

        var exception = message is not null
            ? new Exception(message, innerException)
            : new Exception();

        if (root.TryGetProperty("HResult", out var hresultProp) && hresultProp.ValueKind == JsonValueKind.Number)
        {
            exception.HResult = hresultProp.GetInt32();
        }

        if (root.TryGetProperty("Source", out var sourceProp) && sourceProp.ValueKind == JsonValueKind.String)
        {
            exception.Source = sourceProp.GetString();
        }

        return exception;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("ClassName", value.GetType().FullName);
        writer.WriteString("Message", value.Message);
        writer.WriteString("StackTraceString", value.StackTrace);
        writer.WriteString("Source", value.Source);
        writer.WriteNumber("HResult", value.HResult);

        writer.WritePropertyName("InnerException");
        if (value.InnerException is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            StjSafe.Serialize(writer, value.InnerException, options);
        }

        writer.WriteEndObject();
    }
}

#endif
