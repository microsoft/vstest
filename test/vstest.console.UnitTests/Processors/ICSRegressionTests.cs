// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace vstest.console.UnitTests.Processors;

/// <summary>
/// Regression tests for Issue #4637 / PR #4639:
/// PowerShell splits TestRunParameters arguments at spaces, breaking the XML value.
/// Fix: Arguments starting with "TestRunParameters" are merged back together.
/// </summary>
[TestClass]
public class ICSRegressionTests
{
    private readonly TestableRunSettingsProvider _settingsProvider;
    private readonly CliRunSettingsArgumentExecutor _executor;

    public ICSRegressionTests()
    {
        _settingsProvider = new TestableRunSettingsProvider();
        _executor = new CliRunSettingsArgumentExecutor(_settingsProvider, CommandLineOptions.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        CommandLineOptions.Reset();
    }

    #region Issue #4637 - PowerShell TestRunParameters fails with spaces

    [TestMethod]
    public void Initialize_SplitTestRunParameters_ShouldMergeCorrectly()
    {
        // PowerShell splits TestRunParameters at spaces, e.g.:
        //   TestRunParameters.Parameter(name="myParam", value="myValue")
        // becomes two args: ["...myParam\",", "value=..."]
        var args = new string[]
        {
            "TestRunParameters.Parameter(name=\"myParam\",",
            "value=\"myValue\")"
        };

        _executor.Initialize(args);

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.Contains(
            "<Parameter name=\"myParam\" value=\"myValue\" />",
            _settingsProvider.ActiveRunSettings.SettingsXml!);
    }

    [TestMethod]
    public void Initialize_NonSplitTestRunParameters_ShouldWorkNormally()
    {
        // When the argument is not split (e.g., cmd.exe), it should work as-is.
        var args = new string[]
        {
            "TestRunParameters.Parameter(name=\"myParam\",value=\"myValue\")"
        };

        _executor.Initialize(args);

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.Contains(
            "<Parameter name=\"myParam\" value=\"myValue\" />",
            _settingsProvider.ActiveRunSettings.SettingsXml!);
    }

    [TestMethod]
    public void Initialize_MultipleSplitTestRunParameters_ShouldMergeEachCorrectly()
    {
        // Multiple parameters, each split across two args.
        var args = new string[]
        {
            "TestRunParameters.Parameter(name=\"param1\",",
            "value=\"value1\")",
            "TestRunParameters.Parameter(name=\"param2\",",
            "value=\"value2\")"
        };

        _executor.Initialize(args);

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.Contains(
            "<Parameter name=\"param1\" value=\"value1\" />",
            _settingsProvider.ActiveRunSettings.SettingsXml!);
        Assert.Contains(
            "<Parameter name=\"param2\" value=\"value2\" />",
            _settingsProvider.ActiveRunSettings.SettingsXml!);
    }

    [TestMethod]
    public void Initialize_SplitTestRunParametersWithValueContainingSpaces_ShouldMerge()
    {
        // Value itself contains a space after split by PowerShell.
        var args = new string[]
        {
            "TestRunParameters.Parameter(name=\"myParam\",",
            "value=\"my Value\")"
        };

        _executor.Initialize(args);

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.Contains(
            "<Parameter name=\"myParam\" value=\"my Value\" />",
            _settingsProvider.ActiveRunSettings.SettingsXml!);
    }

    #endregion
}
