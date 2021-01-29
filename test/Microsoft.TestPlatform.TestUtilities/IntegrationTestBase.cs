// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Base class for integration tests.
    /// </summary>
    public class IntegrationTestBase
    {
        public const string DesktopRunnerFramework = "net451";
        public const string CoreRunnerFramework = "netcoreapp2.1";

        private const string TotalTestsMessage = "Total tests: {0}";
        private const string PassedTestsMessage = " Passed: {0}";
        private const string FailedTestsMessage = " Failed: {0}";
        private const string SkippedTestsMessage = " Skipped: {0}";
        private const string TestSummaryStatusMessageFormat = "Total tests: {0} Passed: {1} Failed: {2} Skipped: {3}";
        private string standardTestOutput = string.Empty;
        private string standardTestError = string.Empty;
        private string standardTestOutputWithWhiteSpace = string.Empty;
        private string standardTestErrorWithWhiteSpace = string.Empty;
        private int runnerExitCode = -1;

        private string arguments = string.Empty;

        protected readonly IntegrationTestEnvironment testEnvironment;

        private readonly string TestAdapterRelativePath = @"mstest.testadapter\{0}\build\_common".Replace('\\', Path.DirectorySeparatorChar);
        private readonly string NUnitTestAdapterRelativePath = @"nunit3testadapter\{0}\build".Replace('\\', Path.DirectorySeparatorChar);
        private readonly string XUnitTestAdapterRelativePath = @"xunit.runner.visualstudio\{0}\build\_common".Replace('\\', Path.DirectorySeparatorChar);
        private readonly string ChutzpahTestAdapterRelativePath = @"chutzpah\{0}\tools".Replace('\\', Path.DirectorySeparatorChar);

        protected readonly bool IsWindows = System.Environment.OSVersion.Platform.ToString().StartsWith("Win");

        public enum UnitTestFramework
        {
            NUnit, XUnit, MSTest, CPP, Chutzpah
        }

        public IntegrationTestBase()
        {
            this.testEnvironment = new IntegrationTestEnvironment();
        }

        public string StdOut => this.standardTestOutput;
        public string StdOutWithWhiteSpace => this.standardTestOutputWithWhiteSpace;

        public string StdErr => this.standardTestError;
        public string StdErrWithWhiteSpace => this.standardTestErrorWithWhiteSpace;

        /// <summary>
        /// Prepare arguments for <c>vstest.console.exe</c>.
        /// </summary>
        /// <param name="testAssembly">Name of the test assembly.</param>
        /// <param name="testAdapterPath">Path to test adapter.</param>
        /// <param name="runSettings">Text of run settings.</param>
        /// <param name="framework"></param>
        /// <param name="inIsolation"></param>
        /// <returns>Command line arguments string.</returns>
        public static string PrepareArguments(string testAssembly, string testAdapterPath, string runSettings,
            string framework, string inIsolation = "", string resultsDirectory = null)
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
                // Append run settings
                arguments = string.Concat(arguments, " /framework:", framework.AddDoubleQuote());
            }

            arguments = string.Concat(arguments, " /logger:", "console;verbosity=normal".AddDoubleQuote());

            if (!string.IsNullOrWhiteSpace(inIsolation))
            {
                arguments = string.Concat(arguments, " ", inIsolation);
            }

            if (!string.IsNullOrWhiteSpace(resultsDirectory))
            {
                // Append results directory
                arguments = string.Concat(arguments, " /ResultsDirectory:", resultsDirectory.AddDoubleQuote());
            }

            return arguments;
        }

        /// <summary>
        /// Invokes <c>vstest.console</c> with specified arguments.
        /// </summary>
        /// <param name="arguments">Arguments provided to <c>vstest.console</c>.exe</param>
        public void InvokeVsTest(string arguments)
        {
            this.ExecuteVsTestConsole(arguments, out this.standardTestOutput, out this.standardTestError, out this.runnerExitCode);
            this.FormatStandardOutCome();
        }


        /// <summary>
        /// Invokes our local copy of dotnet that is patched with artifacts from the build with specified arguments.
        /// </summary>
        /// <param name="arguments">Arguments provided to <c>vstest.console</c>.exe</param>
        public void InvokeDotnetTest(string arguments)
        {
            var vstestConsolePath = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "netcoreapp2.1", "vstest.console.dll");
            var env = "VSTEST_CONSOLE_PATH";
            var originalVstestConsolePath = Environment.GetEnvironmentVariable(env);

            try
            {
                Environment.SetEnvironmentVariable(env, vstestConsolePath);
                if (arguments.Contains(".csproj"))
                {                   
                    arguments = $@"-p:VsTestConsolePath=""{vstestConsolePath}"" " + arguments;
                }

                this.ExecutePatchedDotnet("test", arguments, out this.standardTestOutput, out this.standardTestError, out this.runnerExitCode);
                this.FormatStandardOutCome();
            }
            finally
            {
                Environment.SetEnvironmentVariable(env, originalVstestConsolePath);
            }
        }

        /// <summary>
        /// Invokes <c>vstest.console</c> to execute tests in a test assembly.
        /// </summary>
        /// <param name="testAssembly">A test assembly.</param>
        /// <param name="testAdapterPath">Path to test adapters.</param>
        /// <param name="framework">Dotnet Framework of test assembly.</param>
        /// <param name="runSettings">Run settings for execution.</param>
        public void InvokeVsTestForExecution(string testAssembly,
            string testAdapterPath,
            string framework,
            string runSettings = "")
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
        /// Execute Tests that are not supported with given Runner framework.
        /// </summary>
        /// <param name="runnerFramework">Runner Framework</param>
        /// <param name="framework">Framework for which Tests are not supported</param>
        /// <param name="message">Message to be shown</param>
        public void ExecuteNotSupportedRunnerFrameworkTests(string runnerFramework, string framework, string message)
        {
            if (runnerFramework.StartsWith(framework))
            {
                Assert.Inconclusive(message);
            }
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
                var summaryStatus = string.Format(TotalTestsMessage, totalTestCount);
                if (passedTestsCount != 0)
                {
                    summaryStatus += string.Format(PassedTestsMessage, passedTestsCount);
                }

                if (failedTestsCount != 0)
                {
                    summaryStatus += string.Format(FailedTestsMessage, failedTestsCount);
                }

                if (skippedTestsCount != 0)
                {
                    summaryStatus += string.Format(SkippedTestsMessage, skippedTestsCount);
                }

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

        public void StdErrorDoesNotContains(string substring)
        {
            Assert.IsFalse(this.standardTestError.Contains(substring), "StdErrorOutput - [{0}] did not contain expected string '{1}'", this.standardTestError, substring);
        }

        public void StdOutputContains(string substring)
        {
            Assert.IsTrue(this.standardTestOutput.Contains(substring), $"StdOutout:{Environment.NewLine} Expected substring: {substring}{Environment.NewLine}Actual string: {this.standardTestOutput}");
        }

        public void StdOutputDoesNotContains(string substring)
        {
            Assert.IsFalse(this.standardTestOutput.Contains(substring), $"StdOutout:{Environment.NewLine} Not expected substring: {substring}{Environment.NewLine}Actual string: {this.standardTestOutput}");
        }

        public void ExitCodeEquals(int exitCode)
        {
            Assert.AreEqual(exitCode, this.runnerExitCode, $"ExitCode - [{this.runnerExitCode}] doesn't match expected '{exitCode}'.");
        }

        /// <summary>
        /// Validates if the test results have the specified set of passed tests.
        /// </summary>
        /// <param name="passedTests">Set of passed tests.</param>
        /// <remarks>Provide the full test name similar to this format SampleTest.TestCode.TestMethodPass.</remarks>
        public void ValidatePassedTests(params string[] passedTests)
        {
            // Convert the unicode character to its unicode value for assertion
            this.standardTestOutput = Regex.Replace(this.standardTestOutput, @"[^\x00-\x7F]", c => string.Format(@"\u{0:x4}", (int)c.Value[0]));
            foreach (var test in passedTests)
            {
                // Check for tick or ? both, in some cases as unicode character for tick is not available
                // in std out and gets replaced by ?
                var flag = this.standardTestOutput.Contains("Passed " + test)
                           || this.standardTestOutput.Contains("Passed " + GetTestMethodName(test))
                           || this.standardTestOutput.Contains("\\ufffd " + test)
                           || this.standardTestOutput.Contains("\\ufffd " + GetTestMethodName(test));
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

        /// <summary>
        /// Validate that the discovered tests list doesn't contain specified tests.
        /// </summary>
        /// <param name="testsList">List of tests expected not to be discovered.</param>
        public void ValidateTestsNotDiscovered(params string[] testsList)
        {
            foreach (var test in testsList)
            {
                var flag = this.standardTestOutput.Contains(test)
                           || this.standardTestOutput.Contains(GetTestMethodName(test));
                Assert.IsFalse(flag, $"Test {test} should not appear in discovered tests list." +
                                    $"{System.Environment.NewLine}Std Output: {this.standardTestOutput}" +
                                    $"{System.Environment.NewLine}Std Error: { this.standardTestError}");
            }
        }

        public void ValidateFullyQualifiedDiscoveredTests(string filePath, params string[] discoveredTestsList)
        {
            var fileOutput = File.ReadAllLines(filePath);
            Assert.IsTrue(fileOutput.Length == 3);

            foreach (var test in discoveredTestsList)
            {
                var flag = fileOutput.Contains(test)
                           || fileOutput.Contains(GetTestMethodName(test));
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

        protected string GetAssetFullPath(string assetName, string targetFramework)
        {
            return this.testEnvironment.GetTestAsset(assetName, targetFramework);
        }

        protected string GetProjectFullPath(string projectName)
        {
            return this.testEnvironment.GetTestProject(projectName);
        }

        protected string GetProjectAssetFullPath(string projectName, string assetName)
        {
            var projectPath = this.testEnvironment.GetTestProject(projectName);
            return Path.Combine(Path.GetDirectoryName(projectPath), assetName);
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
                adapterRelativePath = string.Format(NUnitTestAdapterRelativePath, this.testEnvironment.DependencyVersions["NUnit3AdapterVersion"]);
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
        public virtual string GetConsoleRunnerPath()
        {
            string consoleRunnerPath = string.Empty;

            if (this.IsDesktopRunner())
            {
                consoleRunnerPath = Path.Combine(this.testEnvironment.PublishDirectory, "vstest.console.exe");
            }
            else if (this.IsNetCoreRunner())
            {
                var executablePath = IsWindows ? @"dotnet\dotnet.exe" : @"dotnet-linux/dotnet";
                consoleRunnerPath = Path.Combine(this.testEnvironment.ToolsDirectory, executablePath);
            }
            else
            {
                Assert.Fail("Unknown Runner framework - [{0}]", this.testEnvironment.RunnerFramework);
            }

            Assert.IsTrue(File.Exists(consoleRunnerPath), "GetConsoleRunnerPath: Path not found: {0}", consoleRunnerPath);
            return consoleRunnerPath;
        }

        protected virtual string SetVSTestConsoleDLLPathInArgs(string args)
        {
            var vstestConsoleDll = Path.Combine(this.testEnvironment.PublishDirectory, "vstest.console.dll");
            vstestConsoleDll = vstestConsoleDll.AddDoubleQuote();
            args = string.Concat(
                vstestConsoleDll,
                " ",
                args);
            return args;
        }

        /// <summary>
        /// Returns the VsTestConsole Wrapper.
        /// </summary>
        /// <returns></returns>
        public IVsTestConsoleWrapper GetVsTestConsoleWrapper()
        {
            var logFileName = Path.GetFileName(Path.GetTempFileName());
            var logFileDir = Path.Combine(Path.GetTempPath(), "VSTestConsoleWrapperLogs");

            if (Directory.Exists(logFileDir) == false)
            {
                Directory.CreateDirectory(logFileDir);
            }

            var logFilePath = Path.Combine(logFileDir, logFileName);

            Console.WriteLine($"Logging diagnostics in {logFilePath}");

            string consoleRunnerPath;

            if (this.IsNetCoreRunner())
            {
                consoleRunnerPath = Path.Combine(this.testEnvironment.PublishDirectory, "vstest.console.dll");
            }
            else
            {
                consoleRunnerPath = this.GetConsoleRunnerPath();
            }
            var executablePath = IsWindows ? @"dotnet\dotnet.exe" : @"dotnet-linux/dotnet";
            var dotnetPath = Path.Combine(this.testEnvironment.ToolsDirectory, executablePath);

            if (!File.Exists(dotnetPath))
            {
                throw new FileNotFoundException($"File '{dotnetPath}' was not found.");
            }

            var vstestConsoleWrapper = new VsTestConsoleWrapper(consoleRunnerPath, dotnetPath, new ConsoleParameters() { LogFilePath = logFilePath });
            vstestConsoleWrapper.StartSession();

            return vstestConsoleWrapper;
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

        private void ExecuteVsTestConsole(string args, out string stdOut, out string stdError, out int exitCode)
        {
            if (this.IsNetCoreRunner())
            {
                args = this.SetVSTestConsoleDLLPathInArgs(args);
            }

            this.arguments = args;

            this.ExecuteApplication(this.GetConsoleRunnerPath(), args, out stdOut, out stdError, out exitCode);
        }

        /// <summary>
        /// Executes a local copy of dotnet that has VSTest task installed and possibly other modifications. Do not use this to
        /// do your builds or to run general tests, unless you want your changes to be reflected.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <param name="stdOut"></param>
        /// <param name="stdError"></param>
        /// <param name="exitCode"></param>
        private void ExecutePatchedDotnet(string command, string args, out string stdOut, out string stdError, out int exitCode)
        {
            var environmentVariables = new Dictionary<string, string>
            {
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            };

            var executablePath = IsWindows ? @"dotnet\dotnet.exe" : @"dotnet-linux/dotnet";
            var patchedDotnetPath = Path.Combine(this.testEnvironment.TestArtifactsDirectory, executablePath);
            this.ExecuteApplication(patchedDotnetPath, string.Join(" ", command, args), out stdOut, out stdError, out exitCode, environmentVariables);
        }

        private void ExecuteApplication(string path, string args, out string stdOut, out string stdError, out int exitCode, Dictionary<string, string> environmentVariables = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Executable path must not be null or whitespace.", nameof(path));
            }

            var executableName = Path.GetFileName(path);

            using (Process process = new Process())
            {
                Console.WriteLine($"IntegrationTestBase.Execute: Starting {executableName}");
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                //vstestconsole.StartInfo.WorkingDirectory = testEnvironment.PublishDirectory;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                if (environmentVariables != null)
                {
                    foreach (var variable in environmentVariables)
                    {
                        if (process.StartInfo.EnvironmentVariables.ContainsKey(variable.Key))
                        {
                            process.StartInfo.EnvironmentVariables[variable.Key] = variable.Value;
                        }
                        else
                        {
                            process.StartInfo.EnvironmentVariables.Add(variable.Key, variable.Value);
                        }
                    }
                }

                var stdoutBuffer = new StringBuilder();
                var stderrBuffer = new StringBuilder();
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    stdoutBuffer.AppendLine(eventArgs.Data);
                };

                process.ErrorDataReceived += (sender, eventArgs) => stderrBuffer.AppendLine(eventArgs.Data);

                Console.WriteLine("IntegrationTestBase.Execute: Path = {0}", process.StartInfo.FileName);
                Console.WriteLine("IntegrationTestBase.Execute: Arguments = {0}", process.StartInfo.Arguments);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine(); 
                if (!process.WaitForExit(5 * 60 * 1000)) // 5 minutes
                {
                    Console.WriteLine($"IntegrationTestBase.Execute: Timed out waiting for {executableName}. Terminating the process.");
                    process.Kill();
                }
                else
                {
                    // Ensure async buffers are flushed
                    process.WaitForExit();
                }

                stopwatch.Stop();

                Console.WriteLine($"IntegrationTestBase.Execute: Total execution time: {stopwatch.Elapsed.Duration()}");

                stdError = stderrBuffer.ToString();
                stdOut = stdoutBuffer.ToString();
                exitCode = process.ExitCode;

                Console.WriteLine("IntegrationTestBase.Execute: stdError = {0}", stdError);
                Console.WriteLine("IntegrationTestBase.Execute: stdOut = {0}", stdOut);
                Console.WriteLine($"IntegrationTestBase.Execute: Stopped {executableName}. Exit code = {0}", exitCode);
            }
        }

        private void FormatStandardOutCome()
        {
            this.standardTestErrorWithWhiteSpace = this.standardTestError;
            this.standardTestError = Regex.Replace(this.standardTestError, @"\s+", " ");

            this.standardTestOutputWithWhiteSpace = this.standardTestOutput;
            this.standardTestOutput = Regex.Replace(this.standardTestOutput, @"\s+", " ");
        }

        /// <summary>
        /// Create runsettings file from runConfigurationDictionary at destinationRunsettingsPath
        /// </summary>
        /// <param name="destinationRunsettingsPath">
        /// Destination runsettings path where resulted file saves
        /// </param>
        /// <param name="runConfigurationDictionary">
        /// Contains run configuration settings
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

        /// <summary>
        /// Create runsettings file at destinationRunsettingsPath with the content from xmlString
        /// </summary>
        /// <param name="destinationRunsettingsPath">
        /// Destination runsettings path where resulted file is saved
        /// </param>
        /// <param name="runSettingsXml">
        /// Run settings xml string
        /// </param>
        public static void CreateRunSettingsFile(string destinationRunsettingsPath, string runSettingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(runSettingsXml);
            var stream = new FileHelper().GetStream(destinationRunsettingsPath, FileMode.Create);
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
