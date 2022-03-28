// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TestPlatform.PerformanceTests.PerfInstrumentation;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class TelemetryPerfTestbase : PerformanceTestBase
{
    private const string TelemetryInstrumentationKey = "08de1ac5-2db8-4c30-97c6-2e12695fa610";
    private readonly TelemetryClient _client;
    private readonly string _rootDirectory = new DirectoryInfo(typeof(DiscoveryPerfTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent.FullName;

    public TelemetryPerfTestbase()
    {
        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
        telemetryConfiguration.InstrumentationKey = TelemetryInstrumentationKey;

        _client = new TelemetryClient(telemetryConfiguration);
    }

    /// <summary>
    /// Used for posting the telemetry to AppInsights
    /// </summary>
    /// <param name="handlerMetrics"></param>
    /// <param name="scenario"></param>
    public void PostTelemetry(IDictionary<string, object> handlerMetrics, PerfAnalyzer perfAnalyzer, string projectName, [CallerMemberName] string scenario = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["Version"] = "1.0.1",
            ["Project"] = projectName,
            ["Scenario"] = scenario,
            ["Configuration"] = BuildConfiguration,
        };


        var metrics = new Dictionary<string, double>();

        foreach (var entry in handlerMetrics)
        {
            var stringValue = entry.Value.ToString();
            if (double.TryParse(stringValue, out var doubleValue))
            {
                metrics.Add(entry.Key, doubleValue);
            }
            else
            {
                properties.Add(entry.Key, stringValue);
            }
        }

        foreach (var entry in perfAnalyzer.Events)
        {
            metrics.Add(entry.Name, entry.TimeSinceStart);
        }

        _client.TrackEvent($"{scenario}{projectName}", properties, metrics);
        _client.Flush();
    }

    /// <summary>
    /// Returns the full path to the test asset dll
    /// </summary>
    /// <param name="dllDirectory">Name of the directory of the test dll</param>
    /// <param name="name">Name of the test project without extension</param>
    /// <returns></returns>
    public string[] GetPerfAssetFullPath(string name, string framework = "net6.0")
    {
        var dllPath = Path.Combine(_rootDirectory, "test", "TestAssets", "performance", name, "bin", BuildConfiguration, framework, $"{name}.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(null, dllPath);
        }

        return new[] { dllPath };
    }

    /// <summary>
    /// Returns the default runsettings xml
    /// </summary>
    /// <returns></returns>

    // DONT make this just <RunSettings></RunSettings> it makes Translation layer hang... https://github.com/microsoft/vstest/issues/3519
    public string GetDefaultRunSettings() => "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
}
