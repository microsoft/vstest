// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Cross-serializer compatibility tests for the VSTEST_USE_NEWTONSOFT_JSON_SERIALIZER fallback.
///
/// Since the fallback flag is checked at static init time, we cannot toggle it at runtime.
/// Instead we test the actual behavioral contract that the flag guarantees:
///
/// 1. <b>Identical output</b> — STJ and Newtonsoft produce the same normalized JSON for each
///    message type at both V1 and V7 protocol versions. This guarantees that flipping the
///    fallback flag won't change the wire format.
///
/// 2. <b>Cross-serializer round-trip</b> — JSON produced by one serializer can be deserialized
///    by the other. This simulates cross-version communication (e.g., new runner + old test host
///    or old runner + new test host).
/// </summary>
[TestClass]
[TestCategory("Serialization")]
public class NewtonsoftFallbackTests
{
    // ── Serializer instances ─────────────────────────────────────────────
    private static readonly JsonDataSerializer Stj = JsonDataSerializer.Instance;
    private static readonly NewtonsoftJsonDataSerializer Newtonsoft = NewtonsoftJsonDataSerializer.Instance;

    // ══════════════════════════════════════════════════════════════════════
    //  TestMessage
    // ══════════════════════════════════════════════════════════════════════

    private static readonly TestMessagePayload TestMessagePayload = new()
    {
        MessageLevel = TestMessageLevel.Warning,
        Message = "Test 'CalculatorTests.AddTest' was skipped: requires .NET 8"
    };

    [TestMethod]
    public void TestMessage_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.TestMessage, TestMessagePayload, version: 1);

    [TestMethod]
    public void TestMessage_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.TestMessage, TestMessagePayload, version: 7);

    [TestMethod]
    public void TestMessage_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.TestMessage, TestMessagePayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<TestMessagePayload>(msg);

        Assert.IsNotNull(result);
        Assert.AreEqual(TestMessageLevel.Warning, result.MessageLevel);
        Assert.AreEqual(TestMessagePayload.Message, result.Message);
    }

    [TestMethod]
    public void TestMessage_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.TestMessage, TestMessagePayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<TestMessagePayload>(msg);

        Assert.IsNotNull(result);
        Assert.AreEqual(TestMessageLevel.Warning, result.MessageLevel);
        Assert.AreEqual(TestMessagePayload.Message, result.Message);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VersionCheck
    // ══════════════════════════════════════════════════════════════════════

    private const int VersionCheckPayload = 7;

    [TestMethod]
    public void VersionCheck_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.VersionCheck, VersionCheckPayload, version: 1);

    [TestMethod]
    public void VersionCheck_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.VersionCheck, VersionCheckPayload, version: 7);

    [TestMethod]
    public void VersionCheck_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.VersionCheck, VersionCheckPayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<int>(msg);

        Assert.AreEqual(7, result);
    }

    [TestMethod]
    public void VersionCheck_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.VersionCheck, VersionCheckPayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<int>(msg);

        Assert.AreEqual(7, result);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TestCasesFound
    // ══════════════════════════════════════════════════════════════════════

    private static readonly List<TestCase> TestCasesFoundPayload = new() { BuildTestCase() };

    [TestMethod]
    public void TestCasesFound_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.TestCasesFound, TestCasesFoundPayload, version: 1);

    [TestMethod]
    public void TestCasesFound_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.TestCasesFound, TestCasesFoundPayload, version: 7);

    [TestMethod]
    public void TestCasesFound_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.TestCasesFound, TestCasesFoundPayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<IEnumerable<TestCase>>(msg);

        Assert.IsNotNull(result);
        var tc = result.First();
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.AddTest", tc.FullyQualifiedName);
        Assert.AreEqual(new Uri("executor://MSTestAdapter/v2"), tc.ExecutorUri);
        Assert.AreEqual("Contoso.Math.Tests.dll", tc.Source);
        Assert.AreEqual("AddTest(1, 2, 3)", tc.DisplayName);
        Assert.AreEqual(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), tc.Id);
        Assert.AreEqual(42, tc.LineNumber);
    }

    [TestMethod]
    public void TestCasesFound_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.TestCasesFound, TestCasesFoundPayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<IEnumerable<TestCase>>(msg);

        Assert.IsNotNull(result);
        var tc = result.First();
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.AddTest", tc.FullyQualifiedName);
        Assert.AreEqual(new Uri("executor://MSTestAdapter/v2"), tc.ExecutorUri);
        Assert.AreEqual("Contoso.Math.Tests.dll", tc.Source);
        Assert.AreEqual("AddTest(1, 2, 3)", tc.DisplayName);
        Assert.AreEqual(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), tc.Id);
        Assert.AreEqual(42, tc.LineNumber);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DiscoveryComplete
    // ══════════════════════════════════════════════════════════════════════

    private static readonly DiscoveryCompletePayload DiscoveryCompletePayload = BuildDiscoveryCompletePayload();

    [TestMethod]
    public void DiscoveryComplete_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version: 1);

    [TestMethod]
    public void DiscoveryComplete_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version: 7);

    [TestMethod]
    public void DiscoveryComplete_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<DiscoveryCompletePayload>(msg);

        AssertDiscoveryCompleteFields(result);
    }

    [TestMethod]
    public void DiscoveryComplete_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<DiscoveryCompletePayload>(msg);

        AssertDiscoveryCompleteFields(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ExecutionComplete
    // ══════════════════════════════════════════════════════════════════════

    private static readonly TestRunCompletePayload ExecutionCompletePayload = BuildExecutionCompletePayload();

    [TestMethod]
    public void ExecutionComplete_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.ExecutionComplete, ExecutionCompletePayload, version: 1);

    [TestMethod]
    public void ExecutionComplete_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.ExecutionComplete, ExecutionCompletePayload, version: 7);

    [TestMethod]
    public void ExecutionComplete_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<TestRunCompletePayload>(msg);

        AssertExecutionCompleteFields(result);
    }

    [TestMethod]
    public void ExecutionComplete_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<TestRunCompletePayload>(msg);

        AssertExecutionCompleteFields(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TestRunStatsChange
    // ══════════════════════════════════════════════════════════════════════

    private static readonly TestRunStatsPayload TestRunStatsChangePayload = BuildTestRunStatsChangePayload();

    [TestMethod]
    public void TestRunStatsChange_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version: 1);

    [TestMethod]
    public void TestRunStatsChange_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version: 7);

    [TestMethod]
    public void TestRunStatsChange_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<TestRunStatsPayload>(msg);

        AssertTestRunStatsChangeFields(result);
    }

    [TestMethod]
    public void TestRunStatsChange_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.TestRunStatsChange, TestRunStatsChangePayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<TestRunStatsPayload>(msg);

        AssertTestRunStatsChangeFields(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  StartTestExecutionWithSources
    // ══════════════════════════════════════════════════════════════════════

    private static readonly TestRunCriteriaWithSources StartExecutionPayload = BuildStartExecutionPayload();

    [TestMethod]
    public void StartTestExecutionWithSources_IdenticalOutput_V1()
        => AssertIdenticalOutput(MessageType.StartTestExecutionWithSources, StartExecutionPayload, version: 1);

    [TestMethod]
    public void StartTestExecutionWithSources_IdenticalOutput_V7()
        => AssertIdenticalOutput(MessageType.StartTestExecutionWithSources, StartExecutionPayload, version: 7);

    [TestMethod]
    public void StartTestExecutionWithSources_CrossCompat_StjToNewtonsoft_V7()
    {
        var json = Stj.SerializePayload(MessageType.StartTestExecutionWithSources, StartExecutionPayload, version: 7);
        var msg = Newtonsoft.DeserializeMessage(json);
        var result = Newtonsoft.DeserializePayload<TestRunCriteriaWithSources>(msg);

        AssertStartExecutionFields(result);
    }

    [TestMethod]
    public void StartTestExecutionWithSources_CrossCompat_NewtonsoftToStj_V7()
    {
        var json = Newtonsoft.SerializePayload(MessageType.StartTestExecutionWithSources, StartExecutionPayload, version: 7);
        var msg = Stj.DeserializeMessage(json);
        var result = Stj.DeserializePayload<TestRunCriteriaWithSources>(msg);

        AssertStartExecutionFields(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers — Identical output assertion
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Assert that STJ and Newtonsoft produce identical normalized JSON for the same payload.
    /// </summary>
    private static void AssertIdenticalOutput(string messageType, object payload, int version)
    {
        var stjJson = Stj.SerializePayload(messageType, payload, version);
        var newtonsoftJson = Newtonsoft.SerializePayload(messageType, payload, version);

        var normalizedStj = NormalizeJson(stjJson);
        var normalizedNewtonsoft = NormalizeJson(newtonsoftJson);

        Assert.AreEqual(normalizedNewtonsoft, normalizedStj,
            $"STJ output differs from Newtonsoft for {messageType} v{version}.\n" +
            $"Newtonsoft:\n{newtonsoftJson}\n\nSTJ:\n{stjJson}");
    }

    private static string NormalizeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        return global::Newtonsoft.Json.Linq.JToken.Parse(json).ToString(global::Newtonsoft.Json.Formatting.None);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers — Payload builders
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

    private static TestRunCriteriaWithSources BuildStartExecutionPayload()
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

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers — Payload assertions
    // ══════════════════════════════════════════════════════════════════════

    private static void AssertDiscoveryCompleteFields(DiscoveryCompletePayload? result)
    {
        Assert.IsNotNull(result);
        Assert.AreEqual(150, result.TotalTests);
        Assert.IsFalse(result.IsAborted);
        Assert.IsNotNull(result.LastDiscoveredTests);
        var tests = result.LastDiscoveredTests.ToList();
        Assert.HasCount(1, tests);
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.SubtractTest", tests[0].FullyQualifiedName);
        Assert.AreEqual(new Uri("executor://MSTestAdapter/v2"), tests[0].ExecutorUri);
        Assert.AreEqual("Contoso.Math.Tests.dll", tests[0].Source);
        Assert.AreEqual("SubtractTest", tests[0].DisplayName);
        Assert.AreEqual(new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), tests[0].Id);
    }

    private static void AssertExecutionCompleteFields(TestRunCompletePayload? result)
    {
        Assert.IsNotNull(result);

        Assert.IsNotNull(result.TestRunCompleteArgs);
        Assert.IsFalse(result.TestRunCompleteArgs.IsCanceled);
        Assert.IsFalse(result.TestRunCompleteArgs.IsAborted);
        Assert.IsNotNull(result.TestRunCompleteArgs.TestRunStatistics);
        Assert.AreEqual(1, result.TestRunCompleteArgs.TestRunStatistics.ExecutedTests);

        Assert.IsNotNull(result.LastRunTests);
        var newResults = result.LastRunTests.NewTestResults!.ToList();
        Assert.HasCount(1, newResults);
        Assert.AreEqual(TestOutcome.Passed, newResults[0].Outcome);
        Assert.AreEqual("AddTest(1, 2, 3)", newResults[0].DisplayName);
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.AddTest",
            newResults[0].TestCase.FullyQualifiedName);

        Assert.IsNotNull(result.ExecutorUris);
        Assert.Contains("executor://MSTestAdapter/v2", result.ExecutorUris);
    }

    private static void AssertTestRunStatsChangeFields(TestRunStatsPayload? result)
    {
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TestRunChangedArgs);

        var newResults = result.TestRunChangedArgs.NewTestResults!.ToList();
        Assert.HasCount(1, newResults);
        Assert.AreEqual(TestOutcome.Failed, newResults[0].Outcome);
        Assert.AreEqual("Assert.AreEqual failed. Expected:<0.5>. Actual:<0>.",
            newResults[0].ErrorMessage);
        Assert.AreEqual("DivideTest", newResults[0].DisplayName);
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.DivideTest",
            newResults[0].TestCase.FullyQualifiedName);

        Assert.IsNotNull(result.TestRunChangedArgs.TestRunStatistics);
        Assert.AreEqual(1, result.TestRunChangedArgs.TestRunStatistics.ExecutedTests);

        var activeTests = result.TestRunChangedArgs.ActiveTests!.ToList();
        Assert.HasCount(1, activeTests);
        Assert.AreEqual("Contoso.Math.Tests.CalculatorTests.MultiplyTest",
            activeTests[0].FullyQualifiedName);
    }

    private static void AssertStartExecutionFields(TestRunCriteriaWithSources? result)
    {
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.AdapterSourceMap);
        Assert.IsTrue(result.AdapterSourceMap.ContainsKey("executor://MSTestAdapter/v2"));
        var sources = result.AdapterSourceMap["executor://MSTestAdapter/v2"].ToList();
        Assert.HasCount(2, sources);
        Assert.AreEqual("Contoso.Math.Tests.dll", sources[0]);
        Assert.AreEqual("Contoso.Core.Tests.dll", sources[1]);
        Assert.AreEqual(
            @"<RunSettings><RunConfiguration><ResultsDirectory>.\TestResults</ResultsDirectory></RunConfiguration></RunSettings>",
            result.RunSettings);
        Assert.AreEqual("Contoso.Math.Tests", result.Package);
        Assert.IsNotNull(result.TestExecutionContext);
        Assert.AreEqual(10, result.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
        Assert.IsFalse(result.TestExecutionContext.InIsolation);
        Assert.IsTrue(result.TestExecutionContext.KeepAlive);
    }
}
