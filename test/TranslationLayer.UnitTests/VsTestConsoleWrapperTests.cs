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

        private readonly List<string> testSources = new List<string> { "Hello", "World" };

        private readonly List<TestCase> testCases = new List<TestCase>
                                                      {
                                                          new TestCase("a.b.c", new Uri("d://uri"), "a.dll"),
                                                          new TestCase("d.e.f", new Uri("g://uri"), "d.dll")
                                                      };

        private ConsoleParameters consoleParameters;

        [TestInitialize]
        public void TestInitialize()
        {
            this.consoleParameters = new ConsoleParameters();

            this.mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            this.mockProcessManager = new Mock<IProcessManager>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.consoleWrapper = new VsTestConsoleWrapper(
                this.mockRequestSender.Object,
                this.mockProcessManager.Object,
                this.consoleParameters,
                new Mock<ITestPlatformEventSource>().Object,
                this.mockProcessHelper.Object);

            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(100));
        }

        [TestMethod]
        public void StartSessionShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(inputPort);

            this.consoleWrapper.StartSession();

            Assert.AreEqual(expectedParentProcessId, this.consoleParameters.ParentProcessId, "Parent process Id must be set");
            Assert.AreEqual(inputPort, this.consoleParameters.PortNumber, "Port number must be set");
            Assert.AreEqual(TraceLevel.Verbose, this.consoleParameters.TraceLevel, "Default value of trace level should be verbose.");

            this.mockProcessManager.Verify(pm => pm.StartProcess(this.consoleParameters), Times.Once);
        }

        [TestMethod]
        public void StartSessionShouldThrowExceptionOnBadPort()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.StartSession());
        }

        [TestMethod]
        public void StartSessionShouldCallWhenProcessNotInitialized()
        {
            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);

            // To call private method EnsureInitialize call InitializeExtensions
            this.consoleWrapper.InitializeExtensions(new[] { "path/to/adapter" });

            this.mockProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ConsoleParameters>()));
        }

        [TestMethod]
        public void InitializeExtensionsShouldCachePathToExtensions()
        {
            var pathToExtensions = new[] { "path/to/adapter" };
            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(true);

            this.consoleWrapper.InitializeExtensions(pathToExtensions);

            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);

            this.consoleWrapper.InitializeExtensions(pathToExtensions);

            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToExtensions), Times.Exactly(3));
        }

        [TestMethod]
        public void ProcessExitedEventShouldSetOnProcessExit()
        {
            this.mockProcessManager.Raise(pm => pm.ProcessExited += null, EventArgs.Empty);

            this.mockRequestSender.Verify(rs => rs.OnProcessExited(), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldSucceed()
        {
            var pathToAdditionalExtensions = new List<string> { "Hello", "World" };

            this.consoleWrapper.InitializeExtensions(pathToAdditionalExtensions);

            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToAdditionalExtensions), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldThrowExceptionOnBadConnection()
        {
            this.mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("DummyProcess");
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var exception = Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.InitializeExtensions(new List<string> { "Hello", "World" }));
            Assert.AreEqual("DummyProcess process failed to connect to vstest.console process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.", exception.Message);
            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldSucceed()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            this.consoleWrapper.DiscoverTests(this.testSources, null, options, new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTests(this.testSources, null, options, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldPassOnNullOptions()
        {
            this.consoleWrapper.DiscoverTests(this.testSources, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTests(this.testSources, null, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallTestDiscoveryHandler2IfTestDiscoveryHandler1IsUsed()
        {
            this.consoleWrapper.DiscoverTests(this.testSources, null, new Mock<ITestDiscoveryEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTests(this.testSources, null, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldThrowExceptionOnBadConnection()
        {
            this.mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("DummyProcess");
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var exception = Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.DiscoverTests(new List<string> { "Hello", "World" }, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object));
            Assert.AreEqual("DummyProcess process failed to connect to vstest.console process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.", exception.Message);
            this.mockRequestSender.Verify(rs => rs.DiscoverTests(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldSucceed()
        {
            this.consoleWrapper.RunTests(this.testSources, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testSources, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndNullOptionsShouldPassOnNullOptions()
        {
            this.consoleWrapper.RunTests(
                            this.testSources,
                            "RunSettings",
                            null,
                            new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndOptionsShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            this.consoleWrapper.RunTests(
                            this.testSources,
                            "RunSettings",
                            options,
                            new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testSources, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomHostShouldSucceed()
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testSources,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testSources, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndOptionsUsingACustomHostShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            this.consoleWrapper.RunTestsWithCustomTestHost(
                            this.testSources,
                            "RunSettings",
                            options,
                            new Mock<ITestRunEventsHandler>().Object,
                            new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHost(
                    this.testSources,
                    "RunSettings",
                    options,
                    null,
                    It.IsAny<ITestRunEventsHandler>(),
                    It.IsAny<ITestHostLauncher>()),
                    Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsShouldSucceed()
        {
            this.consoleWrapper.RunTests(this.testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndNullOptionsShouldPassOnNullOptions()
        {
            this.consoleWrapper.RunTests(this.testCases, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndOptionsShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            this.consoleWrapper.RunTests(this.testCases, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testCases,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndNullOptionsUsingACustomHostShouldPassOnNullOptions()
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testCases,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndOptionsUsingACustomHostShouldPassOnOptions()
        {
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testCases,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldSucceed()
        {
            var attachments = new Collection<AttachmentSet>();
            var cancellationToken = new CancellationToken();

            await this.consoleWrapper.ProcessTestRunAttachmentsAsync(
                attachments,
                null,
                true,
                true,
                new Mock<ITestRunAttachmentsProcessingEventsHandler>().Object,
                cancellationToken);

            this.mockRequestSender.Verify(rs => rs.ProcessTestRunAttachmentsAsync(attachments, true, It.IsAny<ITestRunAttachmentsProcessingEventsHandler>(), cancellationToken));
        }

        [TestMethod]
        public void EndSessionShouldSucceed()
        {
            this.consoleWrapper.EndSession();

            this.mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
            this.mockRequestSender.Verify(rs => rs.Close(), Times.Once);
            this.mockProcessManager.Verify(x => x.ShutdownProcess(), Times.Once);
        }
    }
}
