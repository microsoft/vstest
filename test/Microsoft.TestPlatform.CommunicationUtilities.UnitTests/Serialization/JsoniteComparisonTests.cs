// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Jsonite;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.JsoniteReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Tests comparing Jsonite (lightweight BSD-2 JSON parser) against System.Text.Json.
///
/// Jsonite parses JSON into JsonObject/JsonArray/primitive object graphs — it does NOT
/// support typed deserialization to CLR types like STJ does. These tests validate:
///
///   1. Parse fidelity — JSON produced by STJ can be parsed by Jsonite without errors.
///   2. Round-trip — STJ JSON → Jsonite parse → Jsonite serialize → structurally equal.
///   3. Structural correctness — parsed message envelope has expected MessageType/Version/Payload.
///   4. Performance — raw parse speed comparison between Jsonite and STJ JsonDocument.
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class JsoniteComparisonTests
{
    private const int PerfIterations = 1000;

    private static readonly JsonDataSerializer Stj = JsonDataSerializer.Instance;
    private static readonly JsoniteSerializer Jsonite = JsoniteSerializer.Instance;

    // ══════════════════════════════════════════════════════════════════════
    //  Shared test payloads
    // ══════════════════════════════════════════════════════════════════════

    private static readonly TestMessagePayload TestMessagePayload = new()
    {
        MessageLevel = TestMessageLevel.Warning,
        Message = "Test 'CalculatorTests.AddTest' was skipped: requires .NET 8"
    };

    private static readonly List<TestCase> TestCasesFoundPayload = new() { BuildTestCase() };

    private static readonly DiscoveryCompletePayload DiscoveryCompletePayload = BuildDiscoveryCompletePayload();

    private static readonly TestRunCompletePayload ExecutionCompletePayload = BuildExecutionCompletePayload();

    private static readonly TestRunStatsPayload TestRunStatsChangePayload = BuildTestRunStatsChangePayload();

    // ══════════════════════════════════════════════════════════════════════
    //  Parse fidelity — Jsonite can parse STJ-produced JSON
    // ══════════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestMessage_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version);
        AssertJsoniteCanParse(json, MessageType.TestMessage, version);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void VersionCheck_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.VersionCheck, 7, version);
        AssertJsoniteCanParse(json, MessageType.VersionCheck, version);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestCasesFound_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestCasesFound, TestCasesFoundPayload, version);
        AssertJsoniteCanParse(json, MessageType.TestCasesFound, version);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void DiscoveryComplete_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version);
        AssertJsoniteCanParse(json, MessageType.DiscoveryComplete, version);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void ExecutionComplete_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, version);
        AssertJsoniteCanParse(json, MessageType.ExecutionComplete, version);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestRunStatsChange_JsoniteCanParseStjOutput(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version);
        AssertJsoniteCanParse(json, MessageType.TestRunStatsChange, version);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Round-trip — parse with Jsonite, re-serialize, compare structure
    // ══════════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestMessage_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version);
        AssertRoundTripPreservesStructure(json);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void VersionCheck_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.VersionCheck, 7, version);
        AssertRoundTripPreservesStructure(json);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestCasesFound_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestCasesFound, TestCasesFoundPayload, version);
        AssertRoundTripPreservesStructure(json);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void DiscoveryComplete_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version);
        AssertRoundTripPreservesStructure(json);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void ExecutionComplete_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, version);
        AssertRoundTripPreservesStructure(json);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TestRunStatsChange_RoundTrip_PreservesStructure(int version)
    {
        var json = Stj.SerializePayload(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version);
        AssertRoundTripPreservesStructure(json);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Structural correctness — parsed payload has expected fields
    // ══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TestMessage_ParsedPayload_HasExpectedFields()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version: 7);
        var parsed = Jsonite.Parse(json);
        var payload = Jsonite.ExtractPayload(parsed) as JsonObject;

        Assert.IsNotNull(payload, "Payload should be a JsonObject");
        Assert.IsTrue(payload.ContainsKey("MessageLevel"), "Payload should contain MessageLevel");
        Assert.IsTrue(payload.ContainsKey("Message"), "Payload should contain Message");
        Assert.AreEqual(TestMessagePayload.Message, payload["Message"] as string);
    }

    [TestMethod]
    public void TestCasesFound_ParsedPayload_HasTestCaseArray()
    {
        var json = Stj.SerializePayload(MessageType.TestCasesFound, TestCasesFoundPayload, version: 7);
        var parsed = Jsonite.Parse(json);
        var payload = Jsonite.ExtractPayload(parsed);

        Assert.IsInstanceOfType(payload, typeof(JsonArray), "TestCasesFound payload should be an array");
        var arr = (JsonArray)payload;
        Assert.AreEqual(1, arr.Count, "Should contain 1 test case");

        var tc = arr[0] as JsonObject;
        Assert.IsNotNull(tc, "Test case should be a JsonObject");
        Assert.IsTrue(tc.ContainsKey("Source"), "Test case should have Source");
        Assert.AreEqual("Contoso.Math.Tests.dll", tc["Source"] as string);
    }

    [TestMethod]
    public void DiscoveryComplete_ParsedPayload_HasExpectedFields()
    {
        var json = Stj.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version: 7);
        var parsed = Jsonite.Parse(json);
        var payload = Jsonite.ExtractPayload(parsed) as JsonObject;

        Assert.IsNotNull(payload, "Payload should be a JsonObject");
        Assert.IsTrue(payload.ContainsKey("TotalTests"), "Payload should contain TotalTests");
        Assert.IsTrue(payload.ContainsKey("LastDiscoveredTests"), "Payload should contain LastDiscoveredTests");

        // Jsonite parses integers — 150 fits in int
        Assert.AreEqual(150, payload["TotalTests"]);
    }

    [TestMethod]
    public void ExecutionComplete_ParsedPayload_HasExpectedFields()
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, version: 7);
        var parsed = Jsonite.Parse(json);
        var payload = Jsonite.ExtractPayload(parsed) as JsonObject;

        Assert.IsNotNull(payload, "Payload should be a JsonObject");
        Assert.IsTrue(payload.ContainsKey("TestRunCompleteArgs"), "Payload should contain TestRunCompleteArgs");
        Assert.IsTrue(payload.ContainsKey("LastRunTests"), "Payload should contain LastRunTests");
        Assert.IsTrue(payload.ContainsKey("ExecutorUris"), "Payload should contain ExecutorUris");
    }

    [TestMethod]
    public void VersionedMessage_HasCorrectEnvelope()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version: 7);
        var parsed = Jsonite.Parse(json);

        var messageType = Jsonite.ExtractMessageType(parsed);
        var version = Jsonite.ExtractVersion(parsed);

        Assert.AreEqual(MessageType.TestMessage, messageType);
        Assert.AreEqual(7, version);
    }

    [TestMethod]
    public void V1Message_HasNoVersion()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version: 1);
        var parsed = Jsonite.Parse(json);

        var messageType = Jsonite.ExtractMessageType(parsed);
        var version = Jsonite.ExtractVersion(parsed);

        Assert.AreEqual(MessageType.TestMessage, messageType);
        Assert.IsNull(version, "V1 messages should not have a Version field");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Performance — Jsonite parse vs STJ JsonDocument.Parse
    // ══════════════════════════════════════════════════════════════════════

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_Parse_TestMessage_Jsonite()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        // Warm up
        Jsonite.Parse(json);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            Jsonite.Parse(json);
        }

        sw.Stop();
        Console.WriteLine($"Jsonite Parse TestMessage: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_Parse_TestMessage_SystemTextJson()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        // Warm up
        System.Text.Json.JsonDocument.Parse(json).Dispose();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }

        sw.Stop();
        Console.WriteLine($"STJ JsonDocument.Parse TestMessage: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_Parse_ExecutionComplete_Jsonite()
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        // Warm up
        Jsonite.Parse(json);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            Jsonite.Parse(json);
        }

        sw.Stop();
        Console.WriteLine($"Jsonite Parse ExecutionComplete: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_Parse_ExecutionComplete_SystemTextJson()
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        // Warm up
        System.Text.Json.JsonDocument.Parse(json).Dispose();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }

        sw.Stop();
        Console.WriteLine($"STJ JsonDocument.Parse ExecutionComplete: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_RoundTrip_TestMessage_Jsonite()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        // Warm up
        Jsonite.RoundTrip(json);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            Jsonite.RoundTrip(json);
        }

        sw.Stop();
        Console.WriteLine($"Jsonite RoundTrip TestMessage: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void Perf_RoundTrip_ExecutionComplete_Jsonite()
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        // Warm up
        Jsonite.RoundTrip(json);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < PerfIterations; i++)
        {
            Jsonite.RoundTrip(json);
        }

        sw.Stop();
        Console.WriteLine($"Jsonite RoundTrip ExecutionComplete: {sw.ElapsedMilliseconds}ms for {PerfIterations} iterations");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers — assertion methods
    // ══════════════════════════════════════════════════════════════════════

    private static void AssertJsoniteCanParse(string json, string expectedMessageType, int version)
    {
        bool success = Jsonite.TryParse(json, out var result, out var error);

        Assert.IsTrue(success, $"Jsonite failed to parse STJ output for {expectedMessageType} v{version}: {error}");
        Assert.IsNotNull(result, $"Jsonite returned null for {expectedMessageType} v{version}");

        // Verify envelope
        var messageType = Jsonite.ExtractMessageType(result);
        Assert.AreEqual(expectedMessageType, messageType,
            $"Jsonite parsed wrong MessageType for {expectedMessageType} v{version}");

        // Verify payload exists
        var payload = Jsonite.ExtractPayload(result);
        Assert.IsNotNull(payload, $"Jsonite parsed null Payload for {expectedMessageType} v{version}");
    }

    private static void AssertRoundTripPreservesStructure(string json)
    {
        var parsed1 = Jsonite.Parse(json);
        var reserialized = Jsonite.Serialize(parsed1);
        var parsed2 = Jsonite.Parse(reserialized);

        Assert.IsTrue(JsoniteSerializer.DeepEquals(parsed1, parsed2),
            $"Jsonite round-trip changed structure.\nOriginal JSON:\n{json}\nRe-serialized:\n{reserialized}");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers — payload builders (same as other test classes)
    // ══════════════════════════════════════════════════════════════════════

    private static TestCase BuildTestCase()
    {
        return new TestCase(
            "Contoso.Math.Tests.CalculatorTests.AddTest",
            new Uri("executor://MSTestAdapter/v2"),
            "Contoso.Math.Tests.dll")
        {
            DisplayName = "AddTest(1, 2, 3)",
            Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            CodeFilePath = @"C:\src\Contoso.Math.Tests\CalculatorTests.cs",
            LineNumber = 42,
            Traits = { new Trait("Category", "Unit"), new Trait("Priority", "1") }
        };
    }

    private static DiscoveryCompletePayload BuildDiscoveryCompletePayload()
    {
        return new DiscoveryCompletePayload
        {
            TotalTests = 150,
            IsAborted = false,
            LastDiscoveredTests = new List<TestCase>
            {
                new("Contoso.Math.Tests.CalculatorTests.SubtractTest",
                    new Uri("executor://MSTestAdapter/v2"), "Contoso.Math.Tests.dll")
                {
                    DisplayName = "SubtractTest",
                    Id = new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                }
            },
            Metrics = new Dictionary<string, object> { ["TotalTestsDiscovered"] = 150 },
        };
    }

    private static TestRunCompletePayload BuildExecutionCompletePayload()
    {
        var tc = new TestCase(
            "Contoso.Math.Tests.CalculatorTests.AddTest",
            new Uri("executor://MSTestAdapter/v2"),
            "Contoso.Math.Tests.dll")
        {
            Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        };

        var tr = new TestResult(tc)
        {
            Outcome = TestOutcome.Passed,
            DisplayName = "AddTest(1, 2, 3)",
            Duration = TimeSpan.FromMilliseconds(12),
            StartTime = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 3, 20, 10, 0, 0, 12, TimeSpan.Zero),
            ComputerName = "BUILD-AGENT-01",
        };

        var stats = new Dictionary<TestOutcome, long> { [TestOutcome.Passed] = 1 };
        var runStats = new TestRunStatistics(1, stats);

        return new TestRunCompletePayload
        {
            TestRunCompleteArgs = new TestRunCompleteEventArgs(
                runStats, false, false, null, null, null, TimeSpan.FromSeconds(2)),
            LastRunTests = new TestRunChangedEventArgs(
                runStats, new[] { tr }, Array.Empty<TestCase>()),
            ExecutorUris = new List<string> { "executor://MSTestAdapter/v2" },
        };
    }

    private static TestRunStatsPayload BuildTestRunStatsChangePayload()
    {
        var tc = new TestCase(
            "Contoso.Math.Tests.CalculatorTests.DivideTest",
            new Uri("executor://MSTestAdapter/v2"),
            "Contoso.Math.Tests.dll")
        {
            Id = new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"),
        };

        var tr = new TestResult(tc)
        {
            Outcome = TestOutcome.Failed,
            ErrorMessage = "Assert.AreEqual failed. Expected:<0.5>. Actual:<0>.",
            ErrorStackTrace = @"   at Contoso.Math.Tests.CalculatorTests.DivideTest() in C:\src\CalculatorTests.cs:line 55",
            DisplayName = "DivideTest",
            Duration = TimeSpan.FromMilliseconds(3),
            StartTime = new DateTimeOffset(2026, 3, 20, 10, 0, 1, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 3, 20, 10, 0, 1, 3, TimeSpan.Zero),
        };

        var stats = new Dictionary<TestOutcome, long> { [TestOutcome.Failed] = 1 };
        var runStats = new TestRunStatistics(1, stats);

        var inProgress = new List<TestCase>
        {
            new("Contoso.Math.Tests.CalculatorTests.MultiplyTest",
                new Uri("executor://MSTestAdapter/v2"), "Contoso.Math.Tests.dll")
            {
                Id = new Guid("d4e5f6a7-b8c9-0123-defa-234567890123"),
            }
        };

        return new TestRunStatsPayload
        {
            TestRunChangedArgs = new TestRunChangedEventArgs(
                runStats, new[] { tr }, inProgress),
        };
    }
}
