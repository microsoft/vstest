// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestPlatform.Common.Exceptions;

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using global::TestPlatform.Common.UnitTests.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

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
            TestPluginCacheTests.SetupMockExtensions(
                new string[] { typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location },
                () => { });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestPluginCacheTests.ResetExtensionsCache();
        }

        [TestMethod]
        public void TryGetUriFromFriendlyNameShouldReturnUriIfLoggerIsAdded()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.TryGetUriFromFriendlyName("TestLoggerExtension", out var uri);
            Assert.AreEqual(uri.ToString(), new Uri(loggerUri).ToString());
        }

        [TestMethod]
        public void TryGetUriFromFriendlyNameShouldNotReturnUriIfLoggerIsNotAdded()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.TryGetUriFromFriendlyName("TestLoggerExtension1", out var uri);
            Assert.IsNull(uri);
        }

        [TestMethod]
        public void GetResultsDirectoryShouldReturnNullIfRunSettingsIsNull()
        {
            var testLoggerManager = new DummyTestLoggerManager();
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

            var testLoggerManager = new DummyTestLoggerManager();
            string result = testLoggerManager.GetResultsDirectory(runSettingsXml);
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

            var testLoggerManager = new DummyTestLoggerManager();
            string result = testLoggerManager.GetResultsDirectory(runSettingsXml);

            Assert.AreEqual(string.Compare(Constants.DefaultResultsDirectory, result), 0);
        }

        [TestMethod]
        public void HandleTestRunMessageShouldInvokeTestRunMessageHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();
            
            testLoggerManager.HandleTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));

            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void HandleTestRunMessageShouldNotInvokeTestRunMessageHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, "TestRunMessage"));

            Assert.AreEqual(counter, 0);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldInvokeTestRunCompleteHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));

            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldNotInvokeTestRunCompleteHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));

            Assert.AreEqual(counter, 0);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldDisposeLoggerManager()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan()));
            testLoggerManager.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan())); // count should not increase because of second call.

            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void HandleTestRunStatsChangeShouldInvokeTestRunChangedHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleTestRunStatsChange(
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
        public void HandleTestRunStatsChangeShouldNotInvokeTestRunChangedHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleTestRunStatsChange(
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

            Assert.AreEqual(counter, 0);
        }

        [TestMethod]
        public void AddLoggerShouldNotThrowExceptionIfUriIsNull()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    testLoggerManager.InitializeLoggerByUri(null, null);
                });
        }

        [TestMethod]
        public void AddLoggerShouldNotThrowExceptionIfUriIsNonExistent()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            Assert.IsFalse(testLoggerManager.InitializeLoggerByUri(new Uri("logger://NotALogger"), null));
        }

        [TestMethod]
        public void AddLoggerShouldAddDefaultLoggerParameterForTestLoggerWithParameters()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <RunConfiguration>
                    <MaxCpuCount>0</MaxCpuCount>
                    <TargetPlatform> x64 </TargetPlatform>
                    <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion>
                  </RunConfiguration>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""test-logger-with-parameter://logger"" enabled=""true""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

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
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.Dispose();
            testLoggerManager.Dispose();
        }

        [TestMethod]
        public void AddLoggerShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(
                () =>
                {
                    testLoggerManager.InitializeLoggerByUri(new Uri("some://uri"), null);
                });
        }


        [TestMethod]
        public void EnableLoggingShouldThrowObjectDisposedExceptionAfterDisposedIsCalled()
        {
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(
                () =>
                {
                    testLoggerManager.EnableLogging();
                });
        }

        [TestMethod]
        public void LoggerInitialzeShouldCollectLoggersForTelemetry()
        {
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.InitializeLoggerByUri(new Uri(this.loggerUri), new Dictionary<string, string>());

            // Act.
            testLoggerManager.Initialize(null);

            // Verify
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger"));
        }

        /// <summary>
        /// DiscoveryStart event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryStartShouldInvokeDiscoveryStartHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();

            DiscoveryCriteria discoveryCriteria = new DiscoveryCriteria() { TestCaseFilter = "Name=Test1" };
            DiscoveryStartEventArgs discoveryStartEventArgs = new DiscoveryStartEventArgs(discoveryCriteria);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleDiscoveryStart(discoveryStartEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryStart event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryStartShouldNotInvokeDiscoveryStartHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();

            DiscoveryCriteria discoveryCriteria = new DiscoveryCriteria() { TestCaseFilter = "Name=Test1" };
            DiscoveryStartEventArgs discoveryStartEventArgs = new DiscoveryStartEventArgs(discoveryCriteria);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleDiscoveryStart(discoveryStartEventArgs);

            // Assertions when discovery events registered
            Assert.AreEqual(counter, 0);
        }

        /// <summary>
        /// DiscoveredTests event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveredTestsShouldInvokeDiscoveredTestsHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();

            List<TestCase> testCases = new List<TestCase> { new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName") };
            DiscoveredTestsEventArgs discoveredTestsEventArgs = new DiscoveredTestsEventArgs(testCases);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleDiscoveredTests(discoveredTestsEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        [TestMethod]
        public void HandleDiscoveredTestsShouldNotInvokeDiscoveredTestsHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();

            List<TestCase> testCases = new List<TestCase> { new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName") };
            DiscoveredTestsEventArgs discoveredTestsEventArgs = new DiscoveredTestsEventArgs(testCases);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleDiscoveredTests(discoveredTestsEventArgs);

            // Assertions when discovery events registered
            Assert.AreEqual(counter, 0);
        }

        /// <summary>
        /// TestRunStart event handler of loggers should be called only if test run events are registered.
        /// </summary>
        [TestMethod]
        public void HandleTestRunStartShouldInvokeTestRunStartHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();

            TestRunCriteria testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10) { TestCaseFilter = "Name=Test1" };
            TestRunStartEventArgs testRunStartEventArgs = new TestRunStartEventArgs(testRunCriteria);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleTestRunStart(testRunStartEventArgs);

            // Assertions when test run events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// TestRunStart event handler of loggers should be called only if test run events are registered.
        /// </summary>
        [TestMethod]
        public void HandleTestRunStartShouldNotInvokeTestRunStartHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();

            TestRunCriteria testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10) { TestCaseFilter = "Name=Test1" };
            TestRunStartEventArgs testRunStartEventArgs = new TestRunStartEventArgs(testRunCriteria);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleTestRunStart(testRunStartEventArgs);

            // Assertions when test run events registered
            Assert.AreEqual(counter, 0);
        }

        /// <summary>
        /// DiscoveryComplete event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryCompleteShouldInvokeDiscoveryCompleteHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();

            DiscoveryCompleteEventArgs discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(2, false);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryComplete event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotInvokeDiscoveryCompleteHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(2, false);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs);

            // Assertions when discovery events registered
            Assert.AreEqual(counter, 0);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldDisposeLoggerManager()
        {
            counter = 0;
            waitHandle.Reset();
            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(2, false);
            testLoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs);
            testLoggerManager.HandleDiscoveryComplete(discoveryCompleteEventArgs); // count should not increase because of second call.

            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryMessage event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryMessageShouldInvokeDiscoveryMessageHandlerOfLoggers()
        {
            counter = 0;
            waitHandle.Reset();

            string message = "This is the test message";
            TestRunMessageEventArgs testRunMessageEventArgs = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.HandleDiscoveryMessage(testRunMessageEventArgs);

            // Assertions when discovery events registered
            waitHandle.WaitOne();
            Assert.AreEqual(counter, 1);
        }

        /// <summary>
        /// DiscoveryMessage event handler of loggers should be called only if discovery events are registered.
        /// </summary>
        [TestMethod]
        public void HandleDiscoveryMessageShouldNotInvokeDiscoveryMessageHandlerOfLoggersIfDisposed()
        {
            counter = 0;
            waitHandle.Reset();

            string message = "This is the test message";
            TestRunMessageEventArgs testRunMessageEventArgs = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);

            // setup TestLogger
            var testLoggerManager = new DummyTestLoggerManager();
            testLoggerManager.InitializeLoggerByUri(new Uri(loggerUri), new Dictionary<string, string>());
            testLoggerManager.EnableLogging();

            testLoggerManager.Dispose();
            testLoggerManager.HandleDiscoveryMessage(testRunMessageEventArgs);

            Assert.AreEqual(counter, 0);
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggerFromFriendlyNameWhenOnlyFriendlyNamePresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerWithParameterExtension""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggerFromUriWhenOnlyUriPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger uri=""test-logger-with-parameter://logger""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggerFromAssemblyNameWhenAssemblyNameAndCodeBasePresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldNotInitializeLoggersWhenOnlyAssemblyNameIsPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeLoggersFromAssemblyNameWhenInterfaceDoesNotMatch()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(InvalidLogger).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
            Assert.AreEqual(0, InvalidLogger.counter);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeLoggersWhenAssemblyNameInvalid()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = "invalid";

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeLoggersWhenCodeBaseInvalid()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(InvalidLogger).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggerOnceWhenMultipleLoggersWithSameAssemblyNamePresent()
        {
            // Duplicate loggers should be ignored
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggerOnce()
        {
            // Duplicate loggers should be ignored
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerWithParameterExtension""></Logger>
                      <Logger uri=""test-logger-with-parameter://logger""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldNotConsiderLoggerAsInitializedWhenInitializationErrorOccurs()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(LoggerWithInitializationError).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
        }

        [TestMethod]
        public void InitializeShouldThrowWhenLoggerManagerAlreadyDisposed()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => testLoggerManager.Initialize(settingsXml));
        }

        [TestMethod]
        public void InitializeShouldInitilaizeMultipleLoggersIfPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void AreLoggersInitializedShouldReturnTrueWhenLoggersInitialized()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.IsTrue(testLoggerManager.AreLoggersInitialized());
        }

        [TestMethod]
        public void AreLoggersInitializedShouldReturnFalseWhenLoggersNotInitialized()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @"""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.IsFalse(testLoggerManager.AreLoggersInitialized());
        }

        [TestMethod]
        public void AreLoggersInitializedShouldReturnFalseWhenNoLoggersPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.IsFalse(testLoggerManager.AreLoggersInitialized());
        }

        [TestMethod]
        public void InitializeShouldPassConfigurationElementAsParameters()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldSkipEmptyConfigurationValueInParameters()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1> </Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(2, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.IsFalse(ValidLoggerWithParameters.parameters.TryGetValue("Key1", out var key1Value));
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);

            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldUseLastValueInParametersForDuplicateConfigurationValue()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                          <Key1>Value3</Key1>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value3", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);

            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldNotInitializeDisabledLoggers()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @""" enabled=""false""></Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2"));
        }

        [TestMethod]
        public void InitializeShouldInitializeFromAssemblyNameIfAllAttributesPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var assemblyQualifiedName = typeof(ValidLoggerWithParameters).AssemblyQualifiedName;
            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""invalidName"" uri=""invalid://invalidUri"" assemblyQualifiedName=""" + assemblyQualifiedName + @""" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeFromUriIfUriAndNamePresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""invalidName"" uri=""test-logger-with-parameter://logger"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeFromUriIfUnableToFromAssemblyName()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""invalidName"" uri=""test-logger-with-parameter://logger"" assemblyQualifiedName=""invalidAssembly"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeFromNameIfUnableToFromUri()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""TestLoggerWithParameterExtension"" uri=""invalid://invalid"" assemblyQualifiedName=""invalidAssembly"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggersWithTestRunDirectoryIfPresentInRunSettings()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <RunConfiguration>
                    <MaxCpuCount>0</MaxCpuCount>
                    <ResultsDirectory>DummyTestResultsFolder</ResultsDirectory>
                    <TargetPlatform> x64 </TargetPlatform>
                    <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion>
                  </RunConfiguration>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""TestLoggerWithParameterExtension"" uri=""invalid://invalid"" assemblyQualifiedName=""invalidAssembly"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual("DummyTestResultsFolder", ValidLoggerWithParameters.parameters["testRunDirectory"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldInitializeLoggersWithDefaultTestRunDirectoryIfNotPresentInRunSettings()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <RunConfiguration>
                    <MaxCpuCount>0</MaxCpuCount>
                    <TargetPlatform> x64 </TargetPlatform>
                    <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion>
                  </RunConfiguration>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""TestLoggerWithParameterExtension"" uri=""invalid://invalid"" assemblyQualifiedName=""invalidAssembly"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(1, ValidLoggerWithParameters.counter);
            Assert.AreEqual(3, ValidLoggerWithParameters.parameters.Count); // One additional because of testRunDirectory
            Assert.AreEqual("Value1", ValidLoggerWithParameters.parameters["Key1"]);
            Assert.AreEqual(Constants.DefaultResultsDirectory, ValidLoggerWithParameters.parameters["testRunDirectory"]);
            Assert.AreEqual("Value2", ValidLoggerWithParameters.parameters["Key2"]);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, "TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLogger2,TestPlatform.CrossPlatEngine.UnitTests.TestLoggerManagerTests+ValidLoggerWithParameters"));
        }

        [TestMethod]
        public void InitializeShouldNotInitializeIfUnableToFromName()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            var codeBase = typeof(TestLoggerManagerTests).GetTypeInfo().Assembly.Location;

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                      <Logger friendlyName=""TestLoggerExtension"" />
                      <Logger uri=""testlogger://logger2"" enabled=""true""></Logger>
                      <Logger friendlyName=""invalid"" uri=""invalid://invalid"" assemblyQualifiedName=""invalidAssembly"" codeBase=""" + codeBase + @""">
                        <Configuration>
                          <Key1>Value1</Key1>
                          <Key2>Value2</Key2>
                        </Configuration>
                      </Logger>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);

            Assert.ThrowsException<InvalidLoggerException>(() =>
            {
                testLoggerManager.Initialize(settingsXml);
            });
            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
        }

        [TestMethod]
        public void InitializeShouldNotInitializeAnyLoggerIfNoLoggerPresent()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, ""));
        }

        [TestMethod]
        public void InitializeShouldNotInitializeAnyLoggerIfEmptyLoggerRunSettings()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, ""));
        }

        [TestMethod]
        public void InitializeShouldNotThrowWhenLoggersNotPresentInRunSettings()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, ""));
        }

        [TestMethod]
        public void InitializeShouldNotInitializeAnyLoggerIfEmptyLoggersNode()
        {
            ValidLoggerWithParameters.Reset();
            var mockRequestData = new Mock<IRequestData>();
            var mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                  <LoggerRunSettings>
                    <Loggers>
                    </Loggers>
                  </LoggerRunSettings>
                </RunSettings>";

            var testLoggerManager = new DummyTestLoggerManager(mockRequestData.Object);
            testLoggerManager.Initialize(settingsXml);

            Assert.AreEqual(0, ValidLoggerWithParameters.counter);
            mockMetricsCollection.Verify(
                rd => rd.Add(TelemetryDataConstants.LoggerUsed, ""));
        }

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger : ITestLogger
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

        [ExtensionUri("testlogger://logger2")]
        [FriendlyName("TestLoggerExtension2")]
        private class ValidLogger2 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
            }

        }

        [ExtensionUri("testlogger://invalidLogger")]
        [FriendlyName("InvalidTestLoggerExtension")]
        private class InvalidLogger
        {
            public static int counter = 0;

            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                counter++;
            }

        }

        [ExtensionUri("testlogger://loggerWithError")]
        [FriendlyName("ErroredTestLoggerExtension")]
        private class LoggerWithInitializationError : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                throw new Exception();
            }
        }

        [ExtensionUri("test-logger-with-parameter://logger")]
        [FriendlyName("TestLoggerWithParameterExtension")]
        private class ValidLoggerWithParameters : ITestLoggerWithParameters
        {
            public static Dictionary<string, string> parameters;
            public static int counter = 0;

            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                counter += 2;
            }

            public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
            {
                counter++;
                ValidLoggerWithParameters.parameters = parameters;
            }

            public static void Reset()
            {
                counter = 0;
                ValidLoggerWithParameters.parameters = null;
            }
        }

        internal class DummyTestLoggerManager : TestLoggerManager
        {
            public DummyTestLoggerManager() : base(null, TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance))
            {

            }

            public DummyTestLoggerManager(IRequestData requestData) : base(requestData, TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance))
            {

            }
        }
    }
}



