// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.TestUtilities
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
 
    /// <summary>
    /// The VS test console test base.
    /// </summary>
    public class VsTestConsoleTestBase
    {
        private const string TestSummaryStatusMessageFormat = "Total tests: {0}. Passed: {1}. Failed: {2}. Skipped: {3}";
        private string standardTestOutput = string.Empty;

        private string standardTestError = string.Empty;

        /// <summary>
        /// The invoke VS test.
        /// </summary>
        /// <param name="arguments">
        /// The arguments.
        /// </param>
        public void InvokeVsTest(string arguments)
        {
            ExecutionManager.Execute(arguments, out this.standardTestOutput, out this.standardTestError);
            this.FormatStandardOutCome();
        }

        /// <summary>
        /// The invoke VS test.
        /// </summary>
        /// <param name="testAssembly">
        /// The test assembly.
        /// </param>
        /// <param name="testAdapterPath">
        /// The test Adapter Path.
        /// </param>
        /// <param name="runSettings">
        /// The run Settings.
        /// </param>
        public void InvokeVsTestForExecution(string testAssembly, string testAdapterPath, string runSettings = "")
        {
            var arguments = PrepareArguments(testAssembly, testAdapterPath, runSettings);
            this.InvokeVsTest(arguments);
        }

        /// <summary>
        /// The invoke VS test for discovery.
        /// </summary>
        /// <param name="testAssembly">
        /// The test assembly.
        /// </param>
        /// <param name="testAdapterPath">
        /// The test adapter path.
        /// </param>
        /// <param name="runSettings">
        /// The run Settings.
        /// </param>
        public void InvokeVsTestForDiscovery(string testAssembly, string testAdapterPath, string runSettings = "")
        {
            var arguments = PrepareArguments(testAssembly, testAdapterPath, runSettings);
            arguments = string.Concat(arguments, " /listtests");
            this.InvokeVsTest(arguments);
        }
        
        /// <summary>
        /// Validate if the overall Test count and results are matching.
        /// </summary>
        /// <param name="passedTestsCount">passed test count</param>
        /// <param name="failedTestsCount">failed test count</param>
        /// <param name="skippedTestsCount">skipped test count</param>
        public void ValidateSummaryStatus(int passedTestsCount, int failedTestsCount, int skippedTestsCount)
        {
            var summaryStatus = string.Format(
                TestSummaryStatusMessageFormat,
                passedTestsCount + failedTestsCount + skippedTestsCount,
                passedTestsCount,
                failedTestsCount,
                skippedTestsCount);

            Assert.IsTrue(this.standardTestOutput.Contains(summaryStatus), "The Test summary does not match. Expected: {0} Test Output: {1}", this.standardTestOutput, summaryStatus);
        }

        /// <summary>
        /// Validates if the test results have the specified set of passed tests.
        /// </summary>
        /// <param name="passedTests">The set of passed tests.</param>
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
        /// <param name="failedTests">The set of failed tests.</param>
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
                Assert.IsTrue(this.standardTestError.Contains(GetTestMethodName(test)), "No stack trace for failed test: {0}", test);
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
        /// The validate discovered tests.
        /// </summary>
        /// <param name="discoveredTestsList">
        /// The discovered tests list.
        /// </param>
        public void ValidateDiscoveredTests(params string[] discoveredTestsList)
        {
            foreach (var test in discoveredTestsList)
            {
                var flag = this.standardTestOutput.Contains(test)
                           || this.standardTestOutput.Contains(GetTestMethodName(test));
                Assert.IsTrue(flag, "Test {0} does not appear in discovered tests list.", test);
            }
        }

        /// <summary>
        /// The prepare arguments.
        /// </summary>
        /// <param name="testAssembly">
        /// The test assembly.
        /// </param>
        /// <param name="testAdapterPath">
        /// The test adapter path.
        /// </param>
        /// <param name="runSettings">
        /// The run settings.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string PrepareArguments(string testAssembly, string testAdapterPath, string runSettings)
        {
            string arguments;
            if (string.IsNullOrWhiteSpace(runSettings))
            {
                arguments = string.Concat("\"", testAssembly, "\"", " /testadapterpath:\"", testAdapterPath, "\"");
            }
            else
            {
                arguments = string.Concat(
                    "\"",
                    testAssembly,
                    "\"",
                    " /testadapterpath:\"",
                    testAdapterPath,
                    "\"",
                    " /settings:\"",
                    runSettings,
                    "\"");
            }

            return arguments;
        }

        /// <summary>
        /// Gets the test method name from full name.
        /// </summary>
        /// <param name="testFullName">test case complete name</param>
        /// <returns>just the test name</returns>
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

        private void FormatStandardOutCome()
        {
            this.standardTestError = Regex.Replace(this.standardTestError, @"\s+", " ");
            this.standardTestOutput = Regex.Replace(this.standardTestOutput, @"\s+", " ");
        }
    }
}
