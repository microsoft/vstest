// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoveryTests : AcceptanceTestBase
    {
        private readonly string dummyFilePath = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid()}.txt");

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTestForDiscovery(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);

            var listOfTests = new[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };
            this.ValidateDiscoveredTests(listOfTests);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void MultipleSourcesDiscoverAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var listOfTests = new[] {
                "SampleUnitTestProject.UnitTest1.PassingTest",
                "SampleUnitTestProject.UnitTest1.FailingTest",
                "SampleUnitTestProject.UnitTest1.SkippingTest",
                "SampleUnitTestProject.UnitTest1.PassingTest2",
                "SampleUnitTestProject.UnitTest1.FailingTest2",
                "SampleUnitTestProject.UnitTest1.SkippingTest2"
            };

            this.InvokeVsTestForDiscovery(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);

            this.ValidateDiscoveredTests(listOfTests);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void DiscoverFullyQualifiedTests(RunnerInfo runnerInfo)
        {
            try
            {
                AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
                var listOfTests = new[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };

                this.InvokeVsTestForFullyQualifiedDiscovery(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), this.dummyFilePath, string.Empty);

                this.ValidateFullyQualifiedDiscoveredTests(this.dummyFilePath, listOfTests);
                this.ExitCodeEquals(0);
            }
            finally
            {
                File.Delete(this.dummyFilePath);
            }
        }
    }
}
