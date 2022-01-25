using System.Collections.Generic;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer
{
    [TestClass]
    public class ExecutionPerfTests : TelemetryPerfTestbase
    {
        private readonly IVsTestConsoleWrapper vstestConsoleWrapper;
        private readonly RunEventHandler runEventHandler;

        public ExecutionPerfTests()
        {
            vstestConsoleWrapper = GetVsTestConsoleWrapper();
            runEventHandler = new RunEventHandler();
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunMsTest10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, runEventHandler);

            PostTelemetry("RunMsTest10K", runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunXunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, runEventHandler);

            PostTelemetry("RunXunit10K", runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunNunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, runEventHandler);

            PostTelemetry("RunNunit10K", runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunMsTest10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, runEventHandler);

            PostTelemetry("RunMsTest10KWithDefaultAdaptersSkipped", runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunXunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, runEventHandler);

            PostTelemetry("RunXunit10KWithDefaultAdaptersSkipped", runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunNunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            vstestConsoleWrapper.RunTests(testAssemblies, GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, runEventHandler);

            PostTelemetry("RunNunit10KWithDefaultAdaptersSkipped", runEventHandler.Metrics);
        }
    }
}
