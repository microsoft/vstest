// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestDataTests
    {
        [TestMethod]
        public void ConstructorShouldThrowIfMetricsCollectionIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new RequestData(null));
        }

        [TestMethod]
        public void RequestDataShouldReturnNonNullInstanceOfMetricsCollector()
        {
            var metricsCollector = new MetricsCollection();
            var requestData = new RequestData(metricsCollector);

            Assert.AreEqual(metricsCollector, requestData.MetricsCollection);
        }
    }
}
