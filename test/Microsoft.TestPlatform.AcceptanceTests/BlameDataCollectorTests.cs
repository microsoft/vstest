// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Xml;

    [TestClass]
    public class BlameDataCollectorTests : AcceptanceTestBase
    {
        private readonly string resultsDir;

        public BlameDataCollectorTests()
        {
            this.resultsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", null);

            if (Directory.Exists(this.resultsDir))
            {
                Directory.Delete(this.resultsDir, true);
            }
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldGiveCorrectTestCaseName(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("BlameUnitTestProject.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            this.InvokeVsTest(arguments);

            this.ValidateSequenceFileOutput();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldOutputDumpFile(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("BlameUnitTestProject.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame:CollectDump");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            this.InvokeVsTest(arguments);

            this.ValidateDumpFileOutput();
        }

        private void ValidateSequenceFileOutput()
        {
            bool isAttachmentReceived = false;
            bool isValid = false;
            this.StdErrorContains("BlameUnitTestProject.UnitTest1.TestMethod2");
            this.StdOutputContains("Sequence_");
            var resultFiles = Directory.GetFiles(this.resultsDir, "*", SearchOption.AllDirectories);

            foreach (var file in resultFiles)
            {
                if (file.Contains("Sequence_"))
                {
                    isAttachmentReceived = true;
                    isValid = IsValidXml(file);
                }
            }

            Assert.IsTrue(isAttachmentReceived);
            Assert.IsTrue(isValid);
        }

        private void ValidateDumpFileOutput()
        {
            bool isAttachmentReceived = false;
            var resultFiles = Directory.GetFiles(this.resultsDir, "*", SearchOption.AllDirectories);

            foreach(var file in resultFiles)
            {
                if (file.Contains(".dmp"))
                {
                    isAttachmentReceived = true;
                }
            }

            Assert.IsTrue(isAttachmentReceived);
        }

        private bool IsValidXml(string xmlFilePath)
        {
            var file = File.OpenRead(xmlFilePath);
            var reader = XmlReader.Create(file);
            try
            {
                while (reader.Read())
                {
                }
                file.Dispose();
                return true;
            }
            catch (XmlException)
            {
                file.Dispose();
                return false;
            }
        }
    }
}
