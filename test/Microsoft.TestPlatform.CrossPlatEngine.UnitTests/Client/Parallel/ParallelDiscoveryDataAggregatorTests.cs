// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client.Parallel
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ParallelDiscoveryDataAggregatorTests
    {
        [TestMethod]
        public void AggregateShouldAggregateAbortedCorrectly()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.IsFalse(aggregator.IsAborted, "Aborted must be false");

            aggregator.Aggregate(totalTests: 5, isAborted: true);
            Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");
        }

        [TestMethod]
        public void AggregateShouldAggregateTotalTestsCorrectly()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();
            aggregator.Aggregate(totalTests: 2, isAborted: false);
            Assert.AreEqual(2, aggregator.TotalTests, "Aggregated totalTests count does not match");

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.AreEqual(7, aggregator.TotalTests, "Aggregated totalTests count does not match");

            aggregator.Aggregate(totalTests: 3, isAborted: false);
            Assert.AreEqual(10, aggregator.TotalTests, "Aggregated totalTests count does not match");
        }
    }
}
