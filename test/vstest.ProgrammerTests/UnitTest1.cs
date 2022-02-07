// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

using FluentAssertions;

using Microsoft.VisualStudio.TestPlatform.Client;
using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

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
        var commandLineOptions = CommandLineOptions.Instance;

        var fakeTestRuntimeProviderManager = new FakeTestRuntimeProviderManager();
        var fakeProcessHelper = new FakeProcessHelper();
        var testEngine = new TestEngine(fakeTestRuntimeProviderManager, fakeProcessHelper);
        var fakeFileHelper = new FakeFileHelper();
        var testPlatform = new TestPlatform(testEngine, fakeFileHelper, fakeTestRuntimeProviderManager);

        var testRunResultAggregator  = new TestRunResultAggregator();
        var fakeTestPlatformEventSource = new FakeTestPlatformEventSource();

        var fakeAssemblyMetadataProvider = new FakeAssemblyMetadataProvider();
        var inferHelper = new InferHelper(fakeAssemblyMetadataProvider);

        Task<IMetricsPublisher> fakeMetricsPublisherTask = 
        TestRequestManager trm = new(
            commandLineOptions,
            testPlatform,
            testRunResultAggregator,
            fakeTestPlatformEventSource,
            inferHelper,
            metricsPublisherTask,
            
            );


    }

}

internal class FakeAssemblyMetadataProvider : IAssemblyMetadataProvider
{
    public FakeAssemblyMetadataProvider()
    {
    }
}

internal class FakeTestPlatformEventSource : ITestPlatformEventSource
{
    public FakeTestPlatformEventSource()
    {
    }
}

internal class FakeFileHelper : IFileHelper
{
    public FakeFileHelper()
    {
    }
}

internal class FakeProcessHelper : IProcessHelper
{
    public FakeProcessHelper()
    {
    }
}

internal class FakeTestRuntimeProviderManager : ITestRuntimeProviderManager
{
    public FakeTestRuntimeProviderManager()
    {
    }
}

internal class FakeTestHostProcess : FakeProcess
{
    public FakeTestHostProcess(string commandLine) : base(commandLine)
    {
    }

    public CapturedRunSettings? RunSettings { get; internal set; }
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

internal class FakeTestExtensionManager : ITestExtensionManager
{
    public void ClearExtensions()
    {
        throw new NotImplementedException();
    }

    public void UseAdditionalExtensions(IEnumerable<string> pathToAdditionalExtensions, bool skipExtensionFilters)
    {
        throw new NotImplementedException();
    }
}

internal class FakeProcess
{
    public string CommandLine { get; }

    public FakeProcess(string commandLine)
    {
        CommandLine = commandLine;
    }
}



internal class CapturedRunSettings
{
    public int MaxParallelLevel { get; internal set; }
}

internal class FakeOutput : IOutput
{
    public FakeOutput()
    {
    }

    public List<OutputMessage> Messages { get; } = new();
    public StringBuilder CurrentLine { get; } = new();
    public List<string> Lines { get; } = new();

    public void Write(string message, OutputLevel level)
    {
        Messages.Add(new OutputMessage(message, level, isNewLine: false));
        CurrentLine.Append(message);
    }

    public void WriteLine(string message, OutputLevel level)
    {
        Lines.Add(CurrentLine + message);
        CurrentLine.Clear();
    }
}

internal class OutputMessage
{
    public OutputMessage(string message, OutputLevel level, bool isNewLine)
    {
        Message = message;
        Level = level;
        IsNewLine = isNewLine;
    }

    public string Message { get; }
    public OutputLevel Level { get; }
    public bool IsNewLine { get; }
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
