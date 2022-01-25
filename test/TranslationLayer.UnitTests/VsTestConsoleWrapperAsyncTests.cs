// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VsTestConsoleWrapperAsyncTests
    {
        private IVsTestConsoleWrapper consoleWrapper;

        private Mock<IProcessManager> mockProcessManager;

        private Mock<ITranslationLayerRequestSender> mockRequestSender;

        private Mock<IProcessHelper> mockProcessHelper;

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
        public async Task StartSessionAsyncShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(inputPort));

            await consoleWrapper.StartSessionAsync();

            Assert.AreEqual(expectedParentProcessId, consoleParameters.ParentProcessId, "Parent process Id must be set");
            Assert.AreEqual(inputPort, consoleParameters.PortNumber, "Port number must be set");

            mockProcessManager.Verify(pm => pm.StartProcess(consoleParameters), Times.Once);
        }

        [TestMethod]
        public void StartSessionAsyncShouldThrowExceptionOnBadPort()
        {
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(-1));

            Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await consoleWrapper.StartSessionAsync());
        }

        [TestMethod]
        public async Task StartSessionShouldCallWhenProcessNotInitializedAsync()
        {
            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);

            // To call private method EnsureInitialize call InitializeExtensions
            await consoleWrapper.InitializeExtensionsAsync(new[] { "path/to/adapter" });

            mockProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ConsoleParameters>()));
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task StartTestSessionAsyncShouldCallRequestSenderWithCorrectArguments1()
        {
            var testSessionInfo = new TestSessionInfo();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    null,
                    mockEventsHandler.Object,
                    null))
                .Returns(Task.FromResult(testSessionInfo));

            Assert.AreEqual(
                (await consoleWrapper.StartTestSessionAsync(
                    testSources,
                    null,
                    mockEventsHandler.Object).ConfigureAwait(false)).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    null,
                    mockEventsHandler.Object,
                    null),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task StartTestSessionAsyncShouldCallRequestSenderWithCorrectArguments2()
        {
            var testSessionInfo = new TestSessionInfo();
            var testPlatformOptions = new TestPlatformOptions();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    null))
                .Returns(Task.FromResult(testSessionInfo));

            Assert.AreEqual(
                (await consoleWrapper.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object).ConfigureAwait(false)).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    null),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task StartTestSessionAsyncShouldCallRequestSenderWithCorrectArguments3()
        {
            var testSessionInfo = new TestSessionInfo();
            var testPlatformOptions = new TestPlatformOptions();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();
            var mockTesthostLauncher = new Mock<ITestHostLauncher>();

            mockRequestSender.Setup(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object))
                .Returns(Task.FromResult(testSessionInfo));

            Assert.AreEqual(
                (await consoleWrapper.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object).ConfigureAwait(false)).TestSessionInfo,
                testSessionInfo);

            mockRequestSender.Verify(
                rs => rs.StartTestSessionAsync(
                    testSources,
                    null,
                    testPlatformOptions,
                    mockEventsHandler.Object,
                    mockTesthostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task StopTestSessionAsyncShouldCallRequestSenderWithCorrectArguments()
        {
            var testSessionInfo = new TestSessionInfo();
            var mockEventsHandler = new Mock<ITestSessionEventsHandler>();

            mockRequestSender.Setup(
                rs => rs.StopTestSessionAsync(
                    It.IsAny<TestSessionInfo>(),
                    It.IsAny<ITestSessionEventsHandler>()))
                .Returns(Task.FromResult(true));

            Assert.IsTrue(
                await consoleWrapper.StopTestSessionAsync(
                    testSessionInfo,
                    mockEventsHandler.Object).ConfigureAwait(false));

            mockRequestSender.Verify(
                rs => rs.StopTestSessionAsync(
                    testSessionInfo,
                    mockEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task InitializeExtensionsAsyncShouldCachePathToExtensions()
        {
            var pathToExtensions = new[] { "path/to/adapter" };
            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(true);

            await consoleWrapper.InitializeExtensionsAsync(pathToExtensions);

            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(100));

            await consoleWrapper.InitializeExtensionsAsync(pathToExtensions);

            mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToExtensions), Times.Exactly(3));
        }

        [TestMethod]
        public void ProcessExitedEventShouldSetOnProcessExit()
        {
            mockProcessManager.Raise(pm => pm.ProcessExited += null, EventArgs.Empty);

            mockRequestSender.Verify(rs => rs.OnProcessExited(), Times.Once);
        }

        [TestMethod]
        public async Task InitializeExtensionsAsyncShouldSucceed()
        {
            var pathToAdditionalExtensions = new List<string> { "Hello", "World" };

            await consoleWrapper.InitializeExtensionsAsync(pathToAdditionalExtensions);

            mockRequestSender.Verify(rs => rs.InitializeExtensions(pathToAdditionalExtensions), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsAsyncShouldThrowExceptionOnBadConnection()
        {
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(-1));

            Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await consoleWrapper.InitializeExtensionsAsync(new List<string> { "Hello", "World" }));
            mockRequestSender.Verify(rs => rs.InitializeExtensions(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldSucceed()
        {
            await consoleWrapper.DiscoverTestsAsync(testSources, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(testSources, null, It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldPassTestDiscoveryHandler2IfTestDiscoveryHandler1IsInput()
        {
            await consoleWrapper.DiscoverTestsAsync(testSources, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(testSources, null, It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAndNullOptionsAsyncShouldSucceedOnNullOptions()
        {
            await consoleWrapper.DiscoverTestsAsync(testSources, null, null, new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(testSources, null, null, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAndOptionsAsyncShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await consoleWrapper.DiscoverTestsAsync(testSources, null, options, new Mock<ITestDiscoveryEventsHandler2>().Object);

            mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(testSources, null, options, null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsAsyncShouldThrowExceptionOnBadConnection()
        {
            mockRequestSender.Setup(rs => rs.InitializeCommunicationAsync(It.IsAny<int>())).Returns(Task.FromResult(-1));

            Assert.ThrowsExceptionAsync<TransationLayerException>(async () => await consoleWrapper.DiscoverTestsAsync(new List<string> { "Hello", "World" }, null, new TestPlatformOptions(), new Mock<ITestDiscoveryEventsHandler2>().Object));
            mockRequestSender.Verify(rs => rs.DiscoverTestsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
        }

        [TestMethod]
        public async Task DiscoverTestsShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();

            await consoleWrapper.DiscoverTestsAsync(
                    testSources,
                    null,
                    null,
                    testSessionInfo,
                    new Mock<ITestDiscoveryEventsHandler2>().Object)
                .ConfigureAwait(false);

            mockRequestSender.Verify(
                rs => rs.DiscoverTestsAsync(
                    testSources,
                    null,
                    null,
                    testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldSucceed()
        {
            await consoleWrapper.RunTestsAsync(testSources, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndNullOptionsShouldSucceedOnNullOptions()
        {
            await consoleWrapper.RunTestsAsync(testSources, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndOptionsShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await consoleWrapper.RunTestsAsync(testSources, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testSources, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            await consoleWrapper.RunTestsAsync(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    new Mock<ITestRunEventsHandler>().Object)
                .ConfigureAwait(false);

            mockRequestSender.Verify(
                rs => rs.StartTestRunAsync(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndCustomHostShouldSucceed()
        {
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testSources,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndOptionsUsingCustomHostShouldSucceedOnNullOptions()
        {
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testSources,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testSources, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesOnOptionsUsingCustomHostShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();

            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testSources,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testSources, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndACustomHostShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    new Mock<ITestRunEventsHandler>().Object,
                    new Mock<ITestHostLauncher>().Object)
                .ConfigureAwait(false);

            mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHostAsync(
                    testSources,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>(),
                    It.IsAny<ITestHostLauncher>()),
                    Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsShouldSucceed()
        {
            await consoleWrapper.RunTestsAsync(testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsShouldSucceedOnNullOptions()
        {
            await consoleWrapper.RunTestsAsync(testCases, "RunSettings", null, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();

            await consoleWrapper.RunTestsAsync(testCases, "RunSettings", options, new Mock<ITestRunEventsHandler>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunAsync(testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            await consoleWrapper.RunTestsAsync(
                    testCases,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    new Mock<ITestRunEventsHandler>().Object)
                .ConfigureAwait(false);

            mockRequestSender.Verify(
                rs => rs.StartTestRunAsync(
                    testCases,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    It.IsAny<ITestRunEventsHandler>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testCases,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testCases, "RunSettings", It.IsAny<TestPlatformOptions>(), null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsUsingCustomLauncherShouldSucceedOnNullOptions()
        {
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testCases,
                "RunSettings",
                null,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testCases, "RunSettings", null, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndOptionsUsingCustomLauncherShouldSucceedOnOptions()
        {
            var options = new TestPlatformOptions();
            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                testCases,
                "RunSettings",
                options,
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHostAsync(testCases, "RunSettings", options, null, It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSelectedTestsAndACustomHostShouldSucceedWhenUsingSessions()
        {
            var testSessionInfo = new TestSessionInfo();
            var options = new TestPlatformOptions() { TestCaseFilter = "PacMan" };

            await consoleWrapper.RunTestsWithCustomTestHostAsync(
                    testCases,
                    "RunSettings",
                    options,
                    testSessionInfo,
                    new Mock<ITestRunEventsHandler>().Object,
                    new Mock<ITestHostLauncher>().Object)
                .ConfigureAwait(false);

            mockRequestSender.Verify(
                rs => rs.StartTestRunWithCustomHostAsync(
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
        }
    }
}
