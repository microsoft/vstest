// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
        telemetryConfiguration.InstrumentationKey = TelemetryInstrumentationKey;

        _client = new TelemetryClient(telemetryConfiguration);
    }

    /// <summary>
    /// Used for posting the telemetry to AppInsights
    /// </summary>
    /// <param name="handlerMetrics"></param>
    /// <param name="scenario"></param>
    public void PostTelemetry(IDictionary<string, object> handlerMetrics, string projectName = null, [CallerMemberName] string scenario = null)
    {
        var properties = new Dictionary<string, string>
        {
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
        _client.TrackEvent($"{scenario}{projectName}", properties, metrics);
        _client.Flush();
    }

    /// <summary>
    /// Returns the full path to the test asset dll
    /// </summary>
    /// <param name="dllDirectory">Name of the directory of the test dll</param>
    /// <param name="name">Name of the test project without extension</param>
    /// <returns></returns>
    public string[] GetPerfAssetFullPath(string name, string framework = "net451")
    {
        var dllPath = Path.Combine(_rootDirectory, "test", "TestAssets", "performance", name, "bin", BuildConfiguration, framework, $"{name}.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(null, dllPath);
        }

        return new[] { dllPath };
    }

    /// <summary>
    /// Returns the VsTestConsole Wrapper.
    /// </summary>
    /// <returns></returns>
    public IVsTestConsoleWrapper GetVsTestConsoleWrapper()
    {
        var vstestConsoleWrapper = new VsTestConsoleWrapper(GetConsoleRunnerPath(),
            new ConsoleParameters
            {
                // Log into TEMP and use info level, becuase that is what is used by default under VS.
                LogFilePath = Path.Combine(Path.GetTempPath(), "logs", "log.txt"),
                TraceLevel = System.Diagnostics.TraceLevel.Info
            });
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

    // DONT make this just <RunSettings></RunSettings> it makes Translation layer hang... https://github.com/microsoft/vstest/issues/3519
    public string GetDefaultRunSettings() => "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
}
