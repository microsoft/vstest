// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETFullTargetFrameworkInProcess]
        [NETFullTargetFrameworkInIsolation]
        public void ChutzpahRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);

            var testJSFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "test.js");
            var arguments = PrepareArguments(
                testJSFileAbsolutePath,
                this.GetTestAdapterPath(UnitTestFramework.Chutzpah),
                string.Empty,
                this.FrameworkArgValue,
                inIsolation);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFrameworkInProcess]
        [NETFullTargetFrameworkInIsolation]
        public void CPPRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);
            CppRunAllTests(runnerFramework, "x86");
        }

        [CustomDataTestMethod]
        [NETFullTargetFrameworkInProcess]
        public void CPPRunAllTestExecutionPlatformx64(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);
            CppRunAllTests(runnerFramework, "x64");
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETFullTargetFrameworkInProcess]
        [NETFullTargetFrameworkInIsolation]
        public void NUnitRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);

            var arguments = PrepareArguments(
                this.GetAssetFullPath("NUTestProject.dll"),
                this.GetTestAdapterPath(UnitTestFramework.NUnit),
                string.Empty,
                this.FrameworkArgValue,
                inIsolation);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETFullTargetFrameworkInIsolation]
        [NETFullTargetFrameworkInProcess]
        [NETCORETargetFramework]
        public void XUnitRunAllTestExecution(string runnerFramework, string targetFramework, string targetRuntime, string inIsolation)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime, inIsolation);
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
                this.FrameworkArgValue,
                inIsolation);
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        private void CppRunAllTests(string runnerFramework, string platform)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("CPP tests not supported with .Netcore runner.");
                return;
            }

            string assemblyRelativePathFormat =
                @"microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\{0}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
                ? string.Format(assemblyRelativePathFormat, platform)
                : string.Format(assemblyRelativePathFormat, "");
            var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty,
                this.FrameworkArgValue,
                this.testEnvironment.InIsolationValue);

            arguments = string.Concat(arguments, $" /platform:{platform}");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }
    }
}
