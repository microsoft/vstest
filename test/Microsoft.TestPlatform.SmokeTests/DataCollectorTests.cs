// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using System.IO;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#if !NET46
    using System.Runtime.Loader;
#else
    using System.Reflection;
#endif

    [TestClass]
    public class DataCollectorTests : IntegrationTestBase
    {
        private static string InProcTestResultFile = Path.Combine(Path.GetTempPath(), "inproctest.txt");
        private const string InProDataCollectorTestProject = "SimpleTestProject.dll";
        [TestMethod]
        public void RunAllWithInProcDataCollectorSettings()
        {
            // Delete File if already exists
            File.Delete(InProcTestResultFile);

            var runSettings = this.GetInProcDataCollectionRunsettingsFile();

            this.InvokeVsTestForExecution(testEnvironment.GetTestAsset(DataCollectorTests.InProDataCollectorTestProject), this.GetTestAdapterPath(), runSettings);
            this.ValidateSummaryStatus(1, 1, 1);

            ValidateInProcDataCollectionOutput();
        }

        private static void ValidateInProcDataCollectionOutput()
        {
            Assert.IsTrue(File.Exists(InProcTestResultFile), "Datacollector test file doesn't exist: {0}.", InProcTestResultFile);
            var actual = File.ReadAllText(InProcTestResultFile);
            var expected = @"TestSessionStart : <Configuration><Port>4312</Port></Configuration> TestCaseStart : PassingTest TestCaseEnd : PassingTest TestCaseStart : FailingTest TestCaseEnd : FailingTest TestCaseStart : SkippingTest TestCaseEnd : SkippingTest TestSessionEnd";
            actual = actual.Replace(" ", string.Empty).Replace("\r\n", string.Empty);
            expected = expected.Replace(" ", string.Empty).Replace("\r\n", string.Empty);
            Assert.AreEqual(expected, actual);
        }

        private string GetInProcDataCollectionRunsettingsFile()
        {
            var runSettings = Path.Combine(Path.GetDirectoryName(testEnvironment.GetTestAsset(DataCollectorTests.InProDataCollectorTestProject)), "runsettingstest.runsettings");
            var inprocasm = testEnvironment.GetTestAsset("SimpleDataCollector.dll");
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
                                </RunSettings>";

            fileContents = string.Format(fileContents, assemblyName, inprocasm);
            File.WriteAllText(runSettings, fileContents);

            return runSettings;
        }
    }
}
