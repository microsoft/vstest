// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ResultsDirectoryTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TrxFileShouldBeCreatedInResultsDirectory(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var trxFileName = "TestResultsbla.trx";
            var trxFileNamePattern = "TestResultsbla*.trx";
            var resultsDir = Path.GetTempPath();
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            // Delete if already exists
            var dir = new DirectoryInfo(resultsDir);
            foreach (var file in dir.EnumerateFiles(trxFileNamePattern))
            {
                file.Delete();
            }

            this.InvokeVsTest(arguments);

            Assert.IsTrue(Directory.EnumerateFiles(resultsDir, trxFileNamePattern).Any(), $"Expected Trx file with pattern: {trxFileNamePattern} not created in results directory");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void ResultsDirectoryRelativePathShouldWork(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var trxFileName = "TestResults.trx";
            var trxFileNamePattern = "TestResults*.trx";
            var relativeDirectory = @"relative\directory";
            var resultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory);

            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{relativeDirectory}");

            if (Directory.Exists(resultsDirectory))
            {
                Directory.Delete(resultsDirectory, true);
            }

            this.InvokeVsTest(arguments);

            Assert.IsTrue(Directory.EnumerateFiles(resultsDirectory, trxFileNamePattern).Any(), $"Expected Trx file with pattern: { trxFileNamePattern} not created in results directory");
        }
    }
}
