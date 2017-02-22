// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.TestPlatformHelpers
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using vstest.console.UnitTests.TestDoubles;

    [TestClass]
    public class TestRequestManagerTests
    {
        private DummyLoggerEvents mockLoggerEvents;
        private TestLoggerManager mockLoggerManager;
        private CommandLineOptions commandLineOptions;
        private Mock<ITestPlatform> mockTestPlatform;
        private TestRequestManager testRequestManager;
        private Mock<IDiscoveryRequest> mockDiscoveryRequest;
        private Mock<ITestRunRequest> mockRunRequest;

        public TestRequestManagerTests()
        {
            this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
            this.mockLoggerManager = new DummyTestLoggerManager(this.mockLoggerEvents);
            this.commandLineOptions = new DummyCommandLineOptions();
            this.mockTestPlatform = new Mock<ITestPlatform>();
            this.mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockRunRequest = new Mock<ITestRunRequest>();
            var mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            var testRunResultAggregator = new DummyTestRunResultAggregator();

            this.testRequestManager = new TestRequestManager(this.commandLineOptions, this.mockTestPlatform.Object,
                mockLoggerManager, testRunResultAggregator, mockTestPlatformEventSource.Object);

            this.mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<DiscoveryCriteria>()))
                .Returns(this.mockDiscoveryRequest.Object);
            this.mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>()))
                .Returns(this.mockRunRequest.Object);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DiscoverTestsShouldUpdateDesignMode(bool designModeValue)
        {
            var runsettings = "<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);
            this.commandLineOptions.IsDesignMode = designModeValue;

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object);

            var designmode = $"<DesignMode>{designModeValue}</DesignMode>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode))));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateDesignModeIfUserHasSetDesignModeInRunSettings()
        {
            var runsettings = "<RunSettings><RunConfiguration><DesignMode>False</DesignMode><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);
            this.commandLineOptions.IsDesignMode = true;

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object);

            var designmode = "<DesignMode>False</DesignMode>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode))));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateDesignModeIfTargetFrameworkIsNotSetInRunSettings()
        {
            var runsettings = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object);

            var designmode = "DesignMode";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.Is<DiscoveryCriteria>(dc => !dc.RunSettings.Contains(designmode))));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateDesignModeIfTargetFrameworkIsSetToNetCoreInRunSettings()
        {
            var runsettings = "<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETCoreApp,Version=v1.0</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object);

            var designmode = "DesignMode";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.Is<DiscoveryCriteria>(dc => !dc.RunSettings.Contains(designmode))));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RunTestsShouldUpdateDesignModeIfRunnerIsInDesignMode(bool designModeValue)
        {
            var runsettings =
                "<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var payload = new TestRunRequestPayload
            {
                RunSettings = runsettings,
                Sources = new List<string> {"c:\\testproject.dll"}
            };
            this.commandLineOptions.IsDesignMode = designModeValue;

            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object);

            var designmode = $"<DesignMode>{designModeValue}</DesignMode>";
            this.mockTestPlatform.Verify(tp => tp.CreateTestRunRequest(It.Is<TestRunCriteria>(rc => rc.TestRunSettings.Contains(designmode))));
        }

        private static DiscoveryRequestPayload CreateDiscoveryPayload(string runsettings)
        {
            var discoveryPayload = new DiscoveryRequestPayload
            {
                RunSettings = runsettings,
                Sources = new[] {"c:\\testproject.dll"}
            };
            return discoveryPayload;
        }
    }
}