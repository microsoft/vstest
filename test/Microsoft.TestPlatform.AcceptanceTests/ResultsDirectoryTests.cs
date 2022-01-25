// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ResultsDirectoryTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TrxFileShouldBeCreatedInResultsDirectory(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);

            var arguments = PrepareArguments(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue);
            var trxFileName = "TestResults.trx";
            var resultsDir = GetResultsDirectory();
            var trxFilePath = Path.Combine(resultsDir, trxFileName);
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            // Delete if already exists
            TryRemoveDirectory(resultsDir);

            InvokeVsTest(arguments);

            Assert.IsTrue(File.Exists(trxFilePath), $"Expected Trx file: {trxFilePath} not created in results directory");
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void ResultsDirectoryRelativePathShouldWork(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(testEnvironment, runnerInfo);

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
            TryRemoveDirectory(resultsDirectory);
        }
    }
}
