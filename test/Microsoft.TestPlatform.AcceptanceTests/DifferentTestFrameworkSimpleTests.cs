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
        [NETFullTargetFramework]
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

        [Ignore] // https://github.com/Microsoft/vstest/issues/689
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void CPPRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("CPP adapter not available for netcore runner in Extensions folder.");
                return;
            }

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
        [NETFullTargetFramework]
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
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void XUnitRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);
            string testAssemblyPath = null;

            // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
            if (this.testEnvironment.TargetFramework.Equals("net451"))
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll", "net46");
            }
            else
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll");
            }

            var arguments = PrepareArguments(
                testAssemblyPath,
                this.GetTestAdapterPath(UnitTestFramework.XUnit),
                string.Empty,
                this.FrameworkArgValue);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }
    }
}
