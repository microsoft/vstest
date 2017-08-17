// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventLogCollectorTests : AcceptanceTestBase
    {
        private readonly string resultsDir;

        public EventLogCollectorTests()
        {
            this.resultsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void EventLogDataCollectorShoudCreateLogFile(string runnerFramework, string targetFramework, string targetRuntime)
        {
            SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths = this.BuildMultipleAssemblyPath("EventLogUnitTestProject.dll").Trim('\"');
            string runSettings = this.GetRunsettingsFilePath();
            string diagFileName = Path.Combine(this.resultsDir, "diaglog.txt");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings, this.FrameworkArgValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}", $" /Diag:{diagFileName}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
            this.VaildateDataCollectorOutput();
        }

        private string GetRunsettingsFilePath()
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");

            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
    <RunSettings>     
      <RunConfiguration> 
        <MaxCpuCount>0</MaxCpuCount>       
        <TargetPlatform> x64 </TargetPlatform>     
        <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion> 
      </RunConfiguration>
      <DataCollectionRunSettings>  
        <DataCollectors>  
            <DataCollector friendlyName=""Event Log"" >
                <Configuration><Setting name = ""EventLogs"" value = ""Application"" /></Configuration>
            </DataCollector>
        </DataCollectors>
      </DataCollectionRunSettings>
    </RunSettings> ";

            File.WriteAllText(runsettingsPath, runSettingsXml);
            return runsettingsPath;
        }

        private void VaildateDataCollectorOutput()
        {
            // Verify attachments
            var isTestRunLevelAttachmentFound = false;

            var resultFiles = Directory.GetFiles(this.resultsDir, "Event Log.xml", SearchOption.AllDirectories);

            foreach (var file in resultFiles)
            {
                // Test Run level attachments are logged in standard output.
                if (file.Contains("Event Log.xml"))
                {
                    this.StdOutputContains(file);
                    isTestRunLevelAttachmentFound = true;
                }
            }

            Assert.IsTrue(isTestRunLevelAttachmentFound);
            var fileContent = File.ReadAllText(resultFiles[0]);
            Assert.IsTrue(fileContent.Contains("<Source>Application</Source>"));
            Assert.IsTrue(fileContent.Contains("<Source>Application</Source>"));
        }
    }
}
