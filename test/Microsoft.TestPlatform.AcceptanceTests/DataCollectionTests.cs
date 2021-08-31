// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void ExecuteTestsWithDataCollection(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var resultsDir = GetResultsDirectory();
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            string runSettings = this.GetRunsettingsFilePath(resultsDir);
            string diagFileName = Path.Combine(resultsDir, "diaglog.txt");
            var extensionsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                Path.GetFileNameWithoutExtension("OutOfProcDataCollector"),
                "bin",
                IntegrationTestEnvironment.BuildConfiguration,
                this.testEnvironment.RunnerFramework);
            var arguments = PrepareArguments(assemblyPaths, null, runSettings, this.FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, $" /Diag:{diagFileName}", $" /TestAdapterPath:{extensionsPath}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.VaildateDataCollectorOutput(resultsDir);

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void ExecuteTestsWithDataCollectionUsingCollectArgument(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var resultsDir = GetResultsDirectory();
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            string diagFileName = Path.Combine(resultsDir, "diaglog.txt");
            var extensionsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                Path.GetFileNameWithoutExtension("OutOfProcDataCollector"),
                "bin",
                IntegrationTestEnvironment.BuildConfiguration,
                this.testEnvironment.RunnerFramework);

            var arguments = PrepareArguments(assemblyPaths, null, null, this.FrameworkArgValue, runnerInfo.InIsolationValue, resultsDir);
            arguments = string.Concat(arguments, $" /Diag:{diagFileName}", $" /Collect:SampleDataCollector", $" /TestAdapterPath:{extensionsPath}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.VaildateDataCollectorOutput(resultsDir);

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetCoreTargetFrameworkDataSource]
        public void DataCollectorAssemblyLoadingShouldNotThrowErrorForNetCore(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var resultsDir = GetResultsDirectory();
            var arguments = PrepareArguments(GetAssetFullPath("AppDomainGetAssembliesTestProject.dll", "netcoreapp2.1"), string.Empty, string.Empty, this.FrameworkArgValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource]
        public void DataCollectorAssemblyLoadingShouldNotThrowErrorForFullFramework(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var resultsDir = GetResultsDirectory();
            var arguments = PrepareArguments(GetAssetFullPath("AppDomainGetAssembliesTestProject.dll"), string.Empty, string.Empty, this.FrameworkArgValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);

            TryRemoveDirectory(resultsDir);
        }

        private static void CreateDataCollectionRunSettingsFile(string destinationRunsettingsPath, Dictionary<string, string> dataCollectionAttributes)
        {
            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);

            doc.AppendChild(xmlDeclaration);
            var runSettingsNode = doc.CreateElement(Constants.RunSettingsName);
            doc.AppendChild(runSettingsNode);
            var dcConfigNode = doc.CreateElement(Constants.DataCollectionRunSettingsName);
            runSettingsNode.AppendChild(dcConfigNode);
            var dataCollectorsNode = doc.CreateElement(Constants.DataCollectorsSettingName);
            dcConfigNode.AppendChild(dataCollectorsNode);
            var dataCollectorNode = doc.CreateElement(Constants.DataCollectorSettingName);
            dataCollectorsNode.AppendChild(dataCollectorNode);

            foreach (var kvp in dataCollectionAttributes)
            {
                dataCollectorNode.SetAttribute(kvp.Key, kvp.Value);
            }

            using (var stream = new FileHelper().GetStream(destinationRunsettingsPath, FileMode.Create))
            {
                doc.Save(stream);
            }
        }

        private void VaildateDataCollectorOutput(string resultsDir)
        {
            // Output of datacollection attachment.
            this.StdOutputContains("filename.txt");
            this.StdOutputContains("TestCaseStarted");
            this.StdOutputContains("TestCaseEnded");
            this.StdOutputContains("SampleUnitTestProject2.UnitTest1.PassingTest2");
            this.StdOutputContains("SampleUnitTestProject2.UnitTest1.FailingTest2");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: SessionStarted");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: TestHostLaunched");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: SessionEnded");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: my warning");
            this.StdErrorContains("Data collector 'SampleDataCollector' message: Data collector caught an exception of type 'System.Exception': 'my exception'. More details:");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: Dispose called.");

            // Verify attachments
            var isTestRunLevelAttachmentFound = false;
            var testCaseLevelAttachmentsCount = 0;
            var diaglogsFileCount = 0;

            var resultFiles = Directory.GetFiles(resultsDir, "*.txt", SearchOption.AllDirectories);

            foreach (var file in resultFiles)
            {
                // Test Run level attachments are logged in standard output.
                if (file.Contains("filename.txt"))
                {
                    this.StdOutputContains(file);
                    isTestRunLevelAttachmentFound = true;
                }

                if (file.Contains("testcasefilename"))
                {
                    testCaseLevelAttachmentsCount++;
                }

                if (file.Contains("diaglog"))
                {
                    diaglogsFileCount++;
                }
            }

            Assert.IsTrue(isTestRunLevelAttachmentFound);
            Assert.AreEqual(3, testCaseLevelAttachmentsCount);
            Assert.AreEqual(3, diaglogsFileCount);
        }

        private string GetRunsettingsFilePath(string resultsDir)
        {
            var runsettingsPath = Path.Combine(resultsDir, "test_" + Guid.NewGuid() + ".runsettings");
            var dataCollectionAttributes = new Dictionary<string, string>();

            dataCollectionAttributes.Add("friendlyName", "SampleDataCollector");
            dataCollectionAttributes.Add("uri", "my://sample/datacollector");

            CreateDataCollectionRunSettingsFile(runsettingsPath, dataCollectionAttributes);
            return runsettingsPath;
        }
    }
}