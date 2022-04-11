﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TestPlatform.PerformanceTests.PerfInstrumentation;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class TelemetryPerfTestBase : PerformanceTestBase
{
    private const string TelemetryInstrumentationKey = "08de1ac5-2db8-4c30-97c6-2e12695fa610";
    private readonly TelemetryClient _client;
    private readonly string _rootDirectory = new DirectoryInfo(typeof(DiscoveryPerfTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent.FullName;

    public TelemetryPerfTestBase()
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
    public void PostTelemetry(IDictionary<string, object> handlerMetrics, PerfAnalyzer perfAnalyzer, string projectName, [CallerMemberName] string? scenario = null)
    {
        var properties = new Dictionary<string, string?>
        {
            // 1.0.1 -first version was called 1.0.1, and has the basic data correct
            // 2 - changes version naming to just number, and adds adapter,
            // in version 2 tests also changed from being just 1 class
            // to be classes that have 20 tests, so e.g. 500 classes with 20 tests each for 10k tests.
            // This layout does not favor any of the tested frameworks and is close to what we would see in the wild.
            ["Version"] = "2",
            ["Project"] = projectName,
            ["Adapter"] = GetAdapterName(projectName),
            ["Scenario"] = scenario,
            ["Configuration"] = BuildConfiguration,
        };

        var metrics = new Dictionary<string, double>();

        if (handlerMetrics is not null)
        {
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
        }

        foreach (var entry in perfAnalyzer.Events)
        {
            metrics.Add(entry.Name, entry.TimeSinceStart);
        }

        _client.TrackEvent($"{scenario}{projectName}", properties, metrics);
        _client.Flush();
    }

    private string GetAdapterName(string projectName)
    {
        var name = projectName.ToLowerInvariant();
        if (name.Contains("xunit"))
            return "xunit";

        if (name.Contains("nunit"))
            return "nunit";

        if (name.Contains("mstest"))
            return "mstest";

        if (name.Contains("perfy"))
            return "perfy";

        throw new InvalidOperationException($"Name of the adapter was not found in the project name {projectName}.");
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
