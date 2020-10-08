// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    [TestClass]
    public class BlameDataCollectorTests : AcceptanceTestBase
    {
        private readonly string resultsDir;

        public const string NETCOREANDFX = "net452;net472;netcoreapp3.1";
        public const string NET50 = "net5.0";

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

            this.VaildateOutput();
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void BlameDataCollectorShouldOutputDumpFile(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("BlameUnitTestProject.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /Blame:CollectDump");
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            this.InvokeVsTest(arguments);

            this.VaildateOutput(true);
        }

        [TestMethod]
        [NetFrameworkRunner("net452;net472;netcoreapp3.1;net5.0")]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner("net452;net472;netcoreapp3.1;net5.0")]
        public void HangDumpOnTimeout(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("timeout.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectHangDump;HangDumpType=full;TestTimeout=3s""");
            this.InvokeVsTest(arguments);

            this.ValidateDump();
        }

        [TestMethod]
        // net5.0 does not suppord dump on exit
        [NetFrameworkRunner("net452;net472;netcoreapp3.1")]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner("net452;net472;netcoreapp3.1")]
        public void CrashDumpWhenThereIsNoTimeout(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("timeout.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectDump;DumpType=full;CollectAlways=true;CollectHangDump""");
            this.InvokeVsTest(arguments);

            this.ValidateDump();
        }

        [TestMethod]
        // net5.0 does not suppord dump on exit
        [NetFrameworkRunner("net452;net472;netcoreapp3.1")]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner("net452;net472;netcoreapp3.1")]
        public void CrashDumpOnExit(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("timeout.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectDump;DumpType=full;CollectAlways=true""");
            this.InvokeVsTest(arguments);

            this.ValidateDump();
        }

        [TestMethod]
        [NetFrameworkRunner("net452;net472;netcoreapp3.1;net5.0")]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner("net452;net472;netcoreapp3.1;net5.0")]
        public void CrashDumpOnStackOverflow(RunnerInfo runnerInfo)
        {
            Environment.SetEnvironmentVariable("PROCDUMP_PATH", Path.Combine(this.testEnvironment.PackageDirectory, @"procdump\0.0.1\bin"));

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("crash.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectDump;DumpType=full""");
            this.InvokeVsTest(arguments);

            this.ValidateDump();
        }

        [TestMethod]
        [NetFrameworkRunner(NET50)]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner(NET50)]
        public void CrashDumpChildProcesses(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("child-crash.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectDump;DumpType=full""");
            this.InvokeVsTest(arguments);

            this.ValidateDump(2);
        }

        [TestMethod]
        [NetFrameworkRunner("net452;net472;netcoreapp3.1;net5.0")]
        // should make no difference, keeping for easy debug 
        // [NetCoreRunner("net452;net472;netcoreapp3.1;net5.0")]
        public void HangDumpChildProcesses(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.GetAssetFullPath("child-hang.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, string.Empty, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $@" /Blame:""CollectHangDump;HangDumpType=full;TestTimeout=15s""");
            this.InvokeVsTest(arguments);

            this.ValidateDump(2);
        }


        private void ValidateDump(int expectedDumpCount = 1)
        {
            var attachments = this.StdOutWithWhiteSpace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .SkipWhile(l => !l.Contains("Attachments:")).Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var output = string.Join(Environment.NewLine, attachments);
            if (!attachments.Any(a => a.Contains("Sequence_")))
            {
                // sequence file is pretty flaky, and easily substituted by diag log
                // throw new AssertFailedException("Expected Sequence file in Attachments, but there was none." 
                //    + Environment.NewLine 
                //    + output);
            }

            var dumps = attachments
                .Where(a => a.EndsWith(".dmp"))
                // On Windows we might collect conhost which tells us nothing 
                // or WerFault in case we would start hanging during crash
                // we don't want these to make cross-platform checks more difficult
                // so we filter them out.
                .Where(a => !a.Contains("WerFault") && !a.Contains("conhost"))
                .Select(a => a.Trim()).ToList();

            if (dumps.Count < expectedDumpCount)
            {
                throw new AssertFailedException($"Expected at least {expectedDumpCount} dump file in Attachments, but there were {dumps.Count}." 
                    + Environment.NewLine 
                    + string.Join(Environment.NewLine, dumps));
            }

            var nonExistingDumps = new List<string>();
            var emptyDumps = new List<string>();
            foreach (var dump in dumps)
            {
                if (!File.Exists(dump))
                {
                    nonExistingDumps.Add(dump);
                }
                else
                {
                    var file = new FileInfo(dump);
                    if (file.Length == 0)
                    {
                        emptyDumps.Add(dump);
                    }
                }
            }

            // allow some child dumps to be missing, they manage to terminate early from time to time
            if ((dumps.Count == 1 && nonExistingDumps.Any()) || (dumps.Count > 1 && nonExistingDumps.Count > 1)
                || emptyDumps.Any())
            {
                var err = new StringBuilder();
                err.AppendLine("Expected all dumps in the list of attachments to exist, and not be empty, but:");
                if (nonExistingDumps.Any())
                {
                    err.AppendLine($"{nonExistingDumps.Count} don't exist:")
                    .AppendLine(string.Join(Environment.NewLine, nonExistingDumps));
                }

                if (emptyDumps.Any())
                {
                    err.AppendLine($"{emptyDumps.Count} are empty:")
                    .AppendLine(string.Join(Environment.NewLine, emptyDumps));
                }

                err.AppendLine("Reported attachments:")
                .AppendLine(output);

                throw new AssertFailedException(err.ToString());
            }
        }

        private void VaildateOutput(bool validateDumpFile = false)
        {
            bool isSequenceAttachmentReceived = false;
            bool isDumpAttachmentReceived = false;
            bool isValid = false;
            this.StdErrorContains("BlameUnitTestProject.UnitTest1.TestMethod2");
            this.StdOutputContains("Sequence_");
            var resultFiles = Directory.GetFiles(this.resultsDir, "*", SearchOption.AllDirectories);

            foreach (var file in resultFiles)
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
