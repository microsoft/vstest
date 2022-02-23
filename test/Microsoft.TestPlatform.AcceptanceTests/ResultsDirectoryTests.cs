// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class ResultsDirectoryTests : AcceptanceTestBase
{
    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void TrxFileShouldBeCreatedInResultsDirectory(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue);
        var trxFileName = "TestResults.trx";
        using var tempDir = new TempDirectory();
        var trxFilePath = Path.Combine(tempDir.Path, trxFileName);
        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
        arguments = string.Concat(arguments, $" /ResultsDirectory:{tempDir.Path}");

        // Delete if already exists
        TempDirectory.TryRemoveDirectory(tempDir.Path);

        InvokeVsTest(arguments);

        Assert.IsTrue(File.Exists(trxFilePath), $"Expected Trx file: {trxFilePath} not created in results directory");
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void ResultsDirectoryRelativePathShouldWork(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue);
        var trxFileName = "TestResults.trx";
        var relativeDirectory = @"relative\directory";
        var resultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory);

        var trxFilePath = Path.Combine(resultsDirectory, trxFileName);
        arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
        arguments = string.Concat(arguments, $" /ResultsDirectory:{relativeDirectory}");

        if (Directory.Exists(resultsDirectory))
        {
            Directory.Delete(resultsDirectory, true);
        }

        InvokeVsTest(arguments);

        Assert.IsTrue(File.Exists(trxFilePath), $"Expected Trx file: {trxFilePath} not created in results directory");
        TempDirectory.TryRemoveDirectory(resultsDirectory);
    }
}
