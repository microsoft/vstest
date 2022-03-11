// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class DebugTests : AcceptanceTestBase
{
    private IVsTestConsoleWrapper _vstestConsoleWrapper;

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [TranslationLayerCompatibilityDataSource("net451", "net451", "LegacyStable")]
    public void AttachDebugger(RunnerInfo runnerInfo, VSTestConsoleInfo vsTestConsoleInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        // Setup();

        var runEventHandler = new AttachDebuggerRunEventHandler(attachDebuggerToProcessResponse: true);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper(TempDirectory, vsTestConsoleInfo);
        _vstestConsoleWrapper.RunTests(GetTestAssemblies(), GetDefaultRunSettings(), runEventHandler);

        // Assert
        Assert.AreEqual(6, runEventHandler.TestResults.Count);
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(2, runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
    }

    private IList<string> GetTestAssemblies()
    {
        var testAssemblies = new List<string>
        {
            GetAssetFullPath("SimpleTestProject.dll"),
            GetAssetFullPath("SimpleTestProject2.dll")
        };

        return testAssemblies;
    }

    class AttachDebuggerRunEventHandler : RunEventHandler
    {
        private readonly bool _attachDebuggerToProcessResult;

        public AttachDebuggerRunEventHandler(bool attachDebuggerToProcessResponse)
        {
            _attachDebuggerToProcessResult = attachDebuggerToProcessResponse;
        }

        public int Pid { get; private set; }

        public override bool AttachDebuggerToProcess(int pid)
        {
            Pid = pid;
            return _attachDebuggerToProcessResult;
        }
    }
}
