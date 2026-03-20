// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Library.IntegrationTests;

[TestClass]
// TODO: these tests potentially not test anything useful that we would not test in other tests? Replace them with integration tests that have portable and full symbols test file, and that collect source info, unless we have such tests already. Right now we test just portable symbol reader it seems.
public class DiaSessionTests : AcceptanceTestBase
{
    public static string? GetAndSetTargetFrameWork(IntegrationTestEnvironment testEnvironment)
    {
        var currentTargetFrameWork = testEnvironment.TargetFramework;
        testEnvironment.TargetFramework =
#if NETFRAMEWORK
            "net462";
#else
            "net8.0";
#endif
        return currentTargetFrameWork;
    }

    [TestMethod]
    public void GetNavigationDataShouldReturnCorrectFileNameAndLineNumber()
    {
        var currentTargetFrameWork = GetAndSetTargetFrameWork(_testEnvironment);
        var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

        var diaSession = new DiaSession(assemblyPath);
        DiaNavigationData? diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "PassingTest");

        Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
        StringAssert.EndsWith(diaNavigationData.FileName!.Replace("\\", "/"), @"\SimpleClassLibrary\Class1.cs".Replace("\\", "/"));

        // Derive expected line numbers from the source file so the test is resilient to code moving around.
        var sourceFile = Path.Combine(_testEnvironment.TestAssetsPath, "SimpleClassLibrary", "Class1.cs");
        var (bodyStart, bodyEnd) = FindMethodBodyRange(sourceFile, "PassingTest");
        // In Debug, PDB min line points to opening brace. In Release, it points to first executable statement.
        var expectedMin = IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase)
            ? bodyStart + 1
            : bodyStart;
        Assert.AreEqual(expectedMin, diaNavigationData.MinLineNumber, "Incorrect min line number");
        Assert.AreEqual(bodyEnd, diaNavigationData.MaxLineNumber, "Incorrect max line number");

        _testEnvironment.TargetFramework = currentTargetFrameWork;
    }

    [TestMethod]
    public void GetNavigationDataShouldReturnCorrectDataForAsyncMethod()
    {
        var currentTargetFrameWork = GetAndSetTargetFrameWork(_testEnvironment);
        var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

        var diaSession = new DiaSession(assemblyPath);
        DiaNavigationData? diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1+<AsyncTestMethod>d__1", "MoveNext");

        Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
        StringAssert.EndsWith(diaNavigationData.FileName!.Replace("\\", "/"), @"\SimpleClassLibrary\Class1.cs".Replace("\\", "/"));

        var sourceFile = Path.Combine(_testEnvironment.TestAssetsPath, "SimpleClassLibrary", "Class1.cs");
        var (bodyStart, bodyEnd) = FindMethodBodyRange(sourceFile, "AsyncTestMethod");
        var expectedMin = IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase)
            ? bodyStart + 1
            : bodyStart;
        Assert.AreEqual(expectedMin, diaNavigationData.MinLineNumber, "Incorrect min line number");
        Assert.AreEqual(bodyEnd, diaNavigationData.MaxLineNumber, "Incorrect max line number");

        _testEnvironment.TargetFramework = currentTargetFrameWork;
    }

    [TestMethod]
    public void GetNavigationDataShouldReturnCorrectDataForOverLoadedMethod()
    {
        var currentTargetFrameWork = GetAndSetTargetFrameWork(_testEnvironment);
        var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

        var diaSession = new DiaSession(assemblyPath);
        DiaNavigationData? diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "OverLoadedMethod");

        Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
        StringAssert.EndsWith(diaNavigationData.FileName!.Replace("\\", "/"), @"\SimpleClassLibrary\Class1.cs".Replace("\\", "/"));

        // Weird why DiaSession is now returning the first overloaded method
        // as compared to before when it used to return second method
        var sourceFile = Path.Combine(_testEnvironment.TestAssetsPath, "SimpleClassLibrary", "Class1.cs");
        var (bodyStart, bodyEnd) = FindMethodBodyRange(sourceFile, "OverLoadedMethod");
        if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
        {
            // Release builds for empty methods have min == max (closing brace is the only sequence point).
            Assert.AreEqual(diaNavigationData.MinLineNumber, diaNavigationData.MaxLineNumber, "Incorrect min line number");
        }
        else
        {
            Assert.AreEqual(bodyStart, diaNavigationData.MinLineNumber, "Incorrect min line number");
            Assert.AreEqual(bodyEnd, diaNavigationData.MaxLineNumber, "Incorrect max line number");
        }

        _testEnvironment.TargetFramework = currentTargetFrameWork;
    }

    [TestMethod]
    public void GetNavigationDataShouldReturnNullForNotExistMethodNameOrNotExistTypeName()
    {
        var currentTargetFrameWork = GetAndSetTargetFrameWork(_testEnvironment);
        var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

        var diaSession = new DiaSession(assemblyPath);

        // Not exist method name
        DiaNavigationData? diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "NotExistMethod");
        Assert.IsNull(diaNavigationData);

        // Not Exist Type name
        diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.NotExistType", "PassingTest");
        Assert.IsNull(diaNavigationData);

        _testEnvironment.TargetFramework = currentTargetFrameWork;
    }

    [TestMethod]
    [Ignore] // TODO: This test tests against a time threshold, which makes it fail on server sometimes.
    public void DiaSessionPerfTest()
    {
        var currentTargetFrameWork = GetAndSetTargetFrameWork(_testEnvironment);
        var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

        var watch = Stopwatch.StartNew();
        var diaSession = new DiaSession(assemblyPath);
        DiaNavigationData? diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.HugeMethodSet", "MSTest_D1_01");
        watch.Stop();
        var diaElapsedMilliseconds = watch.ElapsedMilliseconds;

        Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
        StringAssert.EndsWith(diaNavigationData.FileName!.Replace("\\", "/"), @"\SimpleClassLibrary\HugeMethodSet.cs".Replace("\\", "/"));

        var sourceFile = Path.Combine(_testEnvironment.TestAssetsPath, "SimpleClassLibrary", "HugeMethodSet.cs");
        var (bodyStart, bodyEnd) = FindMethodBodyRange(sourceFile, "MSTest_D1_01");
        var expectedMin = IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase)
            ? bodyStart + 1
            : bodyStart;
        Assert.AreEqual(expectedMin, diaNavigationData.MinLineNumber, "Incorrect min line number");
        Assert.AreEqual(bodyEnd, diaNavigationData.MaxLineNumber, "Incorrect max line number");

        var expectedTime = 150;
        Assert.IsTrue(diaElapsedMilliseconds < expectedTime, $"DiaSession Perf test Actual time:{diaElapsedMilliseconds} ms Expected time:{expectedTime} ms");

        _testEnvironment.TargetFramework = currentTargetFrameWork;
    }

    /// <summary>
    /// Finds the 1-based line numbers of the opening brace and closing brace of a method body.
    /// </summary>
    private static (int BodyStart, int BodyEnd) FindMethodBodyRange(string sourceFile, string methodName)
    {
        var lines = File.ReadAllLines(sourceFile);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains($" {methodName}("))
            {
                int braceDepth = 0;
                int bodyStart = -1;
                for (int j = i; j < lines.Length; j++)
                {
                    var line = lines[j];
                    foreach (var ch in line)
                    {
                        if (ch == '{')
                        {
                            if (bodyStart == -1)
                            {
                                bodyStart = j + 1; // 1-based
                            }

                            braceDepth++;
                        }
                        else if (ch == '}')
                        {
                            braceDepth--;
                            if (braceDepth == 0)
                            {
                                return (bodyStart, j + 1); // 1-based
                            }
                        }
                    }
                }
            }
        }

        throw new InvalidOperationException($"Could not find method '{methodName}' in '{sourceFile}'.");
    }
}
