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
        private readonly List<string> testSources = new() { "Hello", "World" };
        private readonly List<TestCase> testCases = new()
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
            testSessionInfo = new TestSessionInfo();
            mockTestSessionEventsHandler = new Mock<ITestSessionEventsHandler>();
            mockVsTestConsoleWrapper = new Mock<IVsTestConsoleWrapper>();

            testSession = new TestSession(
                testSessionInfo,
                mockTestSessionEventsHandler.Object,
                mockVsTestConsoleWrapper.Object);
        }

        #region ITestSession
        [TestMethod]
        public void AbortTestRunShouldCallConsoleWrapperAbortTestRun()
        {
            testSession.AbortTestRun();

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.AbortTestRun(),
                Times.Once);
        }

        [TestMethod]
        public void CancelDiscoveryShouldCallConsoleWrapperCancelDiscovery()
        {
            testSession.CancelDiscovery();

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.CancelDiscovery(),
                Times.Once);
        }

        [TestMethod]
        public void CancelTestRunShouldCallConsoleWrapperCancelTestRun()
        {
            testSession.CancelTestRun();

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.CancelTestRun(),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments1()
        {
            testSession.DiscoverTests(
                testSources,
                testSettings,
                new Mock<ITestDiscoveryEventsHandler>().Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTests(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            testSession.DiscoverTests(
                testSources,
                testSettings,
                testPlatformOptions,
                mockTestDiscoveryEventsHandler.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTests(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestDiscoveryEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            testSession.RunTests(
                testSources,
                testSettings,
                mockTestRunEventsHandler.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            testSession.RunTests(
                testSources,
                testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            testSession.RunTests(
                testCases,
                testSettings,
                mockTestRunEventsHandler.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    testCases,
                    testSettings,
                    null,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            testSession.RunTests(
                testCases,
                testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTests(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            testSession.RunTestsWithCustomTestHost(
                testSources,
                testSettings,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
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

            testSession.RunTestsWithCustomTestHost(
                testSources,
                testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void RunTestsWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            testSession.RunTestsWithCustomTestHost(
                testCases,
                testSettings,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    testCases,
                    testSettings,
                    null,
                    testSessionInfo,
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

            testSession.RunTestsWithCustomTestHost(
                testCases,
                testSettings,
                testPlatformOptions,
                mockTestRunEventsHandler.Object,
                mockTestHostLauncher.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHost(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public void StopTestSessionShouldCallConsoleWrapperStopTestSessionWithCorrectArguments1()
        {
            testSession.StopTestSession();

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSession(
                    testSessionInfo,
                    mockTestSessionEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public void StopTestSessionShouldCallConsoleWrapperStopTestSessionWithCorrectArguments2()
        {
            var mockTestSessionEventsHandler2 = new Mock<ITestSessionEventsHandler>();

            testSession.StopTestSession(mockTestSessionEventsHandler2.Object);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSession(
                    testSessionInfo,
                    mockTestSessionEventsHandler2.Object),
                Times.Once);
        }
        #endregion

        #region ITestSessionAsync
        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments1()
        {
            await testSession.DiscoverTestsAsync(
                    testSources,
                    testSettings,
                    new Mock<ITestDiscoveryEventsHandler>().Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTestsAsync(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
                    It.IsAny<ITestDiscoveryEventsHandler2>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCallConsoleWrapperDiscoverTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            await testSession.DiscoverTestsAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    mockTestDiscoveryEventsHandler.Object)
                .ConfigureAwait(false); ;

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.DiscoverTestsAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestDiscoveryEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await testSession.RunTestsAsync(
                    testSources,
                    testSettings,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await testSession.RunTestsAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await testSession.RunTestsAsync(
                    testCases,
                    testSettings,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    testCases,
                    testSettings,
                    null,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesShouldCallConsoleWrapperRunTestsWithCorrectArguments2()
        {
            var testPlatformOptions = new TestPlatformOptions();
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            await testSession.RunTestsAsync(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object)
                .ConfigureAwait(false); ;

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsAsync(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithSourcesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await testSession.RunTestsWithCustomTestHostAsync(
                    testSources,
                    testSettings,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    testSources,
                    testSettings,
                    null,
                    testSessionInfo,
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

            await testSession.RunTestsWithCustomTestHostAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    testSources,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task RunTestsAsyncWithTestCasesAndCustomTesthostShouldCallConsoleWrapperRunTestsWithCorrectArguments1()
        {
            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var mockTestHostLauncher = new Mock<ITestHostLauncher>();

            await testSession.RunTestsWithCustomTestHostAsync(
                    testCases,
                    testSettings,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    testCases,
                    testSettings,
                    null,
                    testSessionInfo,
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

            await testSession.RunTestsWithCustomTestHostAsync(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.RunTestsWithCustomTestHostAsync(
                    testCases,
                    testSettings,
                    testPlatformOptions,
                    testSessionInfo,
                    mockTestRunEventsHandler.Object,
                    mockTestHostLauncher.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task StopTestSessionAsyncShouldCallConsoleWrapperStopTestSessionWithCorrectArguments1()
        {
            await testSession.StopTestSessionAsync().ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSessionAsync(
                    testSessionInfo,
                    mockTestSessionEventsHandler.Object),
                Times.Once);
        }

        [TestMethod]
        public async Task StopTestSessionAsyncShouldCallConsoleWrapperStopTestSessionWithCorrectArguments2()
        {
            var mockTestSessionEventsHandler2 = new Mock<ITestSessionEventsHandler>();

            await testSession.StopTestSessionAsync(
                    mockTestSessionEventsHandler2.Object)
                .ConfigureAwait(false);

            mockVsTestConsoleWrapper.Verify(
                vtcw => vtcw.StopTestSessionAsync(
                    testSessionInfo,
                    mockTestSessionEventsHandler2.Object),
                Times.Once);
        }
        #endregion
    }
}
