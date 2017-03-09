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
    using Microsoft.TestPlatform.TestUtilities;

    [TestClass]
    public class DataCollectionTests : AcceptanceTestBase
    {
        private string resultsDir;

        public DataCollectionTests()
        {
            this.resultsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(resultsDir, true);
        }

        [CustomDataTestMethod]
        [NET46TargetFramework]
        [NETCORETargetFramework]
        public void ExecuteTestsWithDataCollection(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            string runSettings = GetRunsettingsFilePath();

            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings, this.FrameworkArgValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");
            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.VaildateDataCollectorOutput();
        }

        private string GetRunsettingsFilePath()
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");
            var dataCollectionAttributes = new Dictionary<string, string>();

            dataCollectionAttributes.Add("friendlyName", "SampleDataCollector");
            dataCollectionAttributes.Add("uri", "my://sample/datacollector");
            var codebase = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                Path.GetFileNameWithoutExtension("OutOfProcDataCollector"),
                "bin",
                this.testEnvironment.BuildConfiguration,
                this.testEnvironment.RunnerFramework,
                "OutOfProcDataCollector.dll");

            var outOfProcAssemblyPath = this.testEnvironment.GetTestAsset("OutOfProcDataCollector.dll");

            dataCollectionAttributes.Add("assemblyQualifiedName", string.Format("OutOfProcDataCollector.SampleDataCollector, {0}", AssemblyUtility.GetAssemblyName(outOfProcAssemblyPath)));
            dataCollectionAttributes.Add("codebase", codebase);
            CreateDataCollectionRunSettingsFile(runsettingsPath, dataCollectionAttributes);
            return runsettingsPath;
        }

        public static void CreateDataCollectionRunSettingsFile(string destinationRunsettingsPath, Dictionary<string, string> dataCollectionAttributes)
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

        public void VaildateDataCollectorOutput()
        {
            // Output of datacollection attachment.
            this.StdOutputContains("filename.txt");
            this.StdOutputContains("TestCaseStarted");
            this.StdOutputContains("TestCaseEnded");
            this.StdOutputContains("SessionEnded");
            this.StdOutputContains("SessionStarted");
            this.StdOutputContains("my warning");
            this.StdErrorContains("Diagnostic data adapter caught an exception of type 'System.Exception': 'my exception'. More details: .");

            // Verify attachments
            bool isTestRunLevelAttachmentFound = false;
            int testCaseLevelAttachmentsCount = 0;

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
            }

            Assert.IsTrue(isTestRunLevelAttachmentFound);
            Assert.AreEqual(3, testCaseLevelAttachmentsCount);
        }
    }
}
