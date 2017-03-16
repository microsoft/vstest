// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NET46TargetFramework]
        public void ChutzpahRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var testJSFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "test.js");
            var arguments = PrepareArguments(
                testJSFileAbsolutePath,
                this.GetTestAdapterPath(UnitTestFramework.Chutzpah),
                string.Empty,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [Ignore]
        [CustomDataTestMethod]
        [NET46TargetFramework]
        public void CPPRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyRelativePath =
                @"microsoft.testplatform.testasset.nativecpp\1.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NET46TargetFramework]
        public void NUnitRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetAssetFullPath("NUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.NUnit),
                string.Empty,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NET46TargetFramework]
        [NETCORETargetFramework]
        public void XUnitRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);
            var arguments = PrepareArguments(
                this.GetAssetFullPath("XUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.XUnit),
                string.Empty,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }
    }
}
