// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

    using Moq;
    using vstest.console.UnitTests.TestDoubles;

    [TestClass]
    public class TestRequestManagerTests
    {
        private DummyLoggerEvents mockLoggerEvents;
        private TestLoggerManager mockLoggerManager;

        public TestRequestManagerTests()
        {
            this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
            this.mockLoggerManager = new DummyTestLoggerManager(this.mockLoggerEvents);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void TestRequestManagerShouldInitializeConsoleLogger()
        {
            CommandLineOptions.Instance.IsDesignMode = false;
            var requestManager = new TestRequestManager(CommandLineOptions.Instance,
                new Mock<ITestPlatform>().Object,
                this.mockLoggerManager,
                TestRunResultAggregator.Instance,
                new Mock<ITestPlatformEventSource>().Object);

            Assert.IsTrue(mockLoggerEvents.EventsSubscribed());
        }

        [TestMethod]
        public void TestRequestManagerShouldNotInitializeConsoleLoggerIfDesignModeIsSet()
        {
            CommandLineOptions.Instance.IsDesignMode = true;
            var requestManager = new TestRequestManager(CommandLineOptions.Instance,
                new Mock<ITestPlatform>().Object,
                this.mockLoggerManager,
                TestRunResultAggregator.Instance,
                new Mock<ITestPlatformEventSource>().Object);

            Assert.IsFalse(mockLoggerEvents.EventsSubscribed());
        }

        [TestMethod]
        public void DiscoverTestsShouldHonorStatsConfiguration()
        {
            // Arrange.
            long batchSize = 512;
            var testStatsTimeout = new TimeSpan(0, 0, 0, 59, 0);

            CommandLineOptions.Instance.BatchSize = batchSize;
            CommandLineOptions.Instance.TestStatsEventTimeout = testStatsTimeout;

            DiscoveryCriteria receivedCriteria = null;
            var mockTestPlatform = new Mock<ITestPlatform>();

            // Setup.
            mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>()))
                .Returns(new Mock<IDiscoveryRequest>().Object)
                .Callback((DiscoveryCriteria criteria) => { receivedCriteria = criteria; });

            var requestManager = new TestRequestManager(CommandLineOptions.Instance,
                mockTestPlatform.Object,
                this.mockLoggerManager,
                TestRunResultAggregator.Instance,
                new Mock<ITestPlatformEventSource>().Object);

            // Act.
            requestManager.DiscoverTests(new DiscoveryRequestPayload() { Sources = new List<string> { "C:\\somerandomfile.foo" } }, new Mock<ITestDiscoveryEventsRegistrar>().Object);

            // Assert.
            Assert.IsNotNull(receivedCriteria);
            Assert.AreEqual(batchSize, receivedCriteria.FrequencyOfDiscoveredTestsEvent);
            Assert.AreEqual(testStatsTimeout, receivedCriteria.DiscoveredTestEventTimeout);
        }
    }
}
