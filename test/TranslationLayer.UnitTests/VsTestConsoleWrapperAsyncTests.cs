// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VsTestConsoleWrapperAsyncTests
    {
        private IVsTestConsoleWrapperAsync consoleWrapper;

        private Mock<IProcessManager> mockProcessManager;

        private Mock<ITranslationLayerRequestSenderAsync> mockRequestSender;

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

            this.mockRequestSender = new Mock<ITranslationLayerRequestSenderAsync>();
            this.mockProcessManager = new Mock<IProcessManager>();
            this.consoleWrapper = new VsTestConsoleWrapperAsync(
                this.mockRequestSender.Object,
                this.mockProcessManager.Object,
                this.consoleParameters,
                new Mock<ITestPlatformEventSource>().Object);

            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
        }

        [TestMethod]
        public async Task StartSessionAsyncShouldListenBeforeLaunchingAndWaitAfterLaunching()
        {
            var calls = new List<string>();
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Callback(() => calls.Add("listen")).Returns(100);
            this.mockProcessManager.Setup(pm => pm.StartProcess(this.consoleParameters))
                .Callback(() => calls.Add("launch"));
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>()))
                .Callback(() => calls.Add("wait")).Returns(true);

            await this.consoleWrapper.StartSessionAsync();

            CollectionAssert.AreEqual(new[] { "listen", "launch", "wait" }, calls);
            this.mockRequestSender.Verify(rs => rs.InitializeCommunicationAsync(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task StartSessionAsyncShouldFailWhenTheConsoleDoesNotConnect()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var error = await Assert.ThrowsExceptionAsync<TransationLayerException>(() => this.consoleWrapper.StartSessionAsync());

            StringAssert.Contains(error.Message, "failed to connect");
        }

        [TestMethod]
        public async Task StartSessionAsyncShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(inputPort);

            await this.consoleWrapper.StartSessionAsync();

            Assert.AreEqual(expectedParentProcessId, this.consoleParameters.ParentProcessId, "Parent process Id must be set");
            Assert.AreEqual(inputPort, this.consoleParameters.PortNumber, "Port number must be set");

            this.mockProcessManager.Verify(pm => pm.StartProcess(this.consoleParameters), Times.Once);
        }

        [TestMethod]
        public async Task StartSessionAsyncShouldThrowExceptionOnBadPort()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

            await Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await this.consoleWrapper.StartSessionAsync());
        }

        [TestMethod]
        public async Task StartSessionShouldCallWhenProcessNotInitializedAsync()
        {
            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);

            // To call private method EnsureInitialize call InitializeExtensions
            await this.consoleWrapper.InitializeExtensionsAsync(new[] { "path/to/adapter" });

            this.mockProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ConsoleParameters>()));
        }

        [TestMethod]
        public async Task InitializeExtensionsAsyncShouldCachePathToExtensions()
        {
            var pathToExtensions = new[] { "path/to/adapter" };
            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(true);

            await this.consoleWrapper.InitializeExtensionsAsync(pathToExtensions);

            this.mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);

            await this.consoleWrapper.InitializeExtensionsAsync(pathToExtensions);

            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToExtensions), Times.Exactly(3));
        }

        [TestMethod]
        public void ProcessExitedEventShouldSetOnProcessExit()
        {
            this.mockProcessManager.Raise(pm => pm.ProcessExited += null, EventArgs.Empty);

            this.mockRequestSender.Verify(rs => rs.OnProcessExited(), Times.Once);
        }

        [TestMethod]
        public async Task InitializeExtensionsAsyncShouldSucceed()
        {
            var pathToAdditionalExtensions = new List<string> { "Hello", "World" };

            await this.consoleWrapper.InitializeExtensionsAsync(pathToAdditionalExtensions);

            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToAdditionalExtensions), Times.Once);
        }

        [TestMethod]
        public async Task InitializeExtensionsAsyncShouldThrowExceptionOnBadConnection()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

            await Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await this.consoleWrapper.InitializeExtensionsAsync(new List<string> { "Hello", "World" }));
            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldSucceed()
        {
            await this.consoleWrapper.DiscoverTestsAsync(this.testSources, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(this.testSources, null, It.IsAny<TestPlatformOptions>(), It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldPassTestDiscoveryHandler2IfTestDiscoveryHandler1IsInput()
        {
            await this.consoleWrapper.DiscoverTestsAsync(this.testSources, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(this.testSources, null, It.IsAny<TestPlatformOptions>(), It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAndNullOptionsAsyncShouldSucceedOnNullOptions()
        {
            await this.consoleWrapper.DiscoverTestsAsync(this.testSources, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(this.testSources, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAndOptionsAsyncShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await this.consoleWrapper.DiscoverTestsAsync(this.testSources, null, options, new Mock<ITestDiscoveryEventsHandler2>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(this.testSources, null, options, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldThrowExceptionOnBadConnection()
        {
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(-1);

            await Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await this.consoleWrapper.DiscoverTestsAsync(new List<string> { "Hello", "World" }, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object));
            this.mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<TestPlatformOptions>(), It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldSucceed()
        {
            await this.consoleWrapper.RunTestsAsync(this.testSources, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testSources, "RunSettings", null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }


        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndNullOptionsShouldSucceedOnNullOptions()
        {
            await this.consoleWrapper.RunTestsAsync(this.testSources, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testSources, "RunSettings", null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }


        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndOptionsShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await this.consoleWrapper.RunTestsAsync(this.testSources, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testSources, "RunSettings", options, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndCustomHostShouldSucceed()
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testSources,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testSources, "RunSettings", null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndOptionsUsingCustomHostShouldSucceedOnNullOptions()
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testSources,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testSources, "RunSettings", null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesOnOptionsUsingCustomHostShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();

            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testSources,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testSources, "RunSettings", options, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsShouldSucceed()
        {
            await this.consoleWrapper.RunTestsAsync(this.testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsShouldSucceedOnNullOptions()
        {
            await this.consoleWrapper.RunTestsAsync(this.testCases, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testCases, "RunSettings", null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();

            await this.consoleWrapper.RunTestsAsync(this.testCases, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunAsync(this.testCases, "RunSettings", options, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testCases,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsUsingCustomLauncherShouldSucceedOnNullOptions()
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testCases,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testCases, "RunSettings", null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsUsingCustomLauncherShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                this.testCases,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(this.testCases, "RunSettings", options, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void EndSessionShouldSucceed()
        {
            this.consoleWrapper.EndSession();

            this.mockRequestSender.Verify(rs => rs.EndSession(), Times.Once);
            this.mockRequestSender.Verify(rs => rs.Close(), Times.Once);
        }
    }
}
