// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    [TestClass]
    public class TelemetryClientTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            var telemetryClient = new TelemetryClient();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TelemetryClient.Dispose();
        }

        [TestMethod]
        public void AddMetricShouldAddMetric()
        {
            TelemetryClient.AddMetric("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");
        }

        [TestMethod]
        public void AdddMetricShouldUpdateMetricIfSameKeyIsPresentAlready()
        {
            TelemetryClient.AddMetric("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");

            TelemetryClient.AddMetric("DummyMessage", "newValue");

            string newValue;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue("DummyMessage", out newValue), true);
            Assert.AreEqual(newValue, "newValue");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetMetricShouldThrowNullExceptionIfValueIsNull()
        {
            TelemetryClient.AddMetric("DummyMessage", null);
        }

        [TestMethod]
        public void GetMetricShouldReturnValidMetricsIfValidItemsAreThere()
        {
            TelemetryClient.AddMetric("DummyMessage", "DummyValue");
            TelemetryClient.AddMetric("DummyMessage2", "DummyValue");

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 2);
        }

        [TestMethod]
        public void GetMetricShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 0);
        }

        [TestMethod]
        public void DisposeShouldClearMetrics()
        {
            TelemetryClient.AddMetric("DummyMessage", "DummyValue");
            TelemetryClient.AddMetric("DummyMessage2", "DummyValue");

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 2);

            TelemetryClient.Dispose();

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 0);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteMetricsShouldAddMetric()
        {
            var dict = new Dictionary<string, string> ();
            dict.Add("Discovery","Completed");

            TelemetryClient.HandleDiscoveryCompleteMetrics(dict);

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 1);

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue("Discovery", out value), true);
            Assert.AreEqual(value, "Completed");
        }

        [TestMethod]
        public void HandleDiscoveryCompleteMetricsShouldNotAddMetricIfInputIsEmpty()
        {
            var dict = new Dictionary<string, string>();
            TelemetryClient.HandleDiscoveryCompleteMetrics(dict);

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 0);
        }

        [TestMethod]
        public void HandleTestRunCompleteMetricsShouldNotAddMetricIfInputIsEmpty()
        {
            var dict = new Dictionary<string, string>();
            TelemetryClient.HandleTestRunCompleteMetrics(dict);

            Assert.AreEqual(TelemetryClient.GetMetrics().Count, 0);
        }

        [TestMethod]
        public void HandleTestRunCompleteMetricsShouldAddTotalTestsRun()
        {
            var dict = new Dictionary<string, string>();
            dict.Add(UnitTestTelemetryDataConstants.TotalTestsRanByAdapter, "2");

            TelemetryClient.HandleTestRunCompleteMetrics(dict);
            TelemetryClient.AddTestRunCompleteMetrics();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.TotalTestsRanByAdapter, out value), true);
            Assert.AreEqual(value, "2");
        }

        [TestMethod]
        public void HandleTestRunCompleteMetricsShouldAddRunState()
        {
            var dict = new Dictionary<string, string>();
            dict.Add(UnitTestTelemetryDataConstants.RunState, "Pending");

            TelemetryClient.HandleTestRunCompleteMetrics(dict);
            TelemetryClient.AddTestRunCompleteMetrics();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.RunState, out value), true);
            Assert.AreEqual(value, "Pending");
        }

        [TestMethod]
        public void HandleTestRunCompleteMetricsShouldAddTimeTakenToRunTests()
        {
            var dict = new Dictionary<string, string>();
            dict.Add(UnitTestTelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, ".02091");

            TelemetryClient.HandleTestRunCompleteMetrics(dict);
            TelemetryClient.AddTestRunCompleteMetrics();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.TimeTakenToRunTestsByAnAdapter, out value), true);
            Assert.AreEqual(value, .02091.ToString());
        }

        [TestMethod]
        public void HandleExecutionEngineTimeShouldAddMaxEngineTime()
        {
            TelemetryClient.HandleExecutionEngineTime(0.2901);
            TelemetryClient.HandleExecutionEngineTime(1.9501);

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.TimeTakenToStartExecutionEngineExe, out value), true);
            Assert.AreEqual(value, 1.9501.ToString());
        }

        [TestMethod]
        public void HandleTestRunCompleteMetricsShouldAddTimeTakenByAllAdapters()
        {
            var dict = new Dictionary<string, string>();
            dict.Add(UnitTestTelemetryDataConstants.TimeTakenByAllAdaptersInSec, ".02091");

            TelemetryClient.HandleTestRunCompleteMetrics(dict);
            TelemetryClient.AddTestRunCompleteMetrics();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.TimeTakenByAllAdaptersInSec, out value), true);
            Assert.AreEqual(value, .02091.ToString());
        }
    }
}
