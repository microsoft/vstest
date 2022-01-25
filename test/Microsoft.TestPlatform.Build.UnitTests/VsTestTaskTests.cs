// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.UnitTests
{
    using System;
    using System.Linq;

    using Microsoft.TestPlatform.Build.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VSTestTaskTests
    {
        private readonly VSTestTask vsTestTask;

        public VSTestTaskTests()
        {
            vsTestTask = new VSTestTask
            {
                TestFileFullPath = @"C:\path\to\test-assembly.dll",
                VSTestFramework = ".NETCoreapp,Version2.0"
            };
        }

        [TestMethod]
        public void CreateArgumentShouldAddOneEntryForCLIRunSettings()
        {
            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";

            vsTestTask.VSTestCLIRunSettings = new string[2];
            vsTestTask.VSTestCLIRunSettings[0] = arg1;
            vsTestTask.VSTestCLIRunSettings[1] = arg2;

            var result = vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual(5, result.Length);

            // First, second and third args would be framework:".NETCoreapp,Version2.0", testfilepath and -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[3]);
            Assert.AreEqual($"{arg2}", result[4]);
        }

        [TestMethod]
        public void CreateArgumentShouldAddCLIRunSettingsArgAtEnd()
        {
            const string codeCoverageOption = "Code Coverage";

            vsTestTask.VSTestCollect = new string[] { codeCoverageOption };
            vsTestTask.VSTestBlame = "Blame";

            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";

            vsTestTask.VSTestCLIRunSettings = new string[2];
            vsTestTask.VSTestCLIRunSettings[0] = arg1;
            vsTestTask.VSTestCLIRunSettings[1] = arg2;

            var result = vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual(7, result.Length);

            // Following are expected  --framework:".NETCoreapp,Version2.0", testfilepath, blame, collect:"Code coverage" -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[5]);
            Assert.AreEqual($"{arg2}", result[6]);
        }

        [TestMethod]
        public void CreateArgumentShouldPassResultsDirectoryCorrectly()
        {
            const string resultsDirectoryValue = @"C:\tmp\Results Directory";
            vsTestTask.VSTestResultsDirectory = resultsDirectoryValue;

            var result = vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual($"--resultsDirectory:\"{resultsDirectoryValue}\"", result[1]);
        }

        [TestMethod]
        public void CreateArgumentShouldNotSetConsoleLoggerVerbosityIfConsoleLoggerIsGivenInArgs()
        {
            vsTestTask.VSTestVerbosity = "diag";
            vsTestTask.VSTestLogger = new string[] { "Console;Verbosity=quiet" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsn()
        {
            vsTestTask.VSTestVerbosity = "n";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsnormal()
        {
            vsTestTask.VSTestVerbosity = "normal";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsd()
        {
            vsTestTask.VSTestVerbosity = "d";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdetailed()
        {
            vsTestTask.VSTestVerbosity = "detailed";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiag()
        {
            vsTestTask.VSTestVerbosity = "diag";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiagnostic()
        {
            vsTestTask.VSTestVerbosity = "diagnostic";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsq()
        {
            vsTestTask.VSTestVerbosity = "q";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsquiet()
        {
            vsTestTask.VSTestVerbosity = "quiet";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsm()
        {
            vsTestTask.VSTestVerbosity = "m";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsminimal()
        {
            vsTestTask.VSTestVerbosity = "minimal";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsNormalWithCapitalN()
        {
            vsTestTask.VSTestVerbosity = "Normal";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsQuietWithCapitalQ()
        {
            vsTestTask.VSTestVerbosity = "Quiet";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldPreserveWhiteSpaceInLogger()
        {
            vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddOneCollectArgumentForEachCollect()
        {
            vsTestTask.VSTestCollect = new string[2];

            vsTestTask.VSTestCollect[0] = "name1";
            vsTestTask.VSTestCollect[1] = "name 2";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--collect:name1")));
            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--collect:\"name 2\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleTestAdapterPaths()
        {
            vsTestTask.VSTestTestAdapterPath = new string[] { "path1", "path2" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:path1")));
            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:path2")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleLoggers()
        {
            vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx", "console" };
            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:console")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            const string expectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
            CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollectWithExtraConfigurations()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            vsTestTask.VSTestCollect = new string[] { "code coverage;someParameter=someValue" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            const string expectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
            CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTraceCollectorDirectoryPathAsTestAdapterForNonCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            vsTestTask.VSTestCollect = new string[] { "not code coverage" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            const string notExpectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
            CollectionAssert.DoesNotContain(allArguments, notExpectedArg, $"Not expected argument: '''{notExpectedArg}''' present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterIfSettingsGiven()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedatacollector\";
            vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";

            var allArguments = vsTestTask.CreateArgument().ToArray();

            const string expectedArg = "--testAdapterPath:c:\\path\\to\\tracedatacollector\\";
            CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTestAdapterPathIfVSTestTraceDataCollectorDirectoryPathIsEmpty()
        {
            vsTestTask.VSTestTraceDataCollectorDirectoryPath = string.Empty;
            vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";
            vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddNoLogoOptionIfSpecifiedByUser()
        {
            vsTestTask.VSTestNoLogo = "--nologo";
            var allArguments = vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--nologo")));
        }
    }
}
