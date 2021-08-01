// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.UnitTests
{
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
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
                BuildEngine = new FakeBuildEngine(),
                TestFileFullPath = new TaskItem(@"C:\path\to\test-assembly.dll"),
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

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, " -- ");
            StringAssert.Contains(commandline, $"\"{arg1}\"");
            StringAssert.Contains(commandline, $"{arg2}");
        }

        [TestMethod]
        public void CreateArgumentShouldAddCLIRunSettingsArgAtEnd()
        {
            const string codeCoverageOption = "Code Coverage";

            this.vsTestTask.VSTestCollect = new string[] { codeCoverageOption };
            this.vsTestTask.VSTestBlame = true;

            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";

            this.vsTestTask.VSTestCLIRunSettings = new string[2];
            this.vsTestTask.VSTestCLIRunSettings[0] = arg1;
            this.vsTestTask.VSTestCLIRunSettings[1] = arg2;

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, " -- ");
            StringAssert.Contains(commandline, $"\"{arg1}\"");
            StringAssert.Contains(commandline, $"{arg2}");
        }

        [TestMethod]
        public void CreateArgumentShouldPassResultsDirectoryCorrectly()
        {
            const string resultsDirectoryValue = @"C:\tmp\Results Directory";
            this.vsTestTask.VSTestResultsDirectory = new TaskItem(resultsDirectoryValue);

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, $"--resultsDirectory:\"{this.vsTestTask.VSTestResultsDirectory.ItemSpec}\"");
        }

        [TestMethod]
        public void CreateArgumentShouldNotSetConsoleLoggerVerbosityIfConsoleLoggerIsGivenInArgs()
        {
            this.vsTestTask.VSTestVerbosity = "diag";
            this.vsTestTask.VSTestLogger = new string[] { "Console;Verbosity=quiet" };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.DoesNotMatch(commandline, new Regex("(--logger:\"Console;Verbosity=normal\")"));
            StringAssert.Contains(commandline, "--logger:\"Console;Verbosity=quiet\"");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsn()
        {
            this.vsTestTask.VSTestVerbosity = "n";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsnormal()
        {
            this.vsTestTask.VSTestVerbosity = "normal";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsd()
        {
            this.vsTestTask.VSTestVerbosity = "d";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdetailed()
        {
            this.vsTestTask.VSTestVerbosity = "detailed";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiag()
        {
            this.vsTestTask.VSTestVerbosity = "diag";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiagnostic()
        {
            this.vsTestTask.VSTestVerbosity = "diagnostic";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsq()
        {
            this.vsTestTask.VSTestVerbosity = "q";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=quiet");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsquiet()
        {
            this.vsTestTask.VSTestVerbosity = "quiet";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=quiet");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsm()
        {
            this.vsTestTask.VSTestVerbosity = "m";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=minimal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsminimal()
        {
            this.vsTestTask.VSTestVerbosity = "minimal";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=minimal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsNormalWithCapitalN()
        {
            this.vsTestTask.VSTestVerbosity = "Normal";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=normal");
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsQuietWithCapitalQ()
        {
            this.vsTestTask.VSTestVerbosity = "Quiet";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:Console;Verbosity=quiet");
        }

        [TestMethod]
        public void CreateArgumentShouldPreserveWhiteSpaceInLogger()
        {
            this.vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx" };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:\"trx;LogFileName=foo bar.trx\"");
        }

        [TestMethod]
        public void CreateArgumentShouldAddOneCollectArgumentForEachCollect()
        {
            this.vsTestTask.VSTestCollect = new string[2];

            this.vsTestTask.VSTestCollect[0] = "name1";
            this.vsTestTask.VSTestCollect[1] = "name 2";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--collect:name1");
            StringAssert.Contains(commandline, "--collect:\"name 2\"");
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleTestAdapterPaths()
        {
            this.vsTestTask.VSTestTestAdapterPath = new ITaskItem[] { new TaskItem("path1"), new TaskItem("path2") };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--testAdapterPath:path1");
            StringAssert.Contains(commandline, "--testAdapterPath:path2");
        }

        [TestMethod]
        public void CreateArgumentShouldAddMultipleLoggers()
        {
            this.vsTestTask.VSTestLogger = new string[] { "trx;LogFileName=foo bar.trx", "console" };
            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--logger:\"trx;LogFileName=foo bar.trx\"");
            StringAssert.Contains(commandline, "--logger:console");
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = new TaskItem(traceDataCollectorDirectoryPath);
            this.vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            string expectedArg = $"--testAdapterPath:\"{this.vsTestTask.VSTestTraceDataCollectorDirectoryPath.ItemSpec}\"";
            StringAssert.Contains(commandline, expectedArg);
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTraceCollectorDirectoryPathAsTestAdapterForNonCodeCoverageCollect()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = new TaskItem(traceDataCollectorDirectoryPath);
            this.vsTestTask.VSTestCollect = new string[] { "not code coverage" };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            string notExpectedArg = $"--testAdapterPath:\"{this.vsTestTask.VSTestTraceDataCollectorDirectoryPath.ItemSpec}\"";
            StringAssert.DoesNotMatch(commandline, new Regex(Regex.Escape(notExpectedArg)));
        }

        [TestMethod]
        public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterIfSettingsGiven()
        {
            const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedatacollector\";
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = new TaskItem(traceDataCollectorDirectoryPath);
            this.vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            string expectedArg = $"--testAdapterPath:{this.vsTestTask.VSTestTraceDataCollectorDirectoryPath.ItemSpec}";
            StringAssert.Contains(commandline, expectedArg);
        }

        [TestMethod]
        public void CreateArgumentShouldNotAddTestAdapterPathIfVSTestTraceDataCollectorDirectoryPathIsEmpty()
        {
            this.vsTestTask.VSTestTraceDataCollectorDirectoryPath = null;
            this.vsTestTask.VSTestSetting = @"c:\path\to\sample.runsettings";
            this.vsTestTask.VSTestCollect = new string[] { "code coverage" };

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.DoesNotMatch(commandline, new Regex(@"(--testAdapterPath:)"));
        }

        [TestMethod]
        public void CreateArgumentShouldAddNoLogoOptionIfSpecifiedByUser()
        {
            this.vsTestTask.VSTestNoLogo = true;

            var commandline = this.vsTestTask.CreateCommandLineArguments();

            StringAssert.Contains(commandline, "--nologo");
        }
    }
}