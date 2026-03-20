// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Wire-format tests for <see cref="MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback"/>
/// ("TestExecution.LaunchAdapterProcessWithDebuggerAttachedCallback").
///
/// Callback after launching adapter process with debugger. Payload is the OS process ID.
///
/// Payload is identical for V1 and V7 serializers because it is a primitive value.
/// The only difference is the outer envelope: V1 omits the Version field, V7 includes it.
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class LaunchAdapterProcessWithDebuggerAttachedCallbackSerializationTests
{
    // ── Example payload ──────────────────────────────────────────────────
    // The OS process ID of the launched adapter process.
    private static readonly int Payload = 54321;

    // ── V1 wire format ───────────────────────────────────────────────────
    // Protocol versions 0, 1, 3 — no Version field in the envelope.
    private static readonly string V1Json = """
        {
          "MessageType": "TestExecution.LaunchAdapterProcessWithDebuggerAttachedCallback",
          "Payload": 54321
        }
        """;

    // ── V7 wire format ───────────────────────────────────────────────────
    // Protocol versions 2, 4, 5, 6, 7 — includes Version in the envelope.
    private static readonly string V7Json = """
        {
          "Version": 7,
          "MessageType": "TestExecution.LaunchAdapterProcessWithDebuggerAttachedCallback",
          "Payload": 54321
        }
        """;

    // ── Serialize ────────────────────────────────────────────────────────

    [TestMethod]
    public void SerializePayloadV1()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, Payload, version: 1);

        AssertJsonEqual(V1Json, json);
    }

    [TestMethod]
    public void SerializePayloadV7()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, Payload, version: 7);

        AssertJsonEqual(V7Json, json);
    }

    // ── Deserialize ──────────────────────────────────────────────────────

    [TestMethod]
    public void DeserializePayloadV1()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V1Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<int>(message);

        Assert.AreEqual(54321, result);
    }

    [TestMethod]
    public void DeserializePayloadV7()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V7Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<int>(message);

        Assert.AreEqual(54321, result);
    }

    // ── Round-trip ───────────────────────────────────────────────────────

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void RoundTrip(int version)
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, Payload, version);
        var message = JsonDataSerializer.Instance.DeserializeMessage(json);
        var result = JsonDataSerializer.Instance.DeserializePayload<int>(message);

        Assert.AreEqual(Payload, result);
    }

    // ── Newtonsoft comparison ────────────────────────────────────────────

    [TestMethod]
    public void NewtonsoftComparisonV1()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, Payload, version: 1);
    }

    [TestMethod]
    public void NewtonsoftComparisonV7()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.LaunchAdapterProcessWithDebuggerAttachedCallback, Payload, version: 7);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Compare JSON ignoring whitespace differences (so the pretty-printed
    /// golden strings can be compared against the compact serializer output).
    /// </summary>
    private static void AssertJsonEqual(string expected, string actual)
    {
        static string Normalize(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }

        Assert.AreEqual(Normalize(expected), Normalize(actual),
            $"JSON mismatch.\nExpected:\n{expected}\nActual:\n{actual}");
    }

    /// <summary>
    /// Strip whitespace from pretty JSON so it can be fed to DeserializeMessage
    /// (which expects compact JSON as it would arrive over the wire).
    /// </summary>
    private static string Minify(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement);
    }
}
