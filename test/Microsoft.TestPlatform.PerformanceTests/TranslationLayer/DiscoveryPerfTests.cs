// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestClass]
    public class DiscoveryPerfTests
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private DiscoveryEventHandler2 discoveryEventHandler2;
        private DirectoryInfo currentDirectory = new DirectoryInfo(typeof(DiscoveryPerfTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent;

        public void Setup()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.discoveryEventHandler2 = new DiscoveryEventHandler2();
        }

        [TestMethod]
        public void Discover10KTests()
        {
            var testAssemblies = new List<string>
                                     {
                                         this.GetPerfAssetFullPath("MSTestAdapterPerfTestProject.dll"),
                                     };

            this.Setup();
            this.vstestConsoleWrapper.DiscoverTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.discoveryEventHandler2);
          
            TelemetryClient client = new TelemetryClient();
            TelemetryConfiguration.Active.InstrumentationKey = "76b373ba-8a55-45dd-b6db-7f1a83288691";
            //client.InstrumentationKey = "76b373ba-8a55-45dd-b6db-7f1a83288691";
            Dictionary<string, string> properties = new Dictionary<string, string>();
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            foreach(var entry in this.discoveryEventHandler2.Metrics)
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

            client.TrackEvent("DiscoveryMsTest10K", properties, metrics);
            client.Flush();
        }

        private string GetPerfAssetFullPath(string dllName)
        {
            return Path.Combine(currentDirectory.FullName, "TestAssets\\PerfAssets", dllName);
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
        public static string BuildConfiguration
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
            var root = currentDirectory.Parent.Parent.Parent;
            // Path to artifacts vstest.console
            return Path.Combine(root.FullName, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
        }

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
