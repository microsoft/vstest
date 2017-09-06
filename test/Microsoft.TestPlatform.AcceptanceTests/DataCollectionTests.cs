// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionTests : AcceptanceTestBase
    {
        private readonly string resultsDir;

        public DataCollectionTests()
        {
            this.resultsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(this.resultsDir))
            {
                Directory.Delete(this.resultsDir, true);
            }
        }

        [CustomDataTestMethod]
        [NETFullTargetFrameworkWithCoreRunner]
        [NETFullTargetFrameworkInIsolation]
        [NETCORETargetFramework]
        public void ExecuteTestsWithDataCollection(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            string runSettings = this.GetRunsettingsFilePath();
            string diagFileName = Path.Combine(this.resultsDir, "diaglog.txt");
            var extensionsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                Path.GetFileNameWithoutExtension("OutOfProcDataCollector"),
                "bin",
                this.testEnvironment.BuildConfiguration,
                this.testEnvironment.RunnerFramework);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings, this.FrameworkArgValue, inIsolation);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}", $" /Diag:{diagFileName}", $" /TestAdapterPath:{extensionsPath}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.VaildateDataCollectorOutput();
        }

        [CustomDataTestMethod]
        [NETFullTargetFrameworkWithCoreRunner]
        [NETFullTargetFrameworkInIsolation]
        [NETCORETargetFramework]
        public void ExecuteTestsWithDataCollectionUsingCollectArgument(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            string diagFileName = Path.Combine(this.resultsDir, "diaglog.txt");
            var extensionsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                Path.GetFileNameWithoutExtension("OutOfProcDataCollector"),
                "bin",
                this.testEnvironment.BuildConfiguration,
                this.testEnvironment.RunnerFramework);

            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), null, this.FrameworkArgValue, inIsolation);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}", $" /Diag:{diagFileName}", $" /Collect:SampleDataCollector", $" /TestAdapterPath:{extensionsPath}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.VaildateDataCollectorOutput();
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

        private void VaildateDataCollectorOutput()
        {
            // Output of datacollection attachment.
            this.StdOutputContains("filename.txt");
            this.StdOutputContains("TestCaseStarted");
            this.StdOutputContains("TestCaseEnded");
            this.StdOutputContains("SampleUnitTestProject2.UnitTest1.PassingTest2");
            this.StdOutputContains("SampleUnitTestProject2.UnitTest1.FailingTest2");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: SessionStarted");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: SessionEnded");
            this.StdOutputContains("Data collector 'SampleDataCollector' message: my warning");
            this.StdErrorContains("Data collector 'SampleDataCollector' message: Data collector caught an exception of type 'System.Exception': 'my exception'. More details:");

            // Verify attachments
            var isTestRunLevelAttachmentFound = false;
            var testCaseLevelAttachmentsCount = 0;
            var diaglogsFileCount = 0;

            var resultFiles = Directory.GetFiles(this.resultsDir, "*.txt", SearchOption.AllDirectories);

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

        private string GetRunsettingsFilePath()
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");
            var dataCollectionAttributes = new Dictionary<string, string>();

            dataCollectionAttributes.Add("friendlyName", "SampleDataCollector");
            dataCollectionAttributes.Add("uri", "my://sample/datacollector");

            CreateDataCollectionRunSettingsFile(runsettingsPath, dataCollectionAttributes);
            return runsettingsPath;
        }
    }
}
