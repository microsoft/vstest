// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

[TestClass]
// We need to dogfood the package built in this repo *-dev and we pack the tp only on windows
[TestCategory("Windows-Review")]
public class LanguagesTests : AcceptanceTestBase
{
    private IVsTestConsoleWrapper? _vstestConsoleWrapper;
    private DiscoveryEventHandler? _discoveryEventHandler;
    private RunEventHandler? _runEventHandler;

    [MemberNotNull(nameof(_vstestConsoleWrapper), nameof(_runEventHandler), nameof(_discoveryEventHandler))]
    private void Setup(Dictionary<string, string?>? environmentVariables = null)
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper(environmentVariables);
        _discoveryEventHandler = new DiscoveryEventHandler();
        _runEventHandler = new RunEventHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [NetCoreTargetFrameworkDataSource]
    [NetFullTargetFrameworkDataSource]
    public void DiscoverWithNonEngLanguage(RunnerInfo runnerInfo)
    {
        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _testEnvironment.DebugInfo!.DebugStopAtEntrypoint = true;

        // Some languages it doesn't have to be the complete supported list
        foreach (var langId in new string[] { "zh-CHS", "zh-CN", "fr", "it", "pl", "tr", "pt-BR", "zh-Hans" })
        {
            Dictionary<string, string?>? environmentVariables = new()
            {
                { "DOTNET_CLI_UI_LANGUAGE", langId },
                // Used for attach to debug
                // { "VSTEST_HOST_DEBUG","1" }
            };
            Setup(environmentVariables);

            // Act
            var testDll = Directory.GetFiles(_testEnvironment.TestAssetsPath, "NonEngSatelliteBug.dll", SearchOption.AllDirectories)
                .Where(x => x.Contains("bin")).Single();
            _vstestConsoleWrapper.DiscoverTests(new string[] { testDll }, GetEmptyRunsettings(), _discoveryEventHandler);
            _vstestConsoleWrapper.RunTests(_discoveryEventHandler.DiscoveredTestCases, GetEmptyRunsettings(), _runEventHandler);
            _runEventHandler.EnsureSuccess();

            // Assert
            Assert.AreEqual(1, _discoveryEventHandler.DiscoveredTestCases.Count);
            int failedTests = _runEventHandler.TestResults.Count(x => x.Outcome == TestOutcome.Failed);
            Assert.IsFalse(failedTests > 0, $"Number of failed tests {failedTests}");
        }
    }
}
