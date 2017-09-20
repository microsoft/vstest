// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricsCollectionFactoryTests
    {
        [TestMethod]
        public void GetMetricsCollectionShouldReturnsNoOpMetricsCollectionIfTelemetryOptedOut()
        {
            var metricsCollection = new MetricsCollectionFactory(true);

            Assert.IsTrue(metricsCollection.GetMetricsCollection() is NoOpMetricsCollection);
        }

        [TestMethod]
        public void GetMetricsCollectionShoulReturnMetricsCollectionIfTelemetryOptedIn()
        {
            var metricsCollection = new MetricsCollectionFactory(false);

            Assert.IsTrue(metricsCollection.GetMetricsCollection() is MetricsCollection);
        }
    }
}
