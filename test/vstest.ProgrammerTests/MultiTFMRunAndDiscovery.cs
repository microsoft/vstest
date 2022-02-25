﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests;

using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

# if DEBUG
#endif

using vstest.ProgrammerTests.Fakes;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Intent;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

public class MultiTFM
{
    public class MultiTFMDiscovery
    {

        [Test(@"
        Given two test assemblies that have the same architecture
        but have different target frameworks.

        When we run test discovery.

        Then two testhosts should be started that target the same framework as each assembly.
    ")]
        public async Task A()
        {
            // -- arrange
            using var fixture = new Fixture(
                new FixtureOptions
                {
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        [FeatureFlag.MULTI_TFM_RUN] = true
                    }
                }
            );

            var mstest1Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest1.dll")
                .WithFramework(KnownFrameworkNames.Net5) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(11, 5)
                .Build();

            var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

            var runTests1 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .DiscoveryInitialize(FakeMessage.NoResponse)
                .StartDiscovery(mstest1Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
                .Build();

            var testhost1 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest1Dll)
                .WithProcess(testhost1Process)
                .WithResponses(runTests1)
                .Build();

            // --

            var mstest2Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest2.dll")
                .WithFramework(KnownFrameworkNames.Net48) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(21, 5)
                .Build();

            var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

            var runTests2 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .DiscoveryInitialize(FakeMessage.NoResponse)
                .StartDiscovery(mstest2Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
                // We actually do get asked to terminate multiple times. In the second host only.
                .SessionEnd(FakeMessage.NoResponse)
                .Build();

            var testhost2 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest2Dll)
                .WithProcess(testhost2Process)
                .WithResponses(runTests2)
                .Build();

            fixture.AddTestHostFixtures(testhost1, testhost2);

            var testRequestManager = fixture.BuildTestRequestManager();

            mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

            // -- act
            var testDiscoveryPayload = new DiscoveryRequestPayload
            {
                Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },
                RunSettings = $"<RunSettings></RunSettings>"
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.DiscoverTests(testDiscoveryPayload, fixture.TestDiscoveryEventsRegistrar, fixture.ProtocolConfig));

            // -- assert
            fixture.AssertNoErrors();
            // We figure out the framework for each assembly so there should be no incompatibility warnings
            // REVIEW: This should be uncommented. Commenting it now, because it is helpful to see those warnings.
            // fixture.TestRunEventsRegistrar.LoggedWarnings.Should().NotContainMatch("Test run detected DLL(s) which were built for different framework*");

            fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
            var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartDiscovery);
            var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest1.dll.
            startWithSources1Text.Should().Contain("mstest1.dll");
            // And we sent net5 as the target framework, because that is the framework of mstest1.dll.
            startWithSources1Text.Should().Contain(KnownFrameworkStrings.Net5);

            var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartDiscovery);
            var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest2.dll.
            startWithSources2Text.Should().Contain("mstest2.dll");
            // And we sent net48 as the target framework, because that is the framework of mstest2.dll.
            startWithSources2Text.Should().Contain(mstest2Dll.FrameworkName.ToString());

            fixture.DiscoveredTests.Should().HaveCount(mstest1Dll.TestCount + mstest2Dll.TestCount);
        }

        [Test(@"
        Given two test assemblies that have the same architecture
        but have different target frameworks.

        When we run test discovery
        and provide runsettings that define the desired target framework.

        Then two testhosts should be started that target the framework chosen by runsettings.
    ")]
        public async Task B()
        {
            // -- arrange
            using var fixture = new Fixture(
                new FixtureOptions
                {
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        [FeatureFlag.MULTI_TFM_RUN] = true
                    }
                }
            );

            var mstest1Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest1.dll")
                .WithFramework(KnownFrameworkNames.Net5) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(11, 5)
                .Build();

            var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

            var runTests1 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .DiscoveryInitialize(FakeMessage.NoResponse)
                .StartDiscovery(mstest1Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
                .Build();

            var testhost1 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest1Dll)
                .WithProcess(testhost1Process)
                .WithResponses(runTests1)
                .Build();

            // --

            var mstest2Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest2.dll")
                .WithFramework(KnownFrameworkNames.Net6) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(21, 5)
                .Build();

            var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

            var runTests2 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .DiscoveryInitialize(FakeMessage.NoResponse)
                .StartDiscovery(mstest2Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
                // We actually do get asked to terminate multiple times. In the second host only.
                .SessionEnd(FakeMessage.NoResponse)
                .Build();

            var testhost2 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest2Dll)
                .WithProcess(testhost2Process)
                .WithResponses(runTests2)
                .Build();

            fixture.AddTestHostFixtures(testhost1, testhost2);

            var testRequestManager = fixture.BuildTestRequestManager();

            mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

            // -- act
            var testDiscoveryPayload = new DiscoveryRequestPayload
            {
                Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },
                RunSettings = $"<RunSettings><RunConfiguration><TargetFramework>{KnownFrameworkStrings.Net7}</TargetFramework></RunConfiguration></RunSettings>"
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.DiscoverTests(testDiscoveryPayload, fixture.TestDiscoveryEventsRegistrar, fixture.ProtocolConfig));

            // -- assert
            fixture.AssertNoErrors();
            // We figure out the framework for each assembly so there should be no incompatibility warnings
            // REVIEW: This should be uncommented. Commenting it now, because it is helpful to see those warnings.
            // fixture.TestRunEventsRegistrar.LoggedWarnings.Should().NotContainMatch("Test run detected DLL(s) which were built for different framework*");

            fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
            var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartDiscovery);
            var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest1.dll and net7 because that is what we have in settings.
            startWithSources1Text.Should().Contain("mstest1.dll");
            startWithSources1Text.Should().Contain(KnownFrameworkStrings.Net7);

            var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartDiscovery);
            var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest2.dll and net7 because that is what we have in settings.
            startWithSources2Text.Should().Contain("mstest2.dll");
            startWithSources2Text.Should().Contain(KnownFrameworkStrings.Net7);

            fixture.DiscoveredTests.Should().HaveCount(mstest1Dll.TestCount + mstest2Dll.TestCount);
        }
    }

    public class MultiTFMExecution
    {
        [Test(@"
            Given two test assemblies that have the same architecture
            but have different target frameworks.

            When we execute tests.

            Then two testhosts should be started that target the same framework as each assembly.
        ")]
        public async Task C()
        {
            // -- arrange
            using var fixture = new Fixture(
                new FixtureOptions
                {
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        [FeatureFlag.MULTI_TFM_RUN] = true
                    }
                }
            );

            var mstest1Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest1.dll")
                .WithFramework(KnownFrameworkNames.Net5) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(11, 5)
                .Build();

            var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

            var runTests1 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest1Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
                .Build();

            var testhost1 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest1Dll)
                .WithProcess(testhost1Process)
                .WithResponses(runTests1)
                .Build();

            // --

            var mstest2Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest2.dll")
                .WithFramework(KnownFrameworkNames.Net6) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(21, 5)
                .Build();

            var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

            var runTests2 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest2Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
                // We actually do get asked to terminate multiple times. In the second host only.
                .SessionEnd(FakeMessage.NoResponse)
                .Build();

            var testhost2 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest2Dll)
                .WithProcess(testhost2Process)
                .WithResponses(runTests2)
                .Build();

            fixture.AddTestHostFixtures(testhost1, testhost2);

            var testRequestManager = fixture.BuildTestRequestManager();

            mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

            // -- act
            var testRunRequestPayload = new TestRunRequestPayload
            {
                Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },
                RunSettings = $"<RunSettings></RunSettings>"
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.RunTests(testRunRequestPayload, testHostLauncher: null, fixture.TestRunEventsRegistrar, fixture.ProtocolConfig));

            // -- assert
            fixture.AssertNoErrors();
            // We figure out the framework for each assembly so there should be no incompatibility warnings
            // REVIEW: This should be uncommented. Commenting it now, because it is helpful to see those warnings.
            // fixture.TestRunEventsRegistrar.LoggedWarnings.Should().NotContainMatch("Test run detected DLL(s) which were built for different framework*");

            fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
            var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest1.dll.
            startWithSources1Text.Should().Contain("mstest1.dll");
            // And we sent net5 as the target framework, because that is the framework of mstest1.dll.
            startWithSources1Text.Should().Contain(KnownFrameworkStrings.Net5);

            var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest2.dll.
            startWithSources2Text.Should().Contain("mstest2.dll");
            // And we sent net48 as the target framework, because that is the framework of mstest2.dll.
            startWithSources2Text.Should().Contain(KnownFrameworkStrings.Net6);

            fixture.ExecutedTests.Should().HaveCount(mstest1Dll.TestCount + mstest2Dll.TestCount);
        }

        [Test(@"
            Given two test assemblies that have the same architecture
            but have different target frameworks.

            When we execute tests
            and provide runsettings that define the desired target framework.

            Then two testhosts should be started that target the framework chosen by runsettings.
        ")]
        public async Task D()
        {
            // -- arrange
            using var fixture = new Fixture(
                new FixtureOptions
                {
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        [FeatureFlag.MULTI_TFM_RUN] = true
                    }
                }
            );

            var mstest1Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest1.dll")
                .WithFramework(KnownFrameworkNames.Net5) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(11, 5)
                .Build();

            var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

            var runTests1 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest1Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
                .Build();

            var testhost1 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest1Dll)
                .WithProcess(testhost1Process)
                .WithResponses(runTests1)
                .Build();

            // --

            var mstest2Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest2.dll")
                .WithFramework(KnownFrameworkNames.Net6) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(21, 5)
                .Build();

            var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

            var runTests2 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest2Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
                // We actually do get asked to terminate multiple times. In the second host only.
                .SessionEnd(FakeMessage.NoResponse)
                .Build();

            var testhost2 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest2Dll)
                .WithProcess(testhost2Process)
                .WithResponses(runTests2)
                .Build();

            fixture.AddTestHostFixtures(testhost1, testhost2);

            var testRequestManager = fixture.BuildTestRequestManager();

            mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

            // -- act
            var testRunRequestPayload = new TestRunRequestPayload
            {
                Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },
                RunSettings = $"<RunSettings><RunConfiguration><TargetFramework>{KnownFrameworkStrings.Net7}</TargetFramework></RunConfiguration></RunSettings>"
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.RunTests(testRunRequestPayload, testHostLauncher: null, fixture.TestRunEventsRegistrar, fixture.ProtocolConfig));

            // -- assert
            fixture.AssertNoErrors();
            // We figure out the framework for each assembly so there should be no incompatibility warnings
            // REVIEW: This should be uncommented. Commenting it now, because it is helpful to see those warnings.
            // fixture.TestRunEventsRegistrar.LoggedWarnings.Should().NotContainMatch("Test run detected DLL(s) which were built for different framework*");

            fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
            var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest1.dll.
            startWithSources1Text.Should().Contain("mstest1.dll");
            startWithSources1Text.Should().Contain(KnownFrameworkStrings.Net7);

            var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest2.dll.
            startWithSources2Text.Should().Contain("mstest2.dll");
            startWithSources2Text.Should().Contain(KnownFrameworkStrings.Net7);

            fixture.ExecutedTests.Should().HaveCount(mstest1Dll.TestCount + mstest2Dll.TestCount);
        }
    }

    public class MultiTFMTestSessions
    {

        [Test(@"
        Given two test assemblies that have the same architecture
        but have different target frameworks.

        When we execute tests
        and provide runsettings that define the desired target framework.

        Then two testhosts should be started that target the framework chosen by runsettings.
    ")][Only]
        public async Task E()
        {
            // -- arrange
            using var fixture = new Fixture(
                new FixtureOptions
                {
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        [FeatureFlag.MULTI_TFM_RUN] = true
                    }
                }
            );

            var mstest1Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest1.dll")
                .WithFramework(KnownFrameworkNames.Net5) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(11, 5)
                .Build();

            var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

            var runTests1 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest1Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
                .Build();

            var testhost1 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest1Dll)
                .WithProcess(testhost1Process)
                .WithResponses(runTests1)
                .Build();

            // --

            var mstest2Dll = new FakeTestDllBuilder()
                .WithPath(@"X:\fake\mstest2.dll")
                .WithFramework(KnownFrameworkNames.Net6) // <---
                .WithArchitecture(Architecture.X64)
                .WithTestCount(21, 5)
                .Build();

            var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

            var runTests2 = new FakeTestHostResponsesBuilder()
                .VersionCheck(5)
                .ExecutionInitialize(FakeMessage.NoResponse)
                .StartTestExecutionWithSources(mstest2Dll.TestResultBatches)
                .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
                // We actually do get asked to terminate multiple times. In the second host only.
                .SessionEnd(FakeMessage.NoResponse)
                .Build();

            var testhost2 = new FakeTestHostFixtureBuilder(fixture)
                .WithTestDll(mstest2Dll)
                .WithProcess(testhost2Process)
                .WithResponses(runTests2)
                .Build();

            fixture.AddTestHostFixtures(testhost1, testhost2);

            var testRequestManager = fixture.BuildTestRequestManager();

            mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

            // -- act

            var startTestSessionPayload = new StartTestSessionPayload
            {
                RunSettings = "<RunSettings></RunSettings>",
                Sources = new[] { mstest1Dll.Path, mstest2Dll.Path }
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.StartTestSession(startTestSessionPayload, testHostLauncher: null, fixture.TestSessionEventsHandler, fixture.ProtocolConfig));

            var testRunRequestPayload = new TestRunRequestPayload
            {
                Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },
                RunSettings = $"<RunSettings><RunConfiguration><TargetFramework>{KnownFrameworkStrings.Net7}</TargetFramework></RunConfiguration></RunSettings>"
            };

            await testRequestManager.ExecuteWithAbort(tm => tm.RunTests(testRunRequestPayload, testHostLauncher: null, fixture.TestRunEventsRegistrar, fixture.ProtocolConfig));

            // -- assert
            fixture.AssertNoErrors();
            // We figure out the framework for each assembly so there should be no incompatibility warnings
            // REVIEW: This should be uncommented. Commenting it now, because it is helpful to see those warnings.
            // fixture.TestRunEventsRegistrar.LoggedWarnings.Should().NotContainMatch("Test run detected DLL(s) which were built for different framework*");

            fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
            var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest1.dll.
            startWithSources1Text.Should().Contain("mstest1.dll");
            startWithSources1Text.Should().Contain(KnownFrameworkStrings.Net7);

            var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
            var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
            // We sent mstest2.dll.
            startWithSources2Text.Should().Contain("mstest2.dll");
            startWithSources2Text.Should().Contain(KnownFrameworkStrings.Net7);

            fixture.ExecutedTests.Should().HaveCount(mstest1Dll.TestCount + mstest2Dll.TestCount);
        }
    }
}

public class MultiTFMRunAndDiscoveryCompatibilityMode
{
    [Exclude]
    public async Task GivenMultipleMsTestAssembliesThatHaveTheSameArchitecture_AndHaveDifferentTargetFrameworks_AndMULTI_TFM_RUNFeatureFlagIsDisabled_WhenTestsAreRun_ThenTwoTesthostsAreStartedBothForTheSameTFM()
    {
        // -- arrange
        using var fixture = new Fixture(
            new FixtureOptions
            {
                FeatureFlags = new Dictionary<string, bool>
                {
                    [FeatureFlag.MULTI_TFM_RUN] = false
                }
            }
        );

        var mstest1Dll = new FakeTestDllBuilder()
            .WithPath(@"X:\fake\mstest1.dll")
            .WithFramework(KnownFrameworkNames.Net5) // <---
            .WithArchitecture(Architecture.X64)
            .WithTestCount(2)
            .Build();

        var testhost1Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost1.exe");

        var runTests1 = new FakeTestHostResponsesBuilder()
            .VersionCheck(5)
            .ExecutionInitialize(FakeMessage.NoResponse)
            .StartTestExecutionWithSources(mstest1Dll.TestResultBatches)
            .SessionEnd(FakeMessage.NoResponse, afterAction: _ => testhost1Process.Exit())
            .SessionEnd(FakeMessage.NoResponse)
            .Build();

        var testhost1 = new FakeTestHostFixtureBuilder(fixture)
            .WithTestDll(mstest1Dll)
            .WithProcess(testhost1Process)
            .WithResponses(runTests1)
            .Build();

        // --

        var mstest2Dll = new FakeTestDllBuilder()
            .WithPath(@"X:\fake\mstest2.dll")
            .WithFramework(KnownFrameworkNames.Net48) // <---
            .WithArchitecture(Architecture.X64)
            // In reality, the dll would fail to load, and no tests would run from this dll,
            // we simulate that by making it have 0 tests.
            .WithTestCount(0)
            .Build();

        var testhost2Process = new FakeProcess(fixture.ErrorAggregator, @"X:\fake\testhost2.exe");

        var runTests2 = new FakeTestHostResponsesBuilder()
            .VersionCheck(5)
            .ExecutionInitialize(FakeMessage.NoResponse)
            .StartTestExecutionWithSources(mstest2Dll.TestResultBatches)
            .SessionEnd(FakeMessage.NoResponse, _ => testhost2Process.Exit())
            .SessionEnd(FakeMessage.NoResponse)
            .Build();

        var testhost2 = new FakeTestHostFixtureBuilder(fixture)
            .WithTestDll(mstest2Dll)
            .WithProcess(testhost2Process)
            .WithResponses(runTests2)
            .Build();

        fixture.AddTestHostFixtures(testhost1, testhost2);

        var testRequestManager = fixture.BuildTestRequestManager();

        mstest1Dll.FrameworkName.Should().NotBe(mstest2Dll.FrameworkName);

        // -- act
        // TODO: Building whole default runconfiguration is needed here, because TestRequestManager does not ensure the basic settings are populated,
        // and all methods that populate them just silently fail, so TestHostProvider does not get any useful settings.
        var runConfiguration = new RunConfiguration().ToXml().OuterXml;
        var testRunRequestPayload = new TestRunRequestPayload
        {
            Sources = new List<string> { mstest1Dll.Path, mstest2Dll.Path },

            RunSettings = $"<RunSettings>{runConfiguration}</RunSettings>"
        };

        await testRequestManager.ExecuteWithAbort(tm => tm.RunTests(testRunRequestPayload, testHostLauncher: null, fixture.TestRunEventsRegistrar, fixture.ProtocolConfig));

        // -- assert
        fixture.AssertNoErrors();
        // We unify the frameworks to netcoreapp1.0 (because the vstest.console dll we are loading is built for netcoreapp and prefers netcoreapp), and because the
        // behavior is to choose the common oldest framework. We then log warning about incompatible sources.
        fixture.TestRunEventsRegistrar.LoggedWarnings.Should().ContainMatch($"Test run detected DLL(s) which were built for different framework and platform versions*{KnownFrameworkNames.Netcoreapp1}*");

        // We started both testhosts, even thought we know one of them is incompatible.
        fixture.ProcessHelper.Processes.Where(p => p.Started).Should().HaveCount(2);
        var startWithSources1 = testhost1.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
        var startWithSources1Text = startWithSources1.Request.Payload.Select(t => t.ToString()).JoinBySpace();
        // We sent mstest1.dll
        startWithSources1Text.Should().Contain("mstest1.dll");
        // And we sent netcoreapp1.0 as the target framework
        startWithSources1Text.Should().Contain(KnownFrameworkStrings.Netcoreapp1);

        var startWithSources2 = testhost2.FakeCommunicationChannel.ProcessedMessages.Single(m => m.Request.MessageType == MessageType.StartTestExecutionWithSources);
        var startWithSources2Text = startWithSources2.Request.Payload.Select(t => t.ToString()).JoinBySpace();
        // We sent mstest2.dll
        startWithSources2Text.Should().Contain("mstest2.dll");
        // And we sent netcoreapp1.0 as the target framework, even though it is incompatible
        startWithSources2Text.Should().Contain(KnownFrameworkStrings.Netcoreapp1);

        fixture.ExecutedTests.Should().HaveCount(mstest1Dll.TestCount);
    }
}

// Test and improvmement ideas:
// TODO: passing null runsettings does not fail fast, instead it fails in Fakes settings code
// TODO: passing empty string fails in the xml parser code
// TODO: passing null sources and null testcases does not fail fast
// TODO: Just calling Exit, Close won't stop the run, we will keep waiting for test run to complete, I think in real life when we exit then Disconnected will be called on the vstest.console side, leading to abort flow.
//.StartTestExecutionWithSources(new FakeMessage<TestMessagePayload>(MessageType.TestMessage, new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = "Loading type failed." }), afterAction: f => { /*f.Process.Exit();*/ f.FakeCommunicationEndpoint.Disconnect(); })
