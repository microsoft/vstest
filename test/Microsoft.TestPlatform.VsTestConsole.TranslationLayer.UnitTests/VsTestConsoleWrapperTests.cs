// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VsTestConsoleWrapperTests
    {
        private IVsTestConsoleWrapper consoleWrapper;

        private Mock<IProcessManager> mockProcessManager;

        private Mock<ITranslationLayerRequestSender> mockRequestSender;

        private List<string> testSources = new List<string> { "Hello", "World" };

        private List<TestCase> testCases = new List<TestCase>
                                              {
                                                  new TestCase("a.b.c", new Uri("d://uri"), "a.dll"),
                                                  new TestCase("d.e.f", new Uri("g://uri"), "d.dll")
                                              };

        private ConsoleParameters consoleParamters;

        [TestInitialize]
        public void TestInitialize()
        {
            this.consoleParamters = new ConsoleParameters();

            this.mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            this.mockProcessManager = new Mock<IProcessManager>();
            this.consoleWrapper = new VsTestConsoleWrapper(this.mockRequestSender.Object, this.mockProcessManager.Object, this.consoleParamters);

            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);
        }

        [TestMethod]
        public void StartSessionShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(inputPort);

            this.consoleWrapper.StartSession();

            Assert.AreEqual(expectedParentProcessId, this.consoleParamters.ParentProcessId, "Parent process Id must be set");
            Assert.AreEqual(inputPort, this.consoleParamters.PortNumber, "Port number must be set");

            this.mockProcessManager.Verify(pm => pm.StartProcess(this.consoleParamters), Times.Once);
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
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.InitializeExtensions(new List<string> { "Hello", "World" }));
            this.mockRequestSender.Verify(rs => rs.InitializeExtensions(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldSucceed()
        {
            this.consoleWrapper.DiscoverTests(this.testSources, null, new Mock<ITestDiscoveryEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.DiscoverTests(this.testSources, null, It.IsAny<ITestDiscoveryEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldThrowExceptionOnBadConnection()
        {
            this.mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.DiscoverTests(new List<string> { "Hello", "World" }, null, new Mock<ITestDiscoveryEventsHandler>().Object));
            this.mockRequestSender.Verify(rs => rs.DiscoverTests(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<ITestDiscoveryEventsHandler>()), Times.Never);
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldSucceed()
        {
            this.consoleWrapper.RunTests(this.testSources, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testSources, "RunSettings", It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomHostShouldSucceed()
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testSources,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testSources, "RunSettings", It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsShouldSucceed()
        {
            this.consoleWrapper.RunTests(this.testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRun(this.testCases, "RunSettings", It.IsAny<ITestRunEventsHandler>()), Times.Once);
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                this.testCases,
                "RunSettings",
                new Mock<ITestRunEventsHandler>().Object,
                new Mock<ITestHostLauncher>().Object);

            this.mockRequestSender.Verify(rs => rs.StartTestRunWithCustomHost(this.testCases, "RunSettings", It.IsAny<ITestRunEventsHandler>(), It.IsAny<ITestHostLauncher>()), Times.Once);
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
