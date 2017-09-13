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
        public void EventLogDataCollectorShoudCreateLogFileHavingEvents(RunnnerInfo runnnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnnerInfo);
            var assemblyPaths = this.testEnvironment.GetTestAsset("EventLogUnitTestProject.dll");

            string runSettings = this.GetRunsettingsFilePath();
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings, this.FrameworkArgValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(3, 0, 0);
            this.VaildateDataCollectorOutput();
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void EventLogDataCollectorShoudCreateLogFile(RunnnerInfo runnnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnnerInfo);
            var assemblyPaths = this.testEnvironment.GetTestAsset("SimpleTestProject.dll");

            string runSettings = this.GetRunsettingsFilePath();
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings, this.FrameworkArgValue);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.StdErrorDoesNotContains("Data collector 'Event Log' message: Data collector 'Event Log' threw an exception during type loading, construction, or initialization:");
            this.StdErrorDoesNotContains("Parameter name:");
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
                <Configuration><Setting name = ""EventLogs"" value = ""Application,System"" /><Setting name=""EntryTypes"" value=""Error,Warning"" /></Configuration>
            </DataCollector>
        </DataCollectors>
      </DataCollectionRunSettings>
    </RunSettings> ";

            File.WriteAllText(runsettingsPath, runSettingsXml);
            return runsettingsPath;
        }

        private string GetRunsettingsFilePathWithCustomSource()
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
                <Configuration><Setting name = ""EventLogs"" value = ""Application,System"" /><Setting name=""EntryTypes"" value=""Error,Warning"" /><Setting name=""EventSources"" value=""CustomEventSource"" /></Configuration>
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

            Assert.IsTrue(fileContent1.Contains("110") && fileContent1.Contains("111") && fileContent1.Contains("112"));
            Assert.IsTrue(fileContent2.Contains("20") && fileContent2.Contains("221") && fileContent2.Contains("222") && fileContent2.Contains("223"));
            Assert.IsTrue(fileContent3.Contains("330") && fileContent3.Contains("331") && fileContent3.Contains("332"));

            Assert.IsTrue(fileContent4.Contains("110") && fileContent4.Contains("111") && fileContent4.Contains("112") && fileContent4.Contains("220") && fileContent4.Contains("221") && fileContent4.Contains("222") && fileContent4.Contains("23") && fileContent3.Contains("330") && fileContent4.Contains("331") && fileContent4.Contains("332"));
        }
    }
}
