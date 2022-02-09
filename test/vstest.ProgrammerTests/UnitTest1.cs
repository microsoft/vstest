// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Diagnostics;
using System.Runtime.Versioning;

using FluentAssertions;

using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using vstest.ProgrammerTests.CommandLine.Fakes;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine;
#pragma warning restore IDE1006 // Naming Styles

// exluded from run
internal class InlineRunSettingsTests
{
    public void GivenInlineRunsettingsWhenCallingVstestConsoleThenTheyPropagateToTestHost()
    {
        using Fixture fixture = new();
        fixture.VstestConsole
            .WithSource(TestDlls.MSTest1)
            .WithArguments($" -- {RunConfiguration.MaxParallelLevel.InlinePath}=3")
            .Execute();

        fixture.Processes.Should().HaveCount(1);
        var process = fixture.Processes.First();
        process.Should().BeAssignableTo<FakeTestHostProcess>();
        var testhost = (FakeTestHostProcess)process;
        testhost.RunSettings.Should().NotBeNull();
        testhost.RunSettings!.MaxParallelLevel.Should().Be(3);
    }
}

public class TestDiscoveryTests
{
    public async Task GivenAnMSTestAssemblyWith5Tests_WhenTestsAreRun_Then5TestsAreExecuted()
    {
        // -- arrange
        var fakeErrorAggregator = new FakeErrorAggregator();
        var commandLineOptions = CommandLineOptions.Instance;

        var fakeCurrentProcess = new FakeProcess(@"C:\temp\vstest.console.exe", string.Empty, fakeErrorAggregator);
        var fakeProcessHelper = new FakeProcessHelper(fakeCurrentProcess, fakeErrorAggregator);

        var fakeCommunicationEndpoint = new FakeCommunicationEndpoint(fakeErrorAggregator);
        TestServiceLocator.Register<ICommunicationEndPoint>(fakeCommunicationEndpoint);
        var fakeTestRuntimeProviderManager = new FakeTestRuntimeProviderManager(fakeProcessHelper, fakeCommunicationEndpoint, fakeErrorAggregator);
        var testEngine = new TestEngine(fakeTestRuntimeProviderManager, fakeProcessHelper);
        var fakeFileHelper = new FakeFileHelper(fakeErrorAggregator);
        var testPlatform = new TestPlatform(testEngine, fakeFileHelper, fakeTestRuntimeProviderManager);

        var testRunResultAggregator = new TestRunResultAggregator();
        var fakeTestPlatformEventSource = new FakeTestPlatformEventSource(fakeErrorAggregator);

        var fakeAssemblyMetadataProvider = new FakeAssemblyMetadataProvider(fakeFileHelper, fakeErrorAggregator);
        var inferHelper = new InferHelper(fakeAssemblyMetadataProvider);

        // This is most likely not the correctl place where to cut this off, plugin cache is probably the better place,
        // but it is not injected, and I don't want to investigate this now.
        var fakeDataCollectorAttachmentsProcessorsFactory = new FakeDataCollectorAttachmentsProcessorsFactory(fakeErrorAggregator);
        var testRunAttachmentsProcessingManager = new TestRunAttachmentsProcessingManager(fakeTestPlatformEventSource, fakeDataCollectorAttachmentsProcessorsFactory);

        Task<IMetricsPublisher> fakeMetricsPublisherTask = Task.FromResult<IMetricsPublisher>(new FakeMetricsPublisher(fakeErrorAggregator));
        TestRequestManager testRequestManager = new(
            commandLineOptions,
            testPlatform,
            testRunResultAggregator,
            fakeTestPlatformEventSource,
            inferHelper,
            fakeMetricsPublisherTask,
            fakeProcessHelper,
            testRunAttachmentsProcessingManager
            );

        // -- act

        // TODO: Get framework name from constants
        // TODO: have mstest1dll canned
        var mstest1Dll = new FakeDllFile(@"C:\temp\mstest1.dll", new FrameworkName(".NETCoreApp,Version=v5.0"), Architecture.X64);
        fakeFileHelper.AddFile(mstest1Dll);
        // TODO: this gives me run configuration that is way too complete, do we a way to generate "bare" runsettings? if not we should add them. Would be also useful to get
        // runsettings from parameter set so people can use it
        // TODO: TestSessionTimeout gives me way to abort the run without having to cancel it externally, but could probably still lead to hangs if that funtionality is broken
        // TODO: few tries later, that is exactly the case when we abort, it still hangs on waiting to complete request, because test run complete was not sent
        // var runConfiguration = new Microsoft.VisualStudio.TestPlatform.ObjectModel.RunConfiguration { TestSessionTimeout = 40_000 }.ToXml().OuterXml;
        var runConfiguration = string.Empty;
        var testRunRequestPayload = new TestRunRequestPayload
        {
            // TODO: passing null sources and null testcases does not fail fast
            Sources = mstest1Dll.Path.ToList(),
            // TODO: passing null runsettings does not fail fast, instead it fails in Fakes settings code
            // TODO: passing empty string fails in the xml parser code
            RunSettings = $"<RunSettings>{runConfiguration}</RunSettings>"
        };

        // var fakeTestHostLauncher = new FakeTestHostLauncher();
        var fakeTestRunEventsRegistrar = new FakeTestRunEventsRegistrar(fakeErrorAggregator);
        var protocolConfig = new ProtocolConfig();

        var cancelAbort = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            // TODO: we make sure the test is running 1 minute at max and then we try to abort
            // if we aborted we write the error to aggregator, this needs to be made into a pattern
            // so we can avoid hanging if the run does not complete correctly
            await Task.Delay(TimeSpan.FromMinutes(10), cancelAbort.Token);
            if (Debugger.IsAttached)
            {
                // we will abort because we are hanging, look at stacks to see what the problem is
                Debugger.Break();
            }
            fakeErrorAggregator.Add(new Exception("errr we aborted"));
            testRequestManager.AbortTestRun();

        });
        testRequestManager.RunTests(testRunRequestPayload, testHostLauncher: null, fakeTestRunEventsRegistrar, protocolConfig);
        cancelAbort.Cancel();
        await task;

        // -- assert
        fakeErrorAggregator.Errors.Should().BeEmpty();
    }
}

internal class Fixture : IDisposable
{
    public Fixture()
    {

    }

    public List<FakeProcess> Processes { get; } = new();

    public VstestConsole VstestConsole { get; } = new();

    public FakeTestExtensionManager TestExtensionManager { get; } = new();

    public void Dispose()
    {

    }
}



internal class CapturedRunSettings
{
    public int MaxParallelLevel { get; internal set; }
}

internal static class RunConfiguration
{
    public static ConfigurationEntry MaxParallelLevel { get; } = new(nameof(MaxParallelLevel));
}

internal class ConfigurationEntry
{
    public ConfigurationEntry(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string InlinePath => $"RunConfiguration.{Name}";

    public string FullPath => $"RunSettings.{InlinePath}";

    public override string ToString()
    {
        return Name;
    }
}

internal class TestDlls
{
    public static string MSTest1 { get; } = $"{nameof(MSTest1)}.dll";
}
