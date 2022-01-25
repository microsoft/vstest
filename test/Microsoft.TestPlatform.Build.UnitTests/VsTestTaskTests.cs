// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.UnitTests;

using System;
using System.Linq;

using Tasks;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
public class VsTestTaskTests
{
    private readonly VsTestTask _vsTestTask;

    public VsTestTaskTests()
    {
        _vsTestTask = new VsTestTask
        {
            TestFileFullPath = @"C:\path\to\test-assembly.dll",
            VsTestFramework = ".NETCoreapp,Version2.0"
        };
    }

    [TestMethod]
    public void CreateArgumentShouldAddOneEntryForCLIRunSettings()
    {
        const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
        const string arg2 = "MSTest.DeploymentEnabled";

        _vsTestTask.VsTestCliRunSettings = new string[2];
        _vsTestTask.VsTestCliRunSettings[0] = arg1;
        _vsTestTask.VsTestCliRunSettings[1] = arg2;

        var result = _vsTestTask.CreateArgument().ToArray();

        Assert.AreEqual(5, result.Length);

        // First, second and third args would be framework:".NETCoreapp,Version2.0", testfilepath and -- respectively.
        Assert.AreEqual($"\"{arg1}\"", result[3]);
        Assert.AreEqual($"{arg2}", result[4]);
    }

    [TestMethod]
    public void CreateArgumentShouldAddCliRunSettingsArgAtEnd()
    {
        const string codeCoverageOption = "Code Coverage";

        _vsTestTask.VsTestCollect = new string[] { codeCoverageOption };
        _vsTestTask.VsTestBlame = "Blame";

        const string arg1 = "RunConfiguration.ResultsDirectory=Path having Space";
        const string arg2 = "MSTest.DeploymentEnabled";

        _vsTestTask.VsTestCliRunSettings = new string[2];
        _vsTestTask.VsTestCliRunSettings[0] = arg1;
        _vsTestTask.VsTestCliRunSettings[1] = arg2;

        var result = _vsTestTask.CreateArgument().ToArray();

        Assert.AreEqual(7, result.Length);

        // Following are expected  --framework:".NETCoreapp,Version2.0", testfilepath, blame, collect:"Code coverage" -- respectively.
        Assert.AreEqual($"\"{arg1}\"", result[5]);
        Assert.AreEqual($"{arg2}", result[6]);
    }

    [TestMethod]
    public void CreateArgumentShouldPassResultsDirectoryCorrectly()
    {
        const string resultsDirectoryValue = @"C:\tmp\Results Directory";
        _vsTestTask.VsTestResultsDirectory = resultsDirectoryValue;

        var result = _vsTestTask.CreateArgument().ToArray();

        Assert.AreEqual($"--resultsDirectory:\"{resultsDirectoryValue}\"", result[1]);
    }

    [TestMethod]
    public void CreateArgumentShouldNotSetConsoleLoggerVerbosityIfConsoleLoggerIsGivenInArgs()
    {
        _vsTestTask.VsTestVerbosity = "diag";
        _vsTestTask.VsTestLogger = new string[] { "Console;Verbosity=quiet" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsn()
    {
        _vsTestTask.VsTestVerbosity = "n";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsnormal()
    {
        _vsTestTask.VsTestVerbosity = "normal";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsd()
    {
        _vsTestTask.VsTestVerbosity = "d";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdetailed()
    {
        _vsTestTask.VsTestVerbosity = "detailed";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiag()
    {
        _vsTestTask.VsTestVerbosity = "diag";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsdiagnostic()
    {
        _vsTestTask.VsTestVerbosity = "diagnostic";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsq()
    {
        _vsTestTask.VsTestVerbosity = "q";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsquiet()
    {
        _vsTestTask.VsTestVerbosity = "quiet";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsm()
    {
        _vsTestTask.VsTestVerbosity = "m";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=minimal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToMinimalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsminimal()
    {
        _vsTestTask.VsTestVerbosity = "minimal";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=minimal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToNormalIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsNormalWithCapitalN()
    {
        _vsTestTask.VsTestVerbosity = "Normal";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=normal")));
    }

    [TestMethod]
    public void CreateArgumentShouldSetConsoleLoggerVerbosityToQuietIfConsoleLoggerIsNotGivenInArgsAndVerbosityIsQuietWithCapitalQ()
    {
        _vsTestTask.VsTestVerbosity = "Quiet";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:Console;Verbosity=quiet")));
    }

    [TestMethod]
    public void CreateArgumentShouldPreserveWhiteSpaceInLogger()
    {
        _vsTestTask.VsTestLogger = new string[] { "trx;LogFileName=foo bar.trx" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
    }

    [TestMethod]
    public void CreateArgumentShouldAddOneCollectArgumentForEachCollect()
    {
        _vsTestTask.VsTestCollect = new string[2];

        _vsTestTask.VsTestCollect[0] = "name1";
        _vsTestTask.VsTestCollect[1] = "name 2";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--collect:name1")));
        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--collect:\"name 2\"")));
    }

    [TestMethod]
    public void CreateArgumentShouldAddMultipleTestAdapterPaths()
    {
        _vsTestTask.VsTestTestAdapterPath = new string[] { "path1", "path2" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:path1")));
        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:path2")));
    }

    [TestMethod]
    public void CreateArgumentShouldAddMultipleLoggers()
    {
        _vsTestTask.VsTestLogger = new string[] { "trx;LogFileName=foo bar.trx", "console" };
        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:\"trx;LogFileName=foo bar.trx\"")));
        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--logger:console")));
    }

    [TestMethod]
    public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollect()
    {
        const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
        _vsTestTask.VsTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
        _vsTestTask.VsTestCollect = new string[] { "code coverage" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        const string expectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
        CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
    }

    [TestMethod]
    public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterForCodeCoverageCollectWithExtraConfigurations()
    {
        const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
        _vsTestTask.VsTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
        _vsTestTask.VsTestCollect = new string[] { "code coverage;someParameter=someValue" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        const string expectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
        CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
    }

    [TestMethod]
    public void CreateArgumentShouldNotAddTraceCollectorDirectoryPathAsTestAdapterForNonCodeCoverageCollect()
    {
        const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedata collector";
        _vsTestTask.VsTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
        _vsTestTask.VsTestCollect = new string[] { "not code coverage" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        const string notExpectedArg = "--testAdapterPath:\"c:\\path\\to\\tracedata collector\"";
        CollectionAssert.DoesNotContain(allArguments, notExpectedArg, $"Not expected argument: '''{notExpectedArg}''' present in [{string.Join(", ", allArguments)}]");
    }

    [TestMethod]
    public void CreateArgumentShouldAddTraceCollectorDirectoryPathAsTestAdapterIfSettingsGiven()
    {
        const string traceDataCollectorDirectoryPath = @"c:\path\to\tracedatacollector\";
        _vsTestTask.VsTestTraceDataCollectorDirectoryPath = traceDataCollectorDirectoryPath;
        _vsTestTask.VsTestSetting = @"c:\path\to\sample.runsettings";

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        const string expectedArg = "--testAdapterPath:c:\\path\\to\\tracedatacollector\\";
        CollectionAssert.Contains(allArguments, expectedArg, $"Expected argument: '''{expectedArg}''' not present in [{string.Join(", ", allArguments)}]");
    }

    [TestMethod]
    public void CreateArgumentShouldNotAddTestAdapterPathIfVsTestTraceDataCollectorDirectoryPathIsEmpty()
    {
        _vsTestTask.VsTestTraceDataCollectorDirectoryPath = string.Empty;
        _vsTestTask.VsTestSetting = @"c:\path\to\sample.runsettings";
        _vsTestTask.VsTestCollect = new string[] { "code coverage" };

        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNull(Array.Find(allArguments, arg => arg.Contains("--testAdapterPath:")));
    }

    [TestMethod]
    public void CreateArgumentShouldAddNoLogoOptionIfSpecifiedByUser()
    {
        _vsTestTask.VsTestNoLogo = "--nologo";
        var allArguments = _vsTestTask.CreateArgument().ToArray();

        Assert.IsNotNull(Array.Find(allArguments, arg => arg.Contains("--nologo")));
    }
}