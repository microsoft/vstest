// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.Fakes;

using FluentAssertions;

using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

internal class Fixture : IDisposable
{
    public FakeErrorAggregator ErrorAggregator { get; } = new();
    public FakeProcessHelper ProcessHelper { get; }
    public FakeProcess CurrentProcess { get; }
    public FakeFileHelper FileHelper { get; }
    public FakeTestRuntimeProviderManager TestRuntimeProviderManager { get; }
    public FakeTestRunEventsRegistrar TestRunEventsRegistrar { get; }
    public TestEngine TestEngine { get; private set; }
    public TestPlatform TestPlatform { get; private set; }
    public TestRunResultAggregator TestRunResultAggregator { get; private set; }
    public FakeTestPlatformEventSource TestPlatformEventSource { get; private set; }
    public FakeAssemblyMetadataProvider AssemblyMetadataProvider { get; private set; }
    public InferHelper InferHelper { get; private set; }
    public FakeDataCollectorAttachmentsProcessorsFactory DataCollectorAttachmentsProcessorsFactory { get; private set; }
    public TestRunAttachmentsProcessingManager TestRunAttachmentsProcessingManager { get; private set; }
    public TestRequestManager TestRequestManager { get; private set; }
    public List<TestResult> ExecutedTests => TestRunEventsRegistrar.RunChangedEvents.SelectMany(er => er.Data.NewTestResults).ToList();

    public ProtocolConfig ProtocolConfig { get; internal set; }

    public Fixture()
    {
        // We need to use static class to find the communication endpoint, this clears all the registrations of previous tests.
        TestServiceLocator.Clear();

        CurrentProcess = new FakeProcess(ErrorAggregator, @"X:\fake\vstest.console.exe", string.Empty, null, null, null, null, null);
        ProcessHelper = new FakeProcessHelper(ErrorAggregator, CurrentProcess);
        FileHelper = new FakeFileHelper(ErrorAggregator);
        TestRuntimeProviderManager = new FakeTestRuntimeProviderManager(ErrorAggregator);
        TestRunEventsRegistrar = new FakeTestRunEventsRegistrar(ErrorAggregator);
        ProtocolConfig = new ProtocolConfig();
    }
    public void Dispose()
    {

    }

    internal void AddTestHostFixtures(params FakeTestHostFixture[] testhosts)
    {
        var providers = testhosts.Select(t => t.FakeTestRuntimeProvider).ToArray();
        TestRuntimeProviderManager.AddTestRuntimeProviders(providers);
    }

    internal TestRequestManagerTestHelper BuildTestRequestManager(
        int? timeout = DebugOptions.DefaultTimeout,
        int? debugTimeout = DebugOptions.DefaultDebugTimeout,
        bool? breakOnAbort = DebugOptions.DefaultBreakOnAbort)
    {
        if (!TestRuntimeProviderManager.TestRuntimeProviders.Any())
            throw new InvalidOperationException("There are runtime providers registered for FakeTestRuntimeProviderManager.");


        TestEngine = new TestEngine(TestRuntimeProviderManager, ProcessHelper);
        TestPlatform = new TestPlatform(TestEngine, FileHelper, TestRuntimeProviderManager);

        TestRunResultAggregator = new TestRunResultAggregator();
        TestPlatformEventSource = new FakeTestPlatformEventSource(ErrorAggregator);

        AssemblyMetadataProvider = new FakeAssemblyMetadataProvider(FileHelper, ErrorAggregator);
        InferHelper = new InferHelper(AssemblyMetadataProvider);

        // This is most likely not the correctl place where to cut this off, plugin cache is probably the better place,
        // but it is not injected, and I don't want to investigate this now.
        DataCollectorAttachmentsProcessorsFactory = new FakeDataCollectorAttachmentsProcessorsFactory(ErrorAggregator);
        TestRunAttachmentsProcessingManager = new TestRunAttachmentsProcessingManager(TestPlatformEventSource, DataCollectorAttachmentsProcessorsFactory);

        Task<IMetricsPublisher> fakeMetricsPublisherTask = Task.FromResult<IMetricsPublisher>(new FakeMetricsPublisher(ErrorAggregator));

        var commandLineOptions = CommandLineOptions.Instance;
        TestRequestManager testRequestManager = new(
            commandLineOptions,
            TestPlatform,
            TestRunResultAggregator,
            TestPlatformEventSource,
            InferHelper,
            fakeMetricsPublisherTask,
            ProcessHelper,
            TestRunAttachmentsProcessingManager
            );

        TestRequestManager = testRequestManager;

        return new TestRequestManagerTestHelper(ErrorAggregator, testRequestManager, new DebugOptions
        {
            Timeout = timeout ?? DebugOptions.DefaultTimeout,
            DebugTimeout = debugTimeout ?? DebugOptions.DefaultDebugTimeout,
            BreakOnAbort = breakOnAbort ?? DebugOptions.DefaultBreakOnAbort,
        });
    }

    internal void AssertNoErrors()
    {
        ErrorAggregator.Errors.Should().BeEmpty();
    }
}

internal class DebugOptions
{
    public const int DefaultTimeout = 5;
    // TODO: This setting is actually quite pointless, because I cannot come up with
    // a useful way to abort quickly enough when debugger is attached and I am just running my tests (pressing F5)
    // but at the same time not abort when I am in the middle of debugging some behavior. Maybe looking at debugger,
    // and asking it if any breakpoints were hit / are set. But that is difficult.
    //
    // So normally I press F5 to investigate, but Ctrl+F5 (run without debugger), to run tests.
    public const int DefaultDebugTimeout = 30 * 60;
    public const bool DefaultBreakOnAbort = true;
    public int Timeout { get; init; } = DefaultTimeout;
    public int DebugTimeout { get; init; } = DefaultDebugTimeout;
    public bool BreakOnAbort { get; init; } = DefaultBreakOnAbort;
}
