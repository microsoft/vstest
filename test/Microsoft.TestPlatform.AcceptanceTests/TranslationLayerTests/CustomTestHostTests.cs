// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;

using FluentAssertions;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class CustomTestHostTests : AcceptanceTestBase
{
    private IVsTestConsoleWrapper _vstestConsoleWrapper;

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [RunnerCompatibilityDataSource(BeforeFeature = Features.ATTACH_DEBUGGER_FLOW)]
    [TestHostCompatibilityDataSource(BeforeFeature = Features.ATTACH_DEBUGGER_FLOW)]
    public void RunTestsWithCustomTestHostLauncherLaunchesTheProcessUsingTheProvidedLauncher(RunnerInfo runnerInfo)
    {
        // Pins the existing functionality.

        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);

        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        var runEventHandler = new RunEventHandler();

        // Act
        var customTestHostLauncher = new TestHostLauncherV1();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestDlls("MSTestProject1.dll", "MSTestProject2.dll"), GetDefaultRunSettings(), runEventHandler, customTestHostLauncher);

        // Assert
        EnsureTestsRunWithoutErrors(runEventHandler, passed: 2, failed: 2, skipped: 2);

        // Ensure we tried to launch testhost process.
        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    // [RunnerCompatibilityDataSource(BeforeFeature = Features.ATTACH_DEBUGGER_FLOW)]
    [TestHostCompatibilityDataSource("net451", "netcoreapp2.1", "LegacyStable", BeforeFeature = Features.ATTACH_DEBUGGER_FLOW, DebugVSTestConsole = true)]
    [Ignore("This is not working for any testhost prior 16.7.0 where the change was introduced. The launch testhost flow was replaced with AttachDebugger in runner, and the new callback to AttachDebugger happens in testhost."
        + "But any testhost prior 16.7.0 where the change was introduced does not know to call back AttachDebugger, and the call never happens.")]
    // You can confirm that the functionality broke between runner and testhost, past this point by using newer runners, against older testhosts.
    // [TestPlatformCompatibilityDataSource(AfterRunnerFeature = Features.ATTACH_DEBUGGER_FLOW, BeforeTestHostFeature = Features.ATTACH_DEBUGGER_FLOW)]
    public void RunTestsWithCustomTestHostLauncherLaunchesTheProcessUsingTheProvidedLauncherWhenITestHostLauncher2IsProvided(RunnerInfo runnerInfo)
    {
        // Ensures compatibility with testhost and runners that were created before 16.3.0. It makes sure that even if user provides
        // an implementation of the ITestHostLauncher2 interface, then testhost expecting ITestHostLauncher still works correctly.

        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        var runEventHandler = new RunEventHandler();

        // Act
        var customTestHostLauncher = new TestHostLauncherV2();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestDlls("MSTestProject1.dll", "MSTestProject2.dll"), GetDefaultRunSettings(), runEventHandler, customTestHostLauncher);

        // Assert
        EnsureTestsRunWithoutErrors(runEventHandler, passed: 2, failed: 2, skipped: 2);

        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");
        customTestHostLauncher.AttachDebuggerProcessId.Should().BeNull("we should not be asked to attach to a debugger, that flow is not used when vstest.console does not support it yet, even when it is given ITestHostLauncher2");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [RunnerCompatibilityDataSource(AfterFeature = Features.ATTACH_DEBUGGER_FLOW)]
    // [TestHostCompatibilityDataSource(AfterFeature = Features.ATTACH_DEBUGGER_FLOW)]
    public void RunTestsWithCustomTestHostLauncherAttachesToDebuggerUsingTheProvidedLauncher(RunnerInfo runnerInfo)
    {

        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        var runEventHandler = new RunEventHandler();

        // Act
        var customTestHostLauncher = new TestHostLauncherV2();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestDlls("MSTestProject1.dll", "MSTestProject2.dll"), GetDefaultRunSettings(), runEventHandler, customTestHostLauncher);

        // Assert
        EnsureTestsRunWithoutErrors(runEventHandler, passed: 2, failed: 2, skipped: 2);

        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.AttachDebuggerProcessId.Should().NotBeNull("we should be asked to attach a debugger to some process and save the pid of the process");
        customTestHostLauncher.LaunchProcessProcessId.Should().BeNull("we should not be asked to launch some real process, that flow is not used when vstest.console supports it and is given ITestHostLauncher2");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [Ignore("This is not working. The compatibility code only checks the protocol version (in handler), which is dictated by testhost. "
        + "It sees 6 but does not realize that the provided CustomTesthostLauncher is not supporting the new feature, it ends up calling back to EditoAttachDebugger" +
        "in translation layer, and that just silently skips the call.")]
    [RunnerCompatibilityDataSource(AfterFeature = Features.ATTACH_DEBUGGER_FLOW)]
    [TestHostCompatibilityDataSource(AfterFeature = Features.ATTACH_DEBUGGER_FLOW)]
    public void RunTestsWithCustomTestHostLauncherUsesLaunchWhenGivenAnOutdatedITestHostLauncher(RunnerInfo runnerInfo)
    {
        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        var runEventHandler = new RunEventHandler();

        // Act
        var customTestHostLauncher = new TestHostLauncherV1();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestDlls("MSTestProject1.dll", "MSTestProject2.dll"), GetDefaultRunSettings(), runEventHandler, customTestHostLauncher);

        // Assert
        EnsureTestsRunWithoutErrors(runEventHandler, passed: 2, failed: 2, skipped: 2);

        customTestHostLauncher.Should().NotBeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");
    }

    private static void EnsureTestsRunWithoutErrors(RunEventHandler runEventHandler, int passed, int failed, int skipped)
    {
        runEventHandler.Errors.Should().BeEmpty();
        runEventHandler.TestResults.Should().HaveCount(passed + failed + skipped);
        runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed).Should().Be(passed);
        runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed).Should().Be(failed);
        runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped).Should().Be(skipped);
    }

    /// <summary>
    /// The custom test host launcher implementing ITestHostLauncher.
    /// </summary>
    private class TestHostLauncherV1 : ITestHostLauncher
    {
        public int? LaunchProcessProcessId { get; private set; }

        /// <inheritdoc />
        public bool IsDebug => true;

        /// <inheritdoc />
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            return LaunchTestHost(defaultTestHostStartInfo, CancellationToken.None);
        }

        /// <inheritdoc />
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
        {
            var processInfo = new ProcessStartInfo(
                defaultTestHostStartInfo.FileName,
                defaultTestHostStartInfo.Arguments)
            {
                WorkingDirectory = defaultTestHostStartInfo.WorkingDirectory
            };
            processInfo.UseShellExecute = false;

            var process = new Process { StartInfo = processInfo };
            process.Start();

            LaunchProcessProcessId = process?.Id;
            return LaunchProcessProcessId ?? -1;
        }
    }

    /// <summary>
    /// The custom test host launcher implementing ITestHostLauncher2, and through that also ITestHostLauncher.
    /// </summary>
    private class TestHostLauncherV2 : TestHostLauncherV1, ITestHostLauncher2
    {

        public int? AttachDebuggerProcessId { get; private set; }

        public bool AttachDebuggerToProcess(int pid) => AttachDebuggerToProcess(pid, CancellationToken.None);

        public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
        {
            AttachDebuggerProcessId = pid;
            return true;
        }
    }
}
