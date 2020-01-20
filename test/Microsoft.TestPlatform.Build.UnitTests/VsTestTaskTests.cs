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
            this.vsTestTask = new VSTestTask
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

            this.vsTestTask.VSTestCLIRunSettings = new string[2];
            this.vsTestTask.VSTestCLIRunSettings[0] = arg1;
            this.vsTestTask.VSTestCLIRunSettings[1] = arg2;

            var result = this.vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual(5, result.Length);

            // First, second and third args would be framework:".NETCoreapp,Version2.0", testfilepath and -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[3]);
            Assert.AreEqual($"{arg2}", result[4]);
        }

        [TestMethod]
        public void CreateArgumentShouldAddCLIRunSettingsArgAtEnd()
        {
            const string codeCoverageOption = "Code Coverage";

            this.vsTestTask.VSTestCollect = new string[] { codeCoverageOption };
            this.vsTestTask.VSTestBlame = "Blame";

            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";

            this.vsTestTask.VSTestCLIRunSettings = new string[2];
            this.vsTestTask.VSTestCLIRunSettings[0] = arg1;
            this.vsTestTask.VSTestCLIRunSettings[1] = arg2;

            var result = this.vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual(7, result.Length);

            // Following are expected  --framework:".NETCoreapp,Version2.0", testfilepath, blame, collect:"Code coverage" -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[5]);
            Assert.AreEqual($"{arg2}", result[6]);
        }

        [TestMethod]
        public void CreateArgumentShouldPassResultsDirectoryCorrectly()
        {
            const string resultsDirectoryValue = @"C:\tmp\Results Directory";
            this.vsTestTask.VSTestResultsDirectory = resultsDirectoryValue;

            var result = this.vsTestTask.CreateArgument().ToArray();

            Assert.AreEqual($"--resultsDirectory:\"{resultsDirectoryValue}\"", result[1]);
        }

        [TestMethod]
        public void CreateArgumentShouldNotSetConsoleLoggerVerbosityIfConsoleLoggerIsGivenInArgs()
        {
            this.vsTestTask.VSTestVerbosity = "diag";
            this.vsTestTask.VSTestLogger = new string[] { "Console;Verbosity=quiet" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsn()
        {
            this.vsTestTask.VSTestVerbosity = "n";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsnormal()
        {
            this.vsTestTask.VSTestVerbosity = "normal";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsd()
        {
            this.vsTestTask.VSTestVerbosity = "d";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdetailed()
        {
            this.vsTestTask.VSTestVerbosity = "detailed";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiag()
        {
            this.vsTestTask.VSTestVerbosity = "diag";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiagnostic()
        {
            this.vsTestTask.VSTestVerbosity = "diagnostic";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsq()
        {
            this.vsTestTask.VSTestVerbosity = "q";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsquiet()
        {
            this.vsTestTask.VSTestVerbosity = "quiet";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsm()
        {
            this.vsTestTask.VSTestVerbosity = "m";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsminimal()
        {
            this.vsTestTask.VSTestVerbosity = "minimal";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsNormalWithCapitalN()
        {
            this.vsTestTask.VSTestVerbosity = "Normal";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsQuietWithCapitalQ()
        {
            this.vsTestTask.VSTestVerbosity = "Quiet";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldPreserveWhiteSpaceInLogger()
        {
            this.vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddOneCollectArgumentForEachCollect()
        {
            this.vsTestTask.VSTestCollect = new string[2];

            this.vsTestTask.VSTestCollect[0] = "name1";
            this.vsTestTask.VSTestCollect[1] = "name 2";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--collect:name1")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--collect:\"name 2\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleTestAdapterPaths()
        {
            this.vsTestTask.VSTestTestAdapterPath = new string[] { "path1", "path2" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--testAdapterPath:path1")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--testAdapterPath:path2")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleLoggers()
        {
            this.vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx", "console" };
            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:console")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            this.vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            const string expectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
            CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTraceCollectorDirectoryPathAsTestAdapterForNonCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            this.vsTestTask.VSTestCollect = new string[] { "not code coverage" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            const string notExpectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
            CollectionAssert.DoesNotContain(allArguments, notExpectedArg, $"Not expected argument: '''{notExpectedArg}''' present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterIfSettingsGiven()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedatacollector\";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
            this.vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            const string expectedArg = "--testAdapterPath:c:\\path\\to\\tracedatacollector\\";
            CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTestAdapterPathIfVSTestTraceDataCollectorDirectoryPathIsEmpty()
        {
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = string.Empty;
            this.vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";
            this.vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNull(allArguments.FirstOrDefault(arg => arg.Contains("--testAdapterPath:")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddNoLogoOptionIfSpecifiedByUser()
        {
            this.vsTestTask.VSTestNoLogo = "--nologo";
            var allArguments = this.vsTestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--nologo")));
        }
    }
}
