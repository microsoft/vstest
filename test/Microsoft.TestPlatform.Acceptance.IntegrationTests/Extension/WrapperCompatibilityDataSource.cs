// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// A data source that provides every version of runner. Use for testing features specific to VSTestConsoleWrapper.
/// 
/// When that adds up to no configuration exception is thrown.
/// </summary>
public class WrapperCompatibilityDataSource : TestDataSourceAttribute<RunnerInfo>
{
    private readonly CompatibilityRowsBuilder _builder;

    public WrapperCompatibilityDataSource()
    {
        _builder = new CompatibilityRowsBuilder(
            // runner
            AcceptanceTestBase.LATEST_TO_LEGACY,
            AcceptanceTestBase.DEFAULT_RUNNER_NETFX_AND_NET,
            // host
            AcceptanceTestBase.LATEST,
            AcceptanceTestBase.DEFAULT_HOST_NET,
            // adapter
            AcceptanceTestBase.LATESTSTABLE,
            AcceptanceTestBase.MSTEST);

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTestHost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool DebugStopAtEntrypoint { get; set; }
    public int JustRow { get; set; } = -1;

    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }

    //public string? BeforeTestHostFeature { get; set; }
    //public string? AfterTestHostFeature { get; set; }

    public string? BeforeAdapterFeature { get; set; }
    public string? AfterAdapterFeature { get; set; }

    public override void CreateData(MethodInfo methodInfo)
    {
        _builder.WithVSIXRunner = true;
        _builder.WithInProcess = false;

        _builder.BeforeRunnerFeature = BeforeFeature;
        _builder.AfterRunnerFeature = AfterFeature;

        //_builder.BeforeTestHostFeature = BeforeTestHostFeature;
        //_builder.AfterTestHostFeature = AfterTestHostFeature;

        _builder.BeforeAdapterFeature = BeforeAdapterFeature;
        _builder.AfterAdapterFeature = AfterAdapterFeature;

        _builder.DebugDataCollector = DebugDataCollector;
        _builder.DebugVSTestConsole = DebugVSTestConsole;
        _builder.DebugTestHost = DebugTestHost;
        _builder.DebugStopAtEntrypoint = DebugStopAtEntrypoint;

        _builder.JustRow = JustRow < 0 ? null : JustRow;

        var data = _builder.CreateData();
        data.ForEach(AddData);
    }
}

