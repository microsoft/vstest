// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoverTests : AcceptanceTestBase
    {
        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler1()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var discoveryEventHandler = new DiscoveryEventHandler();

            vsConsoleWrapper.DiscoverTests(sources, this.GetDefaultRunSettings(), discoveryEventHandler);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler.DiscoveredTestCases.Count);
        }

        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedOut()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };
            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var discoveryEventHandler2 = new DiscoveryEventHandler2();

            vsConsoleWrapper.DiscoverTests(
                sources,
                this.GetDefaultRunSettings(),
                new TestPlatformOptions() { CollectMetrics = false },
                discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler2.DiscoveredTestCases.Count);
        }

        [TestMethod]
        public void DiscoverTestsUsingDiscoveryEventHandler2AndTelemetryOptedIn()
        {
            var sources = new List<string>
                              {
                                  this.GetAssetFullPath("SimpleTestProject.dll"),
                                  this.GetAssetFullPath("SimpleTestProject2.dll")
                              };

            var vsConsoleWrapper = this.GetVsTestConsoleWrapper();
            var discoveryEventHandler2 = new DiscoveryEventHandler2();

            vsConsoleWrapper.DiscoverTests(sources, this.GetDefaultRunSettings(), new TestPlatformOptions() { CollectMetrics = true }, discoveryEventHandler2);

            // Assert.
            Assert.AreEqual(6, discoveryEventHandler2.DiscoveredTestCases.Count);
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TargetDevice));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecByAllAdapters));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.TimeTakenInSecForDiscovery));
            Assert.IsTrue(discoveryEventHandler2.Metrics.ContainsKey(TelemetryDataConstants.DiscoveryState));
        }
    }

    public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
    {
        public List<TestCase> DiscoveredTestCases { get; private set; }

        public DiscoveryEventHandler()
        {
            this.DiscoveredTestCases = new List<TestCase>();
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            // No Op
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            if (lastChunk != null)
            {
                this.DiscoveredTestCases.AddRange(lastChunk);
            }
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
             // No Op
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }
    }

    public class DiscoveryEventHandler2 : ITestDiscoveryEventsHandler2
    {
        public List<TestCase> DiscoveredTestCases { get; private set; }
        public IDictionary<string, object> Metrics { get; private set; }

        public DiscoveryEventHandler2()
        {
            this.DiscoveredTestCases = new List<TestCase>();
        }

        public void HandleRawMessage(string rawMessage)
        {
           // No Op
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            // No Op
        }

        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            if (lastChunk != null)
            {
                this.DiscoveredTestCases.AddRange(lastChunk);
                this.Metrics = discoveryCompleteEventArgs.Metrics;
            }
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            // No Op
        }
    }
}