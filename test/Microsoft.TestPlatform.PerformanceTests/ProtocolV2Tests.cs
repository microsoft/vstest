// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests;

[TestClass]
public class ProtocolV2Tests
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
    public void TestCaseSerialize2()
    {
        Serialize(TestCase, 2);
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Serialize(TestCase, 2);
        }
        sw.Stop();

        VerifyPerformanceResult("TestCaseSerialize2", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestCaseDeserialize2()
    {
        var json = Serialize(TestCase, 2);
        Deserialize<TestCase>(json, 2);

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Deserialize<TestCase>(json, 2);
        }
        sw.Stop();

        VerifyPerformanceResult("TestCaseDeserialize2", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestResultSerialize2()
    {
        Serialize(TestResult, 2);
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Serialize(TestResult, 2);
        }
        sw.Stop();

        VerifyPerformanceResult("TestResultSerialize2", 2000, sw.ElapsedMilliseconds);
    }

    [TestMethod]
    public void TestResultDeserialize2()
    {
        var json = Serialize(TestResult, 2);
        Deserialize<TestResult>(json, 2);

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Deserialize<TestResult>(json, 2);
        }
        sw.Stop();

        VerifyPerformanceResult("TestResultDeserialize2", 2000, sw.ElapsedMilliseconds);
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
