// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Xml;

    [TestClass]
    public class LoggerTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void TrxLoggerWithFriendlyNameShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var trxFileName = "TestResults.trx";
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"trx;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var trxLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults",  trxFileName);
            Assert.IsTrue(IsValidXml(trxLogFilePath), "Invalid content in Trx log file");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void HtmlLoggerWithFriendlyNameShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var htmlFileName = "TestResults.html";
            arguments = string.Concat(arguments, $" /logger:\"html;LogFileName={htmlFileName}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"html;LogFileName={htmlFileName}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var htmlLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", htmlFileName);
            IsFileAndContentEqual(htmlLogFilePath);
        }

        [TestMethod]
        [NetCoreTargetFrameworkDataSource]
        public void TrxLoggerWithExecutorUriShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var trxFileName = "TestResults.trx";
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFileName{trxFileName}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFileName={trxFileName}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var trxLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", trxFileName);
            Assert.IsTrue(IsValidXml(trxLogFilePath), "Invalid content in Trx log file");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void TrxLoggerWithLogFilePrefixShouldGenerateMultipleTrx(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var trxFileNamePattern = "TestResults";

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFilePrefix={trxFileNamePattern}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/TrxLogger/v1;LogFilePrefix={trxFileNamePattern}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var trxFilePaths = Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestResults"), trxFileNamePattern + "_net*.trx");
            Assert.IsTrue(trxFilePaths.Count() > 1);

        }

        [TestMethod]
        [NetCoreTargetFrameworkDataSource]
        public void HtmlLoggerWithExecutorUriShouldProperlyOverwriteFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            var htmlFileName = "TestResults.html";
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/htmlLogger/v1;LogFileName{htmlFileName}\"");
            this.InvokeVsTest(arguments);

            arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, $" /logger:\"logger://Microsoft/TestPlatform/htmlLogger/v1;LogFileName={htmlFileName}\"");
            arguments = string.Concat(arguments, " /testcasefilter:Name~Pass");
            this.InvokeVsTest(arguments);

            var htmlLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", htmlFileName);
            IsFileAndContentEqual(htmlLogFilePath);
        }

        private bool IsValidXml(string xmlFilePath)
        {
            var reader = System.Xml.XmlReader.Create(File.OpenRead(xmlFilePath));
            try
            {
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
            StringBuilder sb = new StringBuilder();
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
    }
}
