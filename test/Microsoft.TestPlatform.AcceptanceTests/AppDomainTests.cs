// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
#if !NET46
    using System.Runtime.Loader;
#else
    using System.Reflection;
#endif

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AppDomainTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NET46TargetFramework]
        public void RunTestExecutionWithDisableAppDomain(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var testAppDomainDetailFileName = Path.Combine(Path.GetTempPath(), "appdomain_test.txt");
            var dataCollectorAppDomainDetailFileName = Path.Combine(Path.GetTempPath(), "appdomain_datacollector.txt");

            // Delete test output files if already exist
            File.Delete(testAppDomainDetailFileName);
            File.Delete(dataCollectorAppDomainDetailFileName);
            var runsettingsFilePath = this.GetInProcDataCollectionRunsettingsFile(true);
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                runsettingsFilePath,
                this.FrameworkArgValue);

            this.InvokeVsTest(arguments);

            Assert.IsTrue(IsFilesContentEqual(testAppDomainDetailFileName, dataCollectorAppDomainDetailFileName), "Different AppDomains, test: {0} datacollector: {1}", File.ReadAllText(testAppDomainDetailFileName), File.ReadAllText(dataCollectorAppDomainDetailFileName));
            this.ValidateSummaryStatus(1, 1, 1);
            File.Delete(runsettingsFilePath);
        }

        private static bool IsFilesContentEqual(string filePath1, string filePath2)
        {
            Assert.IsTrue(File.Exists(filePath1), "File doesn't exist: {0}.", filePath1);
            Assert.IsTrue(File.Exists(filePath2), "File doesn't exist: {0}.", filePath2);
            var content1 = File.ReadAllText(filePath1);
            var content2 = File.ReadAllText(filePath2);
            Assert.IsTrue(string.Equals(content1, content2, StringComparison.Ordinal), "Content miss match file1 content:{2}{0}{2} file2 content:{2}{1}{2}", content1, content2, Environment.NewLine);
            return string.Equals(content1, content2, StringComparison.Ordinal);
        }

        private string GetInProcDataCollectionRunsettingsFile(bool disableAppDomain)
        {
            var runSettings = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".runsettings");
            var inprocasm = this.testEnvironment.GetTestAsset("SimpleDataCollector.dll");
#if !NET46
            var assemblyName = AssemblyLoadContext.GetAssemblyName(inprocasm);
#else
            var assemblyName = AssemblyName.GetAssemblyName(inprocasm);
#endif
            var fileContents = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='SimpleDataCollector.SimpleDataCollector, {0}'  codebase='{1}'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                    <RunConfiguration>
                                       <DisableAppDomain>" + disableAppDomain + @"</DisableAppDomain>
                                    </RunConfiguration>
                                </RunSettings>";

            fileContents = string.Format(fileContents, assemblyName, inprocasm);
            File.WriteAllText(runSettings, fileContents);

            return runSettings;
        }
    }
}
