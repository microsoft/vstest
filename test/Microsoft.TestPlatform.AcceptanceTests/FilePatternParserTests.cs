// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class FilePatternParserTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void WildCardPatternShouldCorrectlyWorkOnFiles(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssembly = this.GetSampleTestAssembly();
            testAssembly = testAssembly.Replace("SimpleTestProject.dll", "*TestProj*.dll");

            var arguments = PrepareArguments(
               testAssembly,
               this.GetTestAdapterPath(),
               string.Empty, this.FrameworkArgValue,
               runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void WildCardPatternShouldCorrectlyWorkOnArbitraryDepthDirectories(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssembly = this.GetSampleTestAssembly();
            var oldAssemblyPath = Path.Combine("Debug", this.testEnvironment.TargetFramework, "SimpleTestProject.dll");
            var newAssemblyPath = Path.Combine("**", this.testEnvironment.TargetFramework, "*TestProj*.dll");
            testAssembly = testAssembly.Replace(oldAssemblyPath, newAssemblyPath);

            var arguments = PrepareArguments(
               testAssembly,
               this.GetTestAdapterPath(),
               string.Empty, string.Empty,
               runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void WildCardPatternShouldCorrectlyWorkForRelativeAssemblyPath(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssembly = this.GetSampleTestAssembly();
            testAssembly = testAssembly.Replace("SimpleTestProject.dll", "*TestProj*.dll");

            var wildCardIndex = testAssembly.IndexOfAny(new char[] { '*' });
            var testAssemblyDirectory = testAssembly.Substring(0, wildCardIndex);
            testAssembly = testAssembly.Substring(wildCardIndex);

            Directory.SetCurrentDirectory(testAssemblyDirectory);

            var arguments = PrepareArguments(
               testAssembly,
               this.GetTestAdapterPath(),
               string.Empty, string.Empty,
               runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void WildCardPatternShouldCorrectlyWorkOnMultipleFiles(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var testAssembly = this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"'); ;
            testAssembly = testAssembly.Replace("SimpleTestProject.dll", "*TestProj*.dll");
            testAssembly = testAssembly.Replace("SimpleTestProject2.dll", "*TestProj*.dll");

            var arguments = PrepareArguments(
               testAssembly,
               this.GetTestAdapterPath(),
               string.Empty, this.FrameworkArgValue,
               runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(2, 2, 2);
        }
    }
}
