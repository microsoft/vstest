// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
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

            this.VaildateOutput("BlameUnitTestProject.UnitTest1.TestMethod2");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldOutputDumpFile(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);            
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame:CollectDump");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            arguments = string.Concat(arguments, " /testcasefilter:ExitWithStackoverFlow");
            this.InvokeVsTest(arguments);

            this.VaildateOutput("SampleUnitTestProject3.UnitTest1.ExitWithStackoverFlow", validateDumpFile: true);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldNotOutputDumpFileWhenNoCrashOccurs(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame:CollectDump");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            arguments = string.Concat(arguments, " /testcasefilter:PassingTest");
            this.InvokeVsTest(arguments);

            StringAssert.DoesNotMatch(this.StdOut, new Regex( @"\.dmp"), "it should not collect a dump, because nothing crashed");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldOutputDumpFileWhenNoCrashOccursButCollectAlwaysIsEnabled(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame:CollectDump;CollectAlways=True");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            arguments = string.Concat(arguments, " /testcasefilter:PassingTest");
            this.InvokeVsTest(arguments);

            StringAssert.Matches(this.StdOut, new Regex(@"\.dmp"), "it should collect dump, even if nothing crashed");
        }

        private void VaildateOutput(string testName, bool validateDumpFile = false)
        {
            bool isSequenceAttachmentReceived = false;
            bool isDumpAttachmentReceived = false;
            bool isValid = false;
            this.StdErrorContains(testName);
            this.StdOutputContains("Sequence_");
            var resultFiles = Directory.GetFiles(this.resultsDir, "*", SearchOption.AllDirectories);

            foreach(var file in resultFiles)
            {
                if (file.Contains("Sequence_"))
                {
                    isSequenceAttachmentReceived = true;
                    isValid = IsValidXml(file);
                }
                else if (validateDumpFile && file.Contains(".dmp"))
                {
                    isDumpAttachmentReceived = true;
                }
            }

            Assert.IsTrue(isSequenceAttachmentReceived);
            Assert.IsTrue(!validateDumpFile || isDumpAttachmentReceived);
            Assert.IsTrue(isValid);
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
