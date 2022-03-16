// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.PerformanceTests;

[TestClass]
public class ProtocolV1Tests
{
    private static readonly TestCase TestCase = new(
                                          "sampleTestClass.sampleTestCase",
                                          new Uri("executor://sampleTestExecutor"),
                                          "sampleTest.dll")
    {
        CodeFilePath = "/user/src/testFile.cs",
        DisplayName = "sampleTestCase",
        Id = new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce"),
        LineNumber = 999,
        Traits = { new Trait("Priority", "0"), new Trait("Category", "unit") }
    };

    private static readonly DateTimeOffset StartTime = new(new DateTime(2007, 3, 10, 0, 0, 0, DateTimeKind.Utc));

    private static readonly TestResult TestResult = new(TestCase)
    {
        // Attachments = ?
        // Messages = ?
        Outcome = TestOutcome.Passed,
        ErrorMessage = "sampleError",
        ErrorStackTrace = "sampleStackTrace",
        DisplayName = "sampleTestResult",
        ComputerName = "sampleComputerName",
        Duration = TimeSpan.MaxValue,
        StartTime = StartTime,
        EndTime = DateTimeOffset.MaxValue
    };

    [TestMethod]
    public void TestCaseSerialize()
    {
        Serialize(TestCase);
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            Serialize(TestCase);
        }
        sw.Stop();

        VerifyPerformanceResult("TestCaseSerialize1", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestCaseDeserialize()
    {
        var json = Serialize(TestCase);
        Deserialize<TestCase>(json);

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Deserialize<TestCase>(json);
        }
        sw.Stop();

        VerifyPerformanceResult("TestCaseDeserialize1", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestResultSerialize()
    {
        Serialize(TestResult);
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            Serialize(TestResult);
        }
        sw.Stop();

        VerifyPerformanceResult("TestResultSerialize1", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestResultDeserialize()
    {
        var json = Serialize(TestResult);
        Deserialize<TestResult>(json);

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Deserialize<TestResult>(json);
        }
        sw.Stop();

        VerifyPerformanceResult("TestResultDeserialize1", 3500, sw.ElapsedMilliseconds);
    }

    private static string Serialize<T>(T data, int version = 1)
    {
        return JsonDataSerializer.Instance.Serialize(data, version);
    }

    private static T Deserialize<T>(string json, int version = 1)
    {
        return JsonDataSerializer.Instance.Deserialize<T>(json, version);
    }

    private static void VerifyPerformanceResult(string scenario, long expectedElapsedTime, long elapsedTime)
    {
        Assert.IsTrue(elapsedTime < expectedElapsedTime, $"Scenario '{scenario}' doesn't match with expected elapsed time.");
        File.AppendAllText(@"E:\ProtocolPerf.txt", $" {scenario} : " + elapsedTime);
    }
}
