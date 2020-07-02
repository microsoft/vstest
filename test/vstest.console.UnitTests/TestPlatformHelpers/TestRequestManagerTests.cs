// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.TestPlatformHelpers
{
	using System;
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
	using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using System.Runtime.Versioning;
	using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

	using Moq;

	using vstest.console.UnitTests.TestDoubles;
	using Microsoft.VisualStudio.TestPlatform.Utilities;
	using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    [TestClass]
	public class TestRequestManagerTests
	{
		private DummyLoggerEvents mockLoggerEvents;
		private CommandLineOptions commandLineOptions;
		private Mock<ITestPlatform> mockTestPlatform;
		private Mock<IOutput> mockOutput;
		private Mock<IDiscoveryRequest> mockDiscoveryRequest;
		private Mock<ITestRunRequest> mockRunRequest;
		private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
		private InferHelper inferHelper;
		private ITestRequestManager testRequestManager;
		private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
		private ProtocolConfig protocolConfig;
		private Task<IMetricsPublisher> mockMetricsPublisherTask;
		private Mock<IMetricsPublisher> mockMetricsPublisher;
		private Mock<IProcessHelper> mockProcessHelper;
		private Mock<ITestRunAttachmentsProcessingManager> mockAttachmentsProcessingManager;

		private const string DefaultRunsettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					 </RunConfiguration>
				</RunSettings>";

		public TestRequestManagerTests()
		{
			this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
			this.commandLineOptions = new DummyCommandLineOptions();
			this.mockOutput = new Mock<IOutput>();
			this.mockTestPlatform = new Mock<ITestPlatform>();
			this.mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
			this.protocolConfig = new ProtocolConfig();
			this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
			this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);
			var testRunResultAggregator = new DummyTestRunResultAggregator();
			this.mockProcessHelper = new Mock<IProcessHelper>();

			this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
			this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
			this.mockAttachmentsProcessingManager = new Mock<ITestRunAttachmentsProcessingManager>();
			this.testRequestManager = new TestRequestManager(
				this.commandLineOptions,
				this.mockTestPlatform.Object,
				testRunResultAggregator,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);
			this.mockTestPlatform.Setup(tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Returns(this.mockDiscoveryRequest.Object);
			this.mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Returns(this.mockRunRequest.Object);
			this.mockAssemblyMetadataProvider.Setup(a => a.GetArchitecture(It.IsAny<string>()))
				.Returns(Architecture.X86);
			this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>()))
				.Returns(new FrameworkName(Constants.DotNetFramework40));
			this.mockProcessHelper.Setup(x => x.GetCurrentProcessId()).Returns(1234);
			this.mockProcessHelper.Setup(x => x.GetProcessName(It.IsAny<int>())).Returns("dotnet.exe");			
		}

		[TestCleanup]
		public void Cleanup()
		{
			CommandLineOptions.Instance.Reset();

            // Opt out the Telemetry
            Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "0");
        }

        [TestMethod]
        public void TestRequestManagerShouldNotInitializeConsoleLoggerIfDesignModeIsSet()
        {
            CommandLineOptions.Instance.IsDesignMode = true;
            this.mockLoggerEvents = new DummyLoggerEvents(TestSessionMessageLogger.Instance);
            var requestManager = new TestRequestManager(CommandLineOptions.Instance,
                new Mock<ITestPlatform>().Object,
                TestRunResultAggregator.Instance,
                new Mock<ITestPlatformEventSource>().Object,
                this.inferHelper,
                this.mockMetricsPublisherTask,
                this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					actualDiscoveryCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					createDiscoveryRequestCalled++;
					actualDiscoveryCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

			string testCaseFilterValue = "TestFilter";
			CommandLineOptions.Instance.TestCaseFilterValue = testCaseFilterValue;
			this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, this.protocolConfig);

			Assert.AreEqual(testCaseFilterValue, actualDiscoveryCriteria.TestCaseFilter, "TestCaseFilter must be set");

            Assert.AreEqual(1, createDiscoveryRequestCalled, "CreateDiscoveryRequest must be invoked only once.");
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			var mockDiscoveryRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();

			string testCaseFilterValue = "TestFilter";
			CommandLineOptions.Instance.TestCaseFilterValue = testCaseFilterValue;
			this.testRequestManager = new TestRequestManager(CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

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
								  Sources = new List<string>() { "a.dll", "b.dll" },
								  RunSettings = @"<RunSettings>
								<RunConfiguration>
									<MaxCpuCount>2</MaxCpuCount>
									<TargetPlatform>x86</TargetPlatform>
									<TargetFrameworkVersion>Framework35</TargetFrameworkVersion>
									<DisableAppDomain>True</DisableAppDomain>
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);


			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify.
			object targetDevice, maxcount, targetPlatform, disableAppDomain;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.MaxCPUcount, out maxcount));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetPlatform, out targetPlatform));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.DisableAppDomain, out disableAppDomain));
			Assert.AreEqual("Other", targetDevice);
			Assert.AreEqual(2, maxcount);
			Assert.AreEqual("X86", targetPlatform.ToString());
			Assert.AreEqual(true, disableAppDomain);
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);


			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify.
			object targetDevice;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
			Assert.AreEqual("Local Machine", targetDevice);
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);


			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify.
			object targetDevice;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
			Assert.AreEqual("Device", targetDevice);
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);


			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify.
			object targetDevice;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
			Assert.AreEqual("Emulator 8.1 U1 WVGA 4 inch 512MB", targetDevice);
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			CommandLineOptions.Instance.SettingsFile = @"c://temp/.testsettings";

			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify
			object commandLineSwitches;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

			var commandLineArray = commandLineSwitches.ToString();

			Assert.IsTrue(commandLineArray.Contains("/settings//.TestSettings"));
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			CommandLineOptions.Instance.SettingsFile = @"c://temp/.vsmdi";

			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify
			object commandLineSwitches;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

			var commandLineArray = commandLineSwitches.ToString();

			Assert.IsTrue(commandLineArray.Contains("/settings//.vsmdi"));
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) => { actualRequestData = requestData; }).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			CommandLineOptions.Instance.SettingsFile = @"c://temp/.testrunConfig";

			// Act
			this.testRequestManager.DiscoverTests(payload, mockDiscoveryRegistrar.Object, mockProtocolConfig);

			// Verify
			object commandLineSwitches;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.CommandLineSwitches, out commandLineSwitches));

			var commandLineArray = commandLineSwitches.ToString();

			Assert.IsTrue(commandLineArray.Contains("/settings//.testrunConfig"));
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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					actualDiscoveryCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					actualDiscoveryCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					actualDiscoveryCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload, new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);
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
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);
			Assert.AreEqual(15, actualTestRunCriteria.FrequencyOfRunStatsChangeEvent);
		}

		[TestMethod]
		public void RunTestsShouldNotThrowForFramework35()
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <TargetFrameworkVersion>Framework35</TargetFrameworkVersion>
					 </RunConfiguration>
				</RunSettings>"
			};

			TestRunCriteria actualTestRunCriteria = null;
			var mockDiscoveryRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockDiscoveryRequest.Object);
			this.mockAssemblyMetadataProvider.Setup(a => a.GetFrameWork(It.IsAny<string>())).Returns(new FrameworkName(Constants.DotNetFramework35));

			var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
			var mockCustomlauncher = new Mock<ITestHostLauncher>();

			this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

			mockRunEventsRegistrar.Verify(lw => lw.LogWarning("Framework35 is not supported. For projects targeting .Net Framework 3.5, test will run in CLR 4.0 \"compatibility mode\"."), Times.Once);
			mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStart(), Times.Once);
			mockTestPlatformEventSource.Verify(mt => mt.ExecutionRequestStop(), Times.Once);
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
					{
						actualRequestData = requestData;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

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
		}

		[TestMethod]
		public void RunTestsShouldCollectTelemetryForLegacySettings()
		{
			// Opt in the Telemetry
			Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a" },
				RunSettings = @"<RunSettings>
									<LegacySettings>
										<Deployment enabled=""true"" deploySatelliteAssemblies=""true"" >
											<DeploymentItem filename="".\test.txt"" />
										</Deployment>
										<Scripts setupScript="".\setup.bat"" cleanupScript="".\cleanup.bat"" />
										<Execution hostProcessPlatform=""MSIL"" parallelTestCount=""4"">
											<Timeouts testTimeout=""120"" />
											<TestTypeSpecific>
												<UnitTestRunConfig>
													<AssemblyResolution />
												</UnitTestRunConfig>
											</TestTypeSpecific>
											<Hosts />
										</Execution>
									</LegacySettings>
							   </RunSettings>"
			};
			var mockProtocolConfig = new ProtocolConfig { Version = 4 };
			IRequestData actualRequestData = null;
			var mockDiscoveryRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualRequestData = requestData;
				}).Returns(mockDiscoveryRequest.Object);

            this.testRequestManager = new TestRequestManager(
                CommandLineOptions.Instance,
                this.mockTestPlatform.Object,
                TestRunResultAggregator.Instance,
                this.mockTestPlatformEventSource.Object,
                this.inferHelper,
                this.mockMetricsPublisherTask,
                this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

            // Act.
            this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

			// Verify
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue("VS.TestRun.LegacySettings.Elements", out var legacySettingsNodes));
			StringAssert.Equals("Deployment, Scripts, Execution, AssemblyResolution, Timeouts, Hosts", legacySettingsNodes);
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue("VS.TestRun.LegacySettings.DeploymentAttributes", out var deploymentAttributes));
			StringAssert.Equals("enabled, deploySatelliteAssemblies", deploymentAttributes);
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue("VS.TestRun.LegacySettings.ExecutionAttributes", out var executionAttributes));
			StringAssert.Equals("hostProcessPlatform, parallelTestCount", executionAttributes);

			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TestSettingsUsed, out var testSettingsUsed));
			Assert.IsFalse((bool)testSettingsUsed);
		}

		[TestMethod]
		public void RunTestsShouldCollectTelemetryForTestSettingsEmbeddedInsideRunSettings()
		{
			// Opt in the Telemetry
			Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a" },
				RunSettings = @"<RunSettings>
									   <MSTest>
											<ForcedLegacyMode>true</ForcedLegacyMode>
											<SettingsFile>..\..\Foo.testsettings</SettingsFile>
									   </MSTest>
							   </RunSettings>"
			};
			var mockProtocolConfig = new ProtocolConfig { Version = 4 };
			IRequestData actualRequestData = null;
			var mockDiscoveryRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualRequestData = requestData;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			// Act.
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

			// Verify
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TestSettingsUsed, out var testSettingsUsed));
			Assert.IsTrue((bool)testSettingsUsed);
		}

		[TestMethod]
		public void RunTestsShouldCollectMetrics()
		{
			// Opt in the Telemetry
			Environment.SetEnvironmentVariable("VSTEST_TELEMETRY_OPTEDIN", "1");

			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings = @"<RunSettings>
								<RunConfiguration>
									<MaxCpuCount>2</MaxCpuCount>
									<TargetPlatform>x86</TargetPlatform>
									<TargetFrameworkVersion>Framework35</TargetFrameworkVersion>
									<DisableAppDomain>True</DisableAppDomain>
								</RunConfiguration>
								<MSPhoneTest>
								  <TargetDevice>169.254.193.190</TargetDevice>
								</MSPhoneTest>
							</RunSettings>"
			};
			var mockProtocolConfig = new ProtocolConfig { Version = 4 };
			IRequestData actualRequestData = null;
			var mockDiscoveryRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualRequestData = requestData;
				}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager = new TestRequestManager(
				CommandLineOptions.Instance,
				this.mockTestPlatform.Object,
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			// Act.
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, mockProtocolConfig);

			// Verify
			object targetDevice, maxcount, targetPlatform, disableAppDomain;
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetDevice, out targetDevice));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.MaxCPUcount, out maxcount));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.TargetPlatform, out targetPlatform));
			Assert.IsTrue(actualRequestData.MetricsCollection.Metrics.TryGetValue(TelemetryDataConstants.DisableAppDomain, out disableAppDomain));
			Assert.AreEqual("Other", targetDevice);
			Assert.AreEqual(2, maxcount);
			Assert.AreEqual("X86", targetPlatform.ToString());
			Assert.AreEqual(true, disableAppDomain);
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
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
				TestRunResultAggregator.Instance,
				this.mockTestPlatformEventSource.Object,
				this.inferHelper,
				this.mockMetricsPublisherTask,
				this.mockProcessHelper.Object,
				this.mockAttachmentsProcessingManager.Object);

			this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);

			Assert.AreEqual(testCaseFilterValue, observedCriteria.TestCaseFilter, "TestCaseFilter must be set");

            Assert.AreEqual(1, createRunRequestCalled, "CreateRunRequest must be invoked only once.");
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>()))
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

		// TODO: add tests in design mode and executor that they are handling all the exceptions properly including printing inner exception.

		[TestMethod]
		public void RunTestsIfThrowsTestPlatformExceptionShouldThrowOut()
		{
			Assert.ThrowsException<TestPlatformException>(() => RunTestsIfThrowsExceptionShouldThrowOut(new TestPlatformException("HelloWorld")));
		}

		[TestMethod]
		public void RunTestsIfThrowsSettingsExceptionShouldThrowOut()
		{
			Assert.ThrowsException<SettingsException>(() => RunTestsIfThrowsExceptionShouldThrowOut(new SettingsException("HelloWorld")));
		}

		[TestMethod]
		public void RunTestsIfThrowsInvalidOperationExceptionShouldThrowOut()
		{
			Assert.ThrowsException<InvalidOperationException>(() => RunTestsIfThrowsExceptionShouldThrowOut(new InvalidOperationException("HelloWorld")));
		}

		[TestMethod]
		public void RunTestsIfThrowsExceptionShouldThrowOut()
		{
			Assert.ThrowsException<NotImplementedException>(() => RunTestsIfThrowsExceptionShouldThrowOut(new NotImplementedException("HelloWorld")));
		}

		[TestMethod]
		public void DiscoverTestsIfThrowsTestPlatformExceptionShouldThrowOut()
		{
			Assert.ThrowsException<TestPlatformException>(() => DiscoverTestsIfThrowsExceptionShouldThrowOut(new TestPlatformException("HelloWorld")));
		}

		[TestMethod]
		public void DiscoverTestsIfThrowsSettingsExceptionShouldThrowOut()
		{
			Assert.ThrowsException<SettingsException>(() => DiscoverTestsIfThrowsExceptionShouldThrowOut(new SettingsException("HelloWorld")));
		}

		[TestMethod]
		public void DiscoverTestsIfThrowsInvalidOperationExceptionShouldThrowOut()
		{
			Assert.ThrowsException<InvalidOperationException>(() => DiscoverTestsIfThrowsExceptionShouldThrowOut(new InvalidOperationException("HelloWorld")));
		}

		[TestMethod]
		public void DiscoverTestsIfThrowsExceptionShouldThrowOut()
		{
			Assert.ThrowsException<NotImplementedException>(() => DiscoverTestsIfThrowsExceptionShouldThrowOut(new NotImplementedException("HelloWorld")));
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
				tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode)), It.IsAny<TestPlatformOptions>()));

			var collectSourceInformation = $"<CollectSourceInformation>{designModeValue}</CollectSourceInformation>";
			this.mockTestPlatform.Verify(
				tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(collectSourceInformation)), It.IsAny<TestPlatformOptions>()));
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
				tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(designmode)), It.IsAny<TestPlatformOptions>()));
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
			this.mockTestPlatform.Verify(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.Is<TestRunCriteria>(rc => rc.TestRunSettings.Contains(designmode)), It.IsAny<TestPlatformOptions>()));
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
				tp => tp.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.Is<DiscoveryCriteria>(dc => dc.RunSettings.Contains(collectSourceInformation)), It.IsAny<TestPlatformOptions>()));
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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

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

		[TestMethod]
		public void RunTestsShouldAddConsoleLoggerInRunSettingsInNonDesignMode()
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
			TestRunCriteria actualTestRunCriteria = null;
			var mockTestRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);

			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualTestRunCriteria.TestRunSettings).LoggerSettingsList;
			Assert.AreEqual(1, loggerSettingsList.Count);
			Assert.AreEqual("Console", loggerSettingsList[0].FriendlyName);
			Assert.IsNotNull(loggerSettingsList[0].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[0].CodeBase);
		}

		[TestMethod]
		public void RunTestsShouldAddConsoleLoggerInRunSettingsIfDesignModeSetFalseInRunSettings()
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>False</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};

			this.commandLineOptions.IsDesignMode = true;
			TestRunCriteria actualTestRunCriteria = null;
			var mockTestRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualTestRunCriteria.TestRunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("Console", loggerSettingsList[1].FriendlyName);
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void DiscoverTestsShouldAddConsoleLoggerInRunSettingsIfDesignModeSetFalseInRunSettings()
		{
			var payload = new DiscoveryRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>False</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};
			this.commandLineOptions.IsDesignMode = true;
			DiscoveryCriteria actualDiscoveryCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
				new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualDiscoveryCriteria.RunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("Console", loggerSettingsList[1].FriendlyName);
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void RunTestsShouldNotAddConsoleLoggerInRunSettingsInDesignMode()
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>True</DesignMode>
					 </RunConfiguration>
				</RunSettings>"
			};

			this.commandLineOptions.IsDesignMode = false;
			TestRunCriteria actualTestRunCriteria = null;
			var mockTestRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

			Assert.IsFalse(actualTestRunCriteria.TestRunSettings.Contains("LoggerRunSettings"));
		}

		[TestMethod]
		public void DiscoverTestsShouldAddConsoleLoggerInRunSettingsInNonDesignMode()
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
			DiscoveryCriteria actualDiscoveryCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
				new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualDiscoveryCriteria.RunSettings).LoggerSettingsList;
			Assert.AreEqual(1, loggerSettingsList.Count);
			Assert.AreEqual("Console", loggerSettingsList[0].FriendlyName);
			Assert.IsNotNull(loggerSettingsList[0].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[0].CodeBase);
		}

		[TestMethod]
		public void DiscoverTestsShouldNotAddConsoleLoggerInRunSettingsInDesignMode()
		{
			var payload = new DiscoveryRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>True</DesignMode>
					 </RunConfiguration>
				</RunSettings>"
			};
			this.commandLineOptions.IsDesignMode = false;
			DiscoveryCriteria actualDiscoveryCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
				new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

			Assert.IsFalse(actualDiscoveryCriteria.RunSettings.Contains("LoggerRunSettings"));
		}

		[TestMethod]
		public void RunTestsShouldOverrideOnlyAssemblyNameIfConsoleLoggerAlreadyPresentInNonDesignMode()
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>False</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
						 <Logger friendlyName=""console"" uri=""logger://tempconsoleUri"" assemblyQualifiedName=""tempAssemblyName"" codeBase=""tempCodeBase"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};

			this.commandLineOptions.IsDesignMode = false;
			TestRunCriteria actualTestRunCriteria = null;
			var mockTestRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualTestRunCriteria.TestRunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("console", loggerSettingsList[1].FriendlyName);
			Assert.AreEqual(new Uri("logger://tempconsoleUri").ToString(), loggerSettingsList[1].Uri.ToString());
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].AssemblyQualifiedName);
			Assert.AreNotEqual("tempCodeBase", loggerSettingsList[1].CodeBase);
			Assert.IsTrue(loggerSettingsList[1].Configuration.InnerXml.Contains("Value1"));
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void DiscoverTestsShouldOverrideOnlyAssemblyNameIfConsoleLoggerAlreadyPresentInNonDesignMode()
		{
			var payload = new DiscoveryRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>False</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
						 <Logger friendlyName=""consoleTemp"" uri=""logger://Microsoft/TestPlatform/ConsoleLogger/v1"" assemblyQualifiedName=""tempAssemblyName"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};
			this.commandLineOptions.IsDesignMode = false;
			DiscoveryCriteria actualDiscoveryCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
				new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualDiscoveryCriteria.RunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("consoleTemp", loggerSettingsList[1].FriendlyName);
			Assert.AreEqual(new Uri("logger://Microsoft/TestPlatform/ConsoleLogger/v1").ToString(), loggerSettingsList[1].Uri.ToString());
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].AssemblyQualifiedName);
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].CodeBase);
			Assert.IsTrue(loggerSettingsList[1].Configuration.InnerXml.Contains("Value1"));
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void RunTestsShouldOverrideOnlyAssemblyNameIfConsoleLoggerAlreadyPresentInDesignMode()
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>True</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
						 <Logger friendlyName=""console"" uri=""logger://tempconsoleUri"" assemblyQualifiedName=""tempAssemblyName"" codeBase=""tempCodeBase"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};

			this.commandLineOptions.IsDesignMode = false;
			TestRunCriteria actualTestRunCriteria = null;
			var mockTestRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					actualTestRunCriteria = runCriteria;
				}).Returns(mockTestRunRequest.Object);
			this.testRequestManager.RunTests(payload, new Mock<ITestHostLauncher>().Object, new Mock<ITestRunEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualTestRunCriteria.TestRunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("console", loggerSettingsList[1].FriendlyName);
			Assert.AreEqual(new Uri("logger://tempconsoleUri").ToString(), loggerSettingsList[1].Uri.ToString());
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].AssemblyQualifiedName);
			Assert.AreNotEqual("tempCodeBase", loggerSettingsList[1].CodeBase);
			Assert.IsTrue(loggerSettingsList[1].Configuration.InnerXml.Contains("Value1"));
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void DiscoverTestsShouldOverrideOnlyAssemblyNameIfConsoleLoggerAlreadyPresentInDesignMode()
		{
			var payload = new DiscoveryRequestPayload()
			{
				Sources = new List<string>() { "a.dll" },
				RunSettings =
					@"<?xml version=""1.0"" encoding=""utf-8""?>
				<RunSettings>
					 <RunConfiguration>
					   <DesignMode>True</DesignMode>
					 </RunConfiguration>
					 <LoggerRunSettings>
					   <Loggers>
						 <Logger friendlyName=""blabla"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
						 <Logger friendlyName=""consoleTemp"" uri=""logger://Microsoft/TestPlatform/ConsoleLogger/v1"" assemblyQualifiedName=""tempAssemblyName"">
						   <Configuration>
							 <Key1>Value1</Key1>
						   </Configuration>
						 </Logger>
					   </Loggers>
					 </LoggerRunSettings>
				</RunSettings>"
			};
			this.commandLineOptions.IsDesignMode = false;
			DiscoveryCriteria actualDiscoveryCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform
				.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>()))
				.Callback(
					(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
					{
						actualDiscoveryCriteria = discoveryCriteria;
					}).Returns(mockDiscoveryRequest.Object);

			this.testRequestManager.DiscoverTests(payload,
				new Mock<ITestDiscoveryEventsRegistrar>().Object, this.protocolConfig);

			var loggerSettingsList = XmlRunSettingsUtilities.GetLoggerRunSettings(actualDiscoveryCriteria.RunSettings).LoggerSettingsList;
			Assert.AreEqual(2, loggerSettingsList.Count);
			Assert.IsNotNull(loggerSettingsList[0].Configuration);
			Assert.AreEqual("blabla", loggerSettingsList[0].FriendlyName);
			Assert.AreEqual("consoleTemp", loggerSettingsList[1].FriendlyName);
			Assert.AreEqual(new Uri("logger://Microsoft/TestPlatform/ConsoleLogger/v1").ToString(), loggerSettingsList[1].Uri.ToString());
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].AssemblyQualifiedName);
			Assert.AreNotEqual("tempAssemblyName", loggerSettingsList[1].CodeBase);
			Assert.IsTrue(loggerSettingsList[1].Configuration.InnerXml.Contains("Value1"));
			Assert.IsNotNull(loggerSettingsList[1].AssemblyQualifiedName);
			Assert.IsNotNull(loggerSettingsList[1].CodeBase);
		}

		[TestMethod]
		public void ProcessTestRunAttachmentsShouldSucceedWithTelemetryEnabled()
        {
			var mockEventsHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
			mockAttachmentsProcessingManager
				.Setup(m => m.ProcessTestRunAttachmentsAsync(It.IsAny<IRequestData>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ITestRunAttachmentsProcessingEventsHandler>(), It.IsAny<CancellationToken>()))
				.Returns((IRequestData r, ICollection<AttachmentSet> a, ITestRunAttachmentsProcessingEventsHandler h, CancellationToken token) => Task.Run(() =>
				{
					r.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsSentForProcessing, 5);
					r.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsAfterProcessing, 1);
				}));

			var payload = new TestRunAttachmentsProcessingPayload()
			{
				Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") },
				CollectMetrics = true
			};

			testRequestManager.ProcessTestRunAttachments(payload, mockEventsHandler.Object, this.protocolConfig);

			mockAttachmentsProcessingManager.Verify(m => m.ProcessTestRunAttachmentsAsync(It.Is<IRequestData>(r => r.IsTelemetryOptedIn), payload.Attachments, mockEventsHandler.Object, It.IsAny<CancellationToken>()));
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStart());
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStop());

			mockMetricsPublisher.Verify(p => p.PublishMetrics(TelemetryDataConstants.TestAttachmentsProcessingCompleteEvent,
				It.Is<Dictionary<string, object>>(m => m.Count == 2 && 
					m.ContainsKey(TelemetryDataConstants.NumberOfAttachmentsSentForProcessing) && (int)m[TelemetryDataConstants.NumberOfAttachmentsSentForProcessing] == 5 &&
					m.ContainsKey(TelemetryDataConstants.NumberOfAttachmentsAfterProcessing) && (int)m[TelemetryDataConstants.NumberOfAttachmentsAfterProcessing] == 1)));
		}

		[TestMethod]
		public void ProcessTestRunAttachmentsShouldSucceedWithTelemetryDisabled()
		{
			var mockEventsHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
			mockAttachmentsProcessingManager
				.Setup(m => m.ProcessTestRunAttachmentsAsync(It.IsAny<IRequestData>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ITestRunAttachmentsProcessingEventsHandler>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true));

			var payload = new TestRunAttachmentsProcessingPayload()
			{
				Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") },
				CollectMetrics = false
			};

			testRequestManager.ProcessTestRunAttachments(payload, mockEventsHandler.Object, this.protocolConfig);

			mockAttachmentsProcessingManager.Verify(m => m.ProcessTestRunAttachmentsAsync(It.Is<IRequestData>(r => !r.IsTelemetryOptedIn), payload.Attachments, mockEventsHandler.Object, It.IsAny<CancellationToken>()));
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStart());
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStop());
		}

		[TestMethod]
		public async Task CancelTestRunAttachmentsProcessingShouldSucceedIfRequestInProgress()
		{
			var mockEventsHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
			mockAttachmentsProcessingManager
				.Setup(m => m.ProcessTestRunAttachmentsAsync(It.IsAny<IRequestData>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ITestRunAttachmentsProcessingEventsHandler>(), It.IsAny<CancellationToken>()))
				.Returns((IRequestData r, ICollection<AttachmentSet> a, ITestRunAttachmentsProcessingEventsHandler h, CancellationToken token) => Task.Run(() =>
				{
					int i = 0;
					while (!token.IsCancellationRequested)
                    {
						i++;
						Console.WriteLine($"Iteration {i}");
						Task.Delay(5).Wait();
                    }

					r.MetricsCollection.Add(TelemetryDataConstants.AttachmentsProcessingState, "Canceled");
				}));

			var payload = new TestRunAttachmentsProcessingPayload()
			{
				Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") },
				CollectMetrics = true
			};

			Task task = Task.Run(() => testRequestManager.ProcessTestRunAttachments(payload, mockEventsHandler.Object, this.protocolConfig));
			await Task.Delay(50);
			testRequestManager.CancelTestRunAttachmentsProcessing();

			await task;

			mockAttachmentsProcessingManager.Verify(m => m.ProcessTestRunAttachmentsAsync(It.IsAny<IRequestData>(), payload.Attachments, mockEventsHandler.Object, It.IsAny<CancellationToken>()));
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStart());
			mockTestPlatformEventSource.Verify(es => es.TestRunAttachmentsProcessingRequestStop());

			mockMetricsPublisher.Verify(p => p.PublishMetrics(TelemetryDataConstants.TestAttachmentsProcessingCompleteEvent,
				It.Is<Dictionary<string, object>>(m => m.Count == 1 && m.ContainsKey(TelemetryDataConstants.AttachmentsProcessingState) && (string)m[TelemetryDataConstants.AttachmentsProcessingState] == "Canceled")));
		}

		[TestMethod]
		public void CancelTestRunAttachmentsProcessingShouldSucceedIfNoRequest()
		{
			testRequestManager.CancelTestRunAttachmentsProcessing();
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

		private void RunTestsIfThrowsExceptionShouldThrowOut(Exception exception)
		{
			var payload = new TestRunRequestPayload()
			{
				Sources = new List<string>() { "a", "b" },
				RunSettings = DefaultRunsettings
			};

			var createRunRequestCalled = 0;
			TestRunCriteria observedCriteria = null;
			var mockRunRequest = new Mock<ITestRunRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, TestRunCriteria runCriteria, TestPlatformOptions options) =>
				{
					createRunRequestCalled++;
					observedCriteria = runCriteria;
				}).Returns(mockRunRequest.Object);

			mockRunRequest.Setup(mr => mr.ExecuteAsync()).Throws(exception);

			var mockRunEventsRegistrar = new Mock<ITestRunEventsRegistrar>();
			var mockCustomlauncher = new Mock<ITestHostLauncher>();

			this.testRequestManager.RunTests(payload, mockCustomlauncher.Object, mockRunEventsRegistrar.Object, this.protocolConfig);
		}

		private void DiscoverTestsIfThrowsExceptionShouldThrowOut(Exception exception)
		{
			var payload = new DiscoveryRequestPayload()
			{
				Sources = new List<string>() { "a.dll", "b.dll" },
				RunSettings = DefaultRunsettings
			};

			DiscoveryCriteria observedCriteria = null;
			var mockDiscoveryRequest = new Mock<IDiscoveryRequest>();
			this.mockTestPlatform.Setup(mt => mt.CreateDiscoveryRequest(It.IsAny<IRequestData>(), It.IsAny<DiscoveryCriteria>(), It.IsAny<TestPlatformOptions>())).Callback(
				(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options) =>
				{
					observedCriteria = discoveryCriteria;
				}).Returns(mockDiscoveryRequest.Object);

			mockDiscoveryRequest.Setup(mr => mr.DiscoverAsync()).Throws(exception);

			var mockDiscoveryEventsRegistrar = new Mock<ITestDiscoveryEventsRegistrar>();
			var mockCustomlauncher = new Mock<ITestHostLauncher>();

			this.testRequestManager.DiscoverTests(payload, mockDiscoveryEventsRegistrar.Object, this.protocolConfig);
		}
	}
}