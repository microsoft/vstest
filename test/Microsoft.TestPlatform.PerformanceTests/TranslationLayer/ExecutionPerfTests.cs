using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.PerformanceTests.TranslationLayer
{

    [TestClass]
    public class ExecutionPerfTests : TelemetryPerfTestbase
    {
        private IVsTestConsoleWrapper vstestConsoleWrapper;
        private RunEventHandler runEventHandler;

        public ExecutionPerfTests()
        {
            this.vstestConsoleWrapper = this.GetVsTestConsoleWrapper();
            this.runEventHandler = new RunEventHandler();
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunMsTest10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.runEventHandler);

            this.PostTelemetry("RunMsTest10K", this.runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunXunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.runEventHandler);

            this.PostTelemetry("RunXunit10K", this.runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunNunit10K()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, this.runEventHandler);

            this.PostTelemetry("RunNunit10K", this.runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunMsTest10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("MSTestAdapterPerfTestProject", "MSTestAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.runEventHandler);

            this.PostTelemetry("RunMsTest10KWithDefaultAdaptersSkipped", this.runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunXunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("XunitAdapterPerfTestProject", "XunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.runEventHandler);

            this.PostTelemetry("RunXunit10KWithDefaultAdaptersSkipped", this.runEventHandler.Metrics);
        }

        [TestMethod]
        [TestCategory("TelemetryPerf")]
        public void RunNunit10KWithDefaultAdaptersSkipped()
        {
            var testAssemblies = new List<string>
                                     {
                                         GetPerfAssetFullPath("NunitAdapterPerfTestProject", "NunitAdapterPerfTestProject.dll"),
                                     };

            this.vstestConsoleWrapper.RunTests(testAssemblies, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true, SkipDefaultAdapters = true }, this.runEventHandler);

            this.PostTelemetry("RunNunit10KWithDefaultAdaptersSkipped", this.runEventHandler.Metrics);
        }
    }
}
