// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
    public void GivenAnMSTestAssemblyWith5Tests_WhenTestsAreDiscovered_Then5TestsAreFound()
    {
        // -- arrange
        var commandLineOptions = CommandLineOptions.Instance;

        var fakeCurrentProcess = new FakeProcess(@"C:\temp\vstest.console.exe");
        var fakeProcessHelper = new FakeProcessHelper(fakeCurrentProcess);

        var fakeCommunicationEndpoint = new FakeCommunicationEndpoint();
        TestServiceLocator.Register<ICommunicationEndPoint>(fakeCommunicationEndpoint);
        var fakeTestRuntimeProviderManager = new FakeTestRuntimeProviderManager(fakeProcessHelper);
        var testEngine = new TestEngine(fakeTestRuntimeProviderManager, fakeProcessHelper);
        var fakeFileHelper = new FakeFileHelper();
        var testPlatform = new TestPlatform(testEngine, fakeFileHelper, fakeTestRuntimeProviderManager);

        var testRunResultAggregator = new TestRunResultAggregator();
        var fakeTestPlatformEventSource = new FakeTestPlatformEventSource();

        var fakeAssemblyMetadataProvider = new FakeAssemblyMetadataProvider(fakeFileHelper);
        var inferHelper = new InferHelper(fakeAssemblyMetadataProvider);

        // This is most likely not the correctl place where to cut this off, plugin cache is probably the better place,
        // but it is not injected, and I don't want to investigate this now.
        var fakeDataCollectorAttachmentsProcessorsFactory = new FakeDataCollectorAttachmentsProcessorsFactory();
        var testRunAttachmentsProcessingManager = new TestRunAttachmentsProcessingManager(fakeTestPlatformEventSource, fakeDataCollectorAttachmentsProcessorsFactory);

        Task<IMetricsPublisher> fakeMetricsPublisherTask = Task.FromResult<IMetricsPublisher>(new FakeMetricsPublisher());
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
        var testRunRequestPayload = new TestRunRequestPayload
        {
            // TODO: passing null sources and null testcases does not fail fast
            Sources = mstest1Dll.Path.ToList(),
            // TODO: passing null runsettings does not fail fast, instead it fails in Fakes settings code
            // TODO: passing empty string fails in the xml parser code
            RunSettings = "<RunSettings></RunSettings>"
        };

        // var fakeTestHostLauncher = new FakeTestHostLauncher();
        var fakeTestRunEventsRegistrar = new FakeTestRunEventsRegistrar();
        var protocolConfig = new ProtocolConfig();

        testRequestManager.RunTests(testRunRequestPayload, testHostLauncher: null, fakeTestRunEventsRegistrar, protocolConfig);

        // -- assert
        // fakeTestRunEventsRegistrar.RunTests.Should().HaveCount(2);
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
