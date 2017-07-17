// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoveryTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void DiscoverAllTests(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            this.InvokeVsTestForDiscovery(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            var listOfTests = new string[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };
            this.ValidateDiscoveredTests(listOfTests);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void MultipleSourcesDiscoverAllTests(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            this.InvokeVsTestForDiscovery(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            var listOfTests = new string[] {
                "SampleUnitTestProject.UnitTest1.PassingTest",
                "SampleUnitTestProject.UnitTest1.FailingTest",
                "SampleUnitTestProject.UnitTest1.SkippingTest",
                "SampleUnitTestProject.UnitTest1.PassingTest2",
                "SampleUnitTestProject.UnitTest1.FailingTest2",
                "SampleUnitTestProject.UnitTest1.SkippingTest2"
            };
            this.ValidateDiscoveredTests(listOfTests);
        }
    }
}
