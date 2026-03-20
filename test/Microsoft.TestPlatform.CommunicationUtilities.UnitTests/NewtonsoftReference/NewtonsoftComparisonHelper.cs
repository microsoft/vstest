// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        // Normalize both for comparison (whitespace-insensitive)
        var normalizedStj = NormalizeJson(stjJson);
        var normalizedNewtonsoft = NormalizeJson(newtonsoftJson);

        Assert.AreEqual(normalizedNewtonsoft, normalizedStj,
            $"STJ output differs from Newtonsoft for {messageType} v{version}.\n" +
            $"Newtonsoft:\n{newtonsoftJson}\n\nSTJ:\n{stjJson}");
    }

    private static string NormalizeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return System.Text.Json.JsonSerializer.Serialize(doc.RootElement);
    }
}
