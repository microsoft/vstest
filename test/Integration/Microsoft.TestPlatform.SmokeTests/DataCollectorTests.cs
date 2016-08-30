// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using System.IO;

    using Microsoft.TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectorTests : IntegrationTestBase
    {
        [TestMethod]
        public void RunAllWithInProcDataCollectorSettings()
        {
            var runSettings = this.GetInProcDataCollectionRunsettingsFile();

            this.InvokeVsTestForExecution(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), runSettings);
            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            ValidateInProcDataCollectionOutput();
        }

        private static void ValidateInProcDataCollectionOutput()
        {
            var fileName = Path.Combine(Path.GetTempPath(), "inproctest.txt");
            Assert.IsTrue(File.Exists(fileName), "Datacollector test file doesn't exist: {0}.", fileName);
            var actual = File.ReadAllText(fileName);
            var expected = @"TestSessionStart : <Configuration><Port>4312</Port></Configuration> TestCaseStart : PassingTest TestCaseEnd : PassingTest TestCaseStart : FailingTest TestCaseEnd : FailingTest TestCaseStart : SkippingTest TestCaseEnd : SkippingTest TestSessionEnd";
            actual = actual.Replace(" ", string.Empty).Replace("\r\n", string.Empty);
            expected = expected.Replace(" ", string.Empty).Replace("\r\n", string.Empty);
            Assert.AreEqual(expected, actual);
        }

        private string GetInProcDataCollectionRunsettingsFile()
        {
            var runSettings = Path.Combine(Path.GetDirectoryName(this.GetSampleTestAssembly()), "runsettingstest.runsettings");
            var testEnvironment = new IntegrationTestEnvironment();
            var inprocasm = testEnvironment.GetTestAsset("SimpleDataCollector.dll");
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
                                </RunSettings>";

            fileContents = string.Format(fileContents, "'" + inprocasm + "'");
            File.WriteAllText(runSettings, fileContents);

            return runSettings;
        }
    }
}
