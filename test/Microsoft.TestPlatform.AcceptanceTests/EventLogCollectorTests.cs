// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
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
        public void EventLogDataCollectorShoudCreateLogFileHavingEvents(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.testEnvironment.GetTestAsset("EventLogUnitTestProject.dll");

            string runSettings = this.GetRunsettingsFilePath();
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(3, 0, 0);
            this.VaildateDataCollectorOutput();
            this.StdOutputDoesNotContains("An exception occurred while collecting final entries from the event log");
            this.StdErrorDoesNotContains("event log has encountered an exception, some events might get lost");
            this.StdOutputDoesNotContains("event log may have been cleared during collection; some events may not have been collected");
            this.StdErrorDoesNotContains("Unable to read event log");
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void EventLogDataCollectorShoudCreateLogFileWithoutEventsIfEventsAreNotLogged(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.testEnvironment.GetTestAsset("SimpleTestProject.dll");

            string runSettings = this.GetRunsettingsFilePath();
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runSettings);
            arguments = string.Concat(arguments, $" /ResultsDirectory:{resultsDir}");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.StdOutputDoesNotContains("An exception occurred while collecting final entries from the event log");
            this.StdErrorDoesNotContains("event log has encountered an exception, some events might get lost");
            this.StdOutputDoesNotContains("event log may have been cleared during collection; some events may not have been collected");
            this.StdErrorDoesNotContains("Unable to read event log");
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

            var eventIdsDics = new Dictionary<string[], bool>();
            eventIdsDics.Add(new[] { "110", "111", "112" }, false);
            eventIdsDics.Add(new[] { "220", "221", "222", "223" }, false);
            eventIdsDics.Add(new[] { "330", "331", "332" }, false);

            // Since there is no guaranty that test will run in a particular order, we will check file for all available list of ids
            Console.WriteLine("File1");
            Console.WriteLine(fileContent1);

            Console.WriteLine("File2");
            Console.WriteLine(fileContent2);

            Console.WriteLine("File3");
            Console.WriteLine(fileContent3);

            Console.WriteLine("File4");
            Console.WriteLine(fileContent4);

            Assert.IsTrue(this.VerifyOrder2(fileContent1, eventIdsDics), string.Format("Event log file content: {0}", fileContent1));
            Assert.IsTrue(this.VerifyOrder2(fileContent2, eventIdsDics), string.Format("Event log file content: {0}", fileContent2));
            Assert.IsTrue(this.VerifyOrder2(fileContent3, eventIdsDics), string.Format("Event log file content: {0}", fileContent3));

            Assert.IsTrue(this.VerifyOrder(fileContent4, new[] { "110", "111", "112", "220", "221", "222", "223", "330", "331", "332" }), string.Format("Event log file content: {0}", fileContent4));
        }

        private bool VerifyOrder2(string content, Dictionary<string[], bool> eventIdsDics)
        {
            foreach (var eventIds in eventIdsDics)
            {
                if (eventIds.Value == false)
                {
                    if (VerifyOrder(content, eventIds.Key))
                    {
                        eventIdsDics[eventIds.Key] = true;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool VerifyOrder(string content, string[] eventIds)
        {
            for (int i = 0; i < eventIds.Length; i++)
            {
                int currentIndex = 0;
                currentIndex = content.IndexOf(eventIds[i], currentIndex);
                if (currentIndex == -1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
