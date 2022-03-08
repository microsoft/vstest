// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer;

[TestClass]
public class TelemetryPerfTestbase
{
    private const string TelemetryInstrumentationKey = "08de1ac5-2db8-4c30-97c6-2e12695fa610";
    private readonly TelemetryClient _client;
    private readonly string _rootDirectory = new DirectoryInfo(typeof(DiscoveryPerfTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent.FullName;

    public TelemetryPerfTestbase()
    {
        _client = new TelemetryClient();
        TelemetryConfiguration.Active.InstrumentationKey = TelemetryInstrumentationKey;
    }

    /// <summary>
    /// Used for posting the telemetry to AppInsights
    /// </summary>
    /// <param name="perfScenario"></param>
    /// <param name="handlerMetrics"></param>
    public void PostTelemetry(string perfScenario, IDictionary<string, object> handlerMetrics)
    {
        var properties = new Dictionary<string, string>();
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
        _client.TrackEvent(perfScenario, properties, metrics);
        _client.Flush();
    }

    /// <summary>
    /// Returns the full path to the test asset dll
    /// </summary>
    /// <param name="dllDirectory">Name of the directory of the test dll</param>
    /// <param name="dllName">Name of the test dll</param>
    /// <returns></returns>
    public string GetPerfAssetFullPath(string dllDirectory, string dllName)
    {
        var dllPath = Path.Combine(_rootDirectory, "test", "TestAssets", "PerfAssets", dllDirectory, "bin", BuildConfiguration, "net451", dllName);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(null, dllPath);
        }

        return dllPath;
    }

    /// <summary>
    /// Returns the VsTestConsole Wrapper.
    /// </summary>
    /// <returns></returns>
    public IVsTestConsoleWrapper GetVsTestConsoleWrapper()
    {
        var vstestConsoleWrapper = new VsTestConsoleWrapper(GetConsoleRunnerPath());
        vstestConsoleWrapper.StartSession();

        return vstestConsoleWrapper;
    }

    private string BuildConfiguration
    {
        get
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }

    private string GetConsoleRunnerPath()
    {
        // Path to artifacts vstest.console
        return Path.Combine(_rootDirectory, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
    }

    /// <summary>
    /// Returns the default runsettings xml
    /// </summary>
    /// <returns></returns>
    public string GetDefaultRunSettings()
    {
        string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
                                        </RunConfiguration>
                                    </RunSettings>";
        return runSettingsXml;
    }
}
