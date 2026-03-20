// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Wire-format tests for <see cref="MessageType.StartTestExecutionWithSources"/>
/// ("TestExecution.StartWithSources").
///
/// This message is sent from the runner to the test host to start executing tests identified
/// by their source assemblies and adapter URIs. The payload is a
/// <see cref="TestRunCriteriaWithSources"/> containing the adapter-to-source map, run settings,
/// test execution context, and optional package name.
///
/// Payload is identical for V1 and V7 serializers because it contains no TestCase/TestResult
/// objects — only primitive types, strings, and a simple <see cref="TestExecutionContext"/>.
/// The only difference is the outer envelope: V1 omits the Version field, V7 includes it.
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class StartTestExecutionWithSourcesSerializationTests
{
    // ── Example payload ──────────────────────────────────────────────────
    // A run criteria targeting two DLLs via the MSTest adapter, with
    // standard run settings and a test execution context.
    private static readonly TestRunCriteriaWithSources Payload = BuildPayload();

    // ── V1 wire format ───────────────────────────────────────────────────
    // Protocol versions 0, 1, 3 — no Version field in the envelope.
    private static readonly string V1Json = """
        {
          "MessageType": "TestExecution.StartWithSources",
          "Payload": {
            "AdapterSourceMap": {
              "executor://MSTestAdapter/v2": [
                "Contoso.Math.Tests.dll",
                "Contoso.Core.Tests.dll"
              ]
            },
            "RunSettings": "\u003CRunSettings\u003E\u003CRunConfiguration\u003E\u003CResultsDirectory\u003E.\\TestResults\u003C/ResultsDirectory\u003E\u003C/RunConfiguration\u003E\u003C/RunSettings\u003E",
            "TestExecutionContext": {
              "FrequencyOfRunStatsChangeEvent": 10,
              "RunStatsChangeEventTimeout": "00:00:30",
              "InIsolation": false,
              "KeepAlive": true,
              "AreTestCaseLevelEventsRequired": true,
              "IsDebug": false,
              "TestCaseFilter": null,
              "FilterOptions": null
            },
            "Package": "Contoso.Math.Tests"
          }
        }
        """;

    // ── V7 wire format ───────────────────────────────────────────────────
    // Protocol versions 2, 4, 5, 6, 7 — includes Version in the envelope.
    // Payload content is identical to V1 because no TestCase/TestResult is involved.
    private static readonly string V7Json = """
        {
          "Version": 7,
          "MessageType": "TestExecution.StartWithSources",
          "Payload": {
            "AdapterSourceMap": {
              "executor://MSTestAdapter/v2": [
                "Contoso.Math.Tests.dll",
                "Contoso.Core.Tests.dll"
              ]
            },
            "RunSettings": "\u003CRunSettings\u003E\u003CRunConfiguration\u003E\u003CResultsDirectory\u003E.\\TestResults\u003C/ResultsDirectory\u003E\u003C/RunConfiguration\u003E\u003C/RunSettings\u003E",
            "TestExecutionContext": {
              "FrequencyOfRunStatsChangeEvent": 10,
              "RunStatsChangeEventTimeout": "00:00:30",
              "InIsolation": false,
              "KeepAlive": true,
              "AreTestCaseLevelEventsRequired": true,
              "IsDebug": false,
              "TestCaseFilter": null,
              "FilterOptions": null
            },
            "Package": "Contoso.Math.Tests"
          }
        }
        """;

    // ── Serialize ────────────────────────────────────────────────────────

    [TestMethod]
    public void SerializePayloadV1()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestExecutionWithSources, Payload, version: 1);

        AssertJsonEqual(V1Json, json);
    }

    [TestMethod]
    public void SerializePayloadV7()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestExecutionWithSources, Payload, version: 7);

        AssertJsonEqual(V7Json, json);
    }

    // ── Deserialize ──────────────────────────────────────────────────────

    [TestMethod]
    public void DeserializePayloadV1()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V1Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<TestRunCriteriaWithSources>(message);

        AssertPayloadFields(result);
    }

    [TestMethod]
    public void DeserializePayloadV7()
    {
        var message = JsonDataSerializer.Instance.DeserializeMessage(Minify(V7Json));
        var result = JsonDataSerializer.Instance.DeserializePayload<TestRunCriteriaWithSources>(message);

        AssertPayloadFields(result);
    }

    // ── Round-trip ───────────────────────────────────────────────────────

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void RoundTrip(int version)
    {
        var json = JsonDataSerializer.Instance.SerializePayload(
            MessageType.StartTestExecutionWithSources, Payload, version);
        var message = JsonDataSerializer.Instance.DeserializeMessage(json);
        var result = JsonDataSerializer.Instance.DeserializePayload<TestRunCriteriaWithSources>(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(Payload.Package, result.Package);
        Assert.AreEqual(Payload.RunSettings, result.RunSettings);
        Assert.IsNotNull(result.AdapterSourceMap);
        Assert.IsTrue(result.AdapterSourceMap.ContainsKey("executor://MSTestAdapter/v2"));
    }

    // ── Newtonsoft comparison ────────────────────────────────────────────

    [TestMethod]
    public void NewtonsoftComparisonV1()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.StartTestExecutionWithSources, Payload, version: 1);
    }

    [TestMethod]
    public void NewtonsoftComparisonV7()
    {
        NewtonsoftComparisonHelper.AssertMatchesNewtonsoft(
            MessageType.StartTestExecutionWithSources, Payload, version: 7);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static TestRunCriteriaWithSources BuildPayload()
    {
        var ctx = new TestExecutionContext(
            frequencyOfRunStatsChangeEvent: 10,
            runStatsChangeEventTimeout: TimeSpan.FromSeconds(30),
            inIsolation: false,
            keepAlive: true,
            isDataCollectionEnabled: false,
            areTestCaseLevelEventsRequired: true,
            hasTestRun: true,
            isDebug: false,
            testCaseFilter: null,
            filterOptions: null);

        return new TestRunCriteriaWithSources(
            new Dictionary<string, IEnumerable<string>>
            {
                ["executor://MSTestAdapter/v2"] = new[] { "Contoso.Math.Tests.dll", "Contoso.Core.Tests.dll" }
            },
            "Contoso.Math.Tests",
            @"<RunSettings><RunConfiguration><ResultsDirectory>.\TestResults</ResultsDirectory></RunConfiguration></RunSettings>",
            ctx);
    }

    private static void AssertPayloadFields(TestRunCriteriaWithSources? result)
    {
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.AdapterSourceMap);
        Assert.IsTrue(result.AdapterSourceMap.ContainsKey("executor://MSTestAdapter/v2"));
        var sources = result.AdapterSourceMap["executor://MSTestAdapter/v2"].ToList();
        Assert.AreEqual(2, sources.Count);
        Assert.AreEqual("Contoso.Math.Tests.dll", sources[0]);
        Assert.AreEqual("Contoso.Core.Tests.dll", sources[1]);
        Assert.AreEqual(
            @"<RunSettings><RunConfiguration><ResultsDirectory>.\TestResults</ResultsDirectory></RunConfiguration></RunSettings>",
            result.RunSettings);
        Assert.AreEqual("Contoso.Math.Tests", result.Package);
        Assert.IsNotNull(result.TestExecutionContext);
        Assert.AreEqual(10, result.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
        Assert.AreEqual(TimeSpan.FromSeconds(30), result.TestExecutionContext.RunStatsChangeEventTimeout);
        Assert.IsFalse(result.TestExecutionContext.InIsolation);
        Assert.IsTrue(result.TestExecutionContext.KeepAlive);
        Assert.IsTrue(result.TestExecutionContext.AreTestCaseLevelEventsRequired);
        Assert.IsFalse(result.TestExecutionContext.IsDebug);
        Assert.IsNull(result.TestExecutionContext.TestCaseFilter);
        Assert.IsNull(result.TestExecutionContext.FilterOptions);
    }

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
