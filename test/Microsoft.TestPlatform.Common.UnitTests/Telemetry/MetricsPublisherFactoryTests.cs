// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricsPublisherFactoryTests
    {
        [TestMethod]
        public void GetPublisherCollectionShouldReturnsNoOpMetricsPublisherIfTelemetryOptedOut()
        {
            var metricsPublisher = new MetricsPublisherFactory(true);

            Assert.IsTrue(metricsPublisher.GetMetricsPublisher() is NoOpMetricsPublisher);
        }

        [TestMethod]
        public void GetPublisherCollectionShoulReturnMetricsPublisherIfTelemetryOptedIn()
        {
            var metricsPublisher = new MetricsPublisherFactory(false);

            Assert.IsTrue(metricsPublisher.GetMetricsPublisher() is MetricsPublisher);
        }
    }
}
