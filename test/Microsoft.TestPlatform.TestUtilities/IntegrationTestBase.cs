// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for integration tests.
    /// </summary>
    public class IntegrationTestBase
    {
        public const string DesktopRunnerFramework = "net451";
        public const string CoreRunnerFramework = "netcoreapp2.0";
        private const string TestSummaryStatusMessageFormat = "Total tests: {0}. Passed: {1}. Failed: {2}. Skipped: {3}";
        private string standardTestOutput = string.Empty;
        private string standardTestError = string.Empty;

        private string arguments = string.Empty;

        protected readonly IntegrationTestEnvironment testEnvironment;

        private const string TestAdapterRelativePath = @"MSTest.TestAdapter\{0}\build\_common";
        private const string NUnitTestAdapterRelativePath = @"nunittestadapter\{0}\lib";
        private const string XUnitTestAdapterRelativePath = @"xunit.runner.visualstudio\{0}\build\_common";
        private const string ChutzpahTestAdapterRelativePath = @"chutzpah\{0}\tools";

        public enum UnitTestFramework
        {
            NUnit, XUnit, MSTest, CPP, Chutzpah
        }

        public IntegrationTestBase()
        {
            this.testEnvironment = new IntegrationTestEnvironment();
        }

        /// <summary>
        /// Prepare arguments for <c>vstest.console.exe</c>.
        /// </summary>
        /// <param name="testAssembly">Name of the test assembly.</param>
        /// <param name="testAdapterPath">Path to test adapter.</param>
        /// <param name="runSettings">Text of run settings.</param>
        /// <param name="framework"></param>
        /// <returns>Command line arguments string.</returns>
        public static string PrepareArguments(string testAssembly, string testAdapterPath, string runSettings, string framework = ".NETFramework,Version=v4.5.1", string inIsolation = "")
        {
            var arguments = testAssembly.AddDoubleQuote();

            if (!string.IsNullOrWhiteSpace(testAdapterPath))
            {
                // Append adapter path
                arguments = string.Concat(arguments, " /testadapterpath:", testAdapterPath.AddDoubleQuote());
            }

            if (!string.IsNullOrWhiteSpace(runSettings))
            {
                // Append run settings
                arguments = string.Concat(arguments, " /settings:", runSettings.AddDoubleQuote());
            }

            if (!string.IsNullOrWhiteSpace(framework))
            {
                // Append framework setting
                arguments = string.Concat(arguments, " /Framework:", framework.AddDoubleQuote());
            }

            arguments = string.Concat(arguments, " /logger:", "console;verbosity=normal".AddDoubleQuote());

            if (!string.IsNullOrWhiteSpace(inIsolation))
            {
                arguments = string.Concat(arguments, " ", inIsolation);
            }

            return arguments;
        }

        /// <summary>
        /// Invokes <c>vstest.console</c> with specified arguments.
        /// </summary>
        /// <param name="arguments">Arguments provided to <c>vstest.console</c>.exe</param>
        public void InvokeVsTest(string arguments)
        {
            this.Execute(arguments, out this.standardTestOutput, out this.standardTestError);
            this.FormatStandardOutCome();
        }

        /// <summary>
        /// Invokes <c>vstest.console</c> to execute tests in a test assembly.
        /// </summary>
        /// <param name="testAssembly">A test assembly.</param>
        /// <param name="testAdapterPath">Path to test adapters.</param>
        /// <param name="runSettings">Run settings for execution.</param>
        /// <param name="framework">Dotnet Framework of test assembly.</param>
        public void InvokeVsTestForExecution(
            string testAssembly,
            string testAdapterPath,
            string runSettings = "",
            string framework = "")
        {
            var arguments = PrepareArguments(testAssembly, testAdapterPath, runSettings, framework, this.testEnvironment.InIsolationValue);
            this.InvokeVsTest(arguments);
        }

        /// <summary>
        /// Invokes <c>vstest.console</c> to discover tests in a test assembly. "/listTests" is appended to the arguments.
        /// </summary>
        /// <param name="testAssembly">A test assembly.</param>
        /// <param name="testAdapterPath">Path to test adapters.</param>
        /// <param name="runSettings">Run settings for execution.</param>
        public void InvokeVsTestForDiscovery(string testAssembly, string testAdapterPath, string runSettings = "", string targetFramework = "")
        {
            var arguments = PrepareArguments(testAssembly, testAdapterPath, runSettings, targetFramework, this.testEnvironment.InIsolationValue);
            arguments = string.Concat(arguments, " /listtests");
            this.InvokeVsTest(arguments);
        }

        /// <summary>
        /// Validate if the overall test count and results are matching.
        /// </summary>
        /// <param name="passedTestsCount">Passed test count</param>
        /// <param name="failedTestsCount">Failed test count</param>
        /// <param name="skippedTestsCount">Skipped test count</param>
        public void ValidateSummaryStatus(int passedTestsCount, int failedTestsCount, int skippedTestsCount)
        {
            var totalTestCount = passedTestsCount + failedTestsCount + skippedTestsCount;
            if (totalTestCount == 0)
            {
                // No test should be found/run
                var summaryStatus = string.Format(
                    TestSummaryStatusMessageFormat,
                    @"\d+",
                    @"\d+",
                    @"\d+",
                    @"\d+");
                StringAssert.DoesNotMatch(
                    this.standardTestOutput,
                    new Regex(summaryStatus),
                    "Excepted: There should not be test summary{2}Actual: {0}{2}Standard Error: {1}{2}Arguments: {3}{2}",
                    this.standardTestOutput,
                    this.standardTestError,
                    Environment.NewLine,
                    this.arguments);
            }
            else
            {
                var summaryStatus = string.Format(
                    TestSummaryStatusMessageFormat,
                    totalTestCount,
                    passedTestsCount,
                    failedTestsCount,
                    skippedTestsCount);

                Assert.IsTrue(
                    this.standardTestOutput.Contains(summaryStatus),
                    "The Test summary does not match.{3}Expected summary: {1}{3}Test Output: {0}{3}Standard Error: {2}{3}Arguments: {4}{3}",
                    this.standardTestOutput,
                    summaryStatus,
                    this.standardTestError,
                    Environment.NewLine,
                    this.arguments);
            }
        }

        public void StdErrorContains(string substring)
        {
            Assert.IsTrue(this.standardTestError.Contains(substring), "StdErrorOutput - [{0}] did not contain expected string '{1}'", this.standardTestError, substring);
        }

        public void StdOutputContains(string substring)
        {
            Assert.IsTrue(this.standardTestOutput.Contains(substring), $"StdOutout:{Environment.NewLine} Expected substring: {substring}{Environment.NewLine}Acutal string: {this.standardTestOutput}");
        }

        public void StdOutputDoesNotContains(string substring)
        {
            Assert.IsFalse(this.standardTestOutput.Contains(substring), $"StdOutout:{Environment.NewLine} Not expected substring: {substring}{Environment.NewLine}Acutal string: {this.standardTestOutput}");
        }

        /// <summary>
        /// Validates if the test results have the specified set of passed tests.
        /// </summary>
        /// <param name="passedTests">Set of passed tests.</param>
        /// <remarks>Provide the full test name similar to this format SampleTest.TestCode.TestMethodPass.</remarks>
        public void ValidatePassedTests(params string[] passedTests)
        {
            foreach (var test in passedTests)
            {
                var flag = this.standardTestOutput.Contains("Passed " + test)
                           || this.standardTestOutput.Contains("Passed " + GetTestMethodName(test));
                Assert.IsTrue(flag, "Test {0} does not appear in passed tests list.", test);
            }
        }

        /// <summary>
        /// Validates if the test results have the specified set of failed tests.
        /// </summary>
        /// <param name="failedTests">Set of failed tests.</param>
        /// <remarks>
        /// Provide the full test name similar to this format SampleTest.TestCode.TestMethodFailed.
        /// Also validates whether these tests have stack trace info.
        /// </remarks>
        public void ValidateFailedTests(params string[] failedTests)
        {
            foreach (var test in failedTests)
            {
                var flag = this.standardTestOutput.Contains("Failed " + test)
                           || this.standardTestOutput.Contains("Failed " + GetTestMethodName(test));
                Assert.IsTrue(flag, "Test {0} does not appear in failed tests list.", test);

                // Verify stack information as well.
                Assert.IsTrue(this.standardTestOutput.Contains(GetTestMethodName(test)), "No stack trace for failed test: {0}", test);
            }
        }

        /// <summary>
        /// Validates if the test results have the specified set of skipped tests.
        /// </summary>
        /// <param name="skippedTests">The set of skipped tests.</param>
        /// <remarks>Provide the full test name similar to this format SampleTest.TestCode.TestMethodSkipped.</remarks>
        public void ValidateSkippedTests(params string[] skippedTests)
        {
            foreach (var test in skippedTests)
            {
                var flag = this.standardTestOutput.Contains("Skipped " + test)
                           || this.standardTestOutput.Contains("Skipped " + GetTestMethodName(test));
                Assert.IsTrue(flag, "Test {0} does not appear in skipped tests list.", test);
            }
        }

        /// <summary>
        /// Validate if the discovered tests list contains provided tests.
        /// </summary>
        /// <param name="discoveredTestsList">List of tests expected to be discovered.</param>
        public void ValidateDiscoveredTests(params string[] discoveredTestsList)
        {
            foreach (var test in discoveredTestsList)
            {
                var flag = this.standardTestOutput.Contains(test)
                           || this.standardTestOutput.Contains(GetTestMethodName(test));
                Assert.IsTrue(flag, $"Test {test} does not appear in discovered tests list." +
                                    $"{System.Environment.NewLine}Std Output: {this.standardTestOutput}" +
                                    $"{System.Environment.NewLine}Std Error: { this.standardTestError}");
            }
        }

        protected string GetSampleTestAssembly()
        {
            return this.GetAssetFullPath("SimpleTestProject.dll");
        }

        protected string GetAssetFullPath(string assetName)
        {
            return this.testEnvironment.GetTestAsset(assetName);
        }

        protected string GetTestAdapterPath(UnitTestFramework testFramework = UnitTestFramework.MSTest)
        {
            string adapterRelativePath = string.Empty;

            if (testFramework == UnitTestFramework.MSTest)
            {
                adapterRelativePath = string.Format(TestAdapterRelativePath, this.testEnvironment.DependencyVersions["MSTestAdapterVersion"]);
            }
            else if (testFramework == UnitTestFramework.NUnit)
            {
                adapterRelativePath = string.Format(NUnitTestAdapterRelativePath, this.testEnvironment.DependencyVersions["NUnitAdapterVersion"]);
            }
            else if (testFramework == UnitTestFramework.XUnit)
            {
                adapterRelativePath = string.Format(XUnitTestAdapterRelativePath, this.testEnvironment.DependencyVersions["XUnitAdapterVersion"]);
            }
            else if (testFramework == UnitTestFramework.Chutzpah)
            {
                adapterRelativePath = string.Format(ChutzpahTestAdapterRelativePath, this.testEnvironment.DependencyVersions["ChutzpahAdapterVersion"]);
            }

            return this.testEnvironment.GetNugetPackage(adapterRelativePath);
        }

        protected bool IsDesktopRunner()
        {
            return this.testEnvironment.RunnerFramework == IntegrationTestBase.DesktopRunnerFramework;
        }

        protected bool IsNetCoreRunner()
        {
            return this.testEnvironment.RunnerFramework == IntegrationTestBase.CoreRunnerFramework;
        }

        /// <summary>
        /// Gets the path to <c>vstest.console.exe</c>.
        /// </summary>
        /// <returns>
        /// Full path to test runner
        /// </returns>
        public string GetConsoleRunnerPath()
        {
            string consoleRunnerPath = string.Empty;

            if (this.IsDesktopRunner())
            {
                consoleRunnerPath = Path.Combine(this.testEnvironment.PublishDirectory, "vstest.console.exe");
            }
            else if (this.IsNetCoreRunner())
            {
                consoleRunnerPath = Path.Combine(this.testEnvironment.ToolsDirectory, @"dotnet\dotnet.exe");
            }
            else
            {
                Assert.Fail("Unknown Runner framework - [{0}]", this.testEnvironment.RunnerFramework);
            }

            Assert.IsTrue(File.Exists(consoleRunnerPath), "GetConsoleRunnerPath: Path not found: {0}", consoleRunnerPath);
            return consoleRunnerPath;
        }

        /// <summary>
        /// Gets the test method name from full name.
        /// </summary>
        /// <param name="testFullName">Fully qualified name of the test.</param>
        /// <returns>Simple name of the test.</returns>
        private static string GetTestMethodName(string testFullName)
        {
            string testMethodName = string.Empty;

            var splits = testFullName.Split('.');
            if (splits.Count() >= 3)
            {
                testMethodName = testFullName.Split('.')[2];
            }

            return testMethodName;
        }

        private void Execute(string args, out string stdOut, out string stdError)
        {
            if (this.IsNetCoreRunner())
            {
                var vstestConsoleDll = Path.Combine(this.testEnvironment.PublishDirectory, "vstest.console.dll");
                vstestConsoleDll = vstestConsoleDll.AddDoubleQuote();
                args = string.Concat(
                    vstestConsoleDll,
                    " ",
                    args);
            }

            this.arguments = args;

            using (Process vstestconsole = new Process())
            {
                Console.WriteLine("IntegrationTestBase.Execute: Starting vstest.console.exe");
                vstestconsole.StartInfo.FileName = this.GetConsoleRunnerPath();
                vstestconsole.StartInfo.Arguments = args;
                vstestconsole.StartInfo.UseShellExecute = false;
                //vstestconsole.StartInfo.WorkingDirectory = testEnvironment.PublishDirectory;
                vstestconsole.StartInfo.RedirectStandardError = true;
                vstestconsole.StartInfo.RedirectStandardOutput = true;
                vstestconsole.StartInfo.CreateNoWindow = true;

                var stdoutBuffer = new StringBuilder();
                var stderrBuffer = new StringBuilder();
                vstestconsole.OutputDataReceived += (sender, eventArgs) => stdoutBuffer.Append(eventArgs.Data);
                vstestconsole.ErrorDataReceived += (sender, eventArgs) => stderrBuffer.Append(eventArgs.Data);

                Console.WriteLine("IntegrationTestBase.Execute: Path = {0}", vstestconsole.StartInfo.FileName);
                Console.WriteLine("IntegrationTestBase.Execute: Arguments = {0}", vstestconsole.StartInfo.Arguments);

                vstestconsole.Start();
                vstestconsole.BeginOutputReadLine();
                vstestconsole.BeginErrorReadLine();
                if (!vstestconsole.WaitForExit(80 * 1000))
                {
                    Console.WriteLine("IntegrationTestBase.Execute: Timed out waiting for vstest.console.exe. Terminating the process.");
                    vstestconsole.Kill();
                }
                else
                {
                    // Ensure async buffers are flushed
                    vstestconsole.WaitForExit();
                }

                stdError = stderrBuffer.ToString();
                stdOut = stdoutBuffer.ToString();
                Console.WriteLine("IntegrationTestBase.Execute: Stopped vstest.console.exe. Exit code = {0}", vstestconsole.ExitCode);
            }
        }

        private void FormatStandardOutCome()
        {
            this.standardTestError = Regex.Replace(this.standardTestError, @"\s+", " ");
            this.standardTestOutput = Regex.Replace(this.standardTestOutput, @"\s+", " ");
        }

        /// <summary>
        /// Create runsettings file from runConfigurationDictionary at destinationRunsettingsPath
        /// </summary>
        /// <param name="destinationRunsettingsPath">
        /// Destination runsettings path where resulted file saves
        /// </param>
        /// <param name="runConfigurationDictionary">
        /// Contains run configuratin settings
        /// </param>
        public static void CreateRunSettingsFile(string destinationRunsettingsPath, IDictionary<string, string> runConfigurationDictionary)
        {
            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);

            doc.AppendChild(xmlDeclaration);
            var runSettingsNode = doc.CreateElement(Constants.RunSettingsName);
            doc.AppendChild(runSettingsNode);
            var runConfigNode = doc.CreateElement(Constants.RunConfigurationSettingsName);
            runSettingsNode.AppendChild(runConfigNode);

            foreach (var settingsEntry in runConfigurationDictionary)
            {
                var childNode = doc.CreateElement(settingsEntry.Key);
                childNode.InnerText = settingsEntry.Value;
                runConfigNode.AppendChild(childNode);
            }

            Stream stream = new FileHelper().GetStream(destinationRunsettingsPath, FileMode.Create);
            doc.Save(stream);
            stream.Dispose();
        }

        protected string BuildMultipleAssemblyPath(params string[] assetNames)
        {
            var assertFullPaths = new string[assetNames.Length];
            for (var i = 0; i < assetNames.Length; i++)
            {
                assertFullPaths[i] = this.GetAssetFullPath(assetNames[i]).AddDoubleQuote();
            }

            return string.Join(" ", assertFullPaths);
        }
    }
}
