// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using FluentAssertions;

using Microsoft.TestPlatform.TestUtilities;
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
    private RunEventHandler _runEventHandler;

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [TranslationLayerCompatibilityDataSource(BeforeFeature = Features.ATTACH_DEBUGGER)]
    public void RunTestsWithCustomTestHostLauncherLaunchesTheProcessUsingTheProvidedLauncher(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();

        var customTestHostLauncher = new TestHostLauncherV1();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestAssemblies(), GetDefaultRunSettings(), _runEventHandler, customTestHostLauncher);

        // Assert
        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");

        _runEventHandler.TestResults.Should().HaveCount(6);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped).Should().Be(2);
    }

    [TestMethod]
    [TranslationLayerCompatibilityDataSource(BeforeFeature = Features.ATTACH_DEBUGGER)]
    public void RunTestsWithCustomTestHostLauncherLaunchesTheProcessUsingTheProvidedLauncherWhenITestHostLauncher2IsProvided(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();

        var customTestHostLauncher = new TestHostLauncherV2();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestAssemblies(), GetDefaultRunSettings(), _runEventHandler, customTestHostLauncher);

        // Assert
        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");
        customTestHostLauncher.AttachDebuggerProcessId.Should().BeNull("we should not be asked to attach to a debugger, that flow is not used when vstest.console does not support it yet, even when it is given ITestHostLauncher2");

        _runEventHandler.TestResults.Should().HaveCount(6);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped).Should().Be(2);
    }

    [TestMethod]
    [TranslationLayerCompatibilityDataSource(AfterFeature = Features.ATTACH_DEBUGGER)]
    public void RunTestsWithCustomTestHostLauncherAttachesToDebuggerUsingTheProvidedLauncher(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();

        var customTestHostLauncher = new TestHostLauncherV2();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestAssemblies(), GetDefaultRunSettings(), _runEventHandler, customTestHostLauncher);

        // Assert
        customTestHostLauncher.Should().BeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.AttachDebuggerProcessId.Should().NotBeNull("we should be asked to attach a debugger to some process and save the pid of the process");
        customTestHostLauncher.LaunchProcessProcessId.Should().BeNull("we should not be asked to launch some real process, that flow is not used when vstest.console supports it and is given ITestHostLauncher2");

        _runEventHandler.TestResults.Should().HaveCount(6);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped).Should().Be(2);
    }

    [TestMethod]
    [Ignore("This is not working. The compatibility code only checks the protocol version (in handler), which is dictated by testhost. "
        + "It sees 6 but does not realize that the provided CustomTesthostLauncher is not supporting the new feature, it ends up calling back to EditoAttachDebugger" +
        "in translation layer, and that just silently skips the call.")]
    [TranslationLayerCompatibilityDataSource("net451", "net451", "Latest", AfterFeature = Features.ATTACH_DEBUGGER, DebugVSTestConsole = true, DebugTesthost=true)]
    public void RunTestsWithCustomTestHostLauncherUsesLaunchWhenGivenAnOutdatedITestHostLauncher(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();

        var customTestHostLauncher = new TestHostLauncherV1();
        _vstestConsoleWrapper.RunTestsWithCustomTestHost(GetTestAssemblies(), GetDefaultRunSettings(), _runEventHandler, customTestHostLauncher);

        // Assert
        customTestHostLauncher.Should().NotBeAssignableTo<ITestHostLauncher2>();
        customTestHostLauncher.LaunchProcessProcessId.Should().NotBeNull("we should launch some real process and save the pid of it");

        _runEventHandler.TestResults.Should().HaveCount(6);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed).Should().Be(2);
        _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped).Should().Be(2);
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

    /// <summary>
    /// The custom test host launcher implementing ITestHostLauncher.
    /// </summary>
    public class TestHostLauncherV1 : ITestHostLauncher
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
    /// The custom test host launcher implementing ITestHostLauncher2.
    /// </summary>
    public class TestHostLauncherV2 : TestHostLauncherV1, ITestHostLauncher2
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
