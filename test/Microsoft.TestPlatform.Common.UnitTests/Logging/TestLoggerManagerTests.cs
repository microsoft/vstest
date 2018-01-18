// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Tests the behaviors of the TestLoggerManager class.
    /// </summary>
    [TestClass]
    public class TestLoggerManagerTests
    {
        private static int counter = 0;
        private static EventWaitHandle waitHandle = new AutoResetEvent(false);
        private string loggerUri = "testlogger://logger";

        [TestInitialize]
        public void Initialize()
        {
            TestPluginCacheTests.SetupMockExtensions();
        }

        [TestCleanup]
        public void Cleanup()
        {
            new DummyTestLoggerManager().Cleanup();
        }

        [TestMethod]
        public void TryGetUriFromFriendlyNameShouldReturnUriIfLoggerIsAdded()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            string uri;
            testLoggerManager.TryGetUriFromFriendlyName("TestLoggerExtension", out uri);
            Assert.AreEqual(uri, loggerUri);
        }

        [TestMethod]
        public void TryGetUriFromFriendlyNameShouldNotReturnUriIfLoggerIsNotAdded()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            string uri;
            testLoggerManager.TryGetUriFromFriendlyName("TestLoggerExtension1", out uri);
            Assert.IsNull(uri);
        }

        [TestMethod]
        public void GetResultsDirectoryShouldReturnNullIfRunSettingsIsNull()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            string result = testLoggerManager.GetResultsDirectory(null);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void GetResultsDirectoryShouldReadResultsDirectoryFromSettingsIfSpecified()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
    <RunSettings>     
      <RunConfiguration> 
        <MaxCpuCount>0</MaxCpuCount>       
        <ResultsDirectory>DummyTestResultsFolder</ResultsDirectory>             
        <TargetPlatform> x64 </TargetPlatform>     
        <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion> 
      </RunConfiguration>     
    </RunSettings> ";

            RunSettings runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runSettingsXml);

            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            string result = testLoggerManager.GetResultsDirectory(runsettings);
            Assert.AreEqual(string.Compare("DummyTestResultsFolder", result), 0);
        }

        [TestMethod]
        public void GetResultsDirectoryShouldReturnDefaultPathIfResultsDirectoryIsNotProvidedInRunSettings()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
    <RunSettings>     
      <RunConfiguration> 
        <MaxCpuCount>0</MaxCpuCount>       
        <TargetPlatform> x64 </TargetPlatform>     
        <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion> 
      </RunConfiguration>     
    </RunSettings> ";

            RunSettings runsettings = new RunSettings();
            runsettings.LoadSettingsXml(runSettingsXml);

            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            string result = testLoggerManager.GetResultsDirectory(runsettings);

            Assert.AreEqual(string.Compare(Constants.DefaultResultsDirectory, result), 0);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunMessageHandlerOfLoggersIfRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.TestRunMessage += null,
                new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldNotInvokeTestRunMessageHandlerOfLoggersIfUnRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.TestRunMessage += null,
                new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            testLoggerManager.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.TestRunMessage += null,
                new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunCompleteHandlerOfLoggersIfRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldNotInvokeTestRunCompleteHandlerOfLoggersIfUnRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            testLoggerManager.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunChangedHandlerOfLoggersIfRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunStatsChange += null,
                new TestRunChangedEventArgs(
                    null,
                    new List<ObjectModel.TestResult>()
                        {
                            new ObjectModel.TestResult(
                                new TestCase(
                                "This is a string.",
                                new Uri("some://uri"),
                                "This is a string."))
                        },
                    null));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldNotInvokeTestRunChangedHandlerOfLoggersIfUnRegistered()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            testLoggerManager.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            testLoggerManager.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunStatsChange += (e, a) => { waitHandle.Set(); },
                new TestRunChangedEventArgs(
                    null,
                    new List<ObjectModel.TestResult>()
                        {
                            new ObjectModel.TestResult(
                                new TestCase(
                                "This is a string.",
                                new Uri("some://uri"),
                                "This is a string."))
                        },
                    null));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            testLoggerManager.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunStatsChange += null,
                new TestRunChangedEventArgs(
                    null,
                    new List<ObjectModel.TestResult>()
                        {
                            new ObjectModel.TestResult(
                                new TestCase(
                                "This is a string.",
                                new Uri("some://uri"),
                                "This is a string."))
                        },
                    null));
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void AddLoggerShouldNotThrowExceptionIfUriIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        testLoggerManager.AddLogger(null, null);
                    });
        }

        [TestMethod]
        public void AddLoggerShouldNotThrowExceptionIfUriIsNonExistent()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            Assert.ThrowsException<InvalidOperationException>(
                () =>
                    {
                        testLoggerManager.AddLogger(new Uri("logger://NotALogger"), null);
                    });
        }

        [TestMethod]
        public void AddLoggerShouldAddDefaultLoggerParameterForTestLoggerWithParameters()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            ValidLoggerWithParameters.Reset();
            testLoggerManager.AddLogger(new Uri("test-logger-with-parameter://logger"), new Dictionary<string, string>());
            Assert.IsNotNull(ValidLoggerWithParameters.parameters, "parameters not getting passed");
            Assert.IsTrue(
                ValidLoggerWithParameters.parameters.ContainsKey(DefaultLoggerParameterNames.TestRunDirectory),
                $"{DefaultLoggerParameterNames.TestRunDirectory} not added to parameters");
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(
                    ValidLoggerWithParameters.parameters[DefaultLoggerParameterNames.TestRunDirectory]),
                $"parameter {DefaultLoggerParameterNames.TestRunDirectory} should not be null, empty or whitespace");
        }

        [TestMethod]
        public void DisposeShouldNotThrowExceptionIfCalledMultipleTimes()
        {
            // Dispose the logger manager multiple times and verify that no exception is thrown.
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            testLoggerManager.Dispose();
            testLoggerManager.Dispose();
        }

        [TestMethod]
        public void AddLoggerShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            testLoggerManager.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(
                () =>
                    {
                        testLoggerManager.AddLogger(new Uri("some://uri"), null);
                    });
        }


        [TestMethod]
        public void EnableLoggingShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            var testLoggerManager = new TestLoggerManager(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance));
            testLoggerManager.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(
                () =>
                    {
                        TestLoggerManager.Instance.EnableLogging();
                    });
        }

        [TestMethod]
        public void RegisterTestRunEventsThrowsExceptionWithNullasArgument()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        TestLoggerManager.Instance.RegisterTestRunEvents(null);
                    });
        }

        [TestMethod]
        public void LoggerInitialzeShouldCollectLoggersForTelemetry()
        {
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();

            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            TestLoggerManager.Instance.AddLogger(new Uri(this.loggerUri), new Dictionary<string, string>());

            // Act.
            TestLoggerManager.Instance.InitializeLoggers(mockRequestData.Object);

            // Verify
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, new Uri(this.loggerUri)));
        }

        /// <summary>
        /// DiscoveryStart event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void DiscoveryRequestRaiseShouldInvokeDiscoveryStartHandlerOfLoggersOnlyIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();
          
            DiscoveryCriteria discoveryCriteria = new DiscoveryCriteria() { TestCaseFilter = "Name=Test1" };
            DiscoveryStartEventArgs discoveryStartEventArgs = new DiscoveryStartEventArgs(discoveryCriteria);
          
            // mock for IDiscoveryRequest
            var discoveryRequest = new Mock<IDiscoveryRequest>();

            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // Register DiscoveryRequest object
            TestLoggerManager.Instance.RegisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryStart += null,
                discoveryStartEventArgs);
          
            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            // Unregister DiscoveryRequest object
            TestLoggerManager.Instance.UnregisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryStart += null,
                discoveryStartEventArgs);
            // Assertions when discovery events unregistered
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveredTests event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void DiscoveryRequestRaiseShouldInvokeDiscoveredTestsHandlerOfLoggersOnlyIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();

            List<TestCase> testCases = new List<TestCase> { new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName") };
            DiscoveredTestsEventArgs discoveredTestsEventArgs = new DiscoveredTestsEventArgs(testCases);

            // mock for IDiscoveryRequest
            var discoveryRequest = new Mock<IDiscoveryRequest>();

            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // Register DiscoveryRequest object
            TestLoggerManager.Instance.RegisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveredTests += null,
                discoveredTestsEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            // Unregister DiscoveryRequest object
            TestLoggerManager.Instance.UnregisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveredTests += null,
                discoveredTestsEventArgs);

            // Assertions when discovery events unregistered
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// TestRunStart event handler of loggers should be called only if test run events are registered.
        /// </summary>
        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunStartHandlerOfLoggersOnlyIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();
  
            TestRunCriteria testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10) { TestCaseFilter = "Name=Test1" };
            TestRunStartEventArgs testRunStartEventArgs = new TestRunStartEventArgs(testRunCriteria);

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();
  
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunStart += null,
                testRunStartEventArgs);

            // Assertions when test run events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            // Unregister TestRunRequest object
            TestLoggerManager.Instance.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunStart += null,
                testRunStartEventArgs);

            // Assertions when test run events unregistered
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryComplete event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void DiscoveryRequestRaiseShouldInvokeDiscoveryCompleteHandlerOfLoggersOnlyIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();

            DiscoveryCompleteEventArgs discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(2, false);

            // mock for IDiscoveryRequest
            var discoveryRequest = new Mock<IDiscoveryRequest>();

            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // Register DiscoveryRequest object
            TestLoggerManager.Instance.RegisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryComplete += null,
                discoveryCompleteEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            // Unregister DiscoveryRequest object
            TestLoggerManager.Instance.UnregisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryComplete += null,
                discoveryCompleteEventArgs);

            // Assertions when discovery events unregistered
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryMessage event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void DiscoveryRequestRaiseShouldInvokeDiscoveryMessageHandlerOfLoggersOnlyIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();

            string message = "This is the test message";
            TestRunMessageEventArgs testRunMessageEventArgs = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);

            // mock for IDiscoveryRequest
            var discoveryRequest = new Mock<IDiscoveryRequest>();

            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // Register DiscoveryRequest object
            TestLoggerManager.Instance.RegisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryMessage += null,
                testRunMessageEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            // Unregister DiscoveryRequest object
            TestLoggerManager.Instance.UnregisterDiscoveryEvents(discoveryRequest.Object);

            //Raise an event on mock object
            discoveryRequest.Raise(
                m => m.OnDiscoveryMessage += null,
                testRunMessageEventArgs);

            // Assertions when discovery events unregistered
            Assert.AreEqual(counter, 1);
        }

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger3 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                events.TestRunMessage += TestMessageHandler;
                events.TestRunComplete += Events_TestRunComplete;
                events.TestResult += Events_TestResult;
                events.TestRunStart += TestRunStartHandler;
              
                events.DiscoveryStart += DiscoveryStartHandler;
                events.DiscoveryMessage += DiscoveryMessageHandler;
                events.DiscoveredTests += DiscoveredTestsHandler;
                events.DiscoveryComplete += DiscoveryCompleteHandler;
            }

            private void Events_TestResult(object sender, TestResultEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

            private void Events_TestRunComplete(object sender, TestRunCompleteEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();

            }

            private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
            {
                if (e.Message.Equals("TestRunMessage"))
                {
                    TestLoggerManagerTests.counter++;
                    TestLoggerManagerTests.waitHandle.Set();

                }
            }

            private void TestRunStartHandler(object sender, TestRunStartEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

            private void DiscoveryMessageHandler(object sender, TestRunMessageEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

            private void DiscoveryStartHandler(object sender, DiscoveryStartEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

            private void DiscoveredTestsHandler(object sender, DiscoveredTestsEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

            private void DiscoveryCompleteHandler(object sender, DiscoveryCompleteEventArgs e)
            {
                TestLoggerManagerTests.counter++;
                TestLoggerManagerTests.waitHandle.Set();
            }

        }

        [ExtensionUri("test-logger-with-parameter://logger")]
        [FriendlyName("TestLoggerWithParameterExtension")]
        private class ValidLoggerWithParameters : ITestLoggerWithParameters
        {
            public static Dictionary<string, string> parameters;
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {

            }

            public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
            {
                ValidLoggerWithParameters.parameters = parameters;
            }

            public static void Reset()
            {
                ValidLoggerWithParameters.parameters = null;
            }
        }

        internal class DummyTestLoggerManager : TestLoggerManager
        {
            public DummyTestLoggerManager() : base(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance)) 
            {

            }

            public void Cleanup()
            {
                Instance = null;
            }
        }
    }
}

