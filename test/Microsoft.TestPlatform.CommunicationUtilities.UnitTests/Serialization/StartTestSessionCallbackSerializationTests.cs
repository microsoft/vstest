// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Wire-format tests for <see cref="MessageType.StartTestSessionCallback"/> ("TestSession.StartTestSessionCallback").
///
/// This message is sent as a callback after a test session has been started.
/// The payload is <see cref="StartTestSessionAckPayload"/> which contains the event args
/// with the session info and metrics.
///
/// Payload is identical for V1 and V7 because no TestCase/TestResult objects are involved.
/// The only difference is the outer envelope: V1 omits the Version field, V7 includes it.
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class StartTestSessionCallbackSerializationTests
{
    // ── Example payload ──────────────────────────────────────────────────
    private static readonly StartTestSessionAckPayload Payload = BuildPayload();

    private static StartTestSessionAckPayload BuildPayload()
    {
        var sessionInfo = new TestSessionInfo();
        typeof(TestSessionInfo).GetProperty("Id")!.SetValue(sessionInfo, new Guid("abcd1234-5678-90ab-cdef-1234567890ab"));

        return new StartTestSessionAckPayload
        {
            EventArgs = new StartTestSessionCompleteEventArgs
            {
                TestSessionInfo = sessionInfo,
                Metrics = new Dictionary<string, object>
                {
                    ["TimeTakenInSec"] = 1.5
                }
            }
        };
    }

    // ── V1 wire format ───────────────────────────────────────────────────
    // Protocol versions 0, 1, 3 — no Version field in the envelope.
    private static readonly string V1Json = """
        {
          "MessageType": "TestSession.StartTestSessionCallback",
          "Payload": {
            "EventArgs": {
              "TestSessionInfo": {
                "Id": "abcd1234-5678-90ab-cdef-1234567890ab"
              },
              "Metrics": {
                "TimeTakenInSec": 1.5
              }
            }
          }
        }
        """;

    // ── V7 wire format ───────────────────────────────────────────────────
    // Protocol versions 2, 4, 5, 6, 7 — includes Version in the envelope.
    private static readonly string V7Json = """
        {
          "Version": 7,
          "MessageType": "TestSession.StartTestSessionCallback",
          "Payload": {
            "EventArgs": {
              "TestSessionInfo": {
                "Id": "abcd1234-5678-90ab-cdef-1234567890ab"
              },
              "Metrics": {
                "TimeTakenInSec": 1.5
              }
            }
          }
        }
        """;

    // ── Serialize ────────────────────────────────────────────────────────

    [TestMethod]
    public void SerializePayloadV1()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestSessionCallback, Payload, version: 1);

        AssertJsonEqual(V1Json, json);
    }

    [TestMethod]
    public void SerializePayloadV7()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestSessionCallback, Payload, version: 7);

        AssertJsonEqual(V7Json, json);
    }

    // ── Deserialize ──────────────────────────────────────────────────────

    [Ignore("TestSessionInfo.Id has a private setter — STJ creates a new instance with a random GUID instead of populating Id")]
    [TestMethod]
    public void DeserializePayloadV1()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V1Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<StartTestSessionAckPayload>(message);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.EventArgs);
        Assert.IsNotNull(result.EventArgs.TestSessionInfo);
        Assert.AreEqual(new Guid("abcd1234-5678-90ab-cdef-1234567890ab"), result.EventArgs.TestSessionInfo.Id);
        Assert.IsNotNull(result.EventArgs.Metrics);
        Assert.IsTrue(result.EventArgs.Metrics.ContainsKey("TimeTakenInSec"));
    }

    [Ignore("TestSessionInfo.Id has a private setter — STJ creates a new instance with a random GUID instead of populating Id")]
    [TestMethod]
    public void DeserializePayloadV7()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V7Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<StartTestSessionAckPayload>(message);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.EventArgs);
        Assert.IsNotNull(result.EventArgs.TestSessionInfo);
        Assert.AreEqual(new Guid("abcd1234-5678-90ab-cdef-1234567890ab"), result.EventArgs.TestSessionInfo.Id);
        Assert.IsNotNull(result.EventArgs.Metrics);
        Assert.IsTrue(result.EventArgs.Metrics.ContainsKey("TimeTakenInSec"));
    }

    // ── Round-trip ───────────────────────────────────────────────────────

    [Ignore("TestSessionInfo.Id has a private setter — STJ creates a new instance with a random GUID instead of populating Id")]
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void RoundTrip(int version)
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestSessionCallback, Payload, version);
        var message = JsonDataSerializer.Instance.DeserializeMessage(json);
        var result = JsonDataSerializer.Instance.DeserializePayload<StartTestSessionAckPayload>(message);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.EventArgs);
        Assert.IsNotNull(result.EventArgs.TestSessionInfo);
        Assert.AreEqual(Payload.EventArgs!.TestSessionInfo!.Id, result.EventArgs.TestSessionInfo.Id);
        Assert.IsNotNull(result.EventArgs.Metrics);
        Assert.IsTrue(result.EventArgs.Metrics.ContainsKey("TimeTakenInSec"));
    }

    // ── Newtonsoft comparison ────────────────────────────────────────────

    [TestMethod]
    public void NewtonsoftComparisonV1()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.StartTestSessionCallback, Payload, version: 1);
    }

    [TestMethod]
    public void NewtonsoftComparisonV7()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.StartTestSessionCallback, Payload, version: 7);
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
