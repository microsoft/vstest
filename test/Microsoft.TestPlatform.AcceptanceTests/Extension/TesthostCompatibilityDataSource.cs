// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// A data source that provides every testhost.
///
/// When that adds up to no configuration exception is thrown.
/// </summary>
public class HostCompatibilityDataSource : TestDataSource<RunnerInfo>
{
    private readonly CompatibilityRowsBuilder _builder;

    public HostCompatibilityDataSource(
        string runnerFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string hostFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string hostVersions = AcceptanceTestBase.LATEST_TO_LEGACY)
    {
        // TODO: We actually don't generate values to use different translation layers, because we don't have a good way to do
        // that right now. Translation layer is loaded directly into the acceptance test, and so we don't have easy way to substitute it.

        _builder = new CompatibilityRowsBuilder(
            runnerFrameworks,
            // runner versions
            AcceptanceTestBase.LATEST,
            hostFrameworks,
            hostVersions,
            // adapter versions
            AcceptanceTestBase.LATESTSTABLE,
            // adapter
            AcceptanceTestBase.MSTEST);

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }
    public string? BeforeAdapterFeature { get; set; }
    public string? AfterAdapterFeature { get; set; }

    public override void CreateData(MethodInfo methodInfo)
    {
        _builder.WithEveryVersionOfRunner = false;
        _builder.WithEveryVersionOfHost = true;
        _builder.WithEveryVersionOfAdapter = false;
        _builder.WithOlderConfigurations = false;
        _builder.WithInProcess = false;

        _builder.BeforeFeature = BeforeFeature;
        _builder.AfterFeature = AfterFeature;
        _builder.BeforeAdapterFeature = BeforeAdapterFeature;
        _builder.AfterAdapterFeature = AfterAdapterFeature;

        _builder.DebugDataCollector = DebugDataCollector;
        _builder.DebugVSTestConsole = DebugVSTestConsole;
        _builder.DebugTesthost = DebugTesthost;
        _builder.NoDefaultBreakpoints = NoDefaultBreakpoints;

        var data = _builder.CreateData();
        data.ForEach(AddData);
    }
}
