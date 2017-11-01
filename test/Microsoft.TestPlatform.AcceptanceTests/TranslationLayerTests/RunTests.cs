// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Run Tests using VsTestConsoleWrapper API's
    /// </summary>
    [TestClass]
    public class RunTests : AcceptanceTestBase
    {
        private List<string> testAssemblies;
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;

        public RunTests()
        {
            this.testAssemblies = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };

            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
        }

        [TestMethod]
        public void RunAllTests()
        {
            this.vstestConsoleWrapper.RunTests(this.testAssemblies, this.GetDefaultRunSettings(), this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunTestsWithTelemetryOptedIn()
        {
            this.vstestConsoleWrapper.RunTests(
                this.testAssemblies,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = true },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetFramework));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TargetOS));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForRun));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringExecution));
            Assert.IsTrue(this.runEventHandler.Metrics.ContainsKey(TelemetryDataConstants.RunState));
        }

        [TestMethod]
        public void RunTestsWithTestCaseFilter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, this.runEventHandler.TestResults.FirstOrDefault().Outcome);
        }

        [TestMethod]
        public void RunTestsWithFastFilter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest | FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithSourceNavigation()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
                this.runEventHandler);

            // Assert
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(TestOutcome.Passed, this.runEventHandler.TestResults.FirstOrDefault().Outcome);

            // Release builds optimize code, hence line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(23, this.runEventHandler.TestResults.FirstOrDefault().TestCase.LineNumber);
            }
            else
            {
                Assert.AreEqual(22, this.runEventHandler.TestResults.FirstOrDefault().TestCase.LineNumber);
            }
        }

        [TestMethod]
        public void RunTestsWithTestAdapterPath()
        {
            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(), "*.TestAdapter.dll").ToList();
            this.vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

            this.vstestConsoleWrapper.RunTests(
                this.testAssemblies,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        }

        [TestMethod]
        public void RunTestsWithNunitAdapter()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("NUTestProject.dll")
                              };

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
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

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithChutzpahAdapter()
        {
            var sources = new List<string>
                              {
                                  Path.Combine(this.testEnvironment.TestAssetsPath, "test.js")
                              };

            var testAdapterPath = Directory.EnumerateFiles(this.GetTestAdapterPath(UnitTestFramework.Chutzpah), "*.TestAdapter.dll").ToList();
            this.vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

            this.vstestConsoleWrapper.RunTests(
                sources,
                this.GetDefaultRunSettings(),
                this.runEventHandler);

            // Assert
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        }

        [TestMethod]
        public void RunTestsWithRunSettingsWithParallel()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
                                    <RunSettings>     
                                        <RunConfiguration>
                                        <MaxCpuCount>2</MaxCpuCount>
                                        </RunConfiguration>
                                    </RunSettings>";

            this.vstestConsoleWrapper.RunTests(
                this.testAssemblies,
                runSettingsXml,
                this.runEventHandler);

            // Assert
            Assert.AreEqual(6, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
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

            this.vstestConsoleWrapper.RunTests(
                sources,
                runSettings,
                this.runEventHandler);

            // Assert
            Assert.AreEqual(5, this.runEventHandler.TestResults.Count);
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
            Assert.AreEqual(2, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
            Assert.AreEqual(1, this.runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));

            File.Delete(testsettingsFile);
        }
    }
}