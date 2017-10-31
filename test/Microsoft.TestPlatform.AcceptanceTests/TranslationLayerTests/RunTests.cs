// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunTests : AcceptanceTestBase
    {
        [TestMethod]
        public void RunAllTests()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(sources, this.GetDefaultRunSettings(), runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunTestsWithTelemetryOptedIn()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = true },
                runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
            Assert.IsTrue(runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
        }

        [TestMethod]
        public void RunTestsWithTestCaseFilter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                runEventHandler);

            // Assert
            Assert.AreEqual(1, runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, runEventHandler.TestResults.FirstOrDefault().Outcome);
        }

        [TestMethod]
        public void RunTestsWithFastFilter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest | FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest" },
                runEventHandler);

            // Assert
            Assert.AreEqual(2, runEventHandler.TestResults.Count);
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithSourceNavigation()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                runEventHandler);

            // Assert
            Assert.AreEqual(1, runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, runEventHandler.TestResults.FirstOrDefault().Outcome);
            Assert.AreEqual(22, runEventHandler.TestResults.FirstOrDefault().TestCase.LineNumber);
        }

        [TestMethod]
        public void RunTestsWithTestAdapterPath()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(UnitTestFramework.MSTest), "*.TestAdapter.dll").ToList();

            vsConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunTestsWithNunitAdapter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("NUTestProject.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                runEventHandler);

            // Assert
            Assert.AreEqual(2, runEventHandler.TestResults.Count);
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithXunitAdapter()
        {
            // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
            string testAssemblyPath = null;
            if (this.testEnvironment.TargetFramework.Equals("net451"))
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll", "net46");
            }
            else
            {
                testAssemblyPath = testEnvironment.GetTestAsset("XUTestProject.dll");
            }

            var sources = new List<string> { testAssemblyPath };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            vsConsoleWrapper.InitializeExtensions(
                new List<string>() { this.GetTestAdapterPath(UnitTestFramework.XUnit) });
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                runEventHandler);

            // Assert
            Assert.AreEqual(2, runEventHandler.TestResults.Count);
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithChutzpahAdapter()
        {
            var sources = new List<string>
                              {
                                  Path.Combine(this.testEnvironment.TestAssetsPath, "test.js")
                              };

            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(UnitTestFramework.Chutzpah), "*.TestAdapter.dll").ToList();

            vsConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                runEventHandler);

            // Assert
            Assert.AreEqual(2, runEventHandler.TestResults.Count);
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithRunSettingsWithParallel()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                this.GetRunSettingsWithParallel(),
                runEventHandler);

            // Assert
            Assert.AreEqual(6, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunTestsWithTestSettings()
        {
            var testsettingsFile = Path.Combine(Path.GetTempPath(), "tempsettings.testsettings");
            string testSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?><TestSettings></TestSettings>";

            File.WriteAllText(testsettingsFile, testSettingsXml, Encoding.UTF8);
            var runSettings = "<RunSettings><MSTest><SettingsFile>" + testsettingsFile + "</SettingsFile></MSTest></RunSettings>";
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("MstestV1UnitTestProject.dll")
                              };

            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var runEventHandler = new RunEventHandler();

            vsConsoleWrapper.RunTests(
                sources,
                runSettings,
                runEventHandler);

            // Assert
            Assert.AreEqual(5, runEventHandler.TestResults.Count);
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(1, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));

            File.Delete(testsettingsFile);
        }
    }
}