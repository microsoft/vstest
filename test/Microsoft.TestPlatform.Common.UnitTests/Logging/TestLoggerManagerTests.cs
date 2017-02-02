// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TestPlatform.Common.UnitTests.ExtensionFramework;
    using TestPlatform.Common.UnitTests.Utilities;
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
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            string uri;
            TestLoggerManager.Instance.TryGetUriFromFriendlyName("TestLoggerExtension", out uri);
            Assert.AreEqual(uri, loggerUri);
        }

        [TestMethod]
        public void TryGetUriFromFriendlyNameShouldNotReturnUriIfLoggerIsNotAdded()
        {
            string uri;
            TestLoggerManager.Instance.TryGetUriFromFriendlyName("TestLoggerExtension1", out uri);
            Assert.IsNull(uri);
        }

        [TestMethod]
        public void GetResultsDirectoryShouldReturnNullIfRunSettingsIsNull()
        {
            string result = TestLoggerManager.Instance.GetResultsDirectory(null);
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

            string result = TestLoggerManager.Instance.GetResultsDirectory(runsettings);
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

            string result = TestLoggerManager.Instance.GetResultsDirectory(runsettings);

            Assert.AreEqual(string.Compare(Constants.DefaultResultsDirectory, result), 0);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunMessageHandlerOfLoggersIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

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
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.TestRunMessage += null,
                new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            TestLoggerManager.Instance.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.TestRunMessage += null,
                new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunCompleteHandlerOfLoggersIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

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
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);

            TestLoggerManager.Instance.UnregisterTestRunEvents(testRunRequest.Object);

            //Raise an event on mock object
            testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void TestRunRequestRaiseShouldInvokeTestRunChangedHandlerOfLoggersIfRegistered()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

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
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            TestLoggerManager.Instance.AddLogger(new Uri(loggerUri), new Dictionary<string, string>());
            TestLoggerManager.Instance.EnableLogging();

            // mock for ITestRunRequest
            var testRunRequest = new Mock<ITestRunRequest>();

            // Register TestRunRequest object
            TestLoggerManager.Instance.RegisterTestRunEvents(testRunRequest.Object);

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

            TestLoggerManager.Instance.UnregisterTestRunEvents(testRunRequest.Object);

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
                        TestLoggerManager.Instance.AddLogger(null, null);
                    });
        }

        [TestMethod]
        public void AddLoggerShouldNotThrowExceptionIfUriIsNonExistent()
        {
            Assert.ThrowsException<InvalidOperationException>(
                () =>
                    {
                        TestLoggerManager.Instance.AddLogger(new Uri("logger://NotALogger"), null);
                    });
        }

        [TestMethod]
        public void AddLoggerShouldAddDefaultLoggerParameterForTestLoggerWithParameters()
        {
            ValidLoggerWithParameters.Reset();
            TestLoggerManager.Instance.AddLogger(new Uri("test-logger-with-parameter://logger"), new Dictionary<string, string>());
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
            var manager = TestLoggerManager.Instance;
            manager.Dispose();
            manager.Dispose();
        }

        [TestMethod]
        public void AddLoggerShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            TestLoggerManager.Instance.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(
                () =>
                    {
                        TestLoggerManager.Instance.AddLogger(new Uri("some://uri"), null);
                    });
        }


        [TestMethod]
        public void EnableLoggingShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            TestLoggerManager.Instance.Dispose();
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

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger3 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                events.TestRunMessage += TestMessageHandler;
                events.TestRunComplete += Events_TestRunComplete;
                events.TestResult += Events_TestResult;
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

