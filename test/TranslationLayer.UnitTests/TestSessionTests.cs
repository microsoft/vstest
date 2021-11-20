// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public class TestSessionTests
    {
        private readonly string testSettings = "TestSettings";
        private readonly List<string> testSources = new List<string> { "Hello", "World" };
        private readonly List<TestCase> testCases = new List<TestCase>
        {
            new TestCase("a.b.c", new Uri("d://uri"), "a.dll"),
            new TestCase("d.e.f", new Uri("g://uri"), "d.dll")
        };

        private TestSessionInfo testSessionInfo;
        private ITestSession testSession;
        private Mock<ITestSessionEventsHandler> mockTestSessionEventsHandler;
        private Mock<IVsTestConsoleWrapper> mockVsTestConsoleWrapper;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testSessionInfo = new TestSessionInfo();
            this.mockTestSessionEventsHandler = new Mock<ITestSessionEventsHandler>();
            this.mockVsTestConsoleWrapper = new Mock<IVsTestConsoleWrapper>();

            this.testSession = new TestSession(
                this.testSessionInfo,
                this.mockTestSessionEventsHandler.Object,
                this.mockVsTestConsoleWrapper.Object);
        }

        #region ITestSession
        [TestMethod]
        public void AbortTestRunShouldCallConsoleWrapperAbortTestRun()
        {
            this.testSession.AbortTestRun();

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.AbortTestRun(),
                Times.Once);
        }

        [TestMethod]
        public void CancelDiscoveryShouldCallConsoleWrapperCancelDiscovery()
        {
            this.testSession.CancelDiscovery();

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.CancelDiscovery(),
                Times.Once);
        }

        [TestMethod]
        public void CancelTestRunShouldCallConsoleWrapperCancelTestRun()
        {
            this.testSession.CancelTestRun();

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.CancelTestRun(),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments1()
        {
            this.testSession.DiscoverTests(
                this.testSources,
                this.testSettings,
                new Mock<ITestDiscoveryEventsHandler>().Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTests(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testSession.DiscoverTests(
                this.testSources,
                this.testSettings,
                testPlatformOptions,
                mockTestDiscoveryEventsHandler.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTests(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestDiscoveryEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testSession.RunTests(
                this.testSources,
                this.testSettings,
                mockTestRunEventsHandler.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testSession.RunTests(
                this.testSources,
                this.testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testSession.RunTests(
                this.testCases,
                this.testSettings,
                mockTestRunEventsHandler.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    this.testCases,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testSession.RunTests(
                this.testCases,
                this.testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            this.testSession.RunTestsWithCustomTestHost(
                this.testSources,
                this.testSettings,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            this.testSession.RunTestsWithCustomTestHost(
                this.testSources,
                this.testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            this.testSession.RunTestsWithCustomTestHost(
                this.testCases,
                this.testSettings,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    this.testCases,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            this.testSession.RunTestsWithCustomTestHost(
                this.testCases,
                this.testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void StopTestSessionShouldCallConsoleWrapperStopTestSessionWithCorrectArguments1()
        {
            this.testSession.StopTestSession();

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSession(
                    this.testSessionInfo,
                    this.mockTestSessionEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void StopTestSessionShouldCallConsoleWrapperStopTestSessionWithCorrectArguments2()
        {
            var mockTestSessionEventsHandler2 = new Mock<ITestSessionEventsHandler>();

            this.testSession.StopTestSession(mockTestSessionEventsHandler2.Object);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSession(
                    this.testSessionInfo,
                    mockTestSessionEventsHandler2.Object),
                Times.Once);
        }
        #endregion

        #region ITestSessionAsync
        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments1()
        {
            await this.testSession.DiscoverTestsAsync(
                    this.testSources,
                    this.testSettings,
                    new Mock<ITestDiscoveryEventsHandler>().Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTestsAsync(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            await this.testSession.DiscoverTestsAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    mockTestDiscoveryEventsHandler.Object)
                .ConfigureAwait(false); ;

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTestsAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestDiscoveryEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await this.testSession.RunTestsAsync(
                    this.testSources,
                    this.testSettings,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await this.testSession.RunTestsAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await this.testSession.RunTestsAsync(
                    this.testCases,
                    this.testSettings,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    this.testCases,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await this.testSession.RunTestsAsync(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false); ;

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await this.testSession.RunTestsWithCustomTestHostAsync(
                    this.testSources,
                    this.testSettings,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    this.testSources,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await this.testSession.RunTestsWithCustomTestHostAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    this.testSources,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await this.testSession.RunTestsWithCustomTestHostAsync(
                    this.testCases,
                    this.testSettings,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    this.testCases,
                    this.testSettings,
                    null,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await this.testSession.RunTestsWithCustomTestHostAsync(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    this.testCases,
                    this.testSettings,
                    testPlatformOptions,
                    this.testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task StopTestSessionAsyncShouldCallConsoleWrapperStopTestSessionWithCorrectArguments1()
        {
            await this.testSession.StopTestSessionAsync().ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSessionAsync(
                    this.testSessionInfo,
                    this.mockTestSessionEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task StopTestSessionAsyncShouldCallConsoleWrapperStopTestSessionWithCorrectArguments2()
        {
            var mockTestSessionEventsHandler2 = new Mock<ITestSessionEventsHandler>();

            await this.testSession.StopTestSessionAsync(
                    mockTestSessionEventsHandler2.Object)
                .ConfigureAwait(false);

            this.mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSessionAsync(
                    this.testSessionInfo,
                    mockTestSessionEventsHandler2.Object),
                Times.Once);
        }
        #endregion
    }
}
