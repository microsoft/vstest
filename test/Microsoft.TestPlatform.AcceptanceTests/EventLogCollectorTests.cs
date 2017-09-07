// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
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

            this.ValidateSummaryStatus(3, 0, 0);
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
            var di = new DirectoryInfo(this.resultsDir);
            var resultFiles = di.EnumerateFiles("Event Log.xml", SearchOption.AllDirectories)
                .OrderBy(d => d.CreationTime)
                .Select(d => d.FullName)
                .ToList();

            Assert.AreEqual(4, resultFiles.Count);
            this.StdOutputContains("Event Log.xml");

            var fileContent1 = File.ReadAllText(resultFiles[0]);
            var fileContent2 = File.ReadAllText(resultFiles[1]);
            var fileContent3 = File.ReadAllText(resultFiles[2]);
            var fileContent4 = File.ReadAllText(resultFiles[3]);

            Assert.IsTrue(fileContent1.Contains("123"));
            Assert.IsTrue(fileContent2.Contains("234"));
            Assert.IsTrue(fileContent3.Contains("345"));
            Assert.IsTrue(fileContent4.Contains("123") && fileContent4.Contains("234") && fileContent4.Contains("345"));
        }
    }
}
