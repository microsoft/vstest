// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    using Moq;
    
    [TestClass]
    public class VsTestConsoleWrapperTests
    {
        private IVsTestConsoleWrapper consoleWrapper;

        private MockTranslationLayerSender mockSender;

        private MockProcessManager mockProcessManager;

        [TestInitialize]
        public void TestInit()
        {
            this.mockSender = new MockTranslationLayerSender();
            this.mockProcessManager = new MockProcessManager();
            this.consoleWrapper = new VsTestConsoleWrapper(mockSender, mockProcessManager);
        }

        [TestMethod]
        public void StartSessionShouldStartVsTestConsoleWithCorrectArguments()
        {
            var inputPort = 123;
            int expectedParentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string actualParentProcessIdString = "";
            string actualPortString = "";
            this.mockSender.SetupPort(inputPort);

            var startProcessCalled = false;
            this.mockProcessManager.VerifyArgs = (args) =>
            {
                startProcessCalled = true;
                actualParentProcessIdString = args.Length > 0 ? args[0] : "";
                actualPortString = args.Length > 1 ? args[1] : "";
            };

            this.consoleWrapper.StartSession();

            int actualPort = int.Parse(actualPortString.Split(':')[1]);
            int actualParentProcessId = int.Parse(actualParentProcessIdString.Split(':')[1]);
            Assert.IsTrue(startProcessCalled, "Start Process must be called");
            Assert.AreEqual(expectedParentProcessId, actualParentProcessId, "Incorrect Parent Process Id fed to process args");
            Assert.AreEqual(inputPort, actualPort, "Incorrect Port number fed to process args");
        }


        [TestMethod]
        public void StartSessionShouldThrowExceptionOnBadPort()
        {
            var inputPort = -1;
            this.mockSender.SetupPort(inputPort);

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.StartSession());
        }

        [TestMethod]
        public void StartSessionShouldCallWhenProcessNotInitialized()
        {
            Mock<IProcessManager> mockProcessManager  = new Mock<IProcessManager>();
            Mock<ITranslationLayerRequestSender> mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            this.consoleWrapper = new VsTestConsoleWrapper(mockRequestSender.Object, mockProcessManager.Object);

            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);

            // To call private method EnsureInitialize call InitializeExtensions
            this.consoleWrapper.InitializeExtensions(new[] { "path/to/adapter" });

            mockProcessManager.Verify(pm => pm.StartProcess(It.IsAny<string[]>()));
        }

        [TestMethod]
        public void InitializeExtensionShouldCachePathToExtensions()
        {
            Mock<IProcessManager> mockProcessManager = new Mock<IProcessManager>();
            Mock<ITranslationLayerRequestSender> mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            this.consoleWrapper = new VsTestConsoleWrapper(mockRequestSender.Object, mockProcessManager.Object);

            var pathToExtensions = new[] { "path/to/adapter" };
            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(true);
            mockRequestSender.Setup(rs => rs.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            this.consoleWrapper.InitializeExtensions(pathToExtensions);

            mockProcessManager.Setup(pm => pm.IsProcessInitialized()).Returns(false);
            mockRequestSender.Setup(rs => rs.InitializeCommunication()).Returns(100);

            this.consoleWrapper.InitializeExtensions(pathToExtensions);

            mockRequestSender.Verify( rs => rs.InitializeExtensions(pathToExtensions), Times.Exactly(3));
        }

        [TestMethod]
        public void ProcessExitedEventShouldSetOnProcessExit()
        {
            Mock<IProcessManager> mockProcessManager = new Mock<IProcessManager>();
            Mock<ITranslationLayerRequestSender> mockRequestSender = new Mock<ITranslationLayerRequestSender>();
            this.consoleWrapper = new VsTestConsoleWrapper(mockRequestSender.Object, mockProcessManager.Object);

            mockProcessManager.Raise(pm => pm.ProcessExited += null, EventArgs.Empty);

            mockRequestSender.Verify(rs => rs.OnProcessExited(), Times.Once);
        }

        [TestMethod]
        public void InitializeExtensionsShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool initExtCalled = false;
            Action<IEnumerable<string>, bool> assertPaths = (paths, loadOnlyWellKnownExtensions) =>
            {
                initExtCalled = true;
                Assert.IsTrue(paths != null && paths.Count() == 2, "Extension Paths must be set correctly.");
            };

            this.mockSender.SetInitExtFunc(assertPaths);

            this.consoleWrapper.InitializeExtensions(new List<string>() { "Hello", "World" });
            Assert.IsTrue(initExtCalled, "Initialize Extensions must be called");
        }


        [TestMethod]
        public void InitializeExtensionsShouldThrowExceptionOnBadConnection()
        {
            this.mockSender.SetConnectionResult(false);
            bool initExtCalled = false;
            Action<IEnumerable<string>, bool> assertPaths = (paths, loadOnlyWellKnownExtensions) =>
            {
                initExtCalled = true;
            };

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.InitializeExtensions(new List<string>() { "Hello", "World" }));

            Assert.IsFalse(initExtCalled, "Initialize Extensions must NOT be called if connection failed");
        }


        [TestMethod]
        public void DiscoverTestsShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool discoverTestsCalled = false;
            Action<IEnumerable<string>, string, ITestDiscoveryEventsHandler> assertSources = 
                (paths, settings, handler) =>
            {
                discoverTestsCalled = true;
                Assert.IsTrue(paths != null && paths.Count() == 2, "Sources must be set correctly.");
                Assert.IsNotNull(handler, "TestDiscoveryEventsHandler must be set correctly.");
            };

            this.mockSender.SetupDiscoverTests(assertSources);

            this.consoleWrapper.DiscoverTests(new List<string>() { "Hello", "World" }, null, new Mock<ITestDiscoveryEventsHandler>().Object);

            Assert.IsTrue(discoverTestsCalled, "Discover Tests must be called on translation layer");
        }

        [TestMethod]
        public void DiscoverTestsShouldThrowExceptionOnBadConnection()
        {
            this.mockSender.SetConnectionResult(false);

            bool discoverTestsCalled = false;
            Action<IEnumerable<string>, string, ITestDiscoveryEventsHandler> assertSources =
                (paths, settings, handler) =>
                {
                    discoverTestsCalled = true;
                };

            Assert.ThrowsException<TransationLayerException>(() => this.consoleWrapper.DiscoverTests(new List<string>() { "Hello", "World" }, null, new Mock<ITestDiscoveryEventsHandler>().Object));

            Assert.IsFalse(discoverTestsCalled, "Discover Tests must NOT be called on translation layer when connection is bad.");
        }

        [TestMethod]
        public void RunTestsWithSourcesShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool runTestsCalled = false;
            Action<IEnumerable<string>, string, ITestRunEventsHandler> assertSources =
                (sources, settings, handler) =>
                {
                    runTestsCalled = true;
                    Assert.IsTrue(sources != null && sources.Count() == 2, "Sources must be set correctly.");
                    Assert.IsTrue(!string.IsNullOrEmpty(settings), "RunSettings must be set correctly.");
                    Assert.IsNotNull(handler, "TestRunEventsHandler must be set correctly.");
                };

            this.mockSender.SetupRunTestsWithSources(assertSources);

            this.consoleWrapper.RunTests(new List<string>() { "Hello", "World" }, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            Assert.IsTrue(runTestsCalled, "Run Tests must be called on translation layer");
        }

        [TestMethod]
        public void RunTestsWithSourcesAndCustomHostShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool runTestsCalled = false;
            Action<IEnumerable<string>, string, ITestRunEventsHandler, ITestHostLauncher> assertSources =
                (sources, settings, handler, customLauncher) =>
                {
                    runTestsCalled = true;
                    Assert.IsTrue(sources != null && sources.Count() == 2, "Sources must be set correctly.");
                    Assert.IsTrue(!string.IsNullOrEmpty(settings), "RunSettings must be set correctly.");
                    Assert.IsNotNull(handler, "TestRunEventsHandler must be set correctly.");
                    Assert.IsNotNull(customLauncher, "Custom Launcher must be set correctly.");
                };

            this.mockSender.SetupRunTestsWithSourcesAndCustomHost(assertSources);

            this.consoleWrapper.RunTestsWithCustomTestHost(new List<string>() { "Hello", "World" }, "RunSettings", 
                new Mock<ITestRunEventsHandler>().Object, new Mock<ITestHostLauncher>().Object);

            Assert.IsTrue(runTestsCalled, "Run Tests must be called on translation layer");
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool runTestsCalled = false;
            Action<IEnumerable<TestCase>, string, ITestRunEventsHandler> assertTests =
                (tests, settings, handler) =>
                {
                    runTestsCalled = true;
                    Assert.IsTrue(tests != null && tests.Count() == 2, "TestCases must be set correctly.");
                    Assert.IsTrue(!string.IsNullOrEmpty(settings), "RunSettings must be set correctly.");
                    Assert.IsNotNull(handler, "TestRunEventsHandler must be set correctly.");
                };

            this.mockSender.SetupRunTestsWithSelectedTests(assertTests);

            var testCases = new List<TestCase>();
            testCases.Add(new TestCase("a.b.c", new Uri("d://uri"), "a.dll"));
            testCases.Add(new TestCase("d.e.f", new Uri("g://uri"), "d.dll"));

            this.consoleWrapper.RunTests(testCases, "RunSettings", new Mock<ITestRunEventsHandler>().Object);

            Assert.IsTrue(runTestsCalled, "Run Tests must be called on translation layer");
        }

        [TestMethod]
        public void RunTestsWithSelectedTestsAndCustomLauncherShouldSucceed()
        {
            this.mockSender.SetConnectionResult(true);

            bool runTestsCalled = false;
            Action<IEnumerable<TestCase>, string, ITestRunEventsHandler, ITestHostLauncher> assertTests =
                (tests, settings, handler, customLauncher) =>
                {
                    runTestsCalled = true;
                    Assert.IsTrue(tests != null && tests.Count() == 2, "TestCases must be set correctly.");
                    Assert.IsTrue(!string.IsNullOrEmpty(settings), "RunSettings must be set correctly.");
                    Assert.IsNotNull(handler, "TestRunEventsHandler must be set correctly.");
                    Assert.IsNotNull(customLauncher, "Custom Launcher must be set correctly.");
                };

            this.mockSender.SetupRunTestsWithSelectedTestsAndCustomHost(assertTests);

            var testCases = new List<TestCase>();
            testCases.Add(new TestCase("a.b.c", new Uri("d://uri"), "a.dll"));
            testCases.Add(new TestCase("d.e.f", new Uri("g://uri"), "d.dll"));

            this.consoleWrapper.RunTestsWithCustomTestHost(testCases, "RunSettings",
                new Mock<ITestRunEventsHandler>().Object, new Mock<ITestHostLauncher>().Object);

            Assert.IsTrue(runTestsCalled, "Run Tests must be called on translation layer");
        }

        [TestMethod]
        public void EndSessionShouldSucceed()
        {
            this.consoleWrapper.EndSession();

            Assert.IsTrue(this.mockSender.IsCloseCalled, "Close method must be called on sender");
            Assert.IsTrue(this.mockSender.IsSessionEnded, "SessionEnd method must be called on sender");
        }


        private class MockTranslationLayerSender : ITranslationLayerRequestSender
        {
            public bool IsCloseCalled = false;
            public bool IsSessionEnded = false;

            public void Close()
            {
                IsCloseCalled = true;
            }

            public void DiscoverTests(IEnumerable<string> sources, string runSettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
            {
                this.discoverFunc(sources, runSettings, discoveryEventsHandler);
            }

            public void Dispose()
            {
                
            }

            public void EndSession()
            {
                IsSessionEnded = true;
            }

            public int InitializeCommunication()
            {
                return port;
            }

            public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
            {
                this.initExtFunc(pathToAdditionalExtensions, false);
            }

            public void StartTestRun(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler)
            {
                this.runTestsWithSelectedTestsFunc.Invoke(testCases, runSettings, runEventsHandler);
            }

            public void StartTestRun(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler)
            {
                this.runTestsWithSourcesFunc.Invoke(sources, runSettings, runEventsHandler);
            }

            public void StartTestRunWithCustomHost(IEnumerable<string> sources, string runSettings, 
                ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher)
            {
                this.runTestsWithSourcesAndCustomLauncherFunc(sources, runSettings, runEventsHandler, customTestHostLauncher);
            }

            public void StartTestRunWithCustomHost(IEnumerable<TestCase> testCases, string runSettings, 
                ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher)
            {
                this.runTestsWithSelectedTestsAndCustomHostFunc(testCases, runSettings, runEventsHandler, customTestHostLauncher);
            }

            public bool WaitForRequestHandlerConnection(int connectionTimeout)
            {
                return this.connectionResult;
            }

            private int port;

            internal void SetupPort(int inputPort)
            {
                this.port = inputPort;
            }

            private bool connectionResult;

            internal void SetConnectionResult(bool connectionResult)
            {
                this.connectionResult = connectionResult;
            }

            private Action<IEnumerable<string>, bool> initExtFunc;

            internal void SetInitExtFunc(Action<IEnumerable<string>, bool> initExtFunc)
            {
                this.initExtFunc = initExtFunc;
            }

            private Action<IEnumerable<string>, string, ITestDiscoveryEventsHandler> discoverFunc;

            internal void SetupDiscoverTests(Action<IEnumerable<string>, string, ITestDiscoveryEventsHandler> discoverFunc)
            {
                this.discoverFunc = discoverFunc;
            }

            private Action<IEnumerable<string>, string, ITestRunEventsHandler> runTestsWithSourcesFunc;

            internal void SetupRunTestsWithSources(Action<IEnumerable<string>, string, ITestRunEventsHandler> runTestsFunc)
            {
                this.runTestsWithSourcesFunc = runTestsFunc;
            }


            private Action<IEnumerable<string>, string, ITestRunEventsHandler, ITestHostLauncher> runTestsWithSourcesAndCustomLauncherFunc;

            internal void SetupRunTestsWithSourcesAndCustomHost(Action<IEnumerable<string>, string, ITestRunEventsHandler, ITestHostLauncher> runTestsFunc)
            {
                this.runTestsWithSourcesAndCustomLauncherFunc = runTestsFunc;
            }

            private Action<IEnumerable<TestCase>, string, ITestRunEventsHandler> runTestsWithSelectedTestsFunc;

            internal void SetupRunTestsWithSelectedTests(Action<IEnumerable<TestCase>, string, ITestRunEventsHandler> runTestsFunc)
            {
                this.runTestsWithSelectedTestsFunc = runTestsFunc;
            }

            private Action<IEnumerable<TestCase>, string, ITestRunEventsHandler, ITestHostLauncher> runTestsWithSelectedTestsAndCustomHostFunc;

            internal void SetupRunTestsWithSelectedTestsAndCustomHost(Action<IEnumerable<TestCase>, string, ITestRunEventsHandler, ITestHostLauncher> runTestsFunc)
            {
                this.runTestsWithSelectedTestsAndCustomHostFunc = runTestsFunc;
            }

            public void CancelTestRun()
            {
                throw new NotImplementedException();
            }

            public void AbortTestRun()
            {
                throw new NotImplementedException();
            }

            public void OnProcessExited()
            {
                throw new NotImplementedException();
            }
        }


        private class MockProcessManager : IProcessManager
        {
            public Action<string[]> VerifyArgs;
            public event EventHandler ProcessExited;

            public bool IsProcessInitialized()
            {
                return true;
            }

            public void ShutdownProcess()
            {
                ProcessExited?.Invoke(this, null);

            }

            public void StartProcess(string[] args)
            {
                if(VerifyArgs != null)
                {
                    VerifyArgs(args);
                }
            }
        }
    }
}
