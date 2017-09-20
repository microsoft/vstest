// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestDataTests
    {
        [TestMethod]
        public void RequestDataShouldReturnValidMetricsCollector()
        {
            var requestData = new RequestData();
            var metricsCollection = new MetricsCollection();
            requestData.MetricsCollection = metricsCollection;

            Assert.AreEqual(metricsCollection, requestData.MetricsCollection);
        }

        [TestMethod]
        public void RequestDataShouldReturnValidProtocolConfig()
        {
            var requestData = new RequestData();
            requestData.ProtocolConfig = new ProtocolConfig { Version = 2 };

            Assert.AreEqual(2, requestData.ProtocolConfig.Version);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RequestDataShouldThrowArgumentNullExpectionOnNullMetricsCollection()
        {
            var requestData = new RequestData();
            requestData.MetricsCollection = null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RequestDataShouldThrowArgumentNullExpectionOnNullProtocolConfig()
        {
            var requestData = new RequestData();
            requestData.ProtocolConfig = null;
        }

        [TestMethod]
        public void RequestDataShouldReturnIsTelemetryOptedOutTrueIfTelemetryOptedOut()
        {
            var requestData = new RequestData();
            requestData.IsTelemetryOptedOut = true;

            Assert.AreEqual(true, requestData.IsTelemetryOptedOut);
        }

        [TestMethod]
        public void RequestDataShouldReturnIsTelemetryOptedOutFalseIfTelemetryOptedIn()
        {
            var requestData = new RequestData();
            requestData.IsTelemetryOptedOut = false;

            Assert.AreEqual(false, requestData.IsTelemetryOptedOut);
        }
    }
}
