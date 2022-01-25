// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System;

[TestClass]
public class LoggerTests : AcceptanceTestBase
{
    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void TrxLoggerWithFriendlyNameShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var testResultsDirectory = GetResultsDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        var trxFileName = "TestResults.trx";
        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
        InvokeVsTest(arguments);

        arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
        arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
        InvokeVsTest(arguments);

        var trxFilePath = Path.Combine(testResultsDirectory, trxFileName);
        Assert.IsTrue(IsValidXml(trxFilePath), "Invalid content in Trx log file");

        TryRemoveDirectory(testResultsDirectory);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void HtmlLoggerWithFriendlyNameShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var testResultsDirectory = GetResultsDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        var htmlFileName = "TestResults.html";
        arguments = string.Concat(arguments, $" /logger:\"html;LogFileName={htmlFileName}\"");
        InvokeVsTest(arguments);

        arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"html;LogFileName={htmlFileName}\"");
        arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
        InvokeVsTest(arguments);

        var htmlLogFilePath = Path.Combine(testResultsDirectory, htmlFileName);
        IsFileAndContentEqual(htmlLogFilePath);

        TryRemoveDirectory(testResultsDirectory);
    }

    [TestMethod]
    [NetCoreTargetFrameworkDataSource]
    public void TrxLoggerWithExecutorUriShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var testResultsDirectory = GetResultsDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        var trxFileName = "TestResults.trx";
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFileName={trxFileName}\"");
        InvokeVsTest(arguments);

        arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFileName={trxFileName}\"");
        arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
        InvokeVsTest(arguments);

        var trxLogFilePath = Path.Combine(testResultsDirectory, trxFileName);
        Assert.IsTrue(IsValidXml(trxLogFilePath), "Invalid content in Trx log file");

        TryRemoveDirectory(testResultsDirectory);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void TrxLoggerWithLogFilePrefixShouldGenerateMultipleTrx(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var testResultsDirectory = GetResultsDirectory();
        var trxFileNamePattern = "TestResults";

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFilePrefix={trxFileNamePattern}\"");
        InvokeVsTest(arguments);

        arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFilePrefix={trxFileNamePattern}\"");
        arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
        InvokeVsTest(arguments);

        var trxFilePaths = Directory.EnumerateFiles(testResultsDirectory, trxFileNamePattern + "_net*.trx");
        Assert.IsTrue(trxFilePaths.Count() > 1);

        TryRemoveDirectory(testResultsDirectory);
    }

    [TestMethod]
    [NetCoreTargetFrameworkDataSource]
    public void HtmlLoggerWithExecutorUriShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var testResultsDirectory = GetResultsDirectory();

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        var htmlFileName = "TestResults.html";
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/htmlLogger/v1;LogFileName{htmlFileName}\"");
        InvokeVsTest(arguments);

        arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, testResultsDirectory);
        arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/htmlLogger/v1;LogFileName={htmlFileName}\"");
        arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
        InvokeVsTest(arguments);

        var htmlLogFilePath = Path.Combine(testResultsDirectory, htmlFileName);
        IsFileAndContentEqual(htmlLogFilePath);

        TryRemoveDirectory(testResultsDirectory);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void TrxLoggerResultSummaryOutcomeValueShouldBeFailedIfNoTestsExecutedAndTreatNoTestsAsErrorIsTrue(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue);
        var trxFileName = "TrxLogger.trx";

        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");

        // Setting /TestCaseFilter to the test name, which does not exists in the assembly, so we will have 0 tests executed
        arguments = string.Concat(arguments, " /TestCaseFilter:TestNameThatMatchesNoTestInTheAssembly");
        arguments = string.Concat(arguments, " -- RunConfiguration.TreatNoTestsAsError=true");

        InvokeVsTest(arguments);

        var trxLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", trxFileName);
        string outcomeValue = GetElementAtributeValueFromTrx(trxLogFilePath, "ResultSummary", "outcome");

        Assert.AreEqual("Failed", outcomeValue);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void TrxLoggerResultSummaryOutcomeValueShouldNotChangeIfNoTestsExecutedAndTreatNoTestsAsErrorIsFalse(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue);
        var trxFileName = "TrxLogger.trx";

        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");

        // Setting /TestCaseFilter to the test name, which does not exists in the assembly, so we will have 0 tests executed
        arguments = string.Concat(arguments, " /TestCaseFilter:TestNameThatMatchesNoTestInTheAssembly");
        arguments = string.Concat(arguments, " -- RunConfiguration.TreatNoTestsAsError=false");

        InvokeVsTest(arguments);

        var trxLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", trxFileName);
        string outcomeValue = GetElementAtributeValueFromTrx(trxLogFilePath, "ResultSummary", "outcome");

        Assert.AreEqual("Completed", outcomeValue);
    }

    private bool IsValidXml(string xmlFilePath)
    {
        try
        {
            using var file = File.OpenRead(xmlFilePath);
            using var reader = XmlReader.Create(file);
            while (reader.Read())
            {
            }

            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private void IsFileAndContentEqual(string filePath)
    {
        StringBuilder sb = new();
        using (var sr = new StreamReader(filePath))
        {
            sb.Append(sr.ReadToEnd());
        }

        string filePathContent = sb.ToString();
        string[] divs = { "Total tests", "Passed", "Failed", "Skipped", "Run duration", "Pass percentage", "SampleUnitTestProject.UnitTest1.PassingTest" };
        foreach (string str in divs)
        {
            StringAssert.Contains(filePathContent, str);
        }
    }

    private static string GetElementAtributeValueFromTrx(string trxFileName, string fieldName, string attributeName)
    {
        using (FileStream file = File.OpenRead(trxFileName))
        using (XmlReader reader = XmlReader.Create(file))
        {
            while (reader.Read())
            {
                if (reader.Name.Equals(fieldName) && reader.NodeType == XmlNodeType.Element && reader.HasAttributes)
                {
                    return reader.GetAttribute(attributeName);
                }
            }
        }

        return null;
    }
}