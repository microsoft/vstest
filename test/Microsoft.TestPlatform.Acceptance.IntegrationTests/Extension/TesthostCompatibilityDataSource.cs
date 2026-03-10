// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// A data source that provides every testhost.
///
/// When that adds up to no configuration exception is thrown.
/// </summary>
public class TestHostCompatibilityDataSource : TestDataSourceAttribute<RunnerInfo>
{
    private readonly CompatibilityRowsBuilder _builder;

    public TestHostCompatibilityDataSource(
        string hostFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string hostVersions = AcceptanceTestBase.LATEST_TO_LEGACY)
    {
        _builder = new CompatibilityRowsBuilder(
            // runner
            AcceptanceTestBase.LATEST,
            AcceptanceTestBase.DEFAULT_RUNNER_NET,
            // host
            hostVersions,
            hostFrameworks,
            // adapter
            AcceptanceTestBase.LATESTSTABLE,
            AcceptanceTestBase.MSTEST);

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTestHost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool DebugStopAtEntrypoint { get; set; }

    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }

    public override void CreateData(MethodInfo methodInfo)
    {
        _builder.BeforeTestHostFeature = BeforeFeature;
        _builder.AfterTestHostFeature = AfterFeature;

        _builder.DebugDataCollector = DebugDataCollector;
        _builder.DebugVSTestConsole = DebugVSTestConsole;
        _builder.DebugTestHost = DebugTestHost;
        _builder.DebugStopAtEntrypoint = DebugStopAtEntrypoint;

        var data = _builder.CreateData();
        data.ForEach(AddData);
    }
}
