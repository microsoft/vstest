// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestClass]
    public class TelemetryPerfTestbase
    {
        private const string TelemetryInstrumentationKey = "76b373ba-8a55-45dd-b6db-7f1a83288691";
        private TelemetryClient client;
        private DirectoryInfo currentDirectory = new DirectoryInfo(typeof(DiscoveryPerfTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent;
        
        public TelemetryPerfTestbase()
        {
            client = new TelemetryClient();
            TelemetryConfiguration.Active.InstrumentationKey = TelemetryInstrumentationKey;
        }

        /// <summary>
        /// Used for posting the telemtery to AppInsights
        /// </summary>
        /// <param name="perfScenario"></param>
        /// <param name="handlerMetrics"></param>
        public void PostTelemetry(string perfScenario, IDictionary<string,object> handlerMetrics)
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
            this.client.TrackEvent(perfScenario, properties, metrics);
            this.client.Flush();
        }

        /// <summary>
        /// Returns the full path to the test asset dll
        /// </summary>
        /// <param name="dllDirectory">Name of the directory of the test dll</param>
        /// <param name="dllName">Name of the test dll</param>
        /// <returns></returns>
        public string GetPerfAssetFullPath(string dllDirectory, string dllName)
        {
            return Path.Combine(this.currentDirectory.FullName, "TestAssets\\PerfAssets", dllDirectory , dllName );
        }

        /// <summary>
        /// Returns the VsTestConsole Wrapper.
        /// </summary>
        /// <returns></returns>
        public IVsTestConsoleWrapper GetVsTestConsoleWrapper()
        {
            var vstestConsoleWrapper = new VsTestConsoleWrapper(this.GetConsoleRunnerPath());
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
            // Find the root
            var root = this.currentDirectory.Parent.Parent.Parent;
            // Path to artifacts vstest.console
            return Path.Combine(root.FullName, BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
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
}
