// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    [TestClass]
    public class DiscoveryTests : AcceptanceTestBase
    {
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
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void DiscoverFullyQualifiedTests(RunnerInfo runnerInfo)
        {
            using var tempDir = new TempDirectory();
            var dummyFilePath = Path.Combine(tempDir.Path, $"{Guid.NewGuid()}.txt");

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var listOfTests = new[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };

            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, this.testEnvironment.InIsolationValue, resultsDirectory: tempDir.Path);
            arguments = string.Concat(arguments, " /ListFullyQualifiedTests", " /ListTestsTargetPath:\"" + dummyFilePath + "\"");
            this.InvokeVsTest(arguments);

            this.ValidateFullyQualifiedDiscoveredTests(dummyFilePath, listOfTests);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverTestsShouldShowProperWarningIfNoTestsOnTestCaseFilter(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            using var tempDir = new TempDirectory();

            var assetFullPath = this.GetAssetFullPath("SimpleTestProject2.dll");
            var arguments = PrepareArguments(assetFullPath, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, this.testEnvironment.InIsolationValue, resultsDirectory: tempDir.Path);
            arguments = string.Concat(arguments, " /listtests");
            arguments = string.Concat(arguments, " /testcasefilter:NonExistTestCaseName");
            arguments = string.Concat(arguments, " /logger:\"console;prefix=true\"");
            this.InvokeVsTest(arguments);

            StringAssert.Contains(this.StdOut, "Warning: No test matches the given testcase filter `NonExistTestCaseName` in");
            StringAssert.Contains(this.StdOut, "SimpleTestProject2.dll");
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        public void TypesToLoadAttributeTests()
        {
            var environment = new IntegrationTestEnvironment();
            var extensionsDirectory = environment.ExtensionsDirectory;
            var extensionsToVerify = new Dictionary<string, string[]>
            {
                {"Microsoft.TestPlatform.Extensions.EventLogCollector.dll", new[] { "Microsoft.TestPlatform.Extensions.EventLogCollector.EventLogDataCollector"} },
                {"Microsoft.TestPlatform.Extensions.BlameDataCollector.dll", new[] { "Microsoft.TestPlatform.Extensions.BlameDataCollector.BlameLogger", "Microsoft.TestPlatform.Extensions.BlameDataCollector.BlameCollector" } },
                {"Microsoft.VisualStudio.TestPlatform.Extensions.Html.TestLogger.dll", new[] { "Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.HtmlLogger" } },
                {"Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll", new[] { "Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger" } },
                {"Microsoft.TestPlatform.TestHostRuntimeProvider.dll", new[] { "Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting.DefaultTestHostManager", "Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting.DotnetTestHostManager" } }
            };

            foreach (var extension in extensionsToVerify.Keys)
            {
                var assemblyFile = Path.Combine(extensionsDirectory, extension);
                var assembly = Assembly.LoadFrom(assemblyFile);

                var expected = extensionsToVerify[extension];
                var actual = TypesToLoadUtilities.GetTypesToLoad(assembly).Select(i => i.FullName).ToArray();

                CollectionAssert.AreEquivalent(expected, actual, $"Specified types using TypesToLoadAttribute in \"{extension}\" assembly doesn't match the expected.");
            }
        }
    }
}
