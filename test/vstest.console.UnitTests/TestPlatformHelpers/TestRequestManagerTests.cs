// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace vstest.console.UnitTests.TestPlatformHelpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System.Runtime.Versioning;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;

    using Moq;

    using vstest.console.UnitTests.TestDoubles;

    [TestClass]
    public class TestRequestManagerTests
    {
        private DummyLoggerEvents mockLoggerEvents;
        private TestLoggerManager mockLoggerManager;
        private CommandLineOptions commandLineOptions;
        private Mock<ITestPlatform> mockTestPlatform;
        private Mock<IDiscoveryRequest> mockDiscoveryRequest;
        private Mock<ITestRunRequest> mockRunRequest;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private InferHelper inferHelper;
        private ITestRequestManager testRequestManager;
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private ProtocolConfig protocolConfig;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;

        private const string DefaultRunsettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>";

        public TestRequestManagerTests()
        {
            this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
            this.mockLoggerManager = new DummyTestLoggerManager(this.mockLoggerEvents);
            this.commandLineOptions = new DummyCommandLineOptions();
            this.mockTestPlatform = new Mock<ITestPlatform>();
            this.mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.protocolConfig = new ProtocolConfig();
            this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
            this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);
            var testRunResultAggregator = new DummyTestRunResultAggregator();

            this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
            this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
            this.testRequestManager = new TestRequestManager(
                this.commandLineOptions,
                this.mockTestPlatform.Object,
                this.mockLoggerManager,
                testRunResultAggregator,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>()))
                .Returns(this.mockDiscoveryRequest.Object);
            this.mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>()))
                .Returns(this.mockRunRequest.Object);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.X86);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework40));
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
                new Mock<ITestPlatformEventSource>().Object,
                this.inferHelper,
            this.mockMetricsPublisherTask);

            Assert.IsTrue(this.mockLoggerEvents.EventsSubscribed());
        }

        [TestMethod]
        public void TestRequestManagerShouldNotInitializeConsoleLoggerIfDesignModeIsSet()
        {
            CommandLineOptions.Instance.IsDesignMode = true;
            this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
            this.mockLoggerManager = new DummyTestLoggerManager(this.mockLoggerEvents);
            var requestManager = new TestRequestManager(CommandLineOptions.Instance,
                new Mock<ITestPlatform>().Object,
                this.mockLoggerManager,
                TestRunResultAggregator.Instance,
                new Mock<ITestPlatformEventSource>().Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            Assert.IsFalse(this.mockLoggerEvents.EventsSubscribed());
        }

        [TestMethod]
        public void InitializeExtensionsShouldCallTestPlatformToClearAndUpdateExtensions()
        {
            var paths = new List<string>() { "a", "b" };
            this.testRequestManager.InitializeExtensions(paths, false);

            this.mockTestPlatform.Verify(mt => mt.ClearExtensions(), Times.Once);
            this.mockTestPlatform.Verify(mt => mt.UpdateExtensions(paths, false), Times.Once);
        }

        [TestMethod]
        public void ResetShouldResetCommandLineOptionsInstance()
        {
            var oldInstance = CommandLineOptions.Instance;
            this.testRequestManager.ResetOptions();

            var newInstance = CommandLineOptions.Instance;

            Assert.AreNotEqual(oldInstance, newInstance, "CommandLineOptions must be cleaned up");
        }

        [TestMethod]
        public void DiscoverTestsShouldReadTheBatchSizeFromSettingsAndSetItForDiscoveryCriteria()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings =
                 @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BatchSize>15</BatchSize>
                     </RunConfiguration>
                </RunSettings>"
            };

            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                {
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);
            Assert.AreEqual(15, actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallTestPlatformAndSucceed()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var createDiscoveryRequestCalled = 0;
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                {
                    createDiscoveryRequestCalled++;
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            string testCaseFilterValue = "TestFilter";
            CommandLineOptions.Instance.TestCaseFilterValue = testCaseFilterValue;
            this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
            this.mockMetricsPublisherTask);

            var success = this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, this.protocolConfig);

            Assert.IsTrue(success, "DiscoverTests call must succeed");
            Assert.AreEqual(testCaseFilterValue, actualDiscoveryCriteria.TestCaseFilter, "TestCaseFilter must be set");

            Assert.AreEqual(createDiscoveryRequestCalled, 1, "CreateDiscoveryRequest must be invoked only once.");
            Assert.AreEqual(2, actualDiscoveryCriteria.Sources.Count(), "All Sources must be used for discovery request");
            Assert.AreEqual("a", actualDiscoveryCriteria.Sources.First(), "First Source in list is incorrect");
            Assert.AreEqual("b", actualDiscoveryCriteria.Sources.ElementAt(1), "Second Source in list is incorrect");

            // Default frequency is set to 10, unless specified in runsettings.
            Assert.AreEqual(10, actualDiscoveryCriteria.FrequencyOfDiscoveredTestsEvent);

            mockDiscoveryRegistrar.Verify(md => md.RegisterDiscoveryEvents(It.IsAny<IDiscoveryRequest>()), Times.Once);
            mockDiscoveryRegistrar.Verify(md => md.UnregisterDiscoveryEvents(It.IsAny<IDiscoveryRequest>()), Times.Once);

            mockDiscoveryRequest.Verify(md => md.DiscoverAsync(), Times.Once);

            mockTestPlatformEventSource.Verify(mt => mt.DiscoveryRequestStart(), Times.Once);
            mockTestPlatformEventSource.Verify(mt => mt.DiscoveryRequestStop(), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldPassSameProtocolConfigInRequestData()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            string testCaseFilterValue = "TestFilter";
            CommandLineOptions.Instance.TestCaseFilterValue = testCaseFilterValue;
            this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
            this.mockMetricsPublisherTask);

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            Assert.AreEqual(4, actualRequestData.ProtocolConfig.Version);
        }


        [TestMethod]
        public void DiscoverTestsShouldCollectMetrics()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
                              {
                                  Sources = new List<string>() { "a", "b" },
                                  RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <MaxCpuCount>2</MaxCpuCount>
                                    <TargetPlatform>x86</TargetPlatform>
                                    <TargetFrameworkVersion>Framework35</TargetFrameworkVersion>
                                </RunConfiguration>
                                <MSPhoneTest>
                                  <TargetDevice>169.254.193.190</TargetDevice>
                                </MSPhoneTest>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);


            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            object targetDevice;
            object maxcount;
            object targetPlatform;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.MaxCPUcount, out maxcount));
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetPlatform, out targetPlatform));
            Assert.AreEqual("Other", targetDevice);
            Assert.AreEqual(2, maxcount);
            Assert.AreEqual("X86", targetPlatform.ToString());

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectTargetDeviceLocalMachineIfTargetDeviceStringisEmpty()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice></TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);


            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            object targetDevice;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
            Assert.AreEqual("Local Machine", targetDevice);

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectTargetDeviceIfTargetDeviceIsDevice()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Device</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);


            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            object targetDevice;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
            Assert.AreEqual("Device", targetDevice);

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectTargetDeviceIfTargetDeviceIsEmulator()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Emulator 8.1 U1 WVGA 4 inch 512MB</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);


            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            object targetDevice;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
            Assert.AreEqual("Emulator 8.1 U1 WVGA 4 inch 512MB", targetDevice);

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectCommands()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Device</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            CommandLineOptions.Instance.Parallel = true;
            CommandLineOptions.Instance.EnableCodeCoverage = true;
            CommandLineOptions.Instance.InIsolation = true;
            CommandLineOptions.Instance.UseVsixExtensions = true;
            CommandLineOptions.Instance.SettingsFile = @"c://temp/.runsettings";

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify
            object commandLineSwitches;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

            var commandLineArray = commandLineSwitches.ToString();

            Assert.IsTrue(commandLineArray.Contains("/Parallel"));
            Assert.IsTrue(commandLineArray.Contains("/EnableCodeCoverage"));
            Assert.IsTrue(commandLineArray.Contains("/InIsolation"));
            Assert.IsTrue(commandLineArray.Contains("/UseVsixExtensions"));
            Assert.IsTrue(commandLineArray.Contains("/settings//.RunSettings"));

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectTestSettings()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Device</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            CommandLineOptions.Instance.SettingsFile = @"c://temp/.testsettings";

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify
            object commandLineSwitches;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

            var commandLineArray = commandLineSwitches.ToString();

            Assert.IsTrue(commandLineArray.Contains("/settings//.TestSettings"));

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectVsmdiFile()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Device</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            CommandLineOptions.Instance.SettingsFile = @"c://temp/.vsmdi";

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify
            object commandLineSwitches;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

            var commandLineArray = commandLineSwitches.ToString();

            Assert.IsTrue(commandLineArray.Contains("/settings//.vsmdi"));

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectTestRunConfigFile()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <TargetDevice>Device</TargetDevice>
                                </RunConfiguration>
                            </RunSettings>"
            };

            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            CommandLineOptions.Instance.SettingsFile = @"c://temp/.testrunConfig";

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify
            object commandLineSwitches;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

            var commandLineArray = commandLineSwitches.ToString();

            Assert.IsTrue(commandLineArray.Contains("/settings//.testrunConfig"));

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void DiscoverTestsShouldUpdateFrameworkAndPlatformIfNotSpecifiedInDesignMode()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };
            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                {
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()));
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()));

            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateFrameworkAndPlatformIfSpecifiedInDesignMode()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     <TargetFrameworkVersion>{Constants.DotNetFramework46}</TargetFrameworkVersion>
                     <TargetPlatform>{Architecture.ARM.ToString()}</TargetPlatform>
                     </RunConfiguration>
                </RunSettings>"
            };
            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.X86);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework451));
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                {
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()), Times.Never);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()), Times.Never);

            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void DiscoverTestsShouldUpdateFrameworkAndPlatformInCommandLineScenariosIfNotSpecified()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };
            this.commandLineOptions.IsDesignMode = false;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>())).Callback(
                (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                {
                    actualDiscoveryCriteria = discoveryCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()));
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()));

            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualDiscoveryCriteria.RunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateFrameworkAndPlatformInCommandLineScenariosIfSpecifiedButInferred()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };
            this.commandLineOptions.IsDesignMode = false;
            this.commandLineOptions.TargetFrameworkVersion = Framework.DefaultFramework;
            this.commandLineOptions.TargetArchitecture = Architecture.X86;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            DiscoveryCriteria actualDiscoveryCriteria = null;
            var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
            this.mockTestPlatform
                .Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>()))
                .Callback(
                    (IRequestData requestData, DiscoveryCriteria discoveryCriteria) =>
                    {
                        actualDiscoveryCriteria = discoveryCriteria;
                    }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.DiscoverTests(payload,
                new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()), Times.Once);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()), Times.Once);

            Assert.IsFalse(actualDiscoveryCriteria.RunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsFalse(actualDiscoveryCriteria.RunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void DiscoverTestsShouldPublishMetrics()
        {
            var payload = new DiscoveryRequestPayload()
            {
                Sources = new List<string>() { "a", "b" }
            };
            var mockProtocolConfig = new ProtocolConfig { Version = 2 };
            var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

            // Act
            this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

            // Verify.
            this.mockMetricsPublisher.Verify(mp => mp.PublishMetrics(TelemetryDataConstants.TestDiscoveryCompleteEvent, It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [TestMethod]
        public void CancelTestRunShouldWaitForCreateTestRunRequest()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            bool createTestRunRequestCalled = false;
            bool cancelCalledPostTestRunRequest = false;

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
               (IRequestData requestData, TestRunCriteria testRunCriteria) =>
               {
                   createTestRunRequestCalled = true;
               }).Returns(mockRunRequest.Object);

            // Run request should not complete before the abort
            mockRunRequest.Setup(mr => mr.WaitForCompletion(It.IsAny<int>())).Callback(() => { Thread.Sleep(20); });

            mockRunRequest.Setup(mr => mr.CancelAsync()).Callback(() =>
            {
                cancelCalledPostTestRunRequest = createTestRunRequestCalled;
            });

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var cancelTask = Task.Run(() => this.testRequestManager.CancelTestRun());
            var runTask = Task.Run(() => this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig));

            Task.WaitAll(cancelTask, runTask);

            Assert.IsTrue(cancelCalledPostTestRunRequest, "CancelRequest must execute after create run request");
        }

        [TestMethod]
        public void CancelShouldNotThrowExceptionIfTestRunRequestHasBeenDisposed()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);
            this.testRequestManager.CancelTestRun();
        }

        [TestMethod]
        public void AbortShouldNotThrowExceptionIfTestRunRequestHasBeenDisposed()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);
            this.testRequestManager.AbortTestRun();
        }

        [TestMethod]
        public void AbortTestRunShouldWaitForCreateTestRunRequest()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };
            
            bool createTestRunRequestCalled = false;
            bool abortCalledPostTestRunRequest = false;

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria testRunCriteria) =>
                {
                    createTestRunRequestCalled = true;
                }).Returns(mockRunRequest.Object);

            // Run request should not complete before the abort
            mockRunRequest.Setup(mr => mr.WaitForCompletion(It.IsAny<int>())).Callback(() => { Thread.Sleep(20); });

            mockRunRequest.Setup(mr => mr.Abort()).Callback(() =>
            {
                abortCalledPostTestRunRequest = createTestRunRequestCalled;
            });

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var cancelTask = Task.Run(() => this.testRequestManager.AbortTestRun());
            var runTask = Task.Run(() => this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig));

            Task.WaitAll(cancelTask, runTask);

            Assert.IsTrue(abortCalledPostTestRunRequest, "Abort Request must execute after create run request");
        }

        [TestMethod]
        public void RunTestsShouldReadTheBatchSizeFromSettingsAndSetItForTestRunCriteria()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings =
                 @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BatchSize>15</BatchSize>
                     </RunConfiguration>
                </RunSettings>"
            };

            TestRunCriteria actualTestRunCriteria = null;
            var mockDiscoveryRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockDiscoveryRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);
            Assert.AreEqual(15, actualTestRunCriteria.FrequencyOfRunStatsChangeEvent);
        }

        [TestMethod]
        public void RunTestsShouldPassSameProtocolConfigInRequestData()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings = DefaultRunsettings
            };
            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                    {
                        actualRequestData = requestData;
                    }).Returns(mockDiscoveryRequest.Object);

            // Act.
            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

            // Verify.
            Assert.AreEqual(4, actualRequestData.ProtocolConfig.Version);
        }

        [TestMethod]
        public void RunTestsShouldCollectCommands()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new TestRunRequestPayload()
                              {
                                  Sources = new List<string>() { "a" },
                                  RunSettings = DefaultRunsettings
                              };
            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                    {
                        actualRequestData = requestData;
                    }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            CommandLineOptions.Instance.Parallel = true;
            CommandLineOptions.Instance.EnableCodeCoverage = true;
            CommandLineOptions.Instance.InIsolation = true;
            CommandLineOptions.Instance.UseVsixExtensions = true;
            CommandLineOptions.Instance.SettingsFile = @"c://temp/.runsettings";

            // Act.
            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

            // Verify
            object commandLineSwitches;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

            var commandLineArray = commandLineSwitches.ToString();

            Assert.IsTrue(commandLineArray.Contains("/Parallel"));
            Assert.IsTrue(commandLineArray.Contains("/EnableCodeCoverage"));
            Assert.IsTrue(commandLineArray.Contains("/InIsolation"));
            Assert.IsTrue(commandLineArray.Contains("/UseVsixExtensions"));
            Assert.IsTrue(commandLineArray.Contains("/settings//.RunSettings"));

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void RunTestsShouldCollectMetrics()
        {
            // Opt in the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings = @"<RunSettings>
                                <RunConfiguration>
                                    <MaxCpuCount>2</MaxCpuCount>
                                    <TargetPlatform>x86</TargetPlatform>
                                    <TargetFrameworkVersion>Framework35</TargetFrameworkVersion>
                                </RunConfiguration>
                                <MSPhoneTest>
                                  <TargetDevice>169.254.193.190</TargetDevice>
                                </MSPhoneTest>
                            </RunSettings>"
            };
            var mockProtocolConfig = new ProtocolConfig { Version = 4 };
            IRequestData actualRequestData = null;
            var mockDiscoveryRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualRequestData = requestData;
                }).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            // Act.
            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

            // Verify
            // Verify.
            object targetDevice;
            object maxcount;
            object targetPlatform;
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.MaxCPUcount, out maxcount));
            Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetPlatform, out targetPlatform));
            Assert.AreEqual("Other", targetDevice);
            Assert.AreEqual(2, maxcount);
            Assert.AreEqual("X86", targetPlatform.ToString());

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallTestPlatformAndSucceed()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            string testCaseFilterValue = "TestFilter";
            payload.TestPlatformOptions = new TestPlatformOptions { TestCaseFilter = testCaseFilterValue };
            this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestLoggerManager.Instance,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask);

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

            Assert.IsTrue(success, "RunTests call must succeed");

            Assert.AreEqual(testCaseFilterValue, observedCriteria.TestCaseFilter, "TestCaseFilter must be set");

            Assert.AreEqual(createRunRequestCalled, 1, "CreateRunRequest must be invoked only once.");
            Assert.AreEqual(2, observedCriteria.Sources.Count(), "All Sources must be used for discovery request");
            Assert.AreEqual("a", observedCriteria.Sources.First(), "First Source in list is incorrect");
            Assert.AreEqual("b", observedCriteria.Sources.ElementAt(1), "Second Source in list is incorrect");

            // Check for the default value for the frequency
            Assert.AreEqual(10, observedCriteria.FrequencyOfRunStatsChangeEvent);
            mockRunEventsRegistrar.Verify(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>()), Times.Once);
            mockRunEventsRegistrar.Verify(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>()), Times.Once);

            mockRunRequest.Verify(md => md.ExecuteAsync(), Times.Once);

            mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStart(), Times.Once);
            mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStop(), Times.Once);
        }

        [TestMethod]
        public void RunTestsMultipleCallsShouldNotRunInParallel()
        {
            var payload1 = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a" },
                RunSettings = DefaultRunsettings
            };

            var payload2 = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "b" },
                RunSettings = DefaultRunsettings
            };

            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>()))
                .Returns(mockRunRequest.Object);

            var mockRunEventsRegistrar1 = new Mock<ITestRunEventsRegistrar>();
            var mockRunEventsRegistrar2 = new Mock<ITestRunEventsRegistrar>();

            // Setup the second one to wait
            var sw = new Stopwatch();
            sw.Start();

            long run1Start = 0;
            long run1Stop = 0;
            long run2Start = 0;
            long run2Stop = 0;
            mockRunEventsRegistrar1.Setup(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run1Start = sw.ElapsedMilliseconds;
                Thread.Sleep(1);
            });
            mockRunEventsRegistrar1.Setup(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run1Stop = sw.ElapsedMilliseconds;
                Thread.Sleep(10);
            });

            mockRunEventsRegistrar2.Setup(md => md.RegisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run2Start = sw.ElapsedMilliseconds;
                Thread.Sleep(10);
            });
            mockRunEventsRegistrar2.Setup(md => md.UnregisterTestRunEvents(It.IsAny<ITestRunRequest>())).Callback(() =>
            {
                Thread.Sleep(10);
                run2Stop = sw.ElapsedMilliseconds;
            });

            var mockCustomlauncher = new Mock<ITestHostLauncher>();
            var task1 = Task.Run(() =>
            {
                this.testRequestManager.RunTests(payload1, mockCustomlauncher.Object, mockRunEventsRegistrar1.Object, this.protocolConfig);
            });
            var task2 = Task.Run(() =>
            {
                this.testRequestManager.RunTests(payload2, mockCustomlauncher.Object, mockRunEventsRegistrar2.Object, this.protocolConfig);
            });

            Task.WaitAll(task1, task2);

            if (run1Start < run2Start)
            {
                Assert.IsTrue((run2Stop > run2Start)
                    && (run2Start > run1Stop)
                    && (run1Stop > run1Start));
            }
            else
            {
                Assert.IsTrue((run1Stop > run1Start)
                    && (run1Start > run2Stop)
                    && (run2Stop > run2Start));
            }
        }

        [TestMethod]
        public void RunTestsShouldPublishMetrics()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

            this.mockMetricsPublisher.Verify(mp => mp.PublishMetrics(TelemetryDataConstants.TestExecutionCompleteEvent, It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsIfThrowsTestPlatformExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new TestPlatformException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        public void RunTestsIfThrowsSettingsExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new SettingsException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        public void RunTestsIfThrowsInvalidOperationExceptionShouldNotThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll", "b.dll" },
                RunSettings = DefaultRunsettings
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new InvalidOperationException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            var success = this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

            Assert.IsFalse(success, "RunTests call must fail due to exception");
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void RunTestsIfThrowsExceptionShouldThrowOut()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a", "b" },
                RunSettings = DefaultRunsettings
            };

            var createRunRequestCalled = 0;
            TestRunCriteria observedCriteria = null;
            var mockRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    createRunRequestCalled++;
                    observedCriteria = runCriteria;
                }).Returns(mockRunRequest.Object);

            mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(new NotImplementedException("HelloWorld"));

            var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
            var mockCustomlauncher = new Mock<ITestHostLauncher>();

            this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DiscoverTestsShouldUpdateDesignModeAndCollectSourceInformation(bool designModeValue)
        {
            var runsettings = "<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);
            this.commandLineOptions.IsDesignMode = designModeValue;

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

            var designmode = $"<DesignMode>{designModeValue}</DesignMode>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode))));

            var collectSourceInformation = $"<CollectSourceInformation>{designModeValue}</CollectSourceInformation>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(collectSourceInformation))));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateDesignModeIfUserHasSetDesignModeInRunSettings()
        {
            var runsettings = "<RunSettings><RunConfiguration><DesignMode>False</DesignMode><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);
            this.commandLineOptions.IsDesignMode = true;

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

            var designmode = "<DesignMode>False</DesignMode>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode))));
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
                Sources = new List<string> { "c:\\testproject.dll" }
            };
            this.commandLineOptions.IsDesignMode = designModeValue;

            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            var designmode = $"<DesignMode>{designModeValue}</DesignMode>";
            this.mockTestPlatform.Verify(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.Is<TestRunCriteria>(rc => rc.TestRunSettings.Contains(designmode))));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DiscoverTestsShouldNotUpdateCollectSourceInformationIfUserHasSetItInRunSettings(bool val)
        {
            var runsettings = $"<RunSettings><RunConfiguration><CollectSourceInformation>{val}</CollectSourceInformation></RunConfiguration></RunSettings>";
            var discoveryPayload = CreateDiscoveryPayload(runsettings);

            this.testRequestManager.DiscoverTests(discoveryPayload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

            var collectSourceInformation = $"<CollectSourceInformation>{val}</CollectSourceInformation>";
            this.mockTestPlatform.Verify(
                tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(collectSourceInformation))));
        }

        [TestMethod]
        public void RunTestsShouldShouldUpdateFrameworkAndPlatformIfNotSpecifiedInDesignMode()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };

            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                                .Returns(new FrameworkName(Constants.DotNetFramework46));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()));
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()));

            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Architecture.ARM.ToString()));

        }

        [TestMethod]
        public void RunTestsShouldNotUpdateFrameworkAndPlatformIfSpecifiedInDesignModeButInferred()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                         <TargetFrameworkVersion>{Constants.DotNetFramework46}</TargetFrameworkVersion>
                         <TargetPlatform>{Architecture.ARM.ToString()}</TargetPlatform>
                     </RunConfiguration>
                </RunSettings>"
            };

            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.X86);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework451));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()), Times.Once);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()), Times.Once);

            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        [DataRow("x86")]
        [DataRow("X86")]
        [DataRow("ARM")]
        [DataRow("aRm")]
        public void RunTestsShouldNotUpdatePlatformIfSpecifiedInDesignModeButInferred(string targetPlatform)
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                         <TargetPlatform>{targetPlatform}</TargetPlatform>
                     </RunConfiguration>
                </RunSettings>"
            };

            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.X86);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework451));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()), Times.Once);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()), Times.Once);

            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(targetPlatform));
        }

        [TestMethod]
        public void RunTestsShouldUpdateFrameworkAndPlatformInCommandLineScenarios()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };

            this.commandLineOptions.IsDesignMode = false;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()));
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()));

            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void RunTestsShouldNotpdateFrameworkAndPlatformInCommandLineScenariosIfSpecifiedButInferred()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };

            this.commandLineOptions.IsDesignMode = false;
            this.commandLineOptions.TargetArchitecture = Architecture.X86;
            this.commandLineOptions.TargetFrameworkVersion = Framework.DefaultFramework;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()), Times.Once);
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()), Times.Once);

            Assert.IsFalse(actualTestRunCriteria.TestRunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsFalse(actualTestRunCriteria.TestRunSettings.Contains(Architecture.ARM.ToString()));
        }

        [TestMethod]
        public void RunTestsWithTestCasesShouldUpdateFrameworkAndPlatformIfNotSpecifiedInDesignMode()
        {
            var actualSources = new List<string>() { "1.dll", "2.dll" };
            var payload = new TestRunRequestPayload()
            {
                TestCases = new List<TestCase>() {
                    new TestCase(){Source = actualSources[0]},
                    new TestCase() { Source = actualSources[0]},
                    new TestCase() { Source = actualSources[1] }
                },
                RunSettings =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                     </RunConfiguration>
                </RunSettings>"
            };

            List<string> archSources = new List<string>(), fxSources = new List<string>();

            this.commandLineOptions.IsDesignMode = true;
            this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>())).Callback<string>(source => archSources.Add(source))
                .Returns(Architecture.ARM);
            this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>())).Callback<string>(source => fxSources.Add(source))
                .Returns(new FrameworkName(Constants.DotNetFramework46));
            TestRunCriteria actualTestRunCriteria = null;
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Callback(
                (IRequestData requestData, TestRunCriteria runCriteria) =>
                {
                    actualTestRunCriteria = runCriteria;
                }).Returns(mockTestRunRequest.Object);

            var success = this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

            this.mockAssemblyMetadataProvider.Verify(a => a.GetArchitecture(It.IsAny<string>()));
            this.mockAssemblyMetadataProvider.Verify(a => a.GetFrameWork(It.IsAny<string>()));

            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Constants.DotNetFramework46));
            Assert.IsTrue(actualTestRunCriteria.TestRunSettings.Contains(Architecture.ARM.ToString()));
            CollectionAssert.AreEqual(actualSources, archSources);
            CollectionAssert.AreEqual(actualSources, fxSources);
        }

        [TestMethod]
        public void RunTestShouldThrowExceptionIfRunSettingWithDCHasTestSettingsInIt()
        {
            var settingXml = @"<RunSettings>
                                    <MSTest>
                                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                                        <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <DataCollectionRunSettings>
                                        <DataCollectors>
                                            <DataCollector friendlyName=""DummyDataCollector1"">
                                            </DataCollector>
                                            <DataCollector friendlyName=""DummyDataCollector2"">
                                            </DataCollector>
                                        </DataCollectors>
                                    </DataCollectionRunSettings>
                                </RunSettings>";

            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings = settingXml
            };

            this.commandLineOptions.EnableCodeCoverage = false;
            bool exceptionThrown = false;

            try
            {
                this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);
            }
            catch (SettingsException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.Contains(@"<SettingsFile>C:\temp.testsettings</SettingsFile>"), ex.Message);
            }

            Assert.IsTrue(exceptionThrown, "Initialize should throw exception");
        }

        [TestMethod]
        public void RunTestShouldThrowExceptionIfRunSettingWithDCHasTestSettingsAndEnableCodeCoverageTrue()
        {
            var settingXml = @"<RunSettings>
                                    <MSTest>
                                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                                        <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <DataCollectionRunSettings>
                                        <DataCollectors>
                                            <DataCollector friendlyName=""DummyDataCollector1"">
                                            </DataCollector>
                                            <DataCollector friendlyName=""DummyDataCollector2"">
                                            </DataCollector>
                                        </DataCollectors>
                                    </DataCollectionRunSettings>
                                </RunSettings>";

            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings = settingXml
            };

            this.commandLineOptions.EnableCodeCoverage = true;
            bool exceptionThrown = false;

            try
            {
                this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);
            }
            catch (SettingsException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.Contains(@"<SettingsFile>C:\temp.testsettings</SettingsFile>"), ex.Message);
            }

            Assert.IsTrue(exceptionThrown, "Initialize should throw exception");
        }

        [TestMethod]
        public void RunTestShouldNotThrowExceptionIfRunSettingHasCodeCoverageDCAndTestSettingsInItWithEnableCoverageTrue()
        {
            var payload = new TestRunRequestPayload()
            {
                Sources = new List<string>() { "a.dll" },
                RunSettings = @"<RunSettings>
                                    <MSTest>
                                        <SettingsFile>C:\temp.testsettings</SettingsFile>
                                        <ForcedLegacyMode>true</ForcedLegacyMode>
                                    </MSTest>
                                    <DataCollectionRunSettings>
                                        <DataCollectors>
                                            <DataCollector friendlyName=""Code Coverage"">
                                            </DataCollector>
                                        </DataCollectors>
                                    </DataCollectionRunSettings>
                                </RunSettings>"
            };

            this.commandLineOptions.EnableCodeCoverage = true;
            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);
        }


        private static DiscoveryRequestPayload CreateDiscoveryPayload(string runsettings)
        {
            var discoveryPayload = new DiscoveryRequestPayload
            {
                RunSettings = runsettings,
                Sources = new[] { "c:\\testproject.dll" }
            };
            return discoveryPayload;
        }
    }
}