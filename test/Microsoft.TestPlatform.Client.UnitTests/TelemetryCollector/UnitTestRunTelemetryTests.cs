// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace microsoft.testplatform.client.unittests.telemetrycollector
{
    using System.Collections;
    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using Microsoft.VisualStudio.TestPlatform.Client.Telemetry;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System;
    using System.Collections.Generic;

    [TestClass]
    public class UnitTestRunTelemetryTests
    {
        /// <summary>
        /// the mock unit test run telemetry collector.
        /// </summary>
        private Mock<UnitTestRunTelemetryCollector> mockUnitTestRunTelemetryCollector;

        private Mock<IProxyExecutionManager> proxyExecutionManager;

        private TestRunRequest testRunRequest;

        private TestRunCriteria testRunCriteria;

        private BaseTestRunCriteria baseTestRunCriteria;

        private Hashtable loggedData;

        private string testSettings = @"<?xml version='1.0' encoding='utf-8'?>
                                    <RunSettings>
                                        <DataCollectionRunSettings>
                                        <DataCollectors>
                                                    <DataCollector uri='datacollector://microsoft/TestImpact2/1.0' assemblyQualifiedName='Microsoft.VisualStudio.TraceCollector.TestImpactDataCollector, Microsoft.VisualStudio.TraceCollector2, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' friendlyName ='Test Impact' >
                                            </DataCollector>
                                        </DataCollectors >
                                        </DataCollectionRunSettings>
                                        <RunConfiguration>
                                            <MaxCpuCount>0</MaxCpuCount>      
                                            <ResultsDirectory>.\TestResults </ResultsDirectory>            
                                            <TargetPlatform> x64 </TargetPlatform>    
                                            <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion>
                                        </RunConfiguration> 
                                    </RunSettings>";

        /// <summary>
        /// the initialize test.
        /// </summary>
        [TestInitialize]
        public void InitializeTests()
        {
            IEnumerable<string> sources = new[] {"dummy"};
            this.baseTestRunCriteria = new BaseTestRunCriteria(5, false, this.testSettings, TimeSpan.MaxValue);
            this.testRunCriteria = new TestRunCriteria(sources, this.baseTestRunCriteria);
            this.proxyExecutionManager = new Mock<IProxyExecutionManager>();
            this.testRunRequest = new TestRunRequest(this.testRunCriteria, this.proxyExecutionManager.Object);
            this.mockUnitTestRunTelemetryCollector = new Mock<UnitTestRunTelemetryCollector>(this.testRunRequest);

            this.loggedData = new Hashtable();
            var telemetryClient = new TelemetryClient();

            this.mockUnitTestRunTelemetryCollector.Setup(
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
        public void CheckIfLogAndPostMethodsAreCalled()
        {
            this.mockUnitTestRunTelemetryCollector.Object.Start();
            this.mockUnitTestRunTelemetryCollector.Object.CollectAndPostTelemetrydata();

            this.mockUnitTestRunTelemetryCollector.Verify(x => x.LogTelemetryData(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
            this.mockUnitTestRunTelemetryCollector.Verify(x => x.PostTelemetryData(), Times.Once());
        }

        [TestMethod]
        public void CheckIfAbleToFetchParallelEnabled()
        {
            var expected = this.mockUnitTestRunTelemetryCollector.Object.CheckIfTestIsRunningInParallel(
                this.testRunRequest);

            Assert.AreEqual(expected, true);
        }

        [TestMethod]
        public void CheckIfPeakWorkingSetIsLogged()
        {
            this.mockUnitTestRunTelemetryCollector.Object.AddDataPoints();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.PeakWorkingSetForRun, out value), true);
            Assert.AreEqual(value, "8192");
        }

        [TestMethod]
        public void CheckIfDataCollectorsEnabledAreLogged()
        {
            this.mockUnitTestRunTelemetryCollector.Object.AddDataPoints();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.DataCollectorsEnabled, out value), true);
            Assert.AreEqual(value, "datacollector://microsoft/TestImpact2/1.0");
        }

        [TestMethod]
        public void CheckIfNumberOfSourcesAreLogged()
        {
            this.mockUnitTestRunTelemetryCollector.Object.AddDataPoints();

            string value;
            Assert.AreEqual(TelemetryClient.GetMetrics().TryGetValue(UnitTestTelemetryDataConstants.NumberOfSourcesSentForRun,out value),true);
            Assert.AreEqual(value, ((uint)1).ToString());
        }

        [TestMethod]
        public void CheckIfTotalTimeTakenIsLogged()
        {
            this.mockUnitTestRunTelemetryCollector.Object.Start();
            this.mockUnitTestRunTelemetryCollector.Object.CollectAndPostTelemetrydata();

            Assert.IsTrue(this.loggedData.ContainsKey(UnitTestTelemetryDataConstants.TimeTakenInSecForRun));
            Assert.AreEqual(
                this.loggedData[UnitTestTelemetryDataConstants.TimeTakenInSecForRun],
                this.mockUnitTestRunTelemetryCollector.Object.stopwatch.Elapsed.TotalSeconds.ToString());
        }
    }
}
