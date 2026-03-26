// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET
using System.Text.Json;
#endif

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Wire-format tests for <see cref="MessageType.TestMessage"/> ("TestSession.Message").
///
/// This message is sent by the test host to report log/trace output (informational, warning, error).
/// The payload is <see cref="TestMessagePayload"/> which contains a severity level and a text message.
///
/// Payload is identical for V1 and V2 serializers because it contains no TestCase/TestResult objects.
/// The only difference is the outer envelope: V1 omits the Version field, V7 includes it.
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class TestMessageSerializationTests
{
    // ── Example payload ──────────────────────────────────────────────────
    // A warning message that a test was skipped. Uses single quotes in the
    // text to exercise character escaping.
    private static readonly TestMessagePayload Payload = new()
    {
        MessageLevel = TestMessageLevel.Warning,
        Message = "Test 'CalculatorTests.AddTest' was skipped: requires .NET 8"
    };

    // ── V1 wire format ───────────────────────────────────────────────────
    // Protocol versions 0, 1, 3 — no Version field in the envelope.
    private static readonly string V1Json = """
        {
          "MessageType": "TestSession.Message",
          "Payload": {
            "MessageLevel": 1,
            "Message": "Test \u0027CalculatorTests.AddTest\u0027 was skipped: requires .NET 8"
          }
        }
        """;

    // ── V7 wire format ───────────────────────────────────────────────────
    // Protocol versions 2, 4, 5, 6, 7 — includes Version in the envelope.
    private static readonly string V7Json = """
        {
          "Version": 7,
          "MessageType": "TestSession.Message",
          "Payload": {
            "MessageLevel": 1,
            "Message": "Test \u0027CalculatorTests.AddTest\u0027 was skipped: requires .NET 8"
          }
        }
        """;

    // ── Serialize ────────────────────────────────────────────────────────

    [TestMethod]
    public void SerializePayloadV1()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.TestMessage, Payload, version: 1);

        AssertJsonEqual(V1Json, json);
    }

    [TestMethod]
    public void SerializePayloadV7()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.TestMessage, Payload, version: 7);

        AssertJsonEqual(V7Json, json);
    }

    // ── Deserialize ──────────────────────────────────────────────────────

    [TestMethod]
    public void DeserializePayloadV1()
    {
        // Simulate receiving a V1 message: deserialize the full message,
        // then extract the payload through DeserializePayload<T>.
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V1Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TestMessageLevel.Warning, result.MessageLevel);
        Assert.AreEqual("Test 'CalculatorTests.AddTest' was skipped: requires .NET 8", result.Message);
    }

    [TestMethod]
    public void DeserializePayloadV7()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V7Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TestMessageLevel.Warning, result.MessageLevel);
        Assert.AreEqual("Test 'CalculatorTests.AddTest' was skipped: requires .NET 8", result.Message);
    }

    // ── Round-trip ───────────────────────────────────────────────────────

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void RoundTrip(int version)
    {
        // Serialize → deserialize → verify the C# object survives the trip.
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.TestMessage, Payload, version);
        var message = JsonDataSerializer.Instance.DeserializeMessage(json);
        var result = JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(Payload.MessageLevel, result.MessageLevel);
        Assert.AreEqual(Payload.Message, result.Message);
    }

    // ── Newtonsoft comparison ────────────────────────────────────────────

#if NET
    [TestMethod]
    public void NewtonsoftComparisonV1()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.TestMessage, Payload, version: 1);
    }

    [TestMethod]
    public void NewtonsoftComparisonV7()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.TestMessage, Payload, version: 7);
    }
#endif

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Compare JSON ignoring whitespace differences (so the pretty-printed
    /// golden strings can be compared against the compact serializer output).
    /// </summary>
#if NET
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
#else
    private static void AssertJsonEqual(string expected, string actual)
    {
        static string Normalize(string json)
            => Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.None);

        Assert.AreEqual(Normalize(expected), Normalize(actual),
            $"JSON mismatch.\nExpected:\n{expected}\nActual:\n{actual}");
    }

    private static string Minify(string json)
        => Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.None);
#endif
}
