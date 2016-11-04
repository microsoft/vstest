// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionTests : IntegrationTestBase
    {
        public string Framework { get; protected set; } = ".NETFramework,Version=v4.6";

        [TestMethod]
        public void ChutzpahRunAllTestExecution()
        {
            var testJSFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "test.js");
            this.InvokeVsTestForExecution(testJSFileAbsolutePath, this.GetTestAdapterPath(UnitTestFramework.Chutzpah));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void CPPRunAllTestExecution()
        {
            var assemblyRelativePath =
                @"microsoft.testplatform.testasset.nativecpp\1.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            this.InvokeVsTestForExecution(assemblyAbsolutePath, string.Empty);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        public void NUnitRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(
                this.GetAssetFullPath("NUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.NUnit));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void RunMultipleTestAssemblies()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.Framework);
            this.ValidateSummaryStatus(2, 2, 2);
        }

        [TestMethod]
        public void RunMultipleTestAssembliesInParallel()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.Framework);
            arguments = string.Concat(arguments, " /Parallel");
            arguments = string.Concat(arguments, " /Platform:x86");
            var testhostProcessName = "testhost.x86";
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                2,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {2} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
        }

        [TestMethod]
        public void RunSelectedTests()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /Tests:PassingTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        }

        [TestMethod]
        public void RunSelectedTestsWithAndOperatorTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithCategoryTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithClassNameTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        public void RunSelectedTestsWithFullyQualifiedNameTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(
                arguments,
                " /TestCaseFilter:\"FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithNameTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithOrOperatorTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithPriorityTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        /// <summary>
        ///     The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx64()
        {
            var platformArg = " /Platform:x64";
            var testhostProcessName = "testhost";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        /// <summary>
        ///     The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx86()
        {
            var platformArg = " /Platform:x86";
            var testhostProcessName = "testhost.x86";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithoutParallelAndPlatformX86()
        {
            var testhostProcessName = "testhost.x86";
            var expectedProcessCreated = 1;
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "1" },
                                                         { "TargetPlatform", "x86" },
                                                         { "TargetFrameworkVersion", "Framework45" },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedProcessCreated);
        }

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithParallelAndPlatformX64()
        {
            var testhostProcessName = "testhost";
            var expectedProcessCreated = 2;
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "2" },
                                                         { "TargetPlatform", "x64" },
                                                         { "TargetFrameworkVersion", "Framework45" },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedProcessCreated);
        }

        [TestMethod]
        public void RunTestExecutionWithTestAdapterPathFromRunSettings()
        {
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            var runsettingsFilePath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                string.Empty,
                runsettingsFilePath,
                this.Framework);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            File.Delete(runsettingsFilePath);
        }

#if NET46
        [TestMethod]
        public void RunTestExecutionWithDisableAppDomain()
        {
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
                this.Framework);

            this.InvokeVsTest(arguments);

            Assert.IsTrue(IsFilesContentEqual(testAppDomainDetailFileName, dataCollectorAppDomainDetailFileName), "Different AppDomains, test: {0} datacollector: {1}", File.ReadAllText(testAppDomainDetailFileName), File.ReadAllText(dataCollectorAppDomainDetailFileName));
            this.ValidateSummaryStatus(1, 1, 1);
            File.Delete(runsettingsFilePath);
        }
#endif

        [TestMethod]
        public void XUnitRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(
                this.GetAssetFullPath("XUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.XUnit));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        private string GetRunsettingsFilePath(Dictionary<string, string> runConfigurationDictionary)
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");
            CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            return runsettingsPath;
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName)
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.Framework);
            arguments = string.Concat(arguments, platformArg);

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);

            cts.Cancel();

            Assert.AreEqual(
                1,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {1} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(1, 1, 1);
        }

        private void RunTestWithRunSettings(
            Dictionary<string, string> runConfigurationDictionary,
            string testhostProcessName,
            int expectedProcessCreated)
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = this.GetRunsettingsFilePath(runConfigurationDictionary);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.Framework);
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                expectedProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedProcessCreated} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
            File.Delete(runsettingsPath);
        }

        private string GetInProcDataCollectionRunsettingsFile(bool disableAppDomain)
        {
            var runSettings = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".runsettings");
            var inprocasm = this.testEnvironment.GetTestAsset("SimpleDataCollector.dll");
            var fileContents = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='SimpleDataCollector.SimpleDataCollector, SimpleDataCollector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase={0}>
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

            fileContents = string.Format(fileContents, "'" + inprocasm + "'");
            File.WriteAllText(runSettings, fileContents);

            return runSettings;
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
    }
}