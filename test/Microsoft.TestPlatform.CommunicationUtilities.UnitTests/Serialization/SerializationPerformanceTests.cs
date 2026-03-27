// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.NewtonsoftReference;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

[TestClass]
[TestCategory("Performance")]
public class SerializationPerformanceTests
{
    private const int Iterations = 1000;

    #region Payloads

    private static readonly TestMessagePayload TestMessagePayload = new()
    {
        MessageLevel = TestMessageLevel.Warning,
        Message = "Test 'CalculatorTests.AddTest' was skipped: requires .NET 8"
    };

    private static readonly List<TestCase> TestCases = new() { BuildTestCase() };

    private static readonly DiscoveryCompletePayload DiscoveryCompletePayload = BuildDiscoveryCompletePayload();

    private static readonly TestRunCompletePayload ExecutionCompletePayload = BuildExecutionCompletePayload();

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

    #endregion

    #region TestMessage — Serialize

    [TestMethod]
    public void Serialize_TestMessage_V1_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize TestMessage V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestMessage_V1_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize TestMessage V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestMessage_V7_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize TestMessage V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestMessage_V7_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize TestMessage V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region TestMessage — Deserialize

    [TestMethod]
    public void Deserialize_TestMessage_V7_SystemTextJson()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        var msg = JsonDataSerializer.Instance.DeserializeMessage(json);
        JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = JsonDataSerializer.Instance.DeserializeMessage(json);
            JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"STJ Deserialize TestMessage V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Deserialize_TestMessage_V7_Newtonsoft()
    {
        var json = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);

        var msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
        NewtonsoftJsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
            NewtonsoftJsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Deserialize TestMessage V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region TestCasesFound — Serialize

    [TestMethod]
    public void Serialize_TestCasesFound_V1_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 1);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize TestCasesFound V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestCasesFound_V1_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 1);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize TestCasesFound V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestCasesFound_V7_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize TestCasesFound V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_TestCasesFound_V7_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize TestCasesFound V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region TestCasesFound — Deserialize

    [TestMethod]
    public void Deserialize_TestCasesFound_V7_SystemTextJson()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);

        var msg = JsonDataSerializer.Instance.DeserializeMessage(json);
        JsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = JsonDataSerializer.Instance.DeserializeMessage(json);
            JsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(msg);
        }
        sw.Stop();
        Console.WriteLine($"STJ Deserialize TestCasesFound V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Deserialize_TestCasesFound_V7_Newtonsoft()
    {
        var json = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);

        var msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
        NewtonsoftJsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
            NewtonsoftJsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(msg);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Deserialize TestCasesFound V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region DiscoveryComplete — Serialize

    [TestMethod]
    public void Serialize_DiscoveryComplete_V1_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize DiscoveryComplete V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_DiscoveryComplete_V1_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize DiscoveryComplete V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_DiscoveryComplete_V7_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize DiscoveryComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_DiscoveryComplete_V7_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize DiscoveryComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region DiscoveryComplete — Deserialize

    [TestMethod]
    public void Deserialize_DiscoveryComplete_V7_SystemTextJson()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);

        var msg = JsonDataSerializer.Instance.DeserializeMessage(json);
        JsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = JsonDataSerializer.Instance.DeserializeMessage(json);
            JsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"STJ Deserialize DiscoveryComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Deserialize_DiscoveryComplete_V7_Newtonsoft()
    {
        var json = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7);

        var msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
        NewtonsoftJsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
            NewtonsoftJsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Deserialize DiscoveryComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region ExecutionComplete — Serialize

    [TestMethod]
    public void Serialize_ExecutionComplete_V1_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize ExecutionComplete V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_ExecutionComplete_V1_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 1);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 1);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize ExecutionComplete V1: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_ExecutionComplete_V7_SystemTextJson()
    {
        JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"STJ Serialize ExecutionComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Serialize_ExecutionComplete_V7_Newtonsoft()
    {
        NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Serialize ExecutionComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region ExecutionComplete — Deserialize

    [TestMethod]
    public void Deserialize_ExecutionComplete_V7_SystemTextJson()
    {
        var json = JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        var msg = JsonDataSerializer.Instance.DeserializeMessage(json);
        JsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = JsonDataSerializer.Instance.DeserializeMessage(json);
            JsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"STJ Deserialize ExecutionComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    [TestMethod]
    public void Deserialize_ExecutionComplete_V7_Newtonsoft()
    {
        var json = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7);

        var msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
        NewtonsoftJsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            msg = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(json);
            NewtonsoftJsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);
        }
        sw.Stop();
        Console.WriteLine($"Newtonsoft Deserialize ExecutionComplete V7: {sw.ElapsedMilliseconds}ms for {Iterations} iterations");
    }

    #endregion

    #region Regression Comparison — STJ must not be slower than Newtonsoft

    // These tests run both implementations side-by-side and fail if STJ is
    // significantly slower than Newtonsoft. Tolerance is 2x — STJ should be
    // faster, but we allow up to 2x slower to account for test machine variance.
    private const double RegressionTolerance = 2.0;

    private static (long stjMs, long newtonsoftMs) MeasureBoth(
        Action stjAction, Action newtonsoftAction)
    {
        // Warm up both
        stjAction();
        newtonsoftAction();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++) newtonsoftAction();
        sw.Stop();
        var newtonsoftMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < Iterations; i++) stjAction();
        sw.Stop();
        var stjMs = sw.ElapsedMilliseconds;

        return (stjMs, newtonsoftMs);
    }

    private static void AssertNoRegression(string label, long stjMs, long newtonsoftMs)
    {
        // Avoid division by zero when both are < 1ms
        var newtonsoftBaseline = Math.Max(newtonsoftMs, 1);
        var ratio = (double)stjMs / newtonsoftBaseline;

        Console.WriteLine($"[{label}] STJ={stjMs}ms  Newtonsoft={newtonsoftMs}ms  Ratio={ratio:F2}x");

        Assert.IsLessThanOrEqualTo(RegressionTolerance, ratio,
            $"Performance regression: STJ ({stjMs}ms) is {ratio:F2}x slower than " +
            $"Newtonsoft ({newtonsoftMs}ms) for {label}. Threshold: {RegressionTolerance}x");
    }

    [TestMethod]
    public void Regression_Serialize_TestMessage_V7()
    {
        var (stj, ns) = MeasureBoth(
            () => JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7),
            () => NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7));
        AssertNoRegression("Serialize TestMessage V7", stj, ns);
    }

    [TestMethod]
    public void Regression_Serialize_TestCasesFound_V7()
    {
        var (stj, ns) = MeasureBoth(
            () => JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7),
            () => NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7));
        AssertNoRegression("Serialize TestCasesFound V7", stj, ns);
    }

    [TestMethod]
    public void Regression_Serialize_DiscoveryComplete_V7()
    {
        var (stj, ns) = MeasureBoth(
            () => JsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7),
            () => NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.DiscoveryComplete, DiscoveryCompletePayload, 7));
        AssertNoRegression("Serialize DiscoveryComplete V7", stj, ns);
    }

    [TestMethod]
    public void Regression_Serialize_ExecutionComplete_V7()
    {
        var (stj, ns) = MeasureBoth(
            () => JsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7),
            () => NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.ExecutionComplete, ExecutionCompletePayload, 7));
        AssertNoRegression("Serialize ExecutionComplete V7", stj, ns);
    }

    [TestMethod]
    public void Regression_Deserialize_TestMessage_V7()
    {
        var stjJson = JsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);
        var nsJson = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestMessage, TestMessagePayload, 7);
        var (stj, ns) = MeasureBoth(
            () => { var m = JsonDataSerializer.Instance.DeserializeMessage(stjJson); JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(m); },
            () => { var m = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(nsJson); NewtonsoftJsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(m); });
        AssertNoRegression("Deserialize TestMessage V7", stj, ns);
    }

    [TestMethod]
    public void Regression_Deserialize_TestCasesFound_V7()
    {
        var stjJson = JsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);
        var nsJson = NewtonsoftJsonDataSerializer.Instance.SerializePayload(MessageType.TestCasesFound, TestCases, 7);
        var (stj, ns) = MeasureBoth(
            () => { var m = JsonDataSerializer.Instance.DeserializeMessage(stjJson); JsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(m); },
            () => { var m = NewtonsoftJsonDataSerializer.Instance.DeserializeMessage(nsJson); NewtonsoftJsonDataSerializer.Instance.DeserializePayload<List<TestCase>>(m); });
        AssertNoRegression("Deserialize TestCasesFound V7", stj, ns);
    }

    #endregion
}
