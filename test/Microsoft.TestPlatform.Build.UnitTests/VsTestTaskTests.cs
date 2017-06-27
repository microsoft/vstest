// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.UnitTests
{
    using System.Linq;

    using Microsoft.TestPlatform.Build.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VsTestTaskTests
    {
        [TestMethod]
        public void CreateArgumentShouldAddDoubleQuotesForCLIRunSettings()
        {
            const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
            const string arg2 = "MSTest.DeploymentEnabled";
            var vstestTask = new VSTestTask { VSTestCLIRunSettings = new string[2] };
            vstestTask.VSTestCLIRunSettings[0] = arg1;
            vstestTask.VSTestCLIRunSettings[1] = arg2;
            
            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var result = vstestTask.CreateArgument().ToArray();

            // First, second and third args would be --framework:abc, testfilepath and -- respectively.
            Assert.AreEqual($"\"{arg1}\"", result[3]);
            Assert.AreEqual($"\"{arg2}\"", result[4]);
        }

        [TestMethod]
        public void CreateArgumentShouldPassResultsDirectoryCorrectly()
        {
            const string resultsDirectoryValue = @"C:\tmp\ResultsDirectory";
            var vstestTask = new VSTestTask {  VSTestResultsDirectory = resultsDirectoryValue };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var result = vstestTask.CreateArgument().ToArray();

            Assert.AreEqual($"--resultsDirectory:\"{resultsDirectoryValue}\"", result[1]);
        }

        [TestMethod]
        public void CreateArgumentShouldNotSetConsoleLoggerVerbosityIfConsoleLoggerIsGivenInArgs()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "diag" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";
            vstestTask.VSTestLogger = "Console;Verbosity=quiet";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:\"Console;Verbosity=normal\"")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:\"Console;Verbosity=quiet\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsn()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "n" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsnormal()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "normal" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsd()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "d" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdetailed()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "detailed" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiag()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "diag" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiagnostic()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "diagnostic" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=normal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsq()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "q" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsquiet()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "quiet" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=quiet")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsm()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "m" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsminimal()
        {
            var vstestTask = new VSTestTask { VSTestVerbosity = "minimal" };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:Console;Verbosity=minimal")));
        }

        [TestMethod]
        public void CreateArgumentShouldPreserveWhiteSpaceInLogger()
        {
            var vstestTask = new VSTestTask();
            
            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";
            vstestTask.VSTestLogger = "trx;LogFileName=foo bar.trx";


            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
        }

        [TestMethod]
        public void CreateArgumentShouldAddOneCollectArgumentForEachCollect()
        {
            var vstestTask = new VSTestTask { VSTestCollect = new string[2] };

            // Add values for required properties.
            vstestTask.TestFileFullPath = "abc";
            vstestTask.VSTestFramework = "abc";

            vstestTask.VSTestCollect[0] = "name1";
            vstestTask.VSTestCollect[1] = "name 2";

            var allArguments = vstestTask.CreateArgument().ToArray();

            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--collect:\"name1\"")));
            Assert.IsNotNull(allArguments.FirstOrDefault(arg => arg.Contains("--collect:\"name 2\"")));
        }
    }
}
