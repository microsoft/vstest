// Copyright (c) Microsoft. All rights reserved.

namespace TestingMSTest
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestPlatform.TestUtilities;

    [TestClass]
    public class End2EndTests:VsTestConsoleTestBase
    {
#if DEBUG
        private const string TestAssemblyRelativePath = @"Samples\SampleUnitTestProject\bin\Debug\SampleUnitTestProject.dll";
#else
        private const string TestAssemblyRelativePath = @"Samples\SampleUnitTestProject\bin\Release\SampleUnitTestProject.dll";
#endif
        private const string TestAdapterRelativePath = @"Samples\packages\MSTest.TestAdapter.1.0.0-preview\build";

        private string testAssembly;

        private string testAdapter;

        [TestInitialize]
        public void InitializeTests()
        {
            this.testAssembly = GetSampleTestAssembly();
            this.testAdapter = GetTestAdapterPath();
        }



        [TestMethod]
        [TestCategory("EndToEnd")]
        public void RunAllTestExecution()
        {            
            this.InvokeVsTestForExecution(this.testAssembly, this.testAdapter);
            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");
        }

        [TestMethod]
        [TestCategory("EndToEnd")]
        public void DiscoverAllTests()
        {
            this.InvokeVsTestForDiscovery(this.testAssembly, this.testAdapter);
            var listOfTests = new string[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };
            this.ValidateDiscoveredTests(listOfTests);
        }

        [TestMethod]
        [TestCategory("EndToEnd")]
        public void RunSelectedTests()
        {
            var arguments = PrepareArguments(this.testAssembly, this.testAdapter, string.Empty);
            arguments = string.Concat(arguments, " /Tests:PassingTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");            
        }

        [TestMethod]
        [TestCategory("EndToEnd")]
        public void RunAllWithTestImpactSettings()
        {
            var runSettings = GetInProcDataCollectionRunsettignsFile();

            this.InvokeVsTestForExecution(this.testAssembly, this.testAdapter, runSettings);
            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            ValidateInProcDataCollectionOutput();
        }

#region PrivateMethods
        private static string GetSampleTestAssembly()
        {
            var currentDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

            var testAssembly = Path.Combine(currentDirectoryInfo.Parent?.FullName, TestAssemblyRelativePath);

            return testAssembly;
        }
        private static string GetTestAdapterPath()
        {
            var currentDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

            var testAdapterPath = Path.Combine(currentDirectoryInfo.Parent?.FullName, TestAdapterRelativePath);

            return testAdapterPath;
        }

        private static string GetInProcDataCollectionRunsettignsFile()
        {
            var runSettings = Path.Combine(Path.GetDirectoryName(GetSampleTestAssembly()), "runsettingstest.runsettings");
#if DEBUG
            var realtiveInProcPath = @"Samples\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll";
#else
            var realtiveInProcPath = @"Samples\TestImpactListener.Tests\bin\Release\TestImpactListener.Tests.dll";
#endif
            var currentDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var inprocasm = Path.Combine(currentDirectoryInfo.Parent?.FullName, realtiveInProcPath);
            var fileContents = @"<RunSettings>
	                                <InProcDataCollectionRunSettings>
		                                <InProcDataCollectors>
			                                <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests.TIListenerTests, TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase={0}>
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

        private static void ValidateInProcDataCollectionOutput()
        {
            var fileName = Path.Combine(Path.GetTempPath(), "inproctest.txt");
            Assert.IsTrue(File.Exists(fileName));
            var actual = File.ReadAllText(fileName);
            var expected = @"TestSessionStart : <Configuration><Port>4312</Port></Configuration> TestCaseStart : PassingTest TestCaseEnd : PassingTest TestCaseStart : FailingTest TestCaseEnd : FailingTest TestCaseStart : SkippingTest TestCaseEnd : SkippingTest TestSessionEnd";
            actual = actual.Replace(" ", "").Replace("\r\n", "");
            expected = expected.Replace(" ", "").Replace("\r\n", "");
            Assert.AreEqual(expected, actual);
        }
#endregion
    }
}
