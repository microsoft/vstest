// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class VsTestConsoleWrapperTests
    {
        private IVsTestConsoleWrapper consoleWrapper;

        private Mock<IProcessManager> mockProcessManager;

        private Mock<IProcessHelper> mockProcessHelper;

        private Mock<ITranslationLayerRequestSender> mockRequestSender;

        private readonly List<string> testSources = new() { "Hello", "World" };

        private readonly List<TestCase> testCases = new()
        {
                                                          new TestCase("a.b.c", new Uri("d://uri"), "a.dll"),
                                                          new TestCase("d.e.f", new Uri("g://uri"), "d.dll")
                                                      };

        private ConsoleParameters consoleParameters;

        [TestInitialize]
        public void TestInitialize()
        {
            consoleParameters = new ConsoleParameters();

            mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            mockProcessManager = new Mock<IProcessManager>();
            mockProcessHelper = new Mock<IProcessHelper>();
            consoleWrapper = new VsTestConsoleWrapper(
                mockRequestSender.Object,
                mockProcessManager.Object,
                consoleParameters,
                new Mock<ITestPlatformEventSource>().Object,
                mockProcessHelper.Object);

            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(100));
        }

        [TestMethod]
        public void StartSessionShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = Process.GetCurrentProcess().Id;
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(inputPort);

            consoleWrapper.StartSession();

            Assert.AreEqual(expectedParentProcessId, consoleParameters.ParentProcessId, "Parent process Id must be set");
            Assert.AreEqual(inputPort, consoleParameters.PortNumber, "Port number must be set");
            Assert.AreEqual(TraceLevel.Verbose, consoleParameters.TraceLevel, "Default value of trace level should be verbose.");

            mockProcessManager.Verify(pm => pm.StartProcess(consoleParameters), Times.Once);
        }

        [TestMethod]
        public void StartSessionShouldThrowExceptionOnBadPort()
        {
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

            Assert.ThrowsException<TransationLayerException>(() => consoleWrapper.StartSession());
        }

        [TestMethod]
        public void StartSessionShouldCallWhenProcessNotInitialized()
        {
            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);

            // To call private method EnsureInitialize call InitializeExtensions
            consoleWrapper.InitializeExtensions(new[] { "path/to/adapter" });

            mockProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ConsoleParameters>()));
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void StartTestSessionShouldCallRequestSenderWithCorrectArguments1()
        {
            var testSessionInfo = new TestSessionInfo();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    null,
                    mockEventsHandler.Object,
                    null))
                .Returns(testSessionInfo);

            Assert.AreEqual(
                consoleWrapper.StartTestSession(
                    testSources,
                    null,
                    mockEventsHandler.Object).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    null,
                    mockEventsHandler.Object,
                    null),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void StartTestSessionShouldCallRequestSenderWithCorrectArguments2()
        {
            var testSessionInfo = new TestSessionInfo();
            var testPlatformOptions = new TestPlatformOptions();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    null))
                .Returns(testSessionInfo);

            Assert.AreEqual(
                consoleWrapper.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    null),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void StartTestSessionShouldCallRequestSenderWithCorrectArguments3()
        {
            var testSessionInfo = new TestSessionInfo();
            var testPlatformOptions = new TestPlatformOptions();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();
            var mockTesthostLauncher = new Mock<ITestHostLauncher>();

            mockRequestSender.Setup(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object))
                .Returns(testSessionInfo);

            Assert.AreEqual(
                consoleWrapper.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSession(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void StopTestSessionShouldCallRequestSenderWithCorrectArguments()
        {
            var testSessionInfo = new TestSessionInfo();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StopTestSession(
                    It.IsAny<TestSessionInfo>(),
                    It.IsAny<ITestSessionEventsHandler>()))
                .Returns(true);

            Assert.IsTrue(
                consoleWrapper.StopTestSession(
                    testSessionInfo,
                    mockEventsHandler.Object));

            mockRequestSender.Verify(
                rs => rs.StopTestSession(
                    testSessionInfo,
                    mockEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldCachePathToExtensions()
        {
            var pathToExtensions = new[] { "path/to/adapter" };
            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(true);

            consoleWrapper.InitializeExtensions(pathToExtensions);

            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);

            consoleWrapper.InitializeExtensions(pathToExtensions);

            mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToExtensions), Times.Exactly(3));
        }

        [TestMethod]
        public void ProcessExitedEventShouldSetOnProcessExit()
        {
            mockProcessManager.Raise(pm => pm.ProcessExited += null, EventArgs.Empty);

            mockRequestSender.Verify(rs => rs.OnProcessExited(), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldSucceed()
        {
            var pathToAdditionalExtensions = new List<string> { "Hello", "World" };

            consoleWrapper.InitializeExtensions(pathToAdditionalExtensions);

            mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToAdditionalExtensions), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldThrowExceptionOnBadConnection()
        {
            mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("DummyProcess");
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var exception = Assert.ThrowsException<TransationLayerException>(() => consoleWrapper.InitializeExtensions(new List<string> { "Hello", "World" }));
            Assert.AreEqual("DummyProcess process failed to connect to vstest.console process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.", exception.Message);
            mockRequestSender.Verify(rs => rs.InitializeExtensions(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldSucceed()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            consoleWrapper.DiscoverTests(testSources, null, options, new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTests(testSources, null, options, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldPassOnNullOptions()
        {
            consoleWrapper.DiscoverTests(testSources, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTests(testSources, null, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallTestDiscoveryHandler2IfTestDiscoveryHandler1IsUsed()
        {
            consoleWrapper.DiscoverTests(testSources, null, new Mock<ITestDiscoveryEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTests(testSources, null, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();

            consoleWrapper.DiscoverTests(
                testSources,
                null,
                null,
                testSessionInfo,
                new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(
                rs => rs.DiscoverTests(
                    testSources,
                    null,
                    null,
                    testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldThrowExceptionOnBadConnection()
        {
            mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("DummyProcess");
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var exception = Assert.ThrowsException<TransationLayerException>(() => consoleWrapper.DiscoverTests(new List<string> { "Hello", "World" }, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object));
            Assert.AreEqual("DummyProcess process failed to connect to vstest.console process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.", exception.Message);
            mockRequestSender.Verify(rs => rs.DiscoverTests(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldSucceed()
        {
            consoleWrapper.RunTests(testSources, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testSources, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndNullOptionsShouldPassOnNullOptions()
        {
            consoleWrapper.RunTests(
                            testSources,
                            "RunSettings",
                            null,
                            new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndOptionsShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            consoleWrapper.RunTests(
                            testSources,
                            "RunSettings",
                            options,
                            new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testSources, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            consoleWrapper.RunTests(
                testSources,
                "RunSettings",
                options,
                testSessionInfo,
                new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(
                rs => rs.StartTestRun(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>()),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomHostShouldSucceed()
        {
            consoleWrapper.RunTestsWithCustomTestHost(
                testSources,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(testSources, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndOptionsUsingACustomHostShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            consoleWrapper.RunTestsWithCustomTestHost(
                            testSources,
                            "RunSettings",
                            options,
                            new Mock<ITestRunEventsHandler>().Object,
                            new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHost(
                    testSources,
                    "RunSettings",
                    options,
                    null,
                    It.IsAny<ITestRunEventsHandler>(),
                    It.IsAny<ITestHostLauncher>()),
                    Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndACustomHostShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            consoleWrapper.RunTestsWithCustomTestHost(
                testSources,
                "RunSettings",
                options,
                testSessionInfo,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHost(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>(),
                    It.IsAny<ITestHostLauncher>()),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsShouldSucceed()
        {
            consoleWrapper.RunTests(testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndNullOptionsShouldPassOnNullOptions()
        {
            consoleWrapper.RunTests(testCases, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndOptionsShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            consoleWrapper.RunTests(testCases, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRun(testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            consoleWrapper.RunTests(
                testCases,
                "RunSettings",
                options,
                testSessionInfo,
                new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(
                rs => rs.StartTestRun(
                    testCases,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>()),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            consoleWrapper.RunTestsWithCustomTestHost(
                testCases,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndNullOptionsUsingACustomHostShouldPassOnNullOptions()
        {
            consoleWrapper.RunTestsWithCustomTestHost(
                testCases,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndOptionsUsingACustomHostShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            consoleWrapper.RunTestsWithCustomTestHost(
                testCases,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndACustomHostShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            consoleWrapper.RunTestsWithCustomTestHost(
                testCases,
                "RunSettings",
                options,
                testSessionInfo,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHost(
                    testCases,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>(),
                    It.IsAny<ITestHostLauncher>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldSucceed()
        {
            var attachments = new Collection<AttachmentSet>();
            var invokedDataCollectors = new Collection<InvokedDataCollector>();
            var cancellationToken = new CancellationToken();

            await consoleWrapper.ProcessTestRunAttachmentsAsync(
                attachments,
                invokedDataCollectors,
                Constants.EmptyRunSettings,
                true,
                true,
                new Mock<ITestRunAttachmentsProcessingEventsHandler>().Object,
                cancellationToken);

            mockRequestSender.Verify(rs => rs.ProcessTestRunAttachmentsAsync(attachments, invokedDataCollectors, Constants.EmptyRunSettings, true, It.IsAny<ITestRunAttachmentsProcessingEventsHandler>(), cancellationToken));
        }

        [TestMethod]
        public void EndSessionShouldSucceed()
        {
            consoleWrapper.EndSession();

            mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
            mockRequestSender.Verify(rs => rs.Close(), Times.Once);
            mockProcessManager.Verify(x => x.ShutdownProcess(), Times.Once);
        }
    }
}
