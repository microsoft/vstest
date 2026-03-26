// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;

/// <summary>
/// Helper for comparing System.Text.Json (STJ) serialization output against the
/// original Newtonsoft.Json implementation.
/// </summary>
internal static class NewtonsoftComparisonHelper
{
    private static readonly NewtonsoftJsonDataSerializer Instance = NewtonsoftJsonDataSerializer.Instance;

    /// <summary>
    /// Serialize with the original Newtonsoft implementation for comparison.
    /// </summary>
    public static string SerializePayload(string messageType, object payload, int version)
        => Instance.SerializePayload(messageType, payload, version);

    /// <summary>
    /// Assert that System.Text.Json and Newtonsoft produce identical JSON for the same payload.
    /// </summary>
    public static void AssertMatchesNewtonsoft(string messageType, object payload, int version)
    {
        var stjJson = JsonDataSerializer.Instance.SerializePayload(messageType, payload, version);
        var newtonsoftJson = Instance.SerializePayload(messageType, payload, version);

        // Normalize both for comparison: collapse whitespace AND sort object properties
        // so the comparison is order-independent (Newtonsoft and STJ may emit properties
        // in different order depending on the target framework).
        var normalizedStj = NormalizeJson(stjJson);
        var normalizedNewtonsoft = NormalizeJson(newtonsoftJson);

        Assert.AreEqual(normalizedNewtonsoft, normalizedStj,
            $"STJ output differs from Newtonsoft for {messageType} v{version}.\n" +
            $"Newtonsoft:\n{newtonsoftJson}\n\nSTJ:\n{stjJson}");
    }

    /// <summary>
    /// Normalize JSON by sorting object properties alphabetically (recursively)
    /// and collapsing whitespace, so comparisons are order- and format-independent.
    /// </summary>
    private static string NormalizeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        using var doc = JsonDocument.Parse(json);
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            WriteSorted(writer, doc.RootElement);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteSorted(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, System.StringComparer.Ordinal))
                {
                    writer.WritePropertyName(prop.Name);
                    WriteSorted(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSorted(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}

#endif