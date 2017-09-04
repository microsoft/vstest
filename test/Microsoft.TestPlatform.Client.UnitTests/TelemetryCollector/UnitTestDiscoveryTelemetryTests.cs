// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Client.UnitTests.TelemetryCollector
{
    using System.Collections;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.Client.Telemetry;

    [TestClass]
    public class UnitTestDiscoveryTelemetryTests
    {
        private Mock<UnitTestDiscoveryTelemetryCollector> mockUnitTestDiscoveryTelemetryCollector;

        private Hashtable loggedData;

        /// <summary>
        /// The initialize tests.
        /// </summary>
        [TestInitialize]
        public void InitializeTests()
        {
            this.loggedData = new Hashtable();

            this.mockUnitTestDiscoveryTelemetryCollector =
                new Mock<UnitTestDiscoveryTelemetryCollector>();

            var telemetryClient = new TelemetryClient();

            this.mockUnitTestDiscoveryTelemetryCollector.Setup(
                    x => x.LogTelemetryData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>(
                    (property, value) =>
                        {
                            if (!this.loggedData.ContainsKey(property))
                            {
                                this.loggedData[property] = value;
                            }
                        });
        }

        
        [TestCleanup]
        public void TestCleanup()
        {
            TelemetryClient.Dispose();
        }


        [TestMethod]
        public void UnitTestDiscoveryTelemetryCollectorShouldLogTimeTakenForDiscoveryIsLogged()
        {
            this.mockUnitTestDiscoveryTelemetryCollector.Object.Start();
            this.mockUnitTestDiscoveryTelemetryCollector.Object.CollectAndPostTelemetrydata();

            Assert.IsTrue(this.loggedData.ContainsKey(UnitTestTelemetryDataConstants.TimeTakenInSecForDiscovery));
            Assert.AreEqual(
                this.loggedData[UnitTestTelemetryDataConstants.TimeTakenInSecForDiscovery],
                this.mockUnitTestDiscoveryTelemetryCollector.Object.stopwatch.Elapsed.TotalSeconds.ToString());
        }

        [TestMethod]
        public void CheckIfLogAndPostMethodsAreCalled()
        {
            this.mockUnitTestDiscoveryTelemetryCollector.Object.Start();
            this.mockUnitTestDiscoveryTelemetryCollector.Object.CollectAndPostTelemetrydata();

            this.mockUnitTestDiscoveryTelemetryCollector.Verify(x=> x.LogTelemetryData(It.IsAny<string>(),It.IsAny<string>()), Times.AtLeastOnce);
            this.mockUnitTestDiscoveryTelemetryCollector.Verify(x => x.PostTelemetryData(), Times.Once());
        }
    }
}